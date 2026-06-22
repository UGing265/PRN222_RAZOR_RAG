namespace BLL.Interfaces.Documents;

public class PageContent
{
    public int? PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
}

public interface IFileParserService
{
    Task<string> ExtractTextAsync(string filePath, string extension, CancellationToken cancellationToken = default);
    Task<List<PageContent>> ExtractPagesAsync(string filePath, string extension, CancellationToken cancellationToken = default);
}
