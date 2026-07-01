namespace BLL.Services.Documents;

public class DocumentIndexingOptions
{
    public int ChunkMinWords { get; set; } = 50;
    public int ChunkMaxWords { get; set; } = 500;
    public int ChunkOverlapWords { get; set; } = 80;
    public int BatchSize { get; set; } = 10;
    public int BatchDelaySeconds { get; set; } = 1;
    public int ChapterMinChunks { get; set; } = 2;
    public int ChapterMaxChunks { get; set; } = 8;
}
