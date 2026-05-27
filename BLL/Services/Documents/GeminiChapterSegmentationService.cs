using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BLL.Interfaces.Documents;
using DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Documents;

public class GeminiChapterSegmentationService : IChapterSegmentationService
{
    private static readonly HttpClient HttpClient = new();
    private readonly ILogger<GeminiChapterSegmentationService> _logger;
    private readonly string[] _apiKeys;
    private readonly string _model;
    private int _keyIndex;

    public GeminiChapterSegmentationService(IConfiguration configuration, ILogger<GeminiChapterSegmentationService> logger)
    {
        _logger = logger;
        var apiKeysFromConfig = configuration.GetSection("Gemini:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        var apiKeysFromEnv = (Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        _apiKeys = apiKeysFromConfig
            .Concat(apiKeysFromEnv)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (_apiKeys.Length == 0)
        {
            throw new InvalidOperationException("Missing Gemini API keys. Set Gemini:ApiKeys or GEMINI_API_KEY.");
        }

        _model = configuration["Gemini:ChatModel"] ?? "gemini-1.5-pro";
    }

    public async Task<List<DocumentChapter>> GenerateChaptersAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return [];
        }

        var chunkPack = string.Join("\n", chunks.OrderBy(x => x.ChunkOrder).Select(x => 
        {
            var cleanContent = x.Content.Replace("\r", "");
            var lines = cleanContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            var preview = cleanContent.Length > 500 ? cleanContent.Substring(0, 500).Replace("\n", " ") + "..." : cleanContent.Replace("\n", " ");
            
            // Trích xuất các dòng ngắn có khả năng là Header/Tiêu đề chương
            var potentialHeaders = lines
                .Where(l => l.Trim().Length > 3 && l.Trim().Length < 80)
                .Where(l => !l.Trim().EndsWith(".") && !l.Trim().EndsWith(","))
                .Take(10);
                
            var headersStr = string.Join(" | ", potentialHeaders);
            
            return $"[CHUNK {x.ChunkOrder}] Preview: {preview} === Headers tiềm năng: {headersStr}";
        }));
        var prompt = """
Bạn là hệ thống chia chương tài liệu học thuật chuyên nghiệp.
Nhiệm vụ của bạn là đọc cực kỳ cẩn thận và phân chia tài liệu thành các chương (chapters) hoàn chỉnh dựa trên các chunk bên dưới.

Yêu cầu BẮT BUỘC:
1. ĐỌC CẨN THẬN VÀ ƯU TIÊN TÌM HEADER: Hãy quét thật kỹ từng dòng nội dung của tất cả các chunk để tìm các dấu hiệu chuyển chương/phần rõ ràng (VD: 'Chapter 1', 'Chương 1', 'PART I', 'Mục lục', 'Introduction', 'Conclusion').
   - Tuyệt đối KHÔNG bỏ sót bất kỳ chương nào có trong sách. Sách có bao nhiêu chương thì HÃY TRẢ VỀ ĐẦY ĐỦ bấy nhiêu chương, không giới hạn số lượng chương.
   - Nếu sách KHÔNG CÓ chia chương rõ ràng, thì hãy tự động phân tích và gộp các chunk lại thành các phần/chủ đề lớn logic nhất.
2. Chỉ trả về JSON hợp lệ, KHÔNG giải thích thêm.
3. Mỗi chương phải có title, summary, startChunkIndex, endChunkIndex, confidenceScore.
4. 'summary' RẤT NGẮN GỌN (1-2 câu).
5. startChunkIndex và endChunkIndex là số nguyên. Các chương phải bao phủ ĐẦY ĐỦ toàn bộ tài liệu (từ chunk đầu tiên đến chunk cuối cùng) và tuyệt đối KHÔNG được chồng lấn nhau.
6. Chỉ dùng chunk có sẵn, không bịa nội dung.
7. Nếu tài liệu quá ngắn, trả về 1 chương duy nhất.

Thông tin tài liệu:
- Title: __TITLE__
- Subject: __SUBJECT__
- School: __SCHOOL__
- Language: __LANGUAGE__

Chunks:
__CHUNKPACK__

Đầu ra JSON dạng:
{
  "chapters": [
    {
      "title": "...",
      "summary": "...",
      "startChunkIndex": 0,
      "endChunkIndex": 4,
      "confidenceScore": 0.87
    }
  ]
}
"""
.Replace("__TITLE__", document.Title)
.Replace("__SUBJECT__", document.Subject ?? string.Empty)
.Replace("__SCHOOL__", document.School ?? string.Empty)
.Replace("__LANGUAGE__", document.Language ?? string.Empty)
.Replace("__CHUNKPACK__", chunkPack);

