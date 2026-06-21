using BLL.DTOs.Auth;

namespace BLL.Interfaces.Auth;

public interface IAuthService
{
    Task SubmitAccountRequestAsync(string fullName, string email, short roleId, CancellationToken cancellationToken = default);
    Task RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default);
    Task<List<AccountRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    Task ApproveAccountRequestAsync(Guid requestId, string verificationUrlFormat, CancellationToken cancellationToken = default);
    Task RejectAccountRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> VerifyAccountRequestAndSetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);

    Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthUserDto> LoginOrRegisterExternalAsync(string email, string fullName, CancellationToken cancellationToken = default);
    string GenerateEmailVerificationToken(string email);
    Task<bool> VerifyEmailTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<AuthUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> ApproveUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RejectOrBlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
