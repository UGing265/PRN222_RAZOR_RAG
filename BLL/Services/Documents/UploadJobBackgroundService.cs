using BLL.Interfaces.Documents;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Documents;

public class UploadJobBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UploadJobBackgroundService> _logger;

    public UploadJobBackgroundService(IServiceScopeFactory scopeFactory, ILogger<UploadJobBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DBContext>();
                var processor = scope.ServiceProvider.GetRequiredService<IUploadProcessingService>();

                var job = await db.UploadJobs
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync(x => x.Status == "pending", stoppingToken);

                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                if (job.DocumentId is null)
                {
                    job.Status = "failed";
                    job.ProgressPercent = 0;
                    job.Message = "Thiếu document id";
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    continue;
                }

                job.Status = "processing";
                job.ProgressPercent = 5;
                job.Message = "Đang chờ đọc file";
                job.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                job.ProgressPercent = 20;
                job.Message = "Đang đọc nội dung";
                job.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                job.ProgressPercent = 55;
                job.Message = "Đang phân tích và chia đoạn";
                job.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                job.ProgressPercent = 85;
                job.Message = "Đang hoàn tất chỉ mục";
                job.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Processing upload job {JobId} for document {DocumentId}", job.Id, job.DocumentId);
                await processor.ProcessAsync(job, stoppingToken);
                job.Status = "done";
                job.ProgressPercent = 100;
                job.Message = "Hoàn tất";
                job.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Completed upload job {JobId}", job.Id);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background upload worker failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
