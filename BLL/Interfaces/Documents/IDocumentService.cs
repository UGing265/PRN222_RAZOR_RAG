using BLL.DTOs.Documents;
using DAL.Entities;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.Documents;

public interface IDocumentService
{
    Task<Document> CreateDocumentAsync(DocumentCreateInput input, IFormFile file, CancellationToken cancellationToken = default);
    Task<(string Key, string Url)> UploadOriginalFileToS3Async(Guid documentId, IFormFile file, CancellationToken cancellationToken = default);
    Task<DocumentFile> AddDocumentFileAsync(Guid documentId, string s3Key, string s3Url, IFormFile file, Func<int, Task>? onProgress = null, CancellationToken cancellationToken = default);
    Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, int chunkPage = 1, int chunkPageSize = 10, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentForOwnerAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetOwnedDocumentBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<MyDocumentsDto> GetMyDocumentsAsync(Guid ownerUserId, string? query, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default);
    Task<int> CountMyDocumentsAsync(Guid ownerUserId, string? query, CancellationToken cancellationToken = default);
    Task<int> CountMyDocumentsByStatusAsync(Guid ownerUserId, string status, CancellationToken cancellationToken = default);
    Task<int> CountMyFilesAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<int> CountMyChunksAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<UploadJobSummaryDto>> GetActiveUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<DeleteDocumentViewData?> GetDeleteDocumentViewDataAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<DeleteDocumentViewData?> GetDeleteDocumentViewDataBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAssetsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Stream> DownloadOriginalFileFromS3Async(Guid documentId, string s3Key, CancellationToken cancellationToken = default);
    Task<List<DocumentChapter>> GenerateChaptersAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<UploadJobSummaryDto>> GetUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
}
