namespace GUI.Models.Documents;

public class DocumentChapterViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int ChapterOrder { get; set; }
    public int StartChunkIndex { get; set; }
    public int EndChunkIndex { get; set; }
    public int ChunkCount => EndChunkIndex >= StartChunkIndex ? EndChunkIndex - StartChunkIndex + 1 : 0;
    public bool IsAiGenerated { get; set; }
    public decimal? ConfidenceScore { get; set; }
}
