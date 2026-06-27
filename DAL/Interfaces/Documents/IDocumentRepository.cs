using DAL.Entities;

namespace DAL.Interfaces.Documents;

public interface IDocumentRepository
{
    Task<Document> AddDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<bool> ExistsDocumentByMd5Async(string md5Hash, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetPendingDocumentsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, Guid? requesterUserId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentFile>> GetDocumentFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<DocumentChapter>> GetDocumentChaptersAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsByOwnerAsync(Guid ownerUserId, string? query, Guid? subjectId, string? sortBy, Guid? documentTypeId, Guid? languageId, Guid? documentSourceId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountDocumentsByOwnerAsync(Guid ownerUserId, string? query, Guid? subjectId, Guid? documentTypeId, Guid? languageId, Guid? documentSourceId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsAsync(string? query, Guid? subjectId, int page, int pageSize, Guid? requesterUserId = null, string? sortBy = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, bool? bookmarkedOnly = null, CancellationToken cancellationToken = default);
    Task<int> CountDocumentsAsync(string? query, Guid? subjectId, Guid? requesterUserId = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, bool? bookmarkedOnly = null, CancellationToken cancellationToken = default);
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
    Task<List<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);
    Task<List<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentSource>> GetDocumentSourcesAsync(CancellationToken cancellationToken = default);

    Task<Subject> AddSubjectAsync(Subject subject, CancellationToken cancellationToken = default);
    Task<Subject?> GetSubjectAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default);
    Task DeleteSubjectAsync(Subject subject, CancellationToken cancellationToken = default);
    Task<DocumentType> AddDocumentTypeAsync(DocumentType documentType, CancellationToken cancellationToken = default);
    Task<Subject?> GetSubjectWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentType?> GetDocumentTypeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentType?> GetDocumentTypeWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateDocumentTypeAsync(DocumentType documentType, CancellationToken cancellationToken = default);
    Task DeleteDocumentTypeAsync(DocumentType documentType, CancellationToken cancellationToken = default);

    Task<Language> AddLanguageAsync(Language language, CancellationToken cancellationToken = default);
    Task<Language?> GetLanguageAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Language?> GetLanguageWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateLanguageAsync(Language language, CancellationToken cancellationToken = default);
    Task DeleteLanguageAsync(Language language, CancellationToken cancellationToken = default);

    Task<DocumentSource> AddDocumentSourceAsync(DocumentSource source, CancellationToken cancellationToken = default);
    Task<DocumentSource?> GetDocumentSourceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentSource?> GetDocumentSourceWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateDocumentSourceAsync(DocumentSource source, CancellationToken cancellationToken = default);
    Task DeleteDocumentSourceAsync(DocumentSource source, CancellationToken cancellationToken = default);


    Task<List<Document>> GetAdminDocumentsAsync(string? query, Guid? subjectId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAdminDocumentsAsync(string? query, Guid? subjectId, CancellationToken cancellationToken = default);
    Task<DocumentReport> AddDocumentReportAsync(DocumentReport report, CancellationToken cancellationToken = default);
    Task<List<DocumentReport>> GetPendingReportsAsync(CancellationToken cancellationToken = default);
    Task<DocumentReport?> GetDocumentReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<List<DocumentReport>> GetDocumentReportsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentReportsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentBookmarksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghép toàn bộ Content của các chunk theo DocumentId, dùng cho CompareService.
    /// Returns Dictionary: key = DocumentId, value = ghép toàn bộ chunk text.
    /// </summary>
    Task<Dictionary<Guid, string>> GetDocumentTextAsync(List<Guid> documentIds, CancellationToken cancellationToken = default);

    Task AddAuditLogAsync(AuditLog log, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<List<Subject>> GetSubjectsAssignedToLecturerAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignSubjectsToLecturerAsync(Guid userId, List<Guid> subjectIds, CancellationToken cancellationToken = default);
    Task<bool> IsSubjectAssignedToLecturerAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, (Guid UserId, string FullName)>> GetSubjectLecturerMapAsync(CancellationToken cancellationToken = default);

    Task<bool> ToggleBookmarkAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsBookmarkedAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetBookmarkedDocumentIdsAsync(Guid userId, List<Guid> documentIds, CancellationToken cancellationToken = default);
}

