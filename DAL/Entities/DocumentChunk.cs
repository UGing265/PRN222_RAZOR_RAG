using System;
using System.Collections.Generic;
using Pgvector;

namespace DAL.Entities;

public partial class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid? ChapterId { get; set; }

    public int ChunkOrder { get; set; }

    public int? PageNumber { get; set; }

    public string Content { get; set; } = null!;

    public int? ContentTokens { get; set; }

    public string? ChunkHash { get; set; }

    public string Metadata { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public Vector? Embedding { get; set; }

    public virtual DocumentChapter? Chapter { get; set; }

    public virtual Document Document { get; set; } = null!;
}
