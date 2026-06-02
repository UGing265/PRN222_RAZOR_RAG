using System;
using System.Collections.Generic;

namespace GUI.Models.Documents;

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
    public string? Description { get; set; }
    public string? Status { get; set; }
    public int TotalChunks { get; set; }
    public int TotalChapters { get; set; }
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int FileCount { get; set; }
    public int ChunkPage { get; set; } = 1;
    public int ChunkPageSize { get; set; } = 10;
    public int TotalChunkPages { get; set; } = 1;
    public List<DocumentFileViewModel> Files { get; set; } = [];
    public List<DocumentChunkViewModel> Chunks { get; set; } = [];
    public List<DocumentChapterViewModel> Chapters { get; set; } = [];
}
