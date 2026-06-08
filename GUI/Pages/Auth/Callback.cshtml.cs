using System.Security.Claims;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Auth;

public class CallbackModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(IAuthService authService, ILogger<CallbackModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Called from the frontend JS after a successful Better Auth sign-in.
    /// Receives the raw session token, validates it via DB, and signs the user
    /// into ASP.NET Cookie Authentication so .NET pages can read User.Identity.
    /// </summary>
    public async Task<IActionResult> OnPostAsync([FromBody] TokenCallbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
            return BadRequest(new { message = "Token không hợp lệ." });

        try
        {
            var session = await _authService.ValidateSessionTokenAsync(request.Token);

            if (session == null)
                return StatusCode(401, new { message = "Phiên đăng nhập không tồn tại hoặc đã hết hạn." });

            if (session.ExpiresAt.ToUniversalTime() < DateTime.UtcNow)
                return StatusCode(401, new { message = "Phiên đăng nhập đã hết hạn." });

            if (session.IsBlocked)
                return StatusCode(401, new { message = "Tài khoản của bạn đã bị khóa." });

            if (!session.IsActive)
                return StatusCode(401, new { message = "Tài khoản của bạn chưa được kích hoạt." });

            // Build ASP.NET claims identity
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new(ClaimTypes.Email, session.Email),
                new(ClaimTypes.Name, session.FullName),
                new(ClaimTypes.Role, session.RoleName),
                // Store the raw Better Auth token so we can use it for API calls if needed
                new("better_auth_token", request.Token),
            };

            if (!string.IsNullOrEmpty(session.Username))
                claims.Add(new("username", session.Username));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = session.ExpiresAt.ToUniversalTime()
                });

            _logger.LogInformation("User {Email} signed in successfully via Better Auth callback.", session.Email);
            return new JsonResult(new { success = true, redirectUrl = "/" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Better Auth callback.");
            return StatusCode(500, new { message = "Lỗi máy chủ khi xác thực." });
        }
    }
}

public record TokenCallbackRequest(string Token);
