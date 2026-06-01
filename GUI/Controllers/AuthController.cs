using System.Security.Claims;
using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using GUI.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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

            var emailLower = model.Email.Trim().ToLowerInvariant();
            if (model.RoleId == 2 && !emailLower.EndsWith("@fe.edu.vn"))
            {
                ModelState.AddModelError(nameof(model.Email), "Giảng viên bắt buộc sử dụng email đuôi @fe.edu.vn.");
                return View(model);
            }

            if (model.RoleId == 3 && !emailLower.EndsWith("@fpt.edu.vn"))
            {
                ModelState.AddModelError(nameof(model.Email), "Sinh viên bắt buộc sử dụng email đuôi @fpt.edu.vn.");
                return View(model);
            }

            var user = await _authService.RegisterAsync(model.FullName, model.Email, model.Password, model.RoleId, cancellationToken);
            TempData["SuccessMessage"] = "Đăng ký thành công! Tài khoản của bạn đang chờ Admin phê duyệt.";
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
