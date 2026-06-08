using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        private readonly IAuthService _authService;

        public LogoutModel(IAuthService authService)
        {
            _authService = authService;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Auth/Login");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Request.Cookies.TryGetValue("better-auth.session_token", out var sessionToken) && !string.IsNullOrEmpty(sessionToken))
            {
                // Invalidate session in database via BLL
                await _authService.InvalidateSessionTokenAsync(sessionToken);

                // Delete cookie from browser
                Response.Cookies.Delete("better-auth.session_token");
            }

            // Sign out from ASP.NET Core Cookie authentication middleware
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = "Đã đăng xuất.";
            return RedirectToPage("/Auth/Login");
        }
    }
}
