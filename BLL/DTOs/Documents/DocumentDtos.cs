using System;
using System.Collections.Generic;

namespace BLL.DTOs.Documents;

public sealed class DocumentCreateInput
{
    public Guid OwnerUserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }

    public Guid? SubjectId { get; init; }
    public Guid? DocumentTypeId { get; init; }
    public Guid? LanguageId { get; init; }
    public string? Visibility { get; init; }
    public Guid? DocumentSourceId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string FileContentType { get; init; } = string.Empty;
}

public sealed class DocumentEditInput
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }

    public Guid? SubjectId { get; init; }
    public Guid? DocumentTypeId { get; init; }
    public Guid? LanguageId { get; init; }
    public string Visibility { get; init; } = "private";
    public Guid? DocumentSourceId { get; init; }
}

public sealed class DocumentListItemDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;


    public Guid? SubjectId { get; init; }
    public string? SubjectName { get; init; }
    public string? SubjectCode { get; init; }
    public Guid? DocumentTypeId { get; init; }
    public string? DocumentTypeName { get; init; }
    public Guid? DocumentSourceId { get; init; }
    public string? DocumentSourceName { get; init; }
    public string Status { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public int ChunkCount { get; init; }
    public string? PreviewText { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? Visibility { get; init; }
    public string? OwnerEmail { get; init; }
    public int ViewCount { get; init; }
    public bool IsBookmarked { get; init; }
}

public sealed class DocumentDetailsDto
{
    public Guid Id { get; init; }
    public Guid OwnerUserId { get; init; }
    public string Title { get; init; } = string.Empty;



    public Guid? SubjectId { get; init; }
    public string? SubjectName { get; init; }
    public string? SubjectCode { get; init; }
    public Guid? DocumentTypeId { get; init; }
    public string? DocumentTypeName { get; init; }
    public Guid? DocumentSourceId { get; init; }
    public string? DocumentSourceName { get; init; }
    public string? Visibility { get; init; }
    public Guid? LanguageId { get; init; }
    public string? LanguageCode { get; init; }
    public string? LanguageName { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public int TotalChunks { get; init; }
    public int TotalChapters { get; init; }
    public int ViewCount { get; init; }
    public int DownloadCount { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public int FileCount { get; init; }
    public bool IsBookmarked { get; init; }
    public List<DocumentFileDto> Files { get; init; } = [];
    public List<DocumentChunkDto> Chunks { get; init; } = [];
    public List<DocumentChapterDto> Chapters { get; init; } = [];
}

public sealed class UploadJobSummaryDto
{
    public Guid Id { get; init; }
    public Guid? DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public string? Message { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class MyDocumentsDto
{
    public List<DocumentListItemDto> Documents { get; init; } = [];
    public int TotalDocuments { get; init; }
    public int PendingDocuments { get; init; }
    public int ApprovedDocuments { get; init; }
    public int RejectedDocuments { get; init; }
    public int TotalFiles { get; init; }
    public int TotalChunks { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public List<UploadJobSummaryDto> ActiveUploadJobs { get; init; } = [];
}

public sealed class DeleteDocumentViewData
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int ChunkCount { get; set; }
}

/// <summary>
/// Result of uploading and triggering the indexing pipeline for a document.
/// </summary>
public sealed class DocumentUploadResultDto
{
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Preview of a single chunk for UI display.
/// </summary>
public sealed class DocumentChunkPreviewDto
{
    public int ChunkIndex { get; init; }
    public string ContentPreview { get; init; } = string.Empty;
}
