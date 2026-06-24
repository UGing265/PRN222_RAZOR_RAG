using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GUI.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AuditLogsModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuditLogsModel> _logger;

    public AuditLogsModel(IAuthService authService, ILogger<AuditLogsModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public AuditLogListDto ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync([FromQuery] int p = 1, CancellationToken cancellationToken = default)
    {
        int pageSize = 20;
        var page = p < 1 ? 1 : p;

        try
        {
            ViewModel = await _authService.GetAuditLogsAsync(page, pageSize, cancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit logs.");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải lịch sử hệ thống.";
        }

        return Page();
    }
}
