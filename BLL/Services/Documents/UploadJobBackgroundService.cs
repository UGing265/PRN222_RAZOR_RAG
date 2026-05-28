using BLL.Interfaces.Documents;
using DAL.Interfaces.Documents;
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
                var db = scope.ServiceProvider.GetRequiredService<IUploadJobRepository>();
                var processor = scope.ServiceProvider.GetRequiredService<IUploadProcessingService>();

                var job = await db.GetNextPendingJobAsync(stoppingToken);
                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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
                job.ProgressPercent = 1;
                job.Message = "Đưa vào hàng đợi xử lý";
                job.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Processing upload job {JobId} for document {DocumentId}", job.Id, job.DocumentId);
                try 
                {
                    await processor.ProcessAsync(job, stoppingToken);
                    job.Status = "done";
                    job.ProgressPercent = 100;
                    job.Message = "Hoàn tất";
                }
                catch (Exception ex)
                {
                    job.Status = "failed";
                    job.Message = "Lỗi xử lý: " + (ex.Message.Length > 200 ? ex.Message.Substring(0, 200) : ex.Message);
                    throw;
                }
                finally 
                {
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                }

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
