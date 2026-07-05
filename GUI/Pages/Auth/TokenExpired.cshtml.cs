using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace GUI.Pages.Auth;

public class TokenExpiredModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TokenExpiredModel> _logger;

    public TokenExpiredModel(IAuthService authService, IMemoryCache memoryCache, ILogger<TokenExpiredModel> logger)
    {
        _authService = authService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string token)
    {
        Token = token;
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Auth/Login");
        }

        var (isValid, isExpired, email) = _authService.ValidateEmailVerificationToken(token);
        if (!isExpired || string.IsNullOrEmpty(email))
        {
            return RedirectToPage("/Auth/Login");
        }

        Email = email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            return RedirectToPage("/Auth/Login");
        }

        var (isValid, isExpired, email) = _authService.ValidateEmailVerificationToken(Token);
        if (string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Đường dẫn không hợp lệ hoặc đã bị thay đổi.";
            return Page();
        }

        Email = email;

        // Chống spam: Mỗi email chỉ được gửi lại tối đa 1 lần mỗi 3 phút
        var cacheKey = $"ResendEmail_Cooldown_{email.ToLowerInvariant()}";
        if (_memoryCache.TryGetValue(cacheKey, out _))
        {
            ErrorMessage = "Vui lòng chờ ít nhất 3 phút trước khi yêu cầu gửi lại email mới để tránh spam.";
            return Page();
        }

        try
        {
            var (success, error) = await _authService.ResendWelcomeEmailAsync(email);
            if (!success)
            {
                ErrorMessage = error ?? "Không thể gửi lại email xác thực.";
                return Page();
            }

            // Đặt thời gian chờ 3 phút
            _memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(3));

            TempData["SuccessMessage"] = "Đã gửi lại email xác thực mới kèm mật khẩu tạm thời mới tới hộp thư của bạn! Vui lòng kiểm tra email và đăng nhập.";
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi lại email xác thực cho {Email}", email);
            ErrorMessage = "Đã xảy ra lỗi hệ thống khi gửi lại email.";
            return Page();
        }
    }
}
