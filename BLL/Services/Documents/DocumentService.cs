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
    private readonly IUploadJobRepository _uploadJobRepository;

    public DocumentService(IDocumentRepository documentRepository, IConfiguration configuration, IFileParserService fileParserService, IEmbeddingService embeddingService, IChapterSegmentationService chapterSegmentationService, IS3StorageService s3StorageService, IUploadJobRepository uploadJobRepository)
    {
        _documentRepository = documentRepository;
        _fileParserService = fileParserService;
        _embeddingService = embeddingService;
        _chapterSegmentationService = chapterSegmentationService;
        _s3StorageService = s3StorageService;
        _uploadJobRepository = uploadJobRepository;
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

    public async Task<DocumentCreateResultDto> CreateDocumentAsync(DocumentCreateInput input, IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        // Compute MD5 hash of the uploaded file to detect duplicates
        string md5Hash;
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            using var stream = file.OpenReadStream();
            var hashBytes = await md5.ComputeHashAsync(stream, cancellationToken);
            md5Hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Check if a document with the same MD5 hash already exists
        if (await _documentRepository.ExistsDocumentByMd5Async(md5Hash, cancellationToken))
        {
            throw new InvalidOperationException("Tài liệu này đã tồn tại trong hệ thống (file trùng lặp). Vui lòng kiểm tra lại.");
        }

        var slug = await EnsureUniqueSlugAsync(BuildSlug(input.Title), cancellationToken);
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OwnerUserId = input.OwnerUserId,
            Title = input.Title,
            Slug = slug,
            Description = input.Description,

            SubjectId = input.SubjectId,
            DocumentTypeId = input.DocumentTypeId,
            AcademicTermId = input.AcademicTermId,
            LanguageId = input.LanguageId,
            Visibility = input.Visibility ?? "school_wide",
            DocumentSourceId = input.DocumentSourceId,
            Status = "processing",
            TotalChunks = 0,
            TotalChapters = 0,
            ViewCount = 0,
            DownloadCount = 0,
            Md5Hash = md5Hash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ApprovedAt = null
        };

        var saved = await _documentRepository.AddDocumentAsync(document, cancellationToken);
        return new DocumentCreateResultDto { Id = saved.Id, Slug = saved.Slug ?? string.Empty };
    }

    public async Task<(string Key, string Url)> UploadOriginalFileToS3Async(Guid documentId, IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);
        return await _s3StorageService.UploadAsync(documentId.ToString("N"), file, cancellationToken);
    }

    public async Task EnqueueUploadJobAsync(Guid ownerUserId, Guid documentId, string fileName, string storagePath, long fileSizeBytes, CancellationToken cancellationToken = default)
    {
        await _uploadJobRepository.AddUploadJobAsync(new UploadJob
        {
            OwnerUserId = ownerUserId,
            DocumentId = documentId,
            FileName = fileName,
            StoragePath = storagePath,
            FileSizeBytes = fileSizeBytes,
            Status = "pending",
            ProgressPercent = 0,
            Message = "Đang chờ xử lý",
            IsNotified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task AddDocumentFileAsync(Guid documentId, string s3Key, string s3Url, IFormFile file, Func<int, Task>? onProgress = null, CancellationToken cancellationToken = default)
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
                        language = document.Language?.Name,
                        visibility = document.Visibility,
                        DocumentSourceId = document.DocumentSourceId,
                        DocumentSourceName = document.DocumentSource?.Name,
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

    public async Task<DocumentDetailsDto?> GetDocumentDetailsAsync(Guid documentId, int chunkPage = 1, int chunkPageSize = 10, bool incrementViewCount = true, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetDocumentWithFilesAsync(documentId, cancellationToken);
        if (document is null) return null;

        chunkPageSize = Math.Clamp(chunkPageSize, 8, 10);
        var orderedChunks = document.DocumentChunks?.OrderBy(x => x.ChunkOrder).ToList() ?? [];
        var totalPages = Math.Max(1, (int)Math.Ceiling(orderedChunks.Count / (double)chunkPageSize));
        chunkPage = Math.Clamp(chunkPage, 1, totalPages);
        var pageChunks = orderedChunks.Skip((chunkPage - 1) * chunkPageSize).Take(chunkPageSize).ToList();

        // Increment view count only on the first page load to avoid inflating count during chunk pagination
        if (incrementViewCount && chunkPage == 1)
        {
            document.ViewCount++;
            await _documentRepository.SaveChangesAsync(cancellationToken);
        }

        return new DocumentDetailsDto
        {
            Id = document.Id,
            OwnerUserId = document.OwnerUserId,
            Title = document.Title,



            SubjectId = document.SubjectId,
            SubjectName = document.Subject?.Name,
            SubjectCode = document.Subject?.Code,
            DocumentTypeId = document.DocumentTypeId,
            DocumentTypeName = document.DocumentType?.Name,
            AcademicTermName = document.AcademicTerm?.Name,
            AcademicTermId = document.AcademicTermId,
            DocumentSourceId = document.DocumentSourceId,
            DocumentSourceName = document.DocumentSource?.Name,
            Visibility = document.Visibility,
            LanguageId = document.LanguageId,
            LanguageCode = document.Language?.Code,
            LanguageName = document.Language?.Name,
            Description = document.Description,
            Status = document.Status,
            TotalChunks = document.TotalChunks,
            TotalChapters = document.TotalChapters,
            ViewCount = document.ViewCount,
            DownloadCount = document.DownloadCount,
            ApprovedAt = document.ApprovedAt,
            FileCount = document.DocumentFiles?.Count ?? 0,
            Files = (document.DocumentFiles ?? []).Select(f => new DocumentFileDto
            {
                Id = f.Id,
                DocumentId = f.DocumentId,
                OriginalFilename = f.OriginalFilename,
                StoragePath = f.StoragePath,
                S3Key = f.S3Key,
                FileUrl = f.FileUrl,
                MimeType = f.MimeType,
                FileSizeBytes = f.FileSizeBytes,
                PageCount = f.PageCount,
                ExtractionStatus = f.ExtractionStatus,
                CreatedAt = f.CreatedAt
            }).ToList(),
            Chapters = (document.DocumentChapters ?? []).OrderBy(x => x.ChapterOrder).Select(c => new DocumentChapterDto
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                ParentChapterId = c.ParentChapterId,
                Title = c.Title,
                Summary = c.Summary,
                ChapterOrder = c.ChapterOrder,
                StartPage = c.StartPage,
                EndPage = c.EndPage,
                StartChunkIndex = c.StartChunkIndex,
                EndChunkIndex = c.EndChunkIndex,
                IsAiGenerated = c.IsAiGenerated,
                ConfidenceScore = c.ConfidenceScore,
                CreatedAt = c.CreatedAt
            }).ToList(),
            Chunks = pageChunks.Select(ch => new DocumentChunkDto
            {
                Id = ch.Id,
                DocumentId = ch.DocumentId,
                ChapterId = ch.ChapterId,
                ChunkOrder = ch.ChunkOrder,
                PageNumber = ch.PageNumber,
                Content = ch.Content,
                ContentTokens = ch.ContentTokens,
                Metadata = ch.Metadata,
                ChunkHash = ch.ChunkHash,
                CreatedAt = ch.CreatedAt
            }).ToList()
        };
    }

    public async Task<DocumentDetailsDto?> GetDocumentDetailsBySlugAsync(string slug, Guid? requesterUserId = null, int chunkPage = 1, int chunkPageSize = 10, bool incrementViewCount = true, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetDocumentBySlugAsync(slug, requesterUserId, isAdmin, cancellationToken);
        if (document is null) return null;
        return await GetDocumentDetailsAsync(document.Id, chunkPage, chunkPageSize, incrementViewCount, cancellationToken);
    }

    public async Task<DocumentDetailsDto?> GetOwnedDocumentDetailsBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetOwnedDocumentBySlugAsync(slug, ownerUserId, cancellationToken);
        if (document is null) return null;
        return await GetDocumentDetailsAsync(document.Id, 1, 10, false, cancellationToken);
    }



    public async Task<MyDocumentsDto> GetMyDocumentsAsync(Guid ownerUserId, string? query, Guid? subjectId, Guid? termId, string? sortBy, Guid? documentTypeId, Guid? languageId, Guid? documentSourceId, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 6, 12);
        page = Math.Max(page, 1);
        var totalDocuments = await _documentRepository.CountDocumentsByOwnerAsync(ownerUserId, query, subjectId, termId, documentTypeId, languageId, documentSourceId, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalDocuments / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var documents = await _documentRepository.GetDocumentsByOwnerAsync(ownerUserId, query, subjectId, termId, sortBy, documentTypeId, languageId, documentSourceId, page, pageSize, cancellationToken);
        var activeUploadJobs = await _documentRepository.GetActiveUploadJobsByOwnerAsync(ownerUserId, cancellationToken);

        var documentIds = documents.Select(x => x.Id).ToList();
        var previewTexts = await _documentRepository.GetPreviewTextsAsync(documentIds, cancellationToken);

        return new MyDocumentsDto
        {
            Documents = documents.Select(x => new DocumentListItemDto
            {
                Id = x.Id,
                Slug = x.Slug ?? string.Empty,
                Title = x.Title,


                SubjectId = x.SubjectId,
                SubjectName = x.Subject?.Name,
                SubjectCode = x.Subject?.Code,
                DocumentTypeId = x.DocumentTypeId,
                DocumentTypeName = x.DocumentType?.Name,
                AcademicTermName = x.AcademicTerm?.Name,
                Status = x.Status,
                Visibility = x.Visibility,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FileCount = x.DocumentFiles.Count,
                ChunkCount = x.TotalChunks,
                PreviewText = !string.IsNullOrWhiteSpace(x.Description) ? x.Description : (previewTexts.TryGetValue(x.Id, out var content) ? content : string.Empty),
                ViewCount = x.ViewCount
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

    public async Task<MyDocumentsDto> GetAllDocumentsAsync(string? query, Guid? subjectId, int page = 1, int pageSize = 6, Guid? requesterUserId = null, string? sortBy = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 6, 12);
        page = Math.Max(page, 1);
        var totalDocuments = await _documentRepository.CountDocumentsAsync(query, subjectId, requesterUserId, documentTypeId, languageId, documentSourceId, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalDocuments / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var documents = await _documentRepository.GetDocumentsAsync(query, subjectId, page, pageSize, requesterUserId, sortBy, documentTypeId, languageId, documentSourceId, cancellationToken);

        var documentIds = documents.Select(x => x.Id).ToList();
        var previewTexts = await _documentRepository.GetPreviewTextsAsync(documentIds, cancellationToken);

        return new MyDocumentsDto
        {
            Documents = documents.Select(x => new DocumentListItemDto
            {
                Id = x.Id,
                Slug = x.Slug ?? string.Empty,
                Title = x.Title,


                SubjectId = x.SubjectId,
                SubjectName = x.Subject?.Name,
                SubjectCode = x.Subject?.Code,
                DocumentTypeId = x.DocumentTypeId,
                DocumentTypeName = x.DocumentType?.Name,
                AcademicTermName = x.AcademicTerm?.Name,
                Status = x.Status,
                Visibility = x.Visibility,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FileCount = x.DocumentFiles.Count,
                ChunkCount = x.TotalChunks,
                PreviewText = !string.IsNullOrWhiteSpace(x.Description) ? x.Description : (previewTexts.TryGetValue(x.Id, out var content) ? content : string.Empty),
                OwnerEmail = x.OwnerUser?.Email,
                ViewCount = x.ViewCount
            }).ToList(),
            TotalDocuments = totalDocuments,
            PendingDocuments = 0,
            ApprovedDocuments = 0,
            RejectedDocuments = 0,
            TotalFiles = documents.Sum(x => x.DocumentFiles.Count),
            TotalChunks = documents.Sum(x => x.TotalChunks),
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActiveUploadJobs = []
        };
    }



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
        await _documentRepository.RemoveDocumentChaptersByDocumentAsync(documentId, cancellationToken);
        await _documentRepository.RemoveDocumentReportsByDocumentAsync(documentId, cancellationToken);
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

    public async Task<List<DocumentChapterDto>> GenerateChaptersAsync(Guid documentId, CancellationToken cancellationToken = default)
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
        return chapters.Select(c => new DocumentChapterDto
        {
            Id = c.Id,
            DocumentId = c.DocumentId,
            ParentChapterId = c.ParentChapterId,
            Title = c.Title,
            Summary = c.Summary,
            ChapterOrder = c.ChapterOrder,
            StartPage = c.StartPage,
            EndPage = c.EndPage,
            StartChunkIndex = c.StartChunkIndex,
            EndChunkIndex = c.EndChunkIndex,
            IsAiGenerated = c.IsAiGenerated,
            ConfidenceScore = c.ConfidenceScore,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetDocumentsByOwnerAsync(ownerUserId, null, null, null, null, null, null, null, 1, 5, cancellationToken);
        var activeJobs = await _documentRepository.GetActiveUploadJobsByOwnerAsync(ownerUserId, cancellationToken);
        var completed = activeJobs.FirstOrDefault(x => x.Status == "done");

        return new DashboardSummaryDto
        {
            TotalDocuments = await _documentRepository.CountDocumentsByOwnerAsync(ownerUserId, null, null, null, null, null, null, cancellationToken),
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
                Subject = x.Subject?.Name,
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

    public async Task UpdateDocumentAsync(Guid documentId, Guid ownerUserId, DocumentEditInput input, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetOwnedDocumentAsync(documentId, ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Tài liệu không tồn tại hoặc bạn không có quyền chỉnh sửa.");

        document.Title = input.Title;
        document.Description = input.Description;

        document.SubjectId = input.SubjectId;
        document.DocumentTypeId = input.DocumentTypeId;
        document.AcademicTermId = input.AcademicTermId;
        document.LanguageId = input.LanguageId;
        document.Visibility = input.Visibility;
        document.DocumentSourceId = input.DocumentSourceId;
        document.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.SaveChangesAsync(cancellationToken);
    }

    public Task<List<UploadJobSummaryDto>> GetUploadJobsAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => GetActiveUploadJobsAsync(ownerUserId, cancellationToken);



    public async Task<List<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _documentRepository.GetSubjectsAsync(cancellationToken);
        return subjects.Select(s => new SubjectDto { Id = s.Id, Code = s.Code, Name = s.Name, AcademicTermId = s.AcademicTermId, CreatedAt = s.CreatedAt }).ToList();
    }

    public async Task<List<SubjectDto>> GetSubjectsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var dbContext = (_documentRepository as DAL.Repositories.Documents.DocumentRepository)?.GetDbContext();
        if (dbContext == null) return [];

        var subjects = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                dbContext.Set<DAL.Entities.Subject>()
                    .Where(s => dbContext.Set<DAL.Entities.Document>().Any(d => d.OwnerUserId == ownerUserId && d.SubjectId == s.Id))
            ),
            cancellationToken
        );

        return subjects.Select(s => new SubjectDto 
        { 
            Id = s.Id, 
            Code = s.Code, 
            Name = s.Name, 
            AcademicTermId = s.AcademicTermId,
            CreatedAt = s.CreatedAt 
        }).ToList();
    }

    public async Task<List<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _documentRepository.GetDocumentTypesAsync(cancellationToken);
        return types.Select(t => new DocumentTypeDto { Id = t.Id, Name = t.Name, Description = t.Description, CreatedAt = t.CreatedAt }).ToList();
    }

    public async Task<List<LanguageDto>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var langs = await _documentRepository.GetLanguagesAsync(cancellationToken);
        return langs.Select(l => new LanguageDto { Id = l.Id, Code = l.Code, Name = l.Name, CreatedAt = l.CreatedAt }).ToList();
    }

    public async Task<List<DocumentSourceDto>> GetDocumentSourcesAsync(CancellationToken cancellationToken = default)
    {
        var sources = await _documentRepository.GetDocumentSourcesAsync(cancellationToken);
        return sources.Select(s => new DocumentSourceDto { Id = s.Id, Name = s.Name, CreatedAt = s.CreatedAt }).ToList();
    }

    public async Task<List<AcademicTermDto>> GetAcademicTermsAsync(CancellationToken cancellationToken = default)
    {
        var terms = await _documentRepository.GetAcademicTermsAsync(cancellationToken);
        return terms.Select(t => new AcademicTermDto { Id = t.Id, Name = t.Name, Order = t.Order, CreatedAt = t.CreatedAt }).ToList();
    }

    public async Task<SubjectDto> CreateSubjectAsync(string code, string name, Guid? academicTermId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Mã môn học không được để trống.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên môn học không được để trống.");

        var normalizedCode = code.Trim().ToUpperInvariant();
        var subjects = await _documentRepository.GetSubjectsAsync(cancellationToken);
        if (subjects.Any(x => x.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Mã môn học đã tồn tại trong hệ thống.");
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = name.Trim(),
            AcademicTermId = academicTermId,
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepository.AddSubjectAsync(subject, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new SubjectDto { Id = subject.Id, Code = subject.Code, Name = subject.Name, AcademicTermId = subject.AcademicTermId, CreatedAt = subject.CreatedAt };
    }

    public async Task<SubjectDto?> UpdateSubjectAsync(Guid id, string code, string name, Guid? academicTermId = null, CancellationToken cancellationToken = default)
    {
        var subject = await _documentRepository.GetSubjectAsync(id, cancellationToken);
        if (subject == null) return null;

        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Mã môn học không được để trống.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên môn học không được để trống.");

        var normalizedCode = code.Trim().ToUpperInvariant();
        var trimmedName = name.Trim();
        var subjects = await _documentRepository.GetSubjectsAsync(cancellationToken);
        if (subjects.Any(x => x.Id != id && x.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Mã môn học đã tồn tại trong hệ thống.");
        }

        subject.Code = normalizedCode;
        subject.Name = trimmedName;
        subject.AcademicTermId = academicTermId;
        
        await _documentRepository.UpdateSubjectAsync(subject, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new SubjectDto { Id = subject.Id, Code = subject.Code, Name = subject.Name, AcademicTermId = subject.AcademicTermId, CreatedAt = subject.CreatedAt };
    }

    public async Task<bool> DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await _documentRepository.GetSubjectAsync(id, cancellationToken);
        if (subject == null) return false;

        await _documentRepository.DeleteSubjectAsync(subject, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DocumentTypeDto> CreateDocumentTypeAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên loại học liệu không được để trống.");

        var trimmedName = name.Trim();
        var docTypes = await _documentRepository.GetDocumentTypesAsync(cancellationToken);
        if (docTypes.Any(x => x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tên loại học liệu đã tồn tại trong hệ thống.");
        }

        var docType = new DocumentType
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Description = description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepository.AddDocumentTypeAsync(docType, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new DocumentTypeDto { Id = docType.Id, Name = docType.Name, Description = docType.Description, CreatedAt = docType.CreatedAt };
    }

    public async Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, string name, string? description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên loại học liệu không được để trống.");

        var trimmedName = name.Trim();
        var docTypes = await _documentRepository.GetDocumentTypesAsync(cancellationToken);
        if (docTypes.Any(x => x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase) && x.Id != id))
        {
            throw new InvalidOperationException("Tên loại học liệu đã tồn tại trong hệ thống.");
        }

        var docType = await _documentRepository.GetDocumentTypeAsync(id, cancellationToken);
        if (docType == null) return null;

        docType.Name = trimmedName;
        docType.Description = description?.Trim();
        
        await _documentRepository.UpdateDocumentTypeAsync(docType, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        
        return new DocumentTypeDto { Id = docType.Id, Name = docType.Name, Description = docType.Description, CreatedAt = docType.CreatedAt };
    }

    public async Task<bool> DeleteDocumentTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var docType = await _documentRepository.GetDocumentTypeAsync(id, cancellationToken);
        if (docType == null) return false;

        await _documentRepository.DeleteDocumentTypeAsync(docType, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }


    public async Task<LanguageDto> CreateLanguageAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Mã ngôn ngữ không được để trống.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên ngôn ngữ không được để trống.");

        var normalizedCode = code.Trim().ToLowerInvariant();
        var trimmedName = name.Trim();
        var languages = await _documentRepository.GetLanguagesAsync(cancellationToken);
        if (languages.Any(x => x.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Mã ngôn ngữ đã tồn tại trong hệ thống.");
        }
        if (languages.Any(x => x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tên ngôn ngữ đã tồn tại trong hệ thống.");
        }

        var language = new Language
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = trimmedName,
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepository.AddLanguageAsync(language, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new LanguageDto { Id = language.Id, Code = language.Code, Name = language.Name, CreatedAt = language.CreatedAt };
    }

    public async Task<LanguageDto?> UpdateLanguageAsync(Guid id, string code, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Mã ngôn ngữ không được để trống.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên ngôn ngữ không được để trống.");

        var normalizedCode = code.Trim().ToLowerInvariant();
        var trimmedName = name.Trim();
        var languages = await _documentRepository.GetLanguagesAsync(cancellationToken);
        if (languages.Any(x => x.Code.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase) && x.Id != id))
        {
            throw new InvalidOperationException("Mã ngôn ngữ đã tồn tại trong hệ thống.");
        }
        if (languages.Any(x => x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase) && x.Id != id))
        {
            throw new InvalidOperationException("Tên ngôn ngữ đã tồn tại trong hệ thống.");
        }

        var language = await _documentRepository.GetLanguageAsync(id, cancellationToken);
        if (language == null) return null;

        language.Code = normalizedCode;
        language.Name = trimmedName;

        await _documentRepository.UpdateLanguageAsync(language, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new LanguageDto { Id = language.Id, Code = language.Code, Name = language.Name, CreatedAt = language.CreatedAt };
    }

    public async Task<bool> DeleteLanguageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var language = await _documentRepository.GetLanguageAsync(id, cancellationToken);
        if (language == null) return false;

        await _documentRepository.DeleteLanguageAsync(language, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }


    public async Task<DocumentSourceDto> CreateDocumentSourceAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên nguồn tài liệu không được để trống.");

        var trimmedName = name.Trim();
        var sources = await _documentRepository.GetDocumentSourcesAsync(cancellationToken);
        if (sources.Any(x => x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tên nguồn tài liệu đã tồn tại trong hệ thống.");
        }

        var source = new DocumentSource
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepository.AddDocumentSourceAsync(source, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new DocumentSourceDto { Id = source.Id, Name = source.Name, CreatedAt = source.CreatedAt };
    }

    public async Task<DocumentSourceDto?> UpdateDocumentSourceAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên nguồn tài liệu không được để trống.");

        var trimmedName = name.Trim();
        var sources = await _documentRepository.GetDocumentSourcesAsync(cancellationToken);
        if (sources.Any(x => x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase) && x.Id != id))
        {
            throw new InvalidOperationException("Tên nguồn tài liệu đã tồn tại trong hệ thống.");
        }

        var source = await _documentRepository.GetDocumentSourceAsync(id, cancellationToken);
        if (source == null) return null;

        source.Name = trimmedName;

        await _documentRepository.UpdateDocumentSourceAsync(source, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new DocumentSourceDto { Id = source.Id, Name = source.Name, CreatedAt = source.CreatedAt };
    }

    public async Task<bool> DeleteDocumentSourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await _documentRepository.GetDocumentSourceAsync(id, cancellationToken);
        if (source == null) return false;

        await _documentRepository.DeleteDocumentSourceAsync(source, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }


    public async Task<AcademicTermDto> CreateAcademicTermAsync(string name, int order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên học kỳ không được để trống.");
        if (order < 0) throw new InvalidOperationException("Thứ tự học kỳ phải lớn hơn hoặc bằng 0.");

        var trimmedName = name.Trim();
        var terms = await _documentRepository.GetAcademicTermsAsync(cancellationToken);
        if (terms.Any(x => x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tên học kỳ đã tồn tại trong hệ thống.");
        }

        var term = new AcademicTerm
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Order = order,
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepository.AddAcademicTermAsync(term, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new AcademicTermDto { Id = term.Id, Name = term.Name, Order = term.Order, CreatedAt = term.CreatedAt };
    }

    public async Task<AcademicTermDto?> UpdateAcademicTermAsync(Guid id, string name, int order, CancellationToken cancellationToken = default)
    {
        var term = await _documentRepository.GetAcademicTermAsync(id, cancellationToken);
        if (term == null) return null;

        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên học kỳ không được để trống.");
        if (order < 0) throw new InvalidOperationException("Thứ tự học kỳ phải lớn hơn hoặc bằng 0.");

        var trimmedName = name.Trim();
        var terms = await _documentRepository.GetAcademicTermsAsync(cancellationToken);
        if (terms.Any(x => x.Id != id && x.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tên học kỳ đã tồn tại trong hệ thống.");
        }

        term.Name = trimmedName;
        term.Order = order;

        await _documentRepository.UpdateAcademicTermAsync(term, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new AcademicTermDto { Id = term.Id, Name = term.Name, Order = term.Order, CreatedAt = term.CreatedAt };
    }

    public async Task<bool> DeleteAcademicTermAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var term = await _documentRepository.GetAcademicTermAsync(id, cancellationToken);
        if (term == null) return false;

        await _documentRepository.DeleteAcademicTermAsync(term, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string BuildSearchText(Document document, string extractedText)
    {
        var safeExtractedText = string.IsNullOrWhiteSpace(extractedText) ? string.Empty : (extractedText.Length > 50000 ? extractedText.Substring(0, 50000) : extractedText);
        return string.Join(" ", new[] { document.Title, document.Description, document.Subject?.Name, document.Subject?.Code, document.DocumentType?.Name, document.Language?.Name, document.AcademicTerm?.Name, safeExtractedText }.Where(x => !string.IsNullOrWhiteSpace(x)));
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

    public async Task<DocumentReportDto> ReportDocumentAsync(Guid documentId, Guid reporterUserId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Lý do báo cáo không được để trống.");

        var document = await _documentRepository.GetDocumentAsync(documentId, cancellationToken);
        if (document is null) throw new InvalidOperationException("Tài liệu không tồn tại.");

        var report = new DocumentReport
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ReporterUserId = reporterUserId,
            Reason = reason.Trim(),
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepository.AddDocumentReportAsync(report, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
        return new DocumentReportDto
        {
            Id = report.Id,
            DocumentId = report.DocumentId,
            ReporterUserId = report.ReporterUserId,
            Reason = report.Reason,
            Status = report.Status,
            CreatedAt = report.CreatedAt,
            DocumentTitle = document.Title,
            DocumentSlug = document.Slug
        };
    }

    public async Task<List<DocumentReportDto>> GetPendingReportsAsync(CancellationToken cancellationToken = default)
    {
        var reports = await _documentRepository.GetPendingReportsAsync(cancellationToken);
        return reports.Select(r => new DocumentReportDto
        {
            Id = r.Id,
            DocumentId = r.DocumentId,
            ReporterUserId = r.ReporterUserId,
            Reason = r.Reason,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            DocumentTitle = r.Document?.Title,
            DocumentSlug = r.Document?.Slug,
            ReporterEmail = r.ReporterUser?.Email
        }).ToList();
    }

    public async Task ResolveReportAsync(Guid reportId, string action, CancellationToken cancellationToken = default)
    {
        var report = await _documentRepository.GetDocumentReportAsync(reportId, cancellationToken)
            ?? throw new InvalidOperationException("Báo cáo không tồn tại.");

        if (action.Equals("delete", StringComparison.OrdinalIgnoreCase))
        {
            await DeleteDocumentAsync(report.DocumentId, cancellationToken);
        }
        else
        {
            var reports = await _documentRepository.GetDocumentReportsByDocumentAsync(report.DocumentId, cancellationToken);
            foreach (var r in reports)
            {
                r.Status = "resolved";
            }
            await _documentRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MyDocumentsDto> GetAdminDocumentsAsync(string? query, Guid? subjectId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 5, 100);
        page = Math.Max(page, 1);
        
        var totalDocuments = await _documentRepository.CountAdminDocumentsAsync(query, subjectId, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalDocuments / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var documents = await _documentRepository.GetAdminDocumentsAsync(query, subjectId, page, pageSize, cancellationToken);

        return new MyDocumentsDto
        {
            Documents = documents.Select(x => new DocumentListItemDto
            {
                Id = x.Id,
                Slug = x.Slug ?? string.Empty,
                Title = x.Title,


                SubjectId = x.SubjectId,
                SubjectName = x.Subject?.Name,
                SubjectCode = x.Subject?.Code,
                DocumentTypeId = x.DocumentTypeId,
                DocumentTypeName = x.DocumentType?.Name,
                AcademicTermName = x.AcademicTerm?.Name,
                Status = x.Status,
                Visibility = x.Visibility,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FileCount = x.DocumentFiles.Count,
                ChunkCount = x.TotalChunks,
                PreviewText = x.Description ?? string.Empty,
                OwnerEmail = x.OwnerUser?.Email,
                ViewCount = x.ViewCount
            }).ToList(),
            TotalDocuments = totalDocuments,
            PendingDocuments = 0,
            ApprovedDocuments = 0,
            RejectedDocuments = 0,
            TotalFiles = documents.Sum(x => x.DocumentFiles.Count),
            TotalChunks = documents.Sum(x => x.TotalChunks),
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActiveUploadJobs = []
        };
    }

    public async Task SeedInitialDataAsync(CancellationToken cancellationToken = default)
    {
        var dbContext = (_documentRepository as DAL.Repositories.Documents.DocumentRepository)?.GetDbContext();
        if (dbContext == null) return;

        string ddlSql = @"
            CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";

            ALTER TABLE public.users ADD COLUMN IF NOT EXISTS is_blocked boolean DEFAULT false NOT NULL;

            CREATE TABLE IF NOT EXISTS public.document_sources (
                id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                name VARCHAR(200) NOT NULL UNIQUE,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT now() NOT NULL
            );

            CREATE TABLE IF NOT EXISTS public.academic_terms (
                id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                name VARCHAR(200) NOT NULL UNIQUE,
                term_order INT NOT NULL DEFAULT 0,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT now() NOT NULL
            );

            INSERT INTO public.document_sources (name) VALUES 
            ('Tự soạn'),
            ('Giáo trình'),
            ('Sưu tầm'),
            ('Tham khảo'),
            ('Đề thi gốc')
            ON CONFLICT (name) DO NOTHING;

            INSERT INTO public.academic_terms (name, term_order) VALUES 
            ('Tiếng Anh Chuẩn Bị', 0),
            ('Kỳ 1', 1),
            ('Kỳ 2', 2),
            ('Kỳ 3', 3),
            ('Kỳ 4', 4),
            ('Kỳ 5', 5),
            ('Kỳ 6', 6),
            ('Kỳ 7', 7),
            ('Kỳ 8', 8),
            ('Kỳ 9', 9)
            ON CONFLICT (name) DO NOTHING;
        ";
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(dbContext.Database, ddlSql, cancellationToken);
    }
}
