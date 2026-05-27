namespace BLL.Services.Documents;

public class DocumentIndexingOptions
{
    public int ChunkMaxWords { get; set; } = 1100;
    public int ChunkOverlapWords { get; set; } = 100;
    public int BatchSize { get; set; } = 60;
    public int BatchDelaySeconds { get; set; } = 2;
}
