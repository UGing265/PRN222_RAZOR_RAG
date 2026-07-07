using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Auth;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly IAuthService _authService;

    public ChangePasswordModel(IAuthService authService)
    {
        _authService = authService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        try
        {
            var (success, error) = await _authService.ChangePasswordAsync(
                userId, CurrentPassword, NewPassword, ConfirmPassword);

            if (!success)
            {
                TempData["ErrorMessage"] = error;
                return Page();
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return Page();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Đổi mật khẩu và xác thực tài khoản thành công! Vui lòng đăng nhập lại bằng mật khẩu mới.";
        return RedirectToPage("/Auth/Login");
    }
}
