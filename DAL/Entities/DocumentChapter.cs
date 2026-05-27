using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class DocumentChapter
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid? ParentChapterId { get; set; }

    public string Title { get; set; } = null!;

    public string? Summary { get; set; }

    public int ChapterOrder { get; set; }

    public int? StartPage { get; set; }

    public int? EndPage { get; set; }

    public int? StartChunkIndex { get; set; }

    public int? EndChunkIndex { get; set; }

    public bool IsAiGenerated { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public virtual ICollection<DocumentChapter> InverseParentChapter { get; set; } = new List<DocumentChapter>();

    public virtual DocumentChapter? ParentChapter { get; set; }
}
