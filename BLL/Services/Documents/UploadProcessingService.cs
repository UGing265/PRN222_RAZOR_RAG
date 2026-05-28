using BLL.Interfaces.Documents;
using DAL.Data;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Documents;

public class UploadProcessingService : IUploadProcessingService
{
    private readonly DBContext _dbContext;
    private readonly IDocumentService _documentService;
    private readonly ILogger<UploadProcessingService> _logger;

    public UploadProcessingService(DBContext dbContext, IDocumentService documentService, ILogger<UploadProcessingService> logger)
    {
        _dbContext = dbContext;
        _documentService = documentService;
        _logger = logger;
    }

    public async Task ProcessAsync(UploadJob job, CancellationToken cancellationToken = default)
    {
        if (job.DocumentId is null)
        {
            throw new InvalidOperationException("Job thiếu document id.");
        }

        if (string.IsNullOrWhiteSpace(job.StoragePath))
        {
            throw new InvalidOperationException("Job chưa có S3 key.");
        }

        job.Status = "processing";
        job.ProgressPercent = 5;
        job.Message = "Đang tải tệp từ lưu trữ S3";
        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var fileName = job.FileName;
        var contentType = ResolveContentType(fileName);
        var tempFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}");

        await using (var s3Stream = await _documentService.DownloadOriginalFileFromS3Async(job.DocumentId.Value, job.StoragePath, cancellationToken))
        await using (var tempWrite = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await s3Stream.CopyToAsync(tempWrite, cancellationToken);
        }

        job.ProgressPercent = 15;
        job.Message = "Đang phân tích nội dung văn bản";
        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tempInfo = new FileInfo(tempFileName);
        var formFile = new TempFileFormFile(tempFileName, fileName, tempInfo.Length, contentType);
        var s3Key = job.StoragePath;
        var s3Url = $"s3://{s3Key}";
        await _documentService.AddDocumentFileAsync(job.DocumentId.Value, s3Key, s3Url, formFile, async (percent) => 
        {
            job.ProgressPercent = percent;
            job.Message = $"Đang tạo chỉ mục và lưu Vector ({percent}%)";
            job.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        job.ProgressPercent = 95;
        job.Message = "Đang phân bổ chương tự động";
        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _documentService.GenerateChaptersAsync(job.DocumentId.Value, cancellationToken);
        File.Delete(tempFileName);

        job.Status = "done";
        job.ProgressPercent = 100;
        job.Message = "Hoàn tất";
        job.UpdatedAt = DateTime.UtcNow;
        job.IsNotified = false;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ResolveContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }

    private sealed class TempFileFormFile : IFormFile
    {
        private readonly string _filePath;

        public TempFileFormFile(string filePath, string fileName, long length, string contentType)
        {
            _filePath = filePath;
            FileName = fileName;
            Length = length;
            ContentTypeValue = contentType;
        }

        public string ContentType => ContentTypeValue;
        public string ContentDisposition => string.Empty;
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length { get; }
        public string Name => "file";
        public string FileName { get; }
        private string ContentTypeValue { get; }

        public void CopyTo(Stream target)
        {
            using var source = File.OpenRead(_filePath);
            source.CopyTo(target);
        }

        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            await using var source = File.OpenRead(_filePath);
            await source.CopyToAsync(target, cancellationToken);
        }

        public Stream OpenReadStream() => File.OpenRead(_filePath);
    }
}
