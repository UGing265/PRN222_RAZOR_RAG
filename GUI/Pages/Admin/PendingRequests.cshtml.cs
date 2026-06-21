using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class PendingRequestsModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<PendingRequestsModel> _logger;

        public PendingRequestsModel(IAuthService authService, ILogger<PendingRequestsModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public List<AccountRequestDto> Requests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            Requests = await _authService.GetPendingRequestsAsync(cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var verificationUrlFormat = Url.Page("/Auth/VerifyEmail", null, new { token = "TOKEN_PLACEHOLDER" }, Request.Scheme);
                await _authService.ApproveAccountRequestAsync(id, verificationUrlFormat!, cancellationToken);
                TempData["SuccessMessage"] = "Đã phê duyệt yêu cầu và gửi email hướng dẫn thiết lập mật khẩu.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin failed to approve request {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi phê duyệt yêu cầu.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _authService.RejectAccountRequestAsync(id, cancellationToken);
                TempData["SuccessMessage"] = "Đã từ chối yêu cầu cấp tài khoản.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin failed to reject request {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi từ chối yêu cầu.";
            }
            return RedirectToPage();
        }
    }
}
