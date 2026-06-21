using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(IAuthService authService, ILogger<RegisterModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Index");
            }
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
                var emailLower = Input.Email.Trim().ToLowerInvariant();
                if (Input.RoleId == 2 && !emailLower.EndsWith("@fe.edu.vn"))
                {
                    ErrorMessage = "Email giảng viên phải kết thúc bằng @fe.edu.vn";
                    return Page();
                }
                if (Input.RoleId == 3 && !emailLower.EndsWith("@fpt.edu.vn"))
                {
                    ErrorMessage = "Email sinh viên phải kết thúc bằng @fpt.edu.vn";
                    return Page();
                }

                await _authService.SubmitAccountRequestAsync(
                    Input.FullName,
                    Input.Email,
                    Input.RoleId,
                    cancellationToken
                );

                SuccessMessage = "Yêu cầu của bạn đã được gửi. Vui lòng chờ Admin phê duyệt.";
                TempData["SuccessMessage"] = SuccessMessage;

                return Page();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during native registration.");
                ErrorMessage = "Đã xảy ra lỗi hệ thống khi đăng ký. Vui lòng thử lại sau.";
                return Page();
            }
        }
    }
}
