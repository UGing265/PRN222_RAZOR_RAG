namespace BLL.DTOs.Documents;

public sealed class DashboardSummaryDto
{
    public int TotalDocuments { get; init; }
    public int TotalChunks { get; init; }
    public int TotalFiles { get; init; }
    public int ApprovedDocuments { get; init; }
    public int PendingDocuments { get; init; }
    public int RejectedDocuments { get; init; }
    public List<DashboardRecentDocumentDto> RecentDocuments { get; init; } = [];
    public List<UploadJobSummaryDto> ActiveUploadJobs { get; init; } = [];
    public string? CompletedUploadMessage { get; init; }
}

public sealed class DashboardRecentDocumentDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public int FileCount { get; init; }
    public int ChunkCount { get; init; }
}
