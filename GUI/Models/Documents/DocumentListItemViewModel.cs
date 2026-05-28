namespace GUI.Models.Documents;

public class DocumentListItemViewModel
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int ChunkCount { get; set; }
    public string? PreviewText { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Visibility { get; set; }
    public string? School { get; set; }
}
