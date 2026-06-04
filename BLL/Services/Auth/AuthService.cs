using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using DAL.Entities;
using DAL.Interfaces.Auth;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITimeLimitedDataProtector _protector;

    public AuthService(IAuthRepository authRepository, IDataProtectionProvider dataProtectionProvider)
    {
        _authRepository = authRepository;
        _protector = dataProtectionProvider.CreateProtector("FptStudentEmailVerification").ToTimeLimitedDataProtector();
    }

    public async Task<AuthUserDto> RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (roleId == 2 && !normalizedEmail.EndsWith("@fe.edu.vn"))
        {
            throw new InvalidOperationException("Giảng viên bắt buộc sử dụng email đuôi @fe.edu.vn.");
        }
        if (roleId == 3 && !normalizedEmail.EndsWith("@fpt.edu.vn"))
        {
            throw new InvalidOperationException("Sinh viên bắt buộc sử dụng email đuôi @fpt.edu.vn.");
        }

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
            IsActive = false,
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

        if (user is null)
        {
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        if (user.IsBlocked)
        {
            throw new InvalidOperationException("Tài khoản của bạn đã bị khóa.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Tài khoản của bạn chưa được kích hoạt hoặc đang chờ Admin phê duyệt.");
        }

        return Map(user);
    }

    public async Task<AuthUserDto> LoginOrRegisterExternalAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        short roleId;
        if (normalizedEmail.EndsWith("@fe.edu.vn"))
        {
            roleId = 2; // Lecturer
        }
        else if (normalizedEmail.EndsWith("@fpt.edu.vn"))
        {
            roleId = 3; // Student
        }
        else
        {
            throw new InvalidOperationException("Chỉ chấp nhận email giảng viên (@fe.edu.vn) hoặc sinh viên FPT (@fpt.edu.vn).");
        }

        var user = await _authRepository.GetUserByEmailWithRoleAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            var now = DateTime.UtcNow;
            user = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = "EXTERNAL_OAUTH_GOOGLE",
                RoleId = roleId,
                IsActive = true, // Tự kích hoạt vì đã đăng nhập bằng mail trường qua Google
                IsBlocked = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            var created = await _authRepository.AddUserAsync(user, cancellationToken);
            user = await _authRepository.GetUserByIdAsync(created.Id, cancellationToken);
        }
        else
        {
            if (user.IsBlocked)
            {
                throw new InvalidOperationException("Tài khoản của bạn đã bị khóa.");
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user, cancellationToken);
            }
        }

        return Map(user!);
    }

    public string GenerateEmailVerificationToken(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _protector.Protect(normalizedEmail, DateTimeOffset.UtcNow.AddMinutes(15));
    }

    public async Task<bool> VerifyEmailTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = _protector.Unprotect(token);
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _authRepository.GetUserByEmailWithRoleAsync(normalizedEmail, cancellationToken);
            if (user is null)
            {
                return false;
            }

            if (user.IsBlocked)
            {
                throw new InvalidOperationException("Tài khoản của bạn đã bị khóa.");
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user, cancellationToken);
            }

            return true;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }

    public async Task<List<AuthUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _authRepository.GetAllUsersWithRolesAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<bool> ApproveUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsActive = true;
        user.IsBlocked = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _authRepository.UpdateUserAsync(user, cancellationToken);
        return true;
    }

    public async Task<bool> RejectOrBlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (!user.IsActive && !user.IsBlocked)
        {
            await _authRepository.DeleteUserAsync(user, cancellationToken);
        }
        else
        {
            user.IsActive = false;
            user.IsBlocked = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _authRepository.UpdateUserAsync(user, cancellationToken);
        }
        return true;
    }

    public async Task<bool> UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsBlocked = false;
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _authRepository.UpdateUserAsync(user, cancellationToken);
        return true;
    }

    private static AuthUserDto Map(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        RoleId = user.RoleId,
        RoleName = user.Role?.Name ?? user.RoleId.ToString(),
        IsActive = user.IsActive,
        IsBlocked = user.IsBlocked
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
