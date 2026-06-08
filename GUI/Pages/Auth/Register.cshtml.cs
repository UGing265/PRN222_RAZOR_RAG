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
                if (Input.RoleId is not 2 and not 3)
                {
                    ModelState.AddModelError(nameof(Input.RoleId), "Chỉ được chọn Giảng Viên hoặc Sinh Viên.");
                    return Page();
                }

                var user = await _authService.RegisterAsync(Input.FullName, Input.Email, Input.Password, Input.RoleId, cancellationToken);

                TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay lập tức.";
                return RedirectToPage("/Auth/Login");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register failed for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Không thể đăng ký lúc này. Vui lòng thử lại.");
                return Page();
            }
        }
    }
}
