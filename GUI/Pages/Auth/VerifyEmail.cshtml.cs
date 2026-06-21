using System.ComponentModel.DataAnnotations;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
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

        [BindProperty]
        public VerifyInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class VerifyInputModel
        {
            [Required]
            public string Token { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Mã xác thực không hợp lệ.";
                return RedirectToPage("/Auth/Login");
            }

            Input.Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var success = await _authService.VerifyAccountRequestAndSetPasswordAsync(Input.Token, Input.Password, cancellationToken);
                if (success)
                {
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                    TempData["SuccessMessage"] = "Thiết lập mật khẩu thành công! Tài khoản của bạn đã được kích hoạt. Hãy đăng nhập.";
                    return RedirectToPage("/Auth/Login");
                }
                else
                {
                    ErrorMessage = "Link xác nhận không hợp lệ hoặc đã hết hạn. Vui lòng liên hệ Admin.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác thực email với token {Token}", Input.Token);
                ErrorMessage = "Đã xảy ra lỗi trong quá trình xác thực email.";
                return Page();
            }
        }
    }
}
