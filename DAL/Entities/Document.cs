using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities;

public partial class Document
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Subject { get; set; }

    public string? School { get; set; }

    public string? Department { get; set; }

    public string Status { get; set; } = null!;

    public string? Language { get; set; }

    public string Visibility { get; set; } = null!;

    [Column("source_type")]
    public string? SourceType { get; set; }

    public int? PageCount { get; set; }

    public int TotalChunks { get; set; }

    public int TotalChapters { get; set; }

    public string? SearchText { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual ICollection<DocumentChapter> DocumentChapters { get; set; } = new List<DocumentChapter>();

    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public virtual ICollection<DocumentFile> DocumentFiles { get; set; } = new List<DocumentFile>();

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
