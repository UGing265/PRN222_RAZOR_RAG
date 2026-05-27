using DAL.Entities;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.Documents;

public interface IDocumentService
{
    Task<Document> CreateDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<(string Key, string Url)> UploadOriginalFileToS3Async(Guid documentId, IFormFile file, CancellationToken cancellationToken = default);
    Task<DocumentFile> AddDocumentFileAsync(Guid documentId, string s3Key, string s3Url, IFormFile file, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAssetsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Stream> DownloadOriginalFileFromS3Async(Guid documentId, string s3Key, CancellationToken cancellationToken = default);
    Task<List<DocumentChapter>> GenerateChaptersAsync(Guid documentId, CancellationToken cancellationToken = default);
}
