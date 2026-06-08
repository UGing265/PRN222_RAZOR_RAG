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
                    TempData["SuccessMessage"] = "Xác thực email thành công! Tài khoản của bạn đã được kích hoạt. Hãy đăng nhập.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Mã xác thực không hợp lệ hoặc đã hết hạn. Vui lòng thử đăng ký lại.";
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
