using System.Threading.Tasks;

namespace BLL.Interfaces.Auth;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
