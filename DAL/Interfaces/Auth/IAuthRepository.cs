using DAL.Entities;

namespace DAL.Interfaces.Auth;

public interface IAuthRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(short roleId, CancellationToken cancellationToken = default);
    Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailWithRoleAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
