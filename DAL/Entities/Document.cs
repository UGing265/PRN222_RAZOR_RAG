using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public partial class Document
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public Guid? MajorId { get; set; }

    public Guid? SubjectId { get; set; }

    [Column("document_type_id")]
    public Guid? DocumentTypeId { get; set; }

    [Column("academic_term")]
    public string? AcademicTerm { get; set; }

    public string Status { get; set; } = null!;

    [Column("language_id")]
    public Guid? LanguageId { get; set; }

    public string Visibility { get; set; } = null!;

    [Column("source_type")]
    public string? SourceType { get; set; }

    public int? PageCount { get; set; }

    public int TotalChunks { get; set; }

    public int TotalChapters { get; set; }

    [Column("view_count")]
    public int ViewCount { get; set; }

    [Column("download_count")]
    public int DownloadCount { get; set; }

    public string? SearchText { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual ICollection<DocumentChapter> DocumentChapters { get; set; } = new List<DocumentChapter>();

    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public virtual ICollection<DocumentFile> DocumentFiles { get; set; } = new List<DocumentFile>();

    public virtual User OwnerUser { get; set; } = null!;

    public virtual Major? Major { get; set; }

    public virtual Subject? Subject { get; set; }

    public virtual DocumentType? DocumentType { get; set; }

    public virtual Language? Language { get; set; }

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public virtual ICollection<UserBookmark> UserBookmarks { get; set; } = new List<UserBookmark>();

    public virtual ICollection<DocumentReport> DocumentReports { get; set; } = new List<DocumentReport>();
}
