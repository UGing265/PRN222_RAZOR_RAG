namespace BLL.Interfaces.Documents;

public interface IFileParserService
{
    Task<string> ExtractTextAsync(string filePath, string extension, CancellationToken cancellationToken = default);
}
