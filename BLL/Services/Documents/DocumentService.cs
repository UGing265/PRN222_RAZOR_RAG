using BLL.Interfaces.Documents;
using DAL.Data;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using System.Diagnostics;
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

    private readonly DBContext _dbContext;
    private readonly ILogger<DocumentService> _logger;
    private readonly IFileParserService _fileParserService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChapterSegmentationService _chapterSegmentationService;
    private readonly IS3StorageService _s3StorageService;
    private readonly DocumentIndexingOptions _indexingOptions;

    public DocumentService(DBContext dbContext, IConfiguration configuration, ILogger<DocumentService> logger, IFileParserService fileParserService, IEmbeddingService embeddingService, IChapterSegmentationService chapterSegmentationService, IS3StorageService s3StorageService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _fileParserService = fileParserService;
        _embeddingService = embeddingService;
        _chapterSegmentationService = chapterSegmentationService;
        _s3StorageService = s3StorageService;
        _indexingOptions = configuration.GetSection("DocumentIndexing").Get<DocumentIndexingOptions>() ?? new DocumentIndexingOptions();
    }

    public async Task<Document> CreateDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return document;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Không thể tạo tài liệu. Vui lòng kiểm tra lại dữ liệu.", ex);
        }
    }

    public async Task<(string Key, string Url)> UploadOriginalFileToS3Async(Guid documentId, IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);
        return await _s3StorageService.UploadAsync(documentId.ToString("N"), file, cancellationToken);
    }

    public async Task<DocumentFile> AddDocumentFileAsync(Guid documentId, string s3Key, string s3Url, IFormFile file, Func<int, Task>? onProgress = null, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var document = await _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document not found.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
            await using (var s3Stream = await _s3StorageService.OpenReadAsync(s3Key, cancellationToken))
            await using (var tempFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await s3Stream.CopyToAsync(tempFile, cancellationToken);
            }

            await using var checksumStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var checksum = await ComputeSha256Async(checksumStream, cancellationToken);
            checksumStream.Close();

            var sw = Stopwatch.StartNew();
            var extractedText = SanitizeForPostgres(await _fileParserService.ExtractTextAsync(tempPath, extension, cancellationToken));
            File.Delete(tempPath);
            var chunks = DocumentChunker.ChunkText(extractedText, 1, _indexingOptions.ChunkMaxWords, _indexingOptions.ChunkOverlapWords).ToList();
            var chunkEntities = new List<DocumentChunk>();
            var totalChunks = chunks.Count;
            var chunkIndex = 0;
            var batchIndex = 0;

            foreach (var batch in chunks.Chunk(_indexingOptions.BatchSize))
            {
                batchIndex++;
                
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

                if (onProgress != null && totalChunks > 0)
                {
                    int progress = 20 + (int)((chunkIndex / (double)totalChunks) * 70);
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

            _dbContext.DocumentFiles.Add(documentFile);
            if (chunkEntities.Count > 0)
            {
                _dbContext.DocumentChunks.AddRange(chunkEntities);
            }

            document.PageCount = documentFile.PageCount ?? document.PageCount;
            document.TotalChunks = chunkEntities.Count;
            document.SearchText = BuildSearchText(document, extractedText);
            document.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
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
    }

    public async Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Documents
                .Include(x => x.DocumentFiles)
                .Include(x => x.DocumentChunks)
                .Include(x => x.DocumentChapters)
                .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Không thể tải tài liệu.", ex);
        }
    }

    public async Task DeleteDocumentAssetsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var files = await _dbContext.DocumentFiles.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);
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
        if (string.IsNullOrWhiteSpace(s3Key))
        {
            throw new InvalidOperationException("S3 key is missing.");
        }

        return await _s3StorageService.OpenReadAsync(s3Key, cancellationToken);
    }

    public async Task<List<DocumentChapter>> GenerateChaptersAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .Include(x => x.DocumentChunks)
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new InvalidOperationException("Document not found.");

        var chunks = document.DocumentChunks?.OrderBy(x => x.ChunkOrder).ToList() ?? [];
        if (chunks.Count == 0)
        {
            return [];
        }

        var chapters = await _chapterSegmentationService.GenerateChaptersAsync(document, chunks, cancellationToken);
        if (chapters.Count == 0)
        {
            return [];
        }

        _dbContext.DocumentChapters.AddRange(chapters);

        foreach (var chapter in chapters)
        {
            var matchedChunks = chunks.Where(x => x.ChunkOrder >= chapter.StartChunkIndex && x.ChunkOrder <= chapter.EndChunkIndex).ToList();
            foreach (var chunk in matchedChunks)
            {
                chunk.ChapterId = chapter.Id;
            }
        }

        document.TotalChapters = chapters.Count;
        document.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return chapters;
    }

    private static string BuildSearchText(Document document, string extractedText)
    {
        return string.Join(" ", new[]
        {
            document.Title,
            document.Description,
            document.Subject,
            document.School,
            document.Department,
            extractedText
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file is null)
        {
            throw new InvalidOperationException("Vui lòng chọn file upload.");
        }

        if (file.Length <= 0)
        {
            throw new InvalidOperationException("File upload đang rỗng.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Chỉ hỗ trợ PDF, DOC, DOCX, PPT, PPTX.");
        }

        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Định dạng MIME của file không hợp lệ.");
        }
    }

    private static string SanitizeForPostgres(string value)
    {
        return value.Replace('\0', ' ');
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
