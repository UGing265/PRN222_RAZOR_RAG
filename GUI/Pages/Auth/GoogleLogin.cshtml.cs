using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class GoogleLoginModel : PageModel
    {
        public IActionResult OnGet()
        {
            // callbackURL phải trỏ về app để .NET xử lý sau khi Google redirect về
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/";
            // Dùng proxy /api/auth để tránh mixed content — .NET sẽ forward sang http://localhost:5000
            var redirectUrl = $"http://localhost:5000/api/auth/sign-in/social?provider=google&callbackURL={Uri.EscapeDataString(callbackUrl)}";
            return Redirect(redirectUrl);
        }
    }
}
