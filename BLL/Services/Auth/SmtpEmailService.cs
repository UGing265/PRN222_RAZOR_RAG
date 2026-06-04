using BLL.Interfaces.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Services.Auth;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtpServer = _configuration["SmtpSettings:Server"];
        var senderEmail = _configuration["SmtpSettings:SenderEmail"];
        var senderPassword = _configuration["SmtpSettings:Password"];
        var senderName = _configuration["SmtpSettings:SenderName"] ?? "FPT RAG System";
        
        var portStr = _configuration["SmtpSettings:Port"];
        int port = int.TryParse(portStr, out int p) ? p : 587;

        // Nếu chưa cấu hình SMTP thì ghi ra Console để Developer dễ dàng copy link test
        if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(senderEmail))
        {
            _logger.LogWarning("SMTP Settings chưa được cấu hình. Đang chạy ở chế độ giả lập (Log Console).");
            
            Console.WriteLine("\n==========================================================================================");
            Console.WriteLine($"[EMAIL GỬI TỚI]: {toEmail}");
            Console.WriteLine($"[TIÊU ĐỀ]: {subject}");
            Console.WriteLine($"[NỘI DUNG]:\n{body}");
            Console.WriteLine("==========================================================================================\n");
            
            await Task.CompletedTask;
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Đã gửi email xác thực thành công tới {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi gửi email tới {ToEmail} qua SMTP.", toEmail);
            
            // Fallback in ra console để tránh làm đứt mạch kiểm thử của user
            Console.WriteLine("\n[FALLBACK CONSOLE LOG do lỗi SMTP]:");
            Console.WriteLine($"[EMAIL GỬI TỚI]: {toEmail}");
            Console.WriteLine($"[NỘI DUNG]:\n{body}\n");
            
            throw;
        }
    }
}
