namespace GUI.Models.Documents;

public class UploadJobViewModel
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
