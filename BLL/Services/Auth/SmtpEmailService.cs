using BLL.Interfaces.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, port, SecureSocketOptions.Auto, cancellationToken);
            
            if (!string.IsNullOrEmpty(senderPassword))
            {
                await client.AuthenticateAsync(senderEmail, senderPassword, cancellationToken);
            }
            
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Đã gửi email xác thực thành công tới {ToEmail} qua MailKit", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi gửi email tới {ToEmail} qua SMTP (MailKit).", toEmail);
            
            // Fallback in ra console để tránh làm đứt mạch kiểm thử của user
            Console.WriteLine("\n[FALLBACK CONSOLE LOG do lỗi SMTP (MailKit)]:");
            Console.WriteLine($"[EMAIL GỬI TỚI]: {toEmail}");
            Console.WriteLine($"[NỘI DUNG]:\n{body}\n");
            
            throw;
        }
    }
}
