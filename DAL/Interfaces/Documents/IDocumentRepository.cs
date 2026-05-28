using DAL.Entities;

namespace DAL.Interfaces.Documents;

public interface IDocumentRepository
{
    Task<Document> AddDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentFile>> GetDocumentFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentChapter>> GetDocumentChaptersAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsByOwnerAsync(Guid ownerUserId, string? query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountDocumentsByOwnerAsync(Guid ownerUserId, string? query, CancellationToken cancellationToken = default);
    Task<int> CountDocumentsByStatusAsync(Guid ownerUserId, string status, CancellationToken cancellationToken = default);
    Task<int> CountFilesByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<int> CountChunksByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<UploadJob>> GetActiveUploadJobsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetOwnedDocumentAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetOwnedDocumentBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<int> CountFilesByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<int> CountChunksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task AddDocumentFilesAsync(IEnumerable<DocumentFile> files, CancellationToken cancellationToken = default);
    Task AddDocumentChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task AddDocumentChaptersAsync(IEnumerable<DocumentChapter> chapters, CancellationToken cancellationToken = default);
    Task RemoveUploadJobsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentFilesByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentChunksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
