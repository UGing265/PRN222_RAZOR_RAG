using DAL.Entities;

namespace GUI.Models.Documents;

public class DocumentDetailsViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? School { get; set; }
    public string? Department { get; set; }
    public string? Visibility { get; set; }
    public string? Language { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public int TotalChunks { get; set; }
    public int TotalChapters { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int FileCount { get; set; }
    public int ChunkPage { get; set; } = 1;
    public int ChunkPageSize { get; set; } = 10;
    public int TotalChunkPages { get; set; } = 1;
    public List<DocumentFile> Files { get; set; } = [];
    public List<DocumentChunkViewModel> Chunks { get; set; } = [];
    public List<DocumentChapterViewModel> Chapters { get; set; } = [];
}
