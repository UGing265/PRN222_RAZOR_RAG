using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using DAL.Entities;
using DAL.Interfaces.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector;
using System.Security.Cryptography;
using System.Text.Json;

namespace BLL.Services.Documents;

public class DocumentService : IDocumentService
{
    private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx", ".ppt", ".pptx"];
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    private readonly IDocumentRepository _documentRepository;
    private readonly IFileParserService _fileParserService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChapterSegmentationService _chapterSegmentationService;
    private readonly IS3StorageService _s3StorageService;
    private readonly DocumentIndexingOptions _indexingOptions;

    public DocumentService(IDocumentRepository documentRepository, IConfiguration configuration, IFileParserService fileParserService, IEmbeddingService embeddingService, IChapterSegmentationService chapterSegmentationService, IS3StorageService s3StorageService)
    {
        _documentRepository = documentRepository;
        _fileParserService = fileParserService;
        _embeddingService = embeddingService;
        _chapterSegmentationService = chapterSegmentationService;
        _s3StorageService = s3StorageService;
        _indexingOptions = configuration.GetSection("DocumentIndexing").Get<DocumentIndexingOptions>() ?? new DocumentIndexingOptions();
    }

    private static string BuildSlug(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }

        var slug = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "document" : slug;
    }

    private static string BuildShortCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(3);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
    {
        while (true)
        {
            var candidate = $"{baseSlug}-{BuildShortCode()}";
            if (await _documentRepository.GetDocumentBySlugAsync(candidate, cancellationToken) is null)
            {
                return candidate;
            }
        }
    }

    public async Task<Document> CreateDocumentAsync(DocumentCreateInput input, IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);
        var slug = await EnsureUniqueSlugAsync(BuildSlug(input.Title), cancellationToken);
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OwnerUserId = input.OwnerUserId,
            Title = input.Title,
            Slug = slug,
            Description = input.Description,
            Subject = input.Subject,
            School = input.School,
            Department = input.Department,
            Language = input.Language,
            Visibility = input.Visibility,
            SourceType = input.SourceType,
            Status = "approved",
            TotalChunks = 0,
            TotalChapters = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ApprovedAt = DateTime.UtcNow
        };

        return await _documentRepository.AddDocumentAsync(document, cancellationToken);
    }

    public async Task<(string Key, string Url)> UploadOriginalFileToS3Async(Guid documentId, IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);
        return await _s3StorageService.UploadAsync(documentId.ToString("N"), file, cancellationToken);
    }

    public async Task<DocumentFile> AddDocumentFileAsync(Guid documentId, string s3Key, string s3Url, IFormFile file, Func<int, Task>? onProgress = null, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);
        var document = await _documentRepository.GetDocumentAsync(documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document not found.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");

        try
        {
            await using (var s3Stream = await _s3StorageService.OpenReadAsync(s3Key, cancellationToken))
            await using (var tempFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await s3Stream.CopyToAsync(tempFile, cancellationToken);
            }

            await using var checksumStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var checksum = await ComputeSha256Async(checksumStream, cancellationToken);

            var extractedText = SanitizeForPostgres(await _fileParserService.ExtractTextAsync(tempPath, extension, cancellationToken));
            var chunks = DocumentChunker.ChunkText(extractedText, 1, _indexingOptions.ChunkMaxWords, _indexingOptions.ChunkOverlapWords).ToList();
            var chunkEntities = new List<DocumentChunk>();
            var totalChunks = chunks.Count;
            var chunkIndex = 0;

            foreach (var batch in chunks.Chunk(_indexingOptions.BatchSize))
            {
                var cleanBatch = batch.Select(SanitizeForPostgres).ToList();
                var embedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                embedCts.CancelAfter(TimeSpan.FromSeconds(120));

                var embeddings = await _embeddingService.EmbedBatchAsync(cleanBatch, embedCts.Token);

                for (int i = 0; i < cleanBatch.Count; i++)
                {
                    var chunk = cleanBatch[i];
                    var embedding = embeddings.ElementAtOrDefault(i) ?? new Vector(new float[3072]);
                    var metadata = SanitizeForPostgres(JsonSerializer.Serialize(new
                    {
                        sourceFileName = SanitizeForPostgres(file.FileName),
                        s3Key,
                        s3Url,
                        checksumSha256 = checksum,
                        chunkOrder = chunkIndex,
                        language = document.Language,
                        visibility = document.Visibility,
                        sourceType = document.SourceType
                    }));

                    chunkEntities.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = documentId,
                        ChunkOrder = chunkIndex,
                        PageNumber = null,
                        Content = chunk,
                        ContentTokens = null,
                        ChunkHash = DocumentChunker.ComputeChunkHash(chunk),
                        Embedding = embedding,
                        Metadata = metadata,
                        CreatedAt = DateTime.UtcNow,
                        ChapterId = null
                    });

                    chunkIndex++;
                }

                if (onProgress is not null && totalChunks > 0)
                {
                    var progress = 20 + (int)((chunkIndex / (double)totalChunks) * 70);
                    await onProgress(progress);
                }

                if (chunkIndex < totalChunks)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_indexingOptions.BatchDelaySeconds), cancellationToken);
                }
            }

            var documentFile = new DocumentFile
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                OriginalFilename = file.FileName,
                StoragePath = s3Key,
                S3Bucket = null,
                S3Key = s3Key,
                FileUrl = s3Url,
                MimeType = file.ContentType,
                FileSizeBytes = file.Length,
                ChecksumSha256 = checksum,
                ExtractionStatus = string.IsNullOrWhiteSpace(extractedText) ? "failed" : "success",
                CreatedAt = DateTime.UtcNow,
                ExtractedText = extractedText
            };

            await _documentRepository.AddDocumentFilesAsync([documentFile], cancellationToken);
            if (chunkEntities.Count > 0)
            {
                await _documentRepository.AddDocumentChunksAsync(chunkEntities, cancellationToken);
            }

            document.PageCount = documentFile.PageCount ?? document.PageCount;
            document.TotalChunks = chunkEntities.Count;
            document.SearchText = BuildSearchText(document, extractedText);
            document.UpdatedAt = DateTime.UtcNow;

            await _documentRepository.SaveChangesAsync(cancellationToken);
            await GenerateChaptersAsync(documentId, cancellationToken);
            return documentFile;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Không thể lưu metadata file upload.", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("Không thể lưu file lên hệ thống.", ex);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, int chunkPage = 1, int chunkPageSize = 10, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetDocumentWithFilesAsync(documentId, cancellationToken);
        if (document is null) return null;

        chunkPageSize = Math.Clamp(chunkPageSize, 8, 10);
        var orderedChunks = document.DocumentChunks?.OrderBy(x => x.ChunkOrder).ToList() ?? [];
        var totalPages = Math.Max(1, (int)Math.Ceiling(orderedChunks.Count / (double)chunkPageSize));
        chunkPage = Math.Clamp(chunkPage, 1, totalPages);
        var pageChunks = orderedChunks.Skip((chunkPage - 1) * chunkPageSize).Take(chunkPageSize).ToList();

        return new DocumentDetailsDto
        {
            Id = document.Id,
            Title = document.Title,
            Subject = document.Subject,
            School = document.School,
            Department = document.Department,
            Visibility = document.Visibility,
            Language = document.Language,
            Description = document.Description,
            Status = document.Status,
            TotalChunks = document.TotalChunks,
            TotalChapters = document.TotalChapters,
            ApprovedAt = document.ApprovedAt,
            FileCount = document.DocumentFiles?.Count ?? 0,
            Files = document.DocumentFiles?.ToList() ?? [],
            Chapters = document.DocumentChapters?.OrderBy(x => x.ChapterOrder).ToList() ?? [],
            Chunks = pageChunks
        };
    }

    public Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default) => _documentRepository.GetDocumentBySlugAsync(slug, cancellationToken);
    public Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default) => _documentRepository.GetDocumentWithFilesAsync(documentId, cancellationToken);
    public Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default) => _documentRepository.GetDocumentWithFilesBySlugAsync(slug, cancellationToken);
    public Task<Document?> GetDocumentForOwnerAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default) => _documentRepository.GetOwnedDocumentAsync(documentId, ownerUserId, cancellationToken);
    public Task<Document?> GetOwnedDocumentBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default) => _documentRepository.GetOwnedDocumentBySlugAsync(slug, ownerUserId, cancellationToken);

    public async Task<MyDocumentsDto> GetMyDocumentsAsync(Guid ownerUserId, string? query, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 6, 12);
        page = Math.Max(page, 1);
        var totalDocuments = await _documentRepository.CountDocumentsByOwnerAsync(ownerUserId, query, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalDocuments / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var documents = await _documentRepository.GetDocumentsByOwnerAsync(ownerUserId, query, page, pageSize, cancellationToken);
        var activeUploadJobs = await _documentRepository.GetActiveUploadJobsByOwnerAsync(ownerUserId, cancellationToken);

        return new MyDocumentsDto
        {
            Documents = documents.Select(x => new DocumentListItemDto
            {
                Id = x.Id,
                Slug = x.Slug ?? string.Empty,
                Title = x.Title,
                Subject = x.Subject,
                Status = x.Status,
                Visibility = x.Visibility,
                School = x.School,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FileCount = x.DocumentFiles.Count,
                ChunkCount = x.DocumentChunks.Count,
                PreviewText = x.DocumentChunks.OrderBy(c => c.ChunkOrder).Select(c => c.Content).FirstOrDefault()
            }).ToList(),
            TotalDocuments = totalDocuments,
            PendingDocuments = await _documentRepository.CountDocumentsByStatusAsync(ownerUserId, "pending", cancellationToken),
            ApprovedDocuments = await _documentRepository.CountDocumentsByStatusAsync(ownerUserId, "approved", cancellationToken),
            RejectedDocuments = await _documentRepository.CountDocumentsByStatusAsync(ownerUserId, "rejected", cancellationToken),
            TotalFiles = await _documentRepository.CountFilesByOwnerAsync(ownerUserId, cancellationToken),
            TotalChunks = await _documentRepository.CountChunksByOwnerAsync(ownerUserId, cancellationToken),
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActiveUploadJobs = activeUploadJobs.Select(x => new UploadJobSummaryDto
            {
                Id = x.Id,
                DocumentId = x.DocumentId,
                FileName = x.FileName,
                FileSizeBytes = x.FileSizeBytes,
                Status = x.Status,
                ProgressPercent = x.ProgressPercent,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList()
        };
    }

    public Task<int> CountMyDocumentsAsync(Guid ownerUserId, string? query, CancellationToken cancellationToken = default) => _documentRepository.CountDocumentsByOwnerAsync(ownerUserId, query, cancellationToken);
    public Task<int> CountMyDocumentsByStatusAsync(Guid ownerUserId, string status, CancellationToken cancellationToken = default) => _documentRepository.CountDocumentsByStatusAsync(ownerUserId, status, cancellationToken);
    public Task<int> CountMyFilesAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => _documentRepository.CountFilesByOwnerAsync(ownerUserId, cancellationToken);
    public Task<int> CountMyChunksAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => _documentRepository.CountChunksByOwnerAsync(ownerUserId, cancellationToken);

    public async Task<List<UploadJobSummaryDto>> GetActiveUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var jobs = await _documentRepository.GetActiveUploadJobsByOwnerAsync(ownerUserId, cancellationToken);
        return jobs.Select(x => new UploadJobSummaryDto
        {
            Id = x.Id,
            DocumentId = x.DocumentId,
            FileName = x.FileName,
            FileSizeBytes = x.FileSizeBytes,
            Status = x.Status,
            ProgressPercent = x.ProgressPercent,
            Message = x.Message,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToList();
    }

    public async Task<DeleteDocumentViewData?> GetDeleteDocumentViewDataAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetOwnedDocumentAsync(documentId, ownerUserId, cancellationToken);
        if (document is null) return null;

        return new DeleteDocumentViewData
        {
            Id = document.Id,
            Title = document.Title,
            FileCount = await _documentRepository.CountFilesByDocumentAsync(documentId, cancellationToken),
            ChunkCount = await _documentRepository.CountChunksByDocumentAsync(documentId, cancellationToken)
        };
    }

    public async Task<DeleteDocumentViewData?> GetDeleteDocumentViewDataBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetOwnedDocumentBySlugAsync(slug, ownerUserId, cancellationToken);
        if (document is null) return null;

        return new DeleteDocumentViewData
        {
            Id = document.Id,
            Title = document.Title,
            FileCount = await _documentRepository.CountFilesByDocumentAsync(document.Id, cancellationToken),
            ChunkCount = await _documentRepository.CountChunksByDocumentAsync(document.Id, cancellationToken)
        };
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetDocumentAsync(documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document not found.");

        await _documentRepository.RemoveUploadJobsByDocumentAsync(documentId, cancellationToken);
        await DeleteDocumentAssetsAsync(documentId, cancellationToken);
        await _documentRepository.RemoveDocumentFilesByDocumentAsync(documentId, cancellationToken);
        await _documentRepository.RemoveDocumentChunksByDocumentAsync(documentId, cancellationToken);
        await _documentRepository.RemoveDocumentAsync(document, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDocumentAssetsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var files = await _documentRepository.GetDocumentFilesAsync(documentId, cancellationToken);
        foreach (var file in files)
        {
            var key = file.S3Key ?? file.StoragePath;
            if (!string.IsNullOrWhiteSpace(key))
            {
                await _s3StorageService.DeleteAsync(key, cancellationToken);
            }
        }
    }

    public async Task<Stream> DownloadOriginalFileFromS3Async(Guid documentId, string s3Key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(s3Key)) throw new InvalidOperationException("S3 key is missing.");
        return await _s3StorageService.OpenReadAsync(s3Key, cancellationToken);
    }

    public async Task<List<DocumentChapter>> GenerateChaptersAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetDocumentWithChunksAsync(documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document not found.");

        var chunks = document.DocumentChunks?.OrderBy(x => x.ChunkOrder).ToList() ?? [];
        if (chunks.Count == 0) return [];

        var chapters = await _chapterSegmentationService.GenerateChaptersAsync(document, chunks, cancellationToken);
        if (chapters.Count == 0) return [];

        await _documentRepository.AddDocumentChaptersAsync(chapters, cancellationToken);
        foreach (var chapter in chapters)
        {
            foreach (var chunk in chunks.Where(x => x.ChunkOrder >= chapter.StartChunkIndex && x.ChunkOrder <= chapter.EndChunkIndex))
            {
                chunk.ChapterId = chapter.Id;
            }
        }

        document.TotalChapters = chapters.Count;
        document.UpdatedAt = DateTime.UtcNow;
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return chapters;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetDocumentsByOwnerAsync(ownerUserId, null, 1, 5, cancellationToken);
        var activeJobs = await _documentRepository.GetActiveUploadJobsByOwnerAsync(ownerUserId, cancellationToken);
        var completed = activeJobs.FirstOrDefault(x => x.Status == "done");

        return new DashboardSummaryDto
        {
            TotalDocuments = await _documentRepository.CountDocumentsByOwnerAsync(ownerUserId, null, cancellationToken),
            TotalChunks = await _documentRepository.CountChunksByOwnerAsync(ownerUserId, cancellationToken),
            TotalFiles = await _documentRepository.CountFilesByOwnerAsync(ownerUserId, cancellationToken),
            ApprovedDocuments = await _documentRepository.CountDocumentsByStatusAsync(ownerUserId, "approved", cancellationToken),
            PendingDocuments = await _documentRepository.CountDocumentsByStatusAsync(ownerUserId, "pending", cancellationToken),
            RejectedDocuments = await _documentRepository.CountDocumentsByStatusAsync(ownerUserId, "rejected", cancellationToken),
            RecentDocuments = documents.Select(x => new DashboardRecentDocumentDto
            {
                Id = x.Id,
                Slug = x.Slug ?? string.Empty,
                Title = x.Title,
                Subject = x.Subject,
                Status = x.Status,
                UpdatedAt = x.UpdatedAt,
                FileCount = x.DocumentFiles.Count,
                ChunkCount = x.DocumentChunks.Count
            }).ToList(),
            ActiveUploadJobs = activeJobs.Select(x => new UploadJobSummaryDto
            {
                Id = x.Id,
                DocumentId = x.DocumentId,
                FileName = x.FileName,
                FileSizeBytes = x.FileSizeBytes,
                Status = x.Status,
                ProgressPercent = x.ProgressPercent,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList(),
            CompletedUploadMessage = completed is null ? null : $"Tệp \"{completed.FileName}\" đã xử lý xong."
        };
    }

    public Task<List<UploadJobSummaryDto>> GetUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => GetActiveUploadJobsAsync(ownerUserId, cancellationToken);

    private static string BuildSearchText(Document document, string extractedText)
    {
        var safeExtractedText = string.IsNullOrWhiteSpace(extractedText) ? string.Empty : (extractedText.Length > 50000 ? extractedText.Substring(0, 50000) : extractedText);
        return string.Join(" ", new[] { document.Title, document.Description, document.Subject, document.School, document.Department, safeExtractedText }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file is null) throw new InvalidOperationException("Vui lòng chọn file upload.");
        if (file.Length <= 0) throw new InvalidOperationException("File upload đang rỗng.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension)) throw new InvalidOperationException("Chỉ hỗ trợ PDF, DOC, DOCX, PPT, PPTX.");
        if (!AllowedMimeTypes.Contains(file.ContentType)) throw new InvalidOperationException("Định dạng MIME của file không hợp lệ.");
    }

    private static string SanitizeForPostgres(string value) => value.Replace('\0', ' ');
    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
