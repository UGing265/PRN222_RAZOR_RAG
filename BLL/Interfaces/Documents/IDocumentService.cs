using BLL.DTOs.Documents;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Interfaces.Documents;

public interface IDocumentService
{
    Task<DocumentCreateResultDto> CreateDocumentAsync(DocumentCreateInput input, IFormFile file, CancellationToken cancellationToken = default);
    Task<(string Key, string Url)> UploadOriginalFileToS3Async(Guid documentId, IFormFile file, CancellationToken cancellationToken = default);
    Task AddDocumentFileAsync(Guid documentId, string s3Key, string s3Url, IFormFile file, Func<int, Task>? onProgress = null, CancellationToken cancellationToken = default);
    Task EnqueueUploadJobAsync(Guid ownerUserId, Guid documentId, string fileName, string storagePath, long fileSizeBytes, CancellationToken cancellationToken = default);
    Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, int chunkPage = 1, int chunkPageSize = 10, bool incrementViewCount = true, Guid? requesterUserId = null, CancellationToken cancellationToken = default);
    Task<DocumentDetailsDto?> GetDocumentDetailsBySlugAsync(string slug, Guid? requesterUserId = null, int chunkPage = 1, int chunkPageSize = 10, bool incrementViewCount = true, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<DocumentDetailsDto?> GetOwnedDocumentDetailsBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<MyDocumentsDto> GetMyDocumentsAsync(Guid ownerUserId, string? query, Guid? subjectId, Guid? termId, string? sortBy, Guid? documentTypeId, Guid? languageId, Guid? documentSourceId, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default);
    Task<MyDocumentsDto> GetAllDocumentsAsync(string? query, Guid? subjectId, int page = 1, int pageSize = 6, Guid? requesterUserId = null, string? sortBy = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, bool? bookmarkedOnly = null, CancellationToken cancellationToken = default);
    Task<List<UploadJobSummaryDto>> GetActiveUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<DeleteDocumentViewData?> GetDeleteDocumentViewDataBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task UpdateDocumentAsync(Guid documentId, Guid ownerUserId, DocumentEditInput input, CancellationToken cancellationToken = default);
    Task DeleteDocumentAssetsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Stream> DownloadOriginalFileFromS3Async(Guid documentId, string s3Key, CancellationToken cancellationToken = default);
    Task<List<DocumentChapterDto>> GenerateChaptersAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<UploadJobSummaryDto>> GetUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default);

    Task<List<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<List<SubjectDto>> GetSubjectsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);
    Task<List<LanguageDto>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentSourceDto>> GetDocumentSourcesAsync(CancellationToken cancellationToken = default);
    Task<List<AcademicTermDto>> GetAcademicTermsAsync(CancellationToken cancellationToken = default);
    Task<SubjectDto> CreateSubjectAsync(string code, string name, Guid? academicTermId = null, CancellationToken cancellationToken = default);
    Task<SubjectDto?> UpdateSubjectAsync(Guid id, string code, string name, Guid? academicTermId = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentTypeDto> CreateDocumentTypeAsync(string name, string? description, CancellationToken cancellationToken = default);
    Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, string name, string? description, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LanguageDto> CreateLanguageAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<LanguageDto?> UpdateLanguageAsync(Guid id, string code, string name, CancellationToken cancellationToken = default);
    Task<bool> DeleteLanguageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DocumentSourceDto> CreateDocumentSourceAsync(string name, CancellationToken cancellationToken = default);
    Task<DocumentSourceDto?> UpdateDocumentSourceAsync(Guid id, string name, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentSourceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AcademicTermDto> CreateAcademicTermAsync(string name, int order, CancellationToken cancellationToken = default);
    Task<AcademicTermDto?> UpdateAcademicTermAsync(Guid id, string name, int order, CancellationToken cancellationToken = default);
    Task<bool> DeleteAcademicTermAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentReportDto> ReportDocumentAsync(Guid documentId, Guid reporterUserId, string reason, CancellationToken cancellationToken = default);
    Task<List<DocumentReportDto>> GetPendingReportsAsync(CancellationToken cancellationToken = default);
    Task ResolveReportAsync(Guid reportId, string action, CancellationToken cancellationToken = default);
    Task<MyDocumentsDto> GetAdminDocumentsAsync(string? query, Guid? subjectId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task SeedInitialDataAsync(CancellationToken cancellationToken = default);

    Task<List<SubjectDto>> GetSubjectsAssignedToLecturerAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignSubjectsToLecturerAsync(Guid userId, List<Guid> subjectIds, CancellationToken cancellationToken = default);
    Task<bool> IsSubjectAssignedToLecturerAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, (Guid UserId, string FullName)>> GetSubjectLecturerMapAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload file, save to storage, trigger indexing pipeline (extract → chunk → embed → save).
    /// Returns result DTO with DocumentId, FileName, and initial Status.
    /// </summary>
    Task<DocumentUploadResultDto> UploadAndProcessAsync(IFormFile file, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a document (change Status to "approved"). Only Lecturer/Admin can call this.
    /// </summary>
    Task ApproveDocumentAsync(Guid documentId, Guid approverId, CancellationToken cancellationToken = default);

    Task<bool> ToggleBookmarkAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
}

