using DAL.Entities;

namespace DAL.Interfaces.Documents;

public interface IDocumentRepository
{
    Task<Document> AddDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetPendingDocumentsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, Guid? requesterUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentFile>> GetDocumentFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentChapter>> GetDocumentChaptersAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsByOwnerAsync(Guid ownerUserId, string? query, Guid? subjectId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountDocumentsByOwnerAsync(Guid ownerUserId, string? query, Guid? subjectId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsAsync(string? query, Guid? subjectId, int page, int pageSize, Guid? requesterUserId = null, CancellationToken cancellationToken = default);
    Task<int> CountDocumentsAsync(string? query, Guid? subjectId, Guid? requesterUserId = null, CancellationToken cancellationToken = default);
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
    Task RemoveDocumentChaptersByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, string>> GetPreviewTextsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default);
    Task<List<Major>> GetMajorsAsync(CancellationToken cancellationToken = default);
    Task<List<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);
    Task<List<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<Subject> AddSubjectAsync(Subject subject, CancellationToken cancellationToken = default);
    Task<DocumentType> AddDocumentTypeAsync(DocumentType documentType, CancellationToken cancellationToken = default);
    Task<Language> AddLanguageAsync(Language language, CancellationToken cancellationToken = default);
    Task<List<Document>> GetAdminDocumentsAsync(string? query, Guid? subjectId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAdminDocumentsAsync(string? query, Guid? subjectId, CancellationToken cancellationToken = default);
    Task<DocumentReport> AddDocumentReportAsync(DocumentReport report, CancellationToken cancellationToken = default);
    Task<List<DocumentReport>> GetPendingReportsAsync(CancellationToken cancellationToken = default);
    Task<DocumentReport?> GetDocumentReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<List<DocumentReport>> GetDocumentReportsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentReportsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
