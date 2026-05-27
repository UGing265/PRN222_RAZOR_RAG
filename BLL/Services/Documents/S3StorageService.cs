using Amazon.S3;
using Amazon.S3.Model;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Documents;

public class S3StorageService : IS3StorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IAmazonS3 s3, IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _s3 = s3;
        _logger = logger;
        _bucketName = configuration["AwsS3:BucketName"] ?? throw new InvalidOperationException("Thiếu cấu hình AwsS3:BucketName");
    }

    public async Task<(string Key, string Url)> UploadAsync(string documentId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"documents/{documentId}/{Guid.NewGuid():N}{extension}";

        await using var stream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation("Uploaded file to S3. Bucket={Bucket}, Key={Key}", _bucketName, key);
        return (key, $"s3://{_bucketName}/{key}");
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await _s3.GetObjectAsync(_bucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        }, cancellationToken);
    }
}
