using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using BLL.Interfaces.Notifications;
using BLL.Services.Email;
using DAL.Entities;
using DAL.Interfaces.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly INotificationService _notificationService;
    private readonly IEmailQueue _emailQueue;
    private readonly string _appBaseUrl;

    public AuthService(
        IAuthRepository authRepository,
        IDataProtectionProvider dataProtectionProvider,
        INotificationService notificationService,
        IEmailQueue emailQueue,
        IConfiguration configuration)
    {
        _authRepository = authRepository;
        _protector = dataProtectionProvider.CreateProtector("FptStudentEmailVerification").ToTimeLimitedDataProtector();
        _notificationService = notificationService;
        _emailQueue = emailQueue;
        _appBaseUrl = configuration["App:BaseUrl"] ?? "https://localhost:7065";
    }

    public async Task<AuthUserDto> RegisterAsync(
        string fullName,
        string email,
        short roleId,
        CancellationToken cancellationToken = default)
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

        var tempPassword = PasswordGenerator.Generate(12);
        var passwordHash = HashPassword(tempPassword);

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            RoleId = roleId,
            IsActive = false,
            IsBlocked = false,
            EmailVerified = false,
            MustChangePassword = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _authRepository.AddUserAsync(user, cancellationToken);

        var verificationToken = GenerateEmailVerificationToken(normalizedEmail);
        var verificationUrl = $"{_appBaseUrl.TrimEnd('/')}/Auth/VerifyEmail?token={Uri.EscapeDataString(verificationToken)}";

        var subject = "[FPT RAG] Bạn đã được cấp quyền truy cập hệ thống";
        var roleName = created.Role?.Name ?? roleId.ToString();
        var body = BuildWelcomeEmailBody(created.FullName, roleName, normalizedEmail, tempPassword, verificationUrl);

        _emailQueue.Enqueue(new EmailJob(normalizedEmail, subject, body));

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

        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(password, user.PasswordHash))
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
                user.EmailVerified = true;
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

    public async Task<bool> ApproveUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default)
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
        
        var auditLog = new DAL.Entities.AuditLog
        {
            UserId = adminUserId,
            Action = "ApproveUser",
            TargetTable = "Users",
            TargetId = userId,
            Description = $"Admin approved user {userId}",
            CreatedAt = DateTime.UtcNow
        };
        await _authRepository.AddAuditLogAsync(auditLog, cancellationToken);
        await _notificationService.SendAuditLogCreatedAsync(cancellationToken);
        
        return true;
    }

    public async Task<bool> RejectOrBlockUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default)
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

        var actionName = (!user.IsActive && !user.IsBlocked) ? "RejectUser_Delete" : "BlockUser";
        var auditLog = new DAL.Entities.AuditLog
        {
            UserId = adminUserId,
            Action = actionName,
            TargetTable = "Users",
            TargetId = userId,
            Description = $"Admin performed {actionName} on user {userId}",
            CreatedAt = DateTime.UtcNow
        };
        await _authRepository.AddAuditLogAsync(auditLog, cancellationToken);
        await _notificationService.SendAuditLogCreatedAsync(cancellationToken);

        // Notify client to force logout
        await _notificationService.SendForceLogoutAsync(userId, cancellationToken);
        
        // Broadcast to everyone to refresh the library (hides/shows user's documents)
        await _notificationService.SendLibraryRefreshAsync(cancellationToken);

        return true;
    }

    public async Task<bool> UnblockUserAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken = default)
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

        var auditLog = new DAL.Entities.AuditLog
        {
            UserId = adminUserId,
            Action = "UnblockUser",
            TargetTable = "Users",
            TargetId = userId,
            Description = $"Admin unblocked user {userId}",
            CreatedAt = DateTime.UtcNow
        };
        await _authRepository.AddAuditLogAsync(auditLog, cancellationToken);
        await _notificationService.SendAuditLogCreatedAsync(cancellationToken);
        
        await _notificationService.SendLibraryRefreshAsync(cancellationToken);
        
        return true;
    }

    public async Task<AuditLogListDto> GetAuditLogsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var (items, totalCount, targetNames) = await _authRepository.GetAuditLogsAsync(page, pageSize, cancellationToken);
        
        return new AuditLogListDto
        {
            Items = items.Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserEmail = x.User?.Email ?? "Unknown",
                UserFullName = x.User?.FullName ?? "Unknown",
                Action = x.Action,
                TargetTable = x.TargetTable,
                TargetId = x.TargetId,
                TargetName = targetNames.ContainsKey(x.TargetId) ? targetNames[x.TargetId] : null,
                IpAddress = x.IpAddress,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(int SuccessCount, List<string> Errors)> BulkRegisterFromExcelAsync(Stream excelStream, short roleId, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        int successCount = 0;

        try
        {
            NPOI.SS.UserModel.IWorkbook workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(excelStream);
            var sheet = workbook.GetSheetAt(0);

            if (sheet == null)
            {
                errors.Add("File Excel không hợp lệ hoặc không có dữ liệu.");
                return (successCount, errors);
            }

            var roleExists = await _authRepository.RoleExistsAsync(roleId, cancellationToken);
            if (!roleExists)
            {
                errors.Add("Role không hợp lệ.");
                return (successCount, errors);
            }

            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                string username = GetCellValueSafe(row.GetCell(0)).Trim();
                string fullName = GetCellValueSafe(row.GetCell(1)).Trim();
                string email = GetCellValueSafe(row.GetCell(2)).Trim();

                if (username.Length > 255) 
                {
                    errors.Add($"Dòng {i + 1}: Mã định danh quá dài (vượt 255 ký tự). Có thể do lỗi công thức Excel.");
                    continue;
                }
                if (fullName.Length > 200) 
                {
                    errors.Add($"Dòng {i + 1}: Họ tên quá dài (vượt 200 ký tự). Có thể do lỗi công thức Excel.");
                    continue;
                }
                if (email.Length > 255) 
                {
                    errors.Add($"Dòng {i + 1}: Email quá dài (vượt 255 ký tự). Có thể do lỗi công thức Excel.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                {
                    errors.Add($"Dòng {i + 1}: Thiếu Họ và Tên hoặc Email.");
                    continue;
                }

                var normalizedEmail = email.ToLowerInvariant();
                var existingUser = await _authRepository.EmailExistsAsync(normalizedEmail, cancellationToken);
                if (existingUser)
                {
                    errors.Add($"Dòng {i + 1}: Email '{email}' đã tồn tại.");
                    continue;
                }

                string password = GeneratePasswordFromName(fullName);
                var hashedPassword = HashPassword(password);
                var now = DateTime.UtcNow;

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = fullName,
                    Email = normalizedEmail,
                    Username = string.IsNullOrWhiteSpace(username) ? null : username,
                    PasswordHash = hashedPassword,
                    RoleId = roleId,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                try
                {
                    await _authRepository.AddUserAsync(user, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    errors.Add($"Dòng {i + 1}: Lỗi lưu CSDL - {innerMsg}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi đọc file Excel: {ex.Message}");
        }

        return (successCount, errors);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        if (newPassword != confirmPassword)
            return (false, "Mật khẩu xác nhận không khớp.");

        if (newPassword.Length < 6)
            return (false, "Mật khẩu phải có ít nhất 6 ký tự.");

        var user = await _authRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            return (false, "Người dùng không tồn tại.");

        if (user.PasswordHash == "EXTERNAL_OAUTH_GOOGLE")
            throw new InvalidOperationException(
                "Tài khoản đăng nhập qua Google không thể đổi mật khẩu.");

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return (false, "Mật khẩu hiện tại không đúng.");

        if (VerifyPassword(newPassword, user.PasswordHash))
            return (false, "Mật khẩu mới phải khác mật khẩu cũ.");

        user.PasswordHash = HashPassword(newPassword);
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user, cancellationToken);

        return (true, null);
    }

    private static string BuildWelcomeEmailBody(
        string fullName, string roleName, string email, string tempPassword, string verificationUrl)
    {
        return $@"<p>Xin ch&agrave;o <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
<p>B&#7841;n v&#7915;a &#273;&#432;&#7907;c Admin c&#7845;p t&agrave;i kho&#7843;n truy c&#7853;p <strong>FPT RAG System</strong> v&#7899;i vai tr&ograve; <strong>{System.Net.WebUtility.HtmlEncode(roleName)}</strong>.</p>
<p><strong>Th&ocirc;ng tin &#273;&#259;ng nh&#7853;p t&#7841;m:</strong><br>
Email: <code>{System.Net.WebUtility.HtmlEncode(email)}</code><br>
M&#7853;t kh&#7849;u t&#7841;m: <code>{System.Net.WebUtility.HtmlEncode(tempPassword)}</code>
</p>
<p><strong>B&#432;&#7899;c 1:</strong> Click link x&aacute;c nh&#7853;n trong v&ograve;ng 15 ph&uacute;t:<br>
<a href=""{verificationUrl}"">{verificationUrl}</a>
</p>
<p><strong>B&#432;&#7899;c 2:</strong> &#272;&#259;ng nh&#7853;p b&#7851;ng m&#7853;t kh&#7849;u t&#7841;m &#7903; tr&ecirc;n.</p>
<p><strong>B&#432;&#7899;c 3:</strong> H&#7879; th&#7889;ng s&#7869; y&ecirc;u c&#7847;u b&#7841;n &#273;&#7893;i m&#7853;t kh&#7849;u tr&#432;&#7899;c khi s&#7917; d&#7909;ng.</p>";
    }

    private static string GetCellValueSafe(NPOI.SS.UserModel.ICell cell)
    {
        if (cell == null) return "";

        if (cell.CellType == NPOI.SS.UserModel.CellType.Formula)
        {
            try
            {
                if (cell.CachedFormulaResultType == NPOI.SS.UserModel.CellType.String)
                    return cell.StringCellValue ?? "";
                if (cell.CachedFormulaResultType == NPOI.SS.UserModel.CellType.Numeric)
                    return cell.NumericCellValue.ToString();
                if (cell.CachedFormulaResultType == NPOI.SS.UserModel.CellType.Boolean)
                    return cell.BooleanCellValue.ToString();
            }
            catch
            {
                // Fallback
            }
        }

        var formatter = new NPOI.SS.UserModel.DataFormatter();
        string formatted = formatter.FormatCellValue(cell) ?? "";
        
        if (formatted.StartsWith("LET(") || formatted.StartsWith("REGEXREPLACE(") || (formatted.StartsWith("=") && formatted.Length > 200))
        {
            return ""; 
        }

        return formatted;
    }

    private static string GeneratePasswordFromName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "default@123";

        var normalized = RemoveDiacritics(fullName.Trim().ToLowerInvariant());
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0) return "default@123";
        if (parts.Length == 1) return parts[0];

        var password = parts.Last();
        for (int i = 0; i < parts.Length - 1; i++)
        {
            password += parts[i][0];
        }

        return password;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    private static AuthUserDto Map(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        RoleId = user.RoleId,
        RoleName = user.Role?.Name ?? user.RoleId.ToString(),
        IsActive = user.IsActive,
        IsBlocked = user.IsBlocked,
        EmailVerified = user.EmailVerified,
        MustChangePassword = user.MustChangePassword
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