        Exception? lastError = null;
        for (var attempt = 0; attempt < _apiKeys.Length; attempt++)
        {
            var apiKeyIndex = GetNextApiKeyIndex();
            var apiKey = _apiKeys[apiKeyIndex];
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey}";
                var request = new GeminiGenerateRequest
                {
                    Contents = [new GeminiContent
                    {
                        Role = "user",
                        Parts = [new GeminiPart { Text = prompt }]
                    }],
                    GenerationConfig = new GeminiGenerationConfig
                    {
                        Temperature = 0.2,
                        TopP = 0.8,
                        MaxOutputTokens = 8192,
                        ResponseMimeType = "application/json"
                    }
                };

                using var response = await HttpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    lastError = new InvalidOperationException($"Gemini chapter segmentation failed: {(int)response.StatusCode} {body}");
                    continue;
                }

                var payload = JsonSerializer.Deserialize<GeminiGenerateResponse>(body);
                var text = payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return BuildFallbackChapters(document, chunks);
                }

                var parsed = JsonSerializer.Deserialize<ChapterResponse>(ExtractJson(text));
                return BuildChaptersFromResponse(document, chunks, parsed);
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Chapter segmentation attempt failed. ApiKeyIndex={ApiKeyIndex}", apiKeyIndex);
            }
        }

        _logger.LogError(lastError, "Chapter segmentation failed, falling back to one chapter.");
        return BuildFallbackChapters(document, chunks);
    }

    private List<DocumentChapter> BuildChaptersFromResponse(Document document, IReadOnlyList<DocumentChunk> chunks, ChapterResponse? response)
    {
        if (response?.Chapters is null || response.Chapters.Count == 0)
        {
            return BuildFallbackChapters(document, chunks);
        }

        var maxIndex = chunks.Max(x => x.ChunkOrder);
        var chapters = new List<DocumentChapter>();
        var order = 1;
        foreach (var chapter in response.Chapters.OrderBy(x => x.StartChunkIndex))
        {
            var start = Math.Clamp(chapter.StartChunkIndex, 0, maxIndex);
            var end = Math.Clamp(chapter.EndChunkIndex, start, maxIndex);
            chapters.Add(new DocumentChapter
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                Title = string.IsNullOrWhiteSpace(chapter.Title) ? $"Chương {order}" : chapter.Title.Trim(),
                Summary = chapter.Summary?.Trim(),
                ChapterOrder = order,
                StartChunkIndex = start,
                EndChunkIndex = end,
                IsAiGenerated = true,
                ConfidenceScore = chapter.ConfidenceScore,
                CreatedAt = DateTime.UtcNow
            });
            order++;
        }

        if (chapters.Count == 0)
        {
            return BuildFallbackChapters(document, chunks);
        }

        return chapters;
    }

    private List<DocumentChapter> BuildFallbackChapters(Document document, IReadOnlyList<DocumentChunk> chunks)
    {
        return [new DocumentChapter
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Title = "Chương 1",
            Summary = "Tài liệu được gom thành một chương duy nhất do nội dung ngắn hoặc không tách rõ ràng.",
            ChapterOrder = 1,
            StartChunkIndex = chunks.Min(x => x.ChunkOrder),
            EndChunkIndex = chunks.Max(x => x.ChunkOrder),
            IsAiGenerated = true,
            ConfidenceScore = 0.5m,
            CreatedAt = DateTime.UtcNow
        }];
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return text;
        
        var end = text.LastIndexOf('}');
        if (end <= start) return text;

        var json = text.Substring(start, end - start + 1);
        
        // Simple heuristic to auto-close if truncated
        int openBraces = json.Count(c => c == '{');
        int closeBraces = json.Count(c => c == '}');
        int openBrackets = json.Count(c => c == '[');
        int closeBrackets = json.Count(c => c == ']');
        
        if (openBrackets > closeBrackets)
        {
            json += new string(']', openBrackets - closeBrackets);
        }
        if (openBraces > closeBraces)
        {
            json += new string('}', openBraces - closeBraces);
        }
        
        return json;
    }

    private int GetNextApiKeyIndex()
    {
        var index = Interlocked.Increment(ref _keyIndex);
        return index % _apiKeys.Length;
    }

    private sealed class GeminiGenerateRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("topP")]
        public double TopP { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        [JsonPropertyName("responseMimeType")]
        public string ResponseMimeType { get; set; } = "application/json";
    }

    private sealed class GeminiGenerateResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; } = [];
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private sealed class ChapterResponse
    {
        [JsonPropertyName("chapters")]
        public List<ChapterItem> Chapters { get; set; } = [];
    }

    private sealed class ChapterItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("startChunkIndex")]
        public int StartChunkIndex { get; set; }

        [JsonPropertyName("endChunkIndex")]
        public int EndChunkIndex { get; set; }

        [JsonPropertyName("confidenceScore")]
        public decimal ConfidenceScore { get; set; }
    }
}
