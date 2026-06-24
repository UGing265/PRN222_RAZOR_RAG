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

        public async Task<IActionResult> OnGetAsync(string token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Mã xác thực không hợp lệ.";
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                var verified = await _authService.VerifyEmailTokenAsync(token, cancellationToken);
                if (verified)
                {
                    TempData["SuccessMessage"] = "Email đã được xác nhận. Vui lòng đăng nhập bằng mật khẩu tạm đã được gửi qua email.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Link xác nhận không hợp lệ hoặc đã hết hạn. Liên hệ Admin để được cấp lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác thực email với token {Token}", token);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xác thực email.";
            }

            return RedirectToPage("/Auth/Login");
        }
    }
}
