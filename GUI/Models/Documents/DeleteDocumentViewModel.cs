namespace GUI.Models.Documents;

public class DeleteDocumentViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int ChunkCount { get; set; }
}
