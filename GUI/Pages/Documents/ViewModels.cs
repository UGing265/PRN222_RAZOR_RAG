using BLL.DTOs.Documents;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GUI.Pages.Documents;

public class AdminMetadataViewModel
{
    public List<SubjectDto> Subjects { get; set; } = new();
    public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public List<DocumentSourceDto> DocumentSources { get; set; } = new();
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();
}

public class AllDocumentsViewModel
{
    public int TotalDocuments { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Query { get; set; }
    public string? SortBy { get; set; }
    public List<DocumentListItemViewModel> Documents { get; set; } = new();
}

public class DeleteDocumentViewModel
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int ChunkCount { get; set; }
}

public class DocumentChapterViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int ChapterOrder { get; set; }
    public int StartChunkIndex { get; set; }
    public int EndChunkIndex { get; set; }
    public int ChunkCount => EndChunkIndex >= StartChunkIndex ? EndChunkIndex - StartChunkIndex + 1 : 0;
    public bool IsAiGenerated { get; set; }
    public decimal? ConfidenceScore { get; set; }
}

public class DocumentChunkViewModel
{
    public int ChunkOrder { get; set; }
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string? ChunkHash { get; set; }
    public bool HasEmbedding { get; set; }
}

public class DocumentCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu.")]
    [StringLength(500, ErrorMessage = "Tiêu đề không được vượt quá 500 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    public Guid? SubjectId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public Guid? AcademicTermId { get; set; }

    public Guid? LanguageId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn quyền hiển thị.")]
    [StringLength(30, ErrorMessage = "Quyền hiển thị không được vượt quá 30 ký tự.")]
    public string Visibility { get; set; } = "school_wide";

    public Guid? DocumentSourceId { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn file để upload.")]
    public IFormFile? UploadFile { get; set; }
}

public class DocumentDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public string? MajorName { get; set; }
    public string? MajorCode { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? AcademicTermName { get; set; }
    public string? Visibility { get; set; }
    public Guid? LanguageId { get; set; }
    public string? LanguageCode { get; set; }
    public string? LanguageName { get; set; }
    public string? DocumentSourceName { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public int TotalChunks { get; set; }
    public int TotalChapters { get; set; }
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int FileCount { get; set; }
    public bool IsBookmarked { get; set; }

    public int ChunkPage { get; set; } = 1;
    public int ChunkPageSize { get; set; } = 10;
    public int TotalChunkPages { get; set; } = 1;
    public List<DocumentFileViewModel> Files { get; set; } = [];
    public List<DocumentChunkViewModel> Chunks { get; set; } = [];
    public List<DocumentChapterViewModel> Chapters { get; set; } = [];
}

public class DocumentEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu.")]
    [StringLength(500, ErrorMessage = "Tiêu đề không được vượt quá 500 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    public Guid? SubjectId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public Guid? AcademicTermId { get; set; }

    public Guid? LanguageId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn quyền hiển thị.")]
    [StringLength(30, ErrorMessage = "Quyền hiển thị không được vượt quá 30 ký tự.")]
    public string Visibility { get; set; } = "school_wide";

    public Guid? DocumentSourceId { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }
}

public class DocumentFileViewModel
{
    public Guid Id { get; set; }
    public string? OriginalFilename { get; set; }
    public string? MimeType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ExtractionStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentListItemViewModel
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public string? MajorName { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? AcademicTermName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int ChunkCount { get; set; }
    public string? PreviewText { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Visibility { get; set; }
    public string? OwnerEmail { get; set; }
    public int ViewCount { get; set; }
    public bool IsBookmarked { get; set; }
}

public class MyDocumentsViewModel
{
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int TotalFiles { get; set; }
    public int TotalChunks { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<DocumentListItemViewModel> Documents { get; set; } = new();
    public List<UploadJobViewModel> ActiveUploadJobs { get; set; } = new();
    public List<BLL.DTOs.Documents.DocumentReportDto> PendingReports { get; set; } = new();
}

public class UploadJobViewModel
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
