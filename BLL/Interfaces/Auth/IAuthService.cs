using BLL.DTOs.Auth;

namespace BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthUserDto> RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default);

    Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<List<AuthUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> ApproveUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RejectOrBlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
