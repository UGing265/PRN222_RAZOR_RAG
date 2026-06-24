using DAL.Entities;

namespace DAL.Interfaces.Auth;

public interface IAuthRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(short roleId, CancellationToken cancellationToken = default);
    Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailWithRoleAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(User user, CancellationToken cancellationToken = default);
    Task AddAuditLogAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<(List<AuditLog> Items, int TotalCount, Dictionary<Guid, string> TargetNames)> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
