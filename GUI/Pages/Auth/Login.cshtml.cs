using System.Security.Claims;
using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IAuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public LoginViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

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
                var user = await _authService.ValidateCredentialsAsync(Input.Email, Input.Password, cancellationToken);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác.";
                    return Page();
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Name, user.FullName),
                    new(ClaimTypes.Role, user.RoleName),
                    new("role_id", user.RoleId.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = Input.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    });

                _logger.LogInformation("User {Email} logged in successfully.", user.Email);
                TempData["SuccessMessage"] = "Đăng nhập thành công.";
                
                if (user.RoleName == "Admin")
                {
                    return RedirectToPage("/Admin/Users");
                }
                return RedirectToPage("/Documents/All");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during native login.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
                return Page();
            }
        }
    }
}
