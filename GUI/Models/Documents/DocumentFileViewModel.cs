namespace GUI.Models.Documents;

public class DocumentFileViewModel
{
    public Guid Id { get; set; }
    public string? OriginalFilename { get; set; }
    public string? MimeType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ExtractionStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
