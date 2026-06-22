using System.Text;
using System.Text.Json;
using BLL.Constants;
using BLL.DTOs.Chat;
using BLL.Exceptions;
using BLL.Interfaces.Chat;
using BLL.Interfaces.Documents;
using DAL.Interfaces.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace BLL.Services.Documents;

/// <summary>
/// Tách riêng theo SRP — So sánh tài liệu là nghiệp vụ độc lập với Chat.
/// Hỗ trợ 2 mode: (1) Compare files upload trực tiếp, (2) Compare documents đã lưu trong DB.
/// </summary>
public class CompareService : ICompareService
{
    private readonly IFileParserService _fileParserService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGeminiChatService _geminiChatService;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<CompareService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CompareService(
        IFileParserService fileParserService,
        IEmbeddingService embeddingService,
        IGeminiChatService geminiChatService,
        IDocumentRepository documentRepository,
        ILogger<CompareService> logger)
    {
        _fileParserService = fileParserService;
        _embeddingService = embeddingService;
        _geminiChatService = geminiChatService;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Legacy: So sánh 2 file upload trực tiếp (dùng embedding cosine similarity + Gemini summary).
    /// </summary>
    public async Task<CompareResult> CompareFilesAsync(IFormFile file1, IFormFile file2, CancellationToken cancellationToken = default)
    {
        var text1 = await ExtractTextAsync(file1, cancellationToken);
        var text2 = await ExtractTextAsync(file2, cancellationToken);

        var vector1 = await _embeddingService.EmbedAsync(TruncateText(text1, 8000), cancellationToken);
        var vector2 = await _embeddingService.EmbedAsync(TruncateText(text2, 8000), cancellationToken);

        double similarityPercentage = CalculateCosineSimilarity(vector1.ToArray(), vector2.ToArray()) * 100.0;
        if (similarityPercentage < 0) similarityPercentage = 0;

        var prompt = $"Hãy phân tích sự giống và khác nhau của 2 tài liệu sau một cách ngắn gọn, súc tích (Tập trung vào nội dung chính và cấu trúc). Nếu không có gì đặc biệt, hãy tóm tắt ngắn.\n\n" +
                     $"--- TÀI LIỆU 1 ({file1.FileName}) ---\n{TruncateText(text1, 5000)}\n\n" +
                     $"--- TÀI LIỆU 2 ({file2.FileName}) ---\n{TruncateText(text2, 5000)}";

        var history = new List<GeminiChatMessage>();
        var geminiSummary = await _geminiChatService.GenerateAsync(prompt, history, cancellationToken);

        return new CompareResult
        {
            SimilarityPercentage = Math.Round(similarityPercentage, 2),
            GeminiSummary = geminiSummary
        };
    }

    /// <summary>
    /// So sánh tài liệu đã lưu trong DB theo DocumentIds.
    /// Lấy text từ DocumentChunks → Build COMPARISON_PROMPT → Gọi LLM → Parse JSON → ComparisonResultDto.
    /// Throw LlmResponseParsingException nếu parse JSON thất bại.
    /// </summary>
    public async Task<ComparisonResultDto> CompareDocumentsAsync(
        List<Guid> documentIds,
        string? question,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (documentIds.Count < 2)
            throw new ArgumentException("Cần ít nhất 2 tài liệu để so sánh.", nameof(documentIds));

        _logger.LogInformation("CompareDocumentsAsync started. DocumentIds=[{DocumentIds}], UserId={UserId}",
            string.Join(",", documentIds), userId);

        // 1. Lấy toàn bộ text từ DocumentChunks trong DB
        var documentTexts = await _documentRepository.GetDocumentTextAsync(documentIds, cancellationToken);

        if (documentTexts.Count < 2)
        {
            throw new DocumentNotReadyException("Không đủ tài liệu có nội dung để so sánh. Vui lòng kiểm tra các tài liệu đã được xử lý (chunked) chưa.");
        }

        // 2. Lấy tiêu đề tài liệu
        var docContextBuilder = new StringBuilder();
        var docIndex = 1;
        foreach (var (docId, text) in documentTexts)
        {
            var doc = await _documentRepository.GetDocumentAsync(docId, cancellationToken);
            var title = doc?.Title ?? $"Tài liệu {docIndex}";
            docContextBuilder.AppendLine($"--- {title} (ID: {docId}) ---");
            docContextBuilder.AppendLine(TruncateText(text, 6000));
            docContextBuilder.AppendLine();
            docIndex++;
        }

        // 3. Build prompt từ template
        var prompt = PromptTemplates.COMPARISON_PROMPT
            .Replace("{doc_contexts}", docContextBuilder.ToString())
            .Replace("{user_question}", question ?? "(Không có câu hỏi bổ sung)");

        _logger.LogInformation("CompareDocumentsAsync: Calling LLM. PromptLength={Length}", prompt.Length);

        // 4. Gọi Gemini (non-stream)
        var history = new List<GeminiChatMessage>();
        var rawResponse = await _geminiChatService.GenerateAsync(prompt, history, cancellationToken);

        _logger.LogDebug("CompareDocumentsAsync: Raw LLM response length={Length}", rawResponse.Length);

        // 5. Parse JSON response → ComparisonResultDto
        try
        {
            // Extract JSON from response (LLM may wrap it in ```json ... ```)
            var jsonText = ExtractJson(rawResponse);
            var result = JsonSerializer.Deserialize<ComparisonResultDto>(jsonText, JsonOptions);

            if (result == null)
                throw new LlmResponseParsingException("LLM trả về null sau khi parse.", rawResponse);

            _logger.LogInformation("CompareDocumentsAsync succeeded. Similarity={Similarity}%", result.SimilarityPercentage);
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "CompareDocumentsAsync: JSON parse failed. RawResponse={RawResponse}", TruncateText(rawResponse, 2000));
            throw new LlmResponseParsingException(
                "Không thể parse JSON từ phản hồi của AI. Vui lòng thử lại.",
                rawResponse,
                ex);
        }
    }

    #region Private Helpers

    private static string ExtractJson(string text)
    {
        // Try to extract JSON from ```json ... ``` blocks
        var startMarker = "```json";
        var endMarker = "```";
        var startIdx = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIdx >= 0)
        {
            startIdx += startMarker.Length;
            var endIdx = text.IndexOf(endMarker, startIdx, StringComparison.Ordinal);
            if (endIdx > startIdx)
            {
                return text[startIdx..endIdx].Trim();
            }
        }

        // Try to find raw JSON object
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return text[jsonStart..(jsonEnd + 1)];
        }

        return text.Trim();
    }

    private double CalculateCosineSimilarity(float[] v1, float[] v2)
    {
        if (v1.Length != v2.Length || v1.Length == 0) return 0;
        double dotProduct = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            mag1 += Math.Pow(v1[i], 2);
            mag2 += Math.Pow(v2[i], 2);
        }
        if (mag1 == 0 || mag2 == 0) return 0;
        return dotProduct / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }

    private string TruncateText(string text, int length)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Length <= length ? text : text[..length] + "...";
    }

    private async Task<string> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
        try
        {
            using (var stream = new FileStream(tempFile, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }
            return await _fileParserService.ExtractTextAsync(tempFile, Path.GetExtension(file.FileName), cancellationToken);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    #endregion
}
