using System.Security.Claims;
using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using GUI.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IEmailService emailService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            if (model.RoleId is not 2 and not 3)
            {
                ModelState.AddModelError(nameof(model.RoleId), "Chỉ được chọn Giảng Viên hoặc Sinh Viên.");
                return View(model);
            }

            var user = await _authService.RegisterAsync(model.FullName, model.Email, model.Password, model.RoleId, cancellationToken);

            TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay lập tức.";
            return RedirectToAction(nameof(Login));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register failed for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Không thể đăng ký lúc này. Vui lòng thử lại.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _authService.ValidateCredentialsAsync(model.Email, model.Password, cancellationToken);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            await SignInAsync(user, model.RememberMe);
            TempData["SuccessMessage"] = "Đăng nhập thành công.";
            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Không thể đăng nhập lúc này. Vui lòng thử lại.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(".AspNetCore.Cookies");
        TempData["SuccessMessage"] = "Đã đăng xuất.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleCallback)) };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync("TempExternalCookie");
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Xác thực qua Google thất bại.";
            return RedirectToAction(nameof(Login));
        }

        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            await HttpContext.SignOutAsync("TempExternalCookie");
            TempData["ErrorMessage"] = "Không thể lấy email từ tài khoản Google của bạn.";
            return RedirectToAction(nameof(Login));
        }

        var name = result.Principal?.FindFirstValue(ClaimTypes.Name) ?? email;
        await HttpContext.SignOutAsync("TempExternalCookie");

        try
        {
            var user = await _authService.LoginOrRegisterExternalAsync(email, name, cancellationToken);
            await SignInAsync(user, isPersistent: true);
            TempData["SuccessMessage"] = "Đăng nhập bằng Google thành công.";
            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google login failed for {Email}", email);
            TempData["ErrorMessage"] = "Lỗi hệ thống khi đăng nhập bằng Google.";
            return RedirectToAction(nameof(Login));
        }
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "Mã xác thực không hợp lệ.";
            return RedirectToAction(nameof(Login));
        }

        try
        {
            var verified = await _authService.VerifyEmailTokenAsync(token, cancellationToken);
            if (verified)
            {
                TempData["SuccessMessage"] = "Xác thực email thành công! Tài khoản của bạn đã được kích hoạt. Hãy đăng nhập.";
            }
            else
            {
                TempData["ErrorMessage"] = "Mã xác thực không hợp lệ hoặc đã hết hạn. Vui lòng thử đăng ký lại.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xác thực email với token {Token}", token);
            TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xác thực email.";
        }

        return RedirectToAction(nameof(Login));
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
