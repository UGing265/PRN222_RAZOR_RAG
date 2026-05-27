using BLL.Interfaces.Auth;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly DBContext _dbContext;

    public AuthService(DBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _dbContext.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
        if (existingUser)
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        var roleExists = await _dbContext.Roles.AnyAsync(x => x.Id == roleId, cancellationToken);
        if (!roleExists)
        {
            throw new InvalidOperationException("Role không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(password),
            RoleId = roleId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
