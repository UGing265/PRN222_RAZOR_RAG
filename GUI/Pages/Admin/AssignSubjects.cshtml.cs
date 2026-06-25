using BLL.DTOs.Auth;
using BLL.DTOs.Documents;
using BLL.Interfaces.Auth;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AssignSubjectsModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IDocumentService _documentService;
        private readonly BLL.Interfaces.Notifications.INotificationService _notificationService;

        public AssignSubjectsModel(IAuthService authService, IDocumentService documentService, BLL.Interfaces.Notifications.INotificationService notificationService)
        {
            _authService = authService;
            _documentService = documentService;
            _notificationService = notificationService;
        }

        public AuthUserDto Lecturer { get; set; } = null!;
        public List<Guid> AssignedSubjectIds { get; set; } = new();
        public Dictionary<Guid, (Guid UserId, string FullName)> SubjectLecturerMap { get; set; } = new();
        public List<SubjectDto> AllSubjects { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid userId, CancellationToken cancellationToken)
        {
            var users = await _authService.GetAllUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null || user.RoleId != 2)
            {
                return NotFound();
            }

            Lecturer = user;
            AllSubjects = await _documentService.GetSubjectsAsync(cancellationToken);
            var assigned = await _documentService.GetSubjectsAssignedToLecturerAsync(userId, cancellationToken);
            AssignedSubjectIds = assigned.Select(s => s.Id).ToList();
            SubjectLecturerMap = await _documentService.GetSubjectLecturerMapAsync(cancellationToken);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid userId, List<Guid> subjectIds, CancellationToken cancellationToken)
        {
            var users = await _authService.GetAllUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user is null || user.RoleId != 2)
            {
                return NotFound();
            }

            if (subjectIds != null && subjectIds.Any())
            {
                var allSubjects = await _documentService.GetSubjectsAsync(cancellationToken);
                var validSubjectIds = allSubjects.Where(s => s.AcademicTermId.HasValue).Select(s => s.Id).ToHashSet();
                
                if (subjectIds.Any(id => !validSubjectIds.Contains(id)))
                {
                    TempData["ErrorMessage"] = "Không thể phân công: Một hoặc nhiều môn học chưa được phân vào Học kỳ nào.";
                    return RedirectToPage("/Admin/Users");
                }

                var lecturerMap = await _documentService.GetSubjectLecturerMapAsync(cancellationToken);
                foreach (var subId in subjectIds)
                {
                    if (lecturerMap.TryGetValue(subId, out var info) && info.UserId != userId)
                    {
                        TempData["ErrorMessage"] = "Không thể phân công: Một hoặc nhiều môn học đã được phân công cho giảng viên khác.";
                        return RedirectToPage("/Admin/Users");
                    }
                }
            }

            await _documentService.AssignSubjectsToLecturerAsync(userId, subjectIds ?? new List<Guid>(), cancellationToken);
            await _notificationService.SendSubjectsAssignedUpdatedAsync(userId, cancellationToken);
            TempData["SuccessMessage"] = $"Đã cập nhật danh sách môn học phân công cho giảng viên '{user.FullName}' thành công.";
            return RedirectToPage("/Admin/Users");
        }
    }
}
