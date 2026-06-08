using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth
{
    public class GoogleLoginModel : PageModel
    {
        public IActionResult OnGet()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Page("/Auth/GoogleCallback") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}
