using BLL.DTOs.Chat;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.Documents;

public interface ICompareService
{
    /// <summary>
    /// So sánh 2 file upload trực tiếp (legacy — giữ lại cho backward compatibility).
    /// </summary>
    Task<CompareResult> CompareFilesAsync(IFormFile file1, IFormFile file2, CancellationToken cancellationToken = default);

    /// <summary>
    /// So sánh tài liệu đã lưu trong DB theo DocumentIds.
    /// Lấy text từ DocumentChunks, build COMPARISON_PROMPT, gọi LLM, parse JSON → ComparisonResultDto.
    /// </summary>
    Task<ComparisonResultDto> CompareDocumentsAsync(List<Guid> documentIds, string? question, Guid userId, CancellationToken cancellationToken = default);
}

public class CompareResult
{
    public double SimilarityPercentage { get; set; }
    public string GeminiSummary { get; set; } = string.Empty;
}
