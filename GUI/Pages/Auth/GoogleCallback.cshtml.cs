using System.Security.Claims;
using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class GoogleCallbackModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<GoogleCallbackModel> _logger;

        public GoogleCallbackModel(IAuthService authService, ILogger<GoogleCallbackModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var result = await HttpContext.AuthenticateAsync("TempExternalCookie");
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Xác thực qua Google thất bại.";
                return RedirectToPage("/Auth/Login");
            }

            var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                await HttpContext.SignOutAsync("TempExternalCookie");
                TempData["ErrorMessage"] = "Không thể lấy email từ tài khoản Google của bạn.";
                return RedirectToPage("/Auth/Login");
            }

            var name = result.Principal?.FindFirstValue(ClaimTypes.Name) ?? email;
            await HttpContext.SignOutAsync("TempExternalCookie");

            try
            {
                var user = await _authService.LoginOrRegisterExternalAsync(email, name, cancellationToken);
                await SignInAsync(user, isPersistent: true);
                TempData["SuccessMessage"] = "Đăng nhập bằng Google thành công.";
                return RedirectToPage("/Index");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("/Auth/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed for {Email}", email);
                TempData["ErrorMessage"] = "Lỗi hệ thống khi đăng nhập bằng Google.";
                return RedirectToPage("/Auth/Login");
            }
        }

        private async Task SignInAsync(AuthUserDto user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
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
                    IsPersistent = isPersistent
                });
        }
    }
}
