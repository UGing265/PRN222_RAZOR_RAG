using BLL.DTOs.Auth;

namespace BLL.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthUserDto> RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default);

    Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthUserDto> LoginOrRegisterExternalAsync(string email, string fullName, CancellationToken cancellationToken = default);
    string GenerateEmailVerificationToken(string email);
    Task<bool> VerifyEmailTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<AuthUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<bool> ApproveUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RejectOrBlockUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UnblockUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default);
    Task<AuditLogListDto> GetAuditLogsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<(int SuccessCount, List<string> Errors)> BulkRegisterFromExcelAsync(Stream excelStream, short roleId, CancellationToken cancellationToken = default);
}
