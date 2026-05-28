using BLL.DTOs.Auth;

namespace BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthUserDto> RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default);

    Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
}
