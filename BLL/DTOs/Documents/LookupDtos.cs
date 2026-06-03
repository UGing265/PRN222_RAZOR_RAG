namespace BLL.DTOs.Documents;

public sealed class SubjectDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid? AcademicTermId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class MajorDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class DocumentTypeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class LanguageDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class DocumentSourceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class AcademicTermDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Order { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class DocumentFileDto
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public string OriginalFilename { get; init; } = string.Empty;
    public string? StoragePath { get; init; }
    public string? S3Key { get; init; }
    public string? FileUrl { get; init; }
    public string? MimeType { get; init; }
    public long FileSizeBytes { get; init; }
    public int? PageCount { get; init; }
    public string ExtractionStatus { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class DocumentChunkDto
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public Guid? ChapterId { get; init; }
    public int ChunkOrder { get; init; }
    public int? PageNumber { get; init; }
    public string Content { get; init; } = string.Empty;
    public int? ContentTokens { get; init; }
    public string Metadata { get; init; } = string.Empty;
    public string? ChunkHash { get; init; }
    public bool HasEmbedding { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class DocumentChapterDto
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public Guid? ParentChapterId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public int ChapterOrder { get; init; }
    public int? StartPage { get; init; }
    public int? EndPage { get; init; }
    public int? StartChunkIndex { get; init; }
    public int? EndChunkIndex { get; init; }
    public bool IsAiGenerated { get; init; }
    public decimal? ConfidenceScore { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class DocumentReportDto
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public Guid ReporterUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? DocumentTitle { get; init; }
    public string? DocumentSlug { get; init; }
    public string? ReporterEmail { get; init; }
}

public sealed class DocumentCreateResultDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
}
