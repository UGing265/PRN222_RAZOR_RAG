using BLL.Interfaces.Auth;
using BLL.Interfaces.Documents;
using BLL.Services.Auth;
using BLL.Services.Documents;
using Amazon;
using Amazon.S3;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace BLL.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DBContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                o => o.UseVector()));

        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IS3StorageService, S3StorageService>();
        services.AddScoped<IChapterSegmentationService, GeminiChapterSegmentationService>();
        services.AddScoped<IUploadProcessingService, UploadProcessingService>();
        services.AddScoped<IFileParserService, SimpleFileParserService>();
        services.AddScoped<IEmbeddingService, GeminiEmbeddingService>();
        services.AddScoped<IChapterSegmentationService, GeminiChapterSegmentationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddHostedService<UploadJobBackgroundService>();

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var region = configuration["AwsS3:Region"] ?? "ap-southeast-1";
            var accessKey = configuration["AwsS3:AccessKey"];
            var secretKey = configuration["AwsS3:SecretKey"];
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            };

            if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                return new AmazonS3Client(accessKey, secretKey, config);
            }

            return new AmazonS3Client(config);
        });

        return services;
    }
}
