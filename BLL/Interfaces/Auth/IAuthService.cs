using DAL.Entities;

namespace BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<User> RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default);
    Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
}
