using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using DAL.Entities;
using DAL.Interfaces.Auth;
using System.Security.Cryptography;

namespace BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;

    public AuthService(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task<AuthUserDto> RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _authRepository.EmailExistsAsync(normalizedEmail, cancellationToken);
        if (existingUser)
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        var roleExists = await _authRepository.RoleExistsAsync(roleId, cancellationToken);
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

        var created = await _authRepository.AddUserAsync(user, cancellationToken);
        return Map(created);
    }

    public async Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _authRepository.GetUserByEmailWithRoleAsync(normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        return VerifyPassword(password, user.PasswordHash) ? Map(user) : null;
    }

    private static AuthUserDto Map(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        RoleName = user.Role?.Name ?? user.RoleId.ToString()
    };

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
