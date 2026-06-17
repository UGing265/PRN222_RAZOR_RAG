using BLL.DTOs.Auth;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<UsersModel> _logger;

        public UsersModel(IAuthService authService, ILogger<UsersModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public List<AuthUserDto> UsersList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            UsersList = await _authService.GetAllUsersAsync(cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateUserAsync(string fullName, string email, string password, short roleId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin để tạo tài khoản.";
                return RedirectToPage();
            }

            try
            {
                await _authService.RegisterAsync(fullName, email, password, roleId, cancellationToken);
                TempData["SuccessMessage"] = $"Đã tạo tài khoản cho '{fullName}' thành công.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin failed to create user {Email}", email);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id, CancellationToken cancellationToken = default)
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
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectOrBlockAsync(Guid id, CancellationToken cancellationToken = default)
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
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnblockAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var success = await _authService.UnblockUserAsync(id, cancellationToken);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã mở khóa tài khoản người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể mở khóa tài khoản.";
            }
            return RedirectToPage();
        }
    }
}
