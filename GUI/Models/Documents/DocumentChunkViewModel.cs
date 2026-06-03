namespace GUI.Models.Documents;

public class DocumentChunkViewModel
{
    public int ChunkOrder { get; set; }
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string? ChunkHash { get; set; }
    public bool HasEmbedding { get; set; }
}
