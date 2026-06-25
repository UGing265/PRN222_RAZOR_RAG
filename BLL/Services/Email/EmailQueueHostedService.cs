using BLL.Interfaces.Auth;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BLL.Services.Email;

public sealed class EmailQueueHostedService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IEmailService _emailService;
    private readonly EmailQueueOptions _options;
    private readonly ILogger<EmailQueueHostedService> _logger;

    public EmailQueueHostedService(
        IEmailQueue queue,
        IEmailService emailService,
        IOptions<EmailQueueOptions> options,
        ILogger<EmailQueueHostedService> logger)
    {
        _queue = (EmailQueue)queue;
        _emailService = emailService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueHostedService started. Throttle={Ms}ms, MaxRetries={N}",
            _options.ThrottleDelayMs, _options.MaxRetries);

        try
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessJobAsync(job, stoppingToken);
                if (_options.ThrottleDelayMs > 0)
                    await Task.Delay(_options.ThrottleDelayMs, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProcessJobAsync(EmailJob job, CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                await _emailService.SendEmailAsync(job.To, job.Subject, job.Body, ct);
                _logger.LogInformation("Email sent to {To}: {Subject}", job.To, job.Subject);
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                attempt++;
                if (attempt > _options.MaxRetries)
                {
                    _logger.LogError(ex,
                        "Email send to {To} failed after {Attempts} attempts. Subject: {Subject}",
                        job.To, attempt, job.Subject);
                    return;
                }
                var backoffMs = (int)Math.Pow(2, attempt - 1) * 1000;
                _logger.LogWarning(ex,
                    "Email send to {To} failed (attempt {Attempt}/{Max}). Retrying in {Backoff}ms.",
                    job.To, attempt, _options.MaxRetries, backoffMs);
                await Task.Delay(backoffMs, ct);
            }
        }
    }
}
