using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class GoogleLoginModel : PageModel
    {
        public IActionResult OnGet()
        {
            var callbackUrl = "http://localhost:5155/";
            var redirectUrl = $"http://localhost:5000/api/auth/sign-in/social?provider=google&callbackURL={Uri.EscapeDataString(callbackUrl)}";
            return Redirect(redirectUrl);
        }
    }
}
