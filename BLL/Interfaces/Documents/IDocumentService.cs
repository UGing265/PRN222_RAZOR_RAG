using BLL.DTOs.Documents;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Interfaces.Documents;

public interface IDocumentService
{
    Task<Document> CreateDocumentAsync(DocumentCreateInput input, IFormFile file, CancellationToken cancellationToken = default);
    Task<(string Key, string Url)> UploadOriginalFileToS3Async(Guid documentId, IFormFile file, CancellationToken cancellationToken = default);
    Task<DocumentFile> AddDocumentFileAsync(Guid documentId, string s3Key, string s3Url, IFormFile file, Func<int, Task>? onProgress = null, CancellationToken cancellationToken = default);
    Task EnqueueUploadJobAsync(Guid ownerUserId, Guid documentId, string fileName, string storagePath, long fileSizeBytes, CancellationToken cancellationToken = default);
    Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, int chunkPage = 1, int chunkPageSize = 10, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentBySlugAsync(string slug, Guid? requesterUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentForOwnerAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetOwnedDocumentBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<MyDocumentsDto> GetMyDocumentsAsync(Guid ownerUserId, string? query, Guid? subjectId, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default);
    Task<MyDocumentsDto> GetAllDocumentsAsync(string? query, Guid? subjectId, int page = 1, int pageSize = 6, Guid? requesterUserId = null, CancellationToken cancellationToken = default);
    Task<int> CountMyDocumentsAsync(Guid ownerUserId, string? query, CancellationToken cancellationToken = default);
    Task<int> CountMyDocumentsByStatusAsync(Guid ownerUserId, string status, CancellationToken cancellationToken = default);
    Task<int> CountMyFilesAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<int> CountMyChunksAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<UploadJobSummaryDto>> GetActiveUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<DeleteDocumentViewData?> GetDeleteDocumentViewDataAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<DeleteDocumentViewData?> GetDeleteDocumentViewDataBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Document?> UpdateDocumentAsync(Guid documentId, Guid ownerUserId, DocumentEditInput input, CancellationToken cancellationToken = default);
    Task DeleteDocumentAssetsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Stream> DownloadOriginalFileFromS3Async(Guid documentId, string s3Key, CancellationToken cancellationToken = default);
    Task<List<DocumentChapter>> GenerateChaptersAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<UploadJobSummaryDto>> GetUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Major>> GetMajorsAsync(CancellationToken cancellationToken = default);
    Task<List<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);
    Task<List<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<Subject> CreateSubjectAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<DocumentType> CreateDocumentTypeAsync(string name, string? description, CancellationToken cancellationToken = default);
    Task<Language> CreateLanguageAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<DocumentReport> ReportDocumentAsync(Guid documentId, Guid reporterUserId, string reason, CancellationToken cancellationToken = default);
    Task<List<DocumentReport>> GetPendingReportsAsync(CancellationToken cancellationToken = default);
    Task ResolveReportAsync(Guid reportId, string action, CancellationToken cancellationToken = default);
    Task<MyDocumentsDto> GetAdminDocumentsAsync(string? query, Guid? subjectId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}
