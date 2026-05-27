using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class DocumentFile
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public string OriginalFilename { get; set; } = null!;

    public string? StoragePath { get; set; }

    public string? S3Bucket { get; set; }

    public string? S3Key { get; set; }

    public string? FileUrl { get; set; }

    public string? MimeType { get; set; }

    public long FileSizeBytes { get; set; }

    public string? ChecksumSha256 { get; set; }

    public int? PageCount { get; set; }

    public string? ExtractedText { get; set; }

    public string ExtractionStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Document Document { get; set; } = null!;
}
