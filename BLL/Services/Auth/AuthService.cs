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
    private readonly IEmailService _emailService;

    public AuthService(IAuthRepository authRepository, IDataProtectionProvider dataProtectionProvider, IEmailService emailService)
    {
        _authRepository = authRepository;
        _protector = dataProtectionProvider.CreateProtector("FptStudentEmailVerification").ToTimeLimitedDataProtector();
        _emailService = emailService;
    }

    public async Task SubmitAccountRequestAsync(string fullName, string email, short roleId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // 1. Kiểm tra xem email đã là user chưa
        var existingUser = await _authRepository.EmailExistsAsync(normalizedEmail, cancellationToken);
        if (existingUser)
        {
            throw new InvalidOperationException("Email này đã có tài khoản.");
        }

        // 2. Kiểm tra xem email đã từng gửi yêu cầu chưa
        var existingRequest = await _authRepository.AccountRequestEmailExistsAsync(normalizedEmail, cancellationToken);
        if (existingRequest)
        {
            throw new InvalidOperationException("Email này đã được gửi yêu cầu. Vui lòng chờ Admin phê duyệt.");
        }

        var roleExists = await _authRepository.RoleExistsAsync(roleId, cancellationToken);
        if (!roleExists)
        {
            throw new InvalidOperationException("Vai trò không hợp lệ.");
        }

        var request = new AccountRequest
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            RoleId = roleId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _authRepository.AddAccountRequestAsync(request, cancellationToken);
    }

    public async Task RegisterAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var existingUser = await _authRepository.EmailExistsAsync(normalizedEmail, cancellationToken);
        if (existingUser)
        {
            throw new InvalidOperationException("Email này đã có tài khoản.");
        }

        var roleExists = await _authRepository.RoleExistsAsync(roleId, cancellationToken);
        if (!roleExists)
        {
            throw new InvalidOperationException("Vai trò không hợp lệ.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(password),
            RoleId = roleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _authRepository.AddUserAsync(user, cancellationToken);
    }

    public async Task<List<AccountRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _authRepository.GetPendingAccountRequestsAsync(cancellationToken);
        return requests.Select(r => new AccountRequestDto
        {
            Id = r.Id,
            FullName = r.FullName,
            Email = r.Email,
            RoleId = r.RoleId,
            RoleName = r.Role?.Name ?? r.RoleId.ToString(),
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task ApproveAccountRequestAsync(Guid requestId, string verificationUrlFormat, CancellationToken cancellationToken = default)
    {
        var request = await _authRepository.GetAccountRequestByIdAsync(requestId, cancellationToken);
        if (request is null || request.Status != "pending")
        {
            throw new InvalidOperationException("Yêu cầu không tồn tại hoặc đã được xử lý.");
        }

        // Tạo Token có hiệu lực 24 giờ (hoặc 15 phút tùy policy, nhưng setup password thì nên cho lâu hơn chút, vd 24h)
        var token = _protector.Protect(request.Email, DateTimeOffset.UtcNow.AddHours(24));
        
        request.VerificationToken = token;
        request.TokenExpiresAt = DateTime.UtcNow.AddHours(24);
        // Ở đây mình vẫn giữ Status = pending, chỉ khi user đặt mật khẩu thì status mới thành approved
        await _authRepository.UpdateAccountRequestAsync(request, cancellationToken);

        // Gửi email
        var verificationUrl = verificationUrlFormat.Replace("TOKEN_PLACEHOLDER", Uri.EscapeDataString(token));
        
        var subject = "Kích hoạt tài khoản StudyMate AI";
        var body = $@"
            <div style='font-family: sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #4a1f16;'>Xin chào {request.FullName},</h2>
                <p>Yêu cầu cấp tài khoản hệ thống <b>StudyMate AI</b> của bạn đã được quản trị viên phê duyệt.</p>
                <p>Vui lòng click vào đường dẫn bên dưới để thiết lập mật khẩu và kích hoạt tài khoản:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{verificationUrl}' style='display:inline-block;padding:12px 24px;background-color:#c44b36;color:white;text-decoration:none;border-radius:30px;font-weight:bold;'>Thiết lập mật khẩu</a>
                </div>
                <p style='color: #666; font-size: 13px;'>Đường dẫn này sẽ hết hạn trong vòng 24 giờ.</p>
                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                <p style='color: #888; font-size: 12px;'>Trân trọng,<br>Đội ngũ StudyMate AI</p>
            </div>
        ";
        
        try
        {
            await _emailService.SendEmailAsync(request.Email, subject, body, cancellationToken);
        }
        catch
        {
            // Nếu gửi mail thất bại, thực hiện Rollback: Xóa Token để yêu cầu quay về trạng thái chờ duyệt ban đầu.
            // Điều này là Best Practice để tránh giữ Transaction (Lock DB) quá lâu trong lúc chờ gọi mạng (SMTP).
            request.VerificationToken = null;
            request.TokenExpiresAt = null;
            await _authRepository.UpdateAccountRequestAsync(request, CancellationToken.None); // Dùng None để đảm bảo luôn rollback được dù request đã bị hủy
            throw;
        }
    }

    public async Task RejectAccountRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _authRepository.GetAccountRequestByIdAsync(requestId, cancellationToken);
        if (request is null || request.Status != "pending")
        {
            throw new InvalidOperationException("Yêu cầu không tồn tại hoặc đã được xử lý.");
        }

        request.Status = "rejected";
        await _authRepository.UpdateAccountRequestAsync(request, cancellationToken);
    }

    public async Task<bool> VerifyAccountRequestAndSetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = _protector.Unprotect(token);
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var request = await _authRepository.GetAccountRequestByTokenAsync(token, cancellationToken);
            if (request is null)
            {
                return false; // Token không hợp lệ hoặc đã dùng
            }

            if (request.TokenExpiresAt < DateTime.UtcNow)
            {
                return false; // Hết hạn
            }

            // Tạo User mới
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = HashPassword(newPassword),
                RoleId = request.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _authRepository.AddUserAsync(user, cancellationToken);

            // Cập nhật trạng thái request
            request.Status = "approved";
            request.VerificationToken = null; // Xóa token
            await _authRepository.UpdateAccountRequestAsync(request, cancellationToken);

            return true;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }

    public async Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _authRepository.GetUserByEmailWithRoleAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            // Nếu không có trong Users, kiểm tra xem có đang chờ duyệt không để báo lỗi rõ ràng
            var isPending = await _authRepository.AccountRequestEmailExistsAsync(normalizedEmail, cancellationToken);
            if (isPending)
            {
                throw new InvalidOperationException("Tài khoản của bạn đang chờ Admin phê duyệt hoặc bạn chưa thiết lập mật khẩu qua email xác nhận.");
            }
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
