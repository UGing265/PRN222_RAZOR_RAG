namespace BLL.Services.Documents;

public class DocumentIndexingOptions
{
    public int ChunkMaxWords { get; set; } = 60;
    public int ChunkOverlapWords { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
    public int BatchDelaySeconds { get; set; } = 1;
}
