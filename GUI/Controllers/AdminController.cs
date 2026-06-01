using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAuthService authService, ILogger<AdminController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await _authService.GetAllUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var success = await _authService.ApproveUserAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "Đã phê duyệt người dùng thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể tìm thấy người dùng.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOrBlock(Guid id, CancellationToken cancellationToken)
    {
        var success = await _authService.RejectOrBlockUserAsync(id, cancellationToken);
        if (success)
        {
            TempData["SuccessMessage"] = "Đã thực hiện thao tác thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể xử lý yêu cầu.";
        }
        return RedirectToAction(nameof(Users));
    }
}
