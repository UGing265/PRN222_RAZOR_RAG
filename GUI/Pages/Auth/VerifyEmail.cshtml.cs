using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class VerifyEmailModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<VerifyEmailModel> _logger;

        public VerifyEmailModel(IAuthService authService, ILogger<VerifyEmailModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Mã xác thực không hợp lệ.";
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                var (isValid, isExpired, email) = _authService.ValidateEmailVerificationToken(token);
                if (isValid)
                {
                    bool verified = await _authService.VerifyEmailTokenAsync(token);
                    if (verified)
                    {
                        TempData["SuccessMessage"] = "Xác thực email thành công! Vui lòng đăng nhập.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không thể xác thực email. Tài khoản không tồn tại hoặc đã bị khóa.";
                    }
                    
                    return RedirectToPage("/Auth/Login", new { email = email });
                }
                else if (isExpired)
                {
                    return RedirectToPage("/Auth/TokenExpired", new { token = token });
                }
                else
                {
                    TempData["ErrorMessage"] = "Đường dẫn xác thực không hợp lệ.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra token xác thực {Token}", token);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xử lý.";
            }

            return RedirectToPage("/Auth/Login");
        }
    }
}
