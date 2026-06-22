using BLL.DTOs.Documents;
using GUI.Pages.Documents;
using BLL.Interfaces.Documents;
using BLL.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DocumentsModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<DocumentsModel> _logger;

        public DocumentsModel(IDocumentService documentService, INotificationService notificationService, ILogger<DocumentsModel> logger)
        {
            _documentService = documentService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public MyDocumentsViewModel ViewModel { get; set; } = new();
        public string ActiveTab { get; set; } = "files";
        public string? Query { get; set; }
        public Guid? SelectedSubjectId { get; set; }
        public string? SelectedSubjectName { get; set; }
        public List<SubjectDto> Subjects { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? tab, string? q, Guid? subjectId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            ActiveTab = tab ?? "files";
            Query = q;
            SelectedSubjectId = subjectId;

            var result = await _documentService.GetAdminDocumentsAsync(q, subjectId, page, pageSize, cancellationToken);
            var reports = await _documentService.GetPendingReportsAsync(cancellationToken);
            Subjects = await _documentService.GetSubjectsAsync(cancellationToken);

            if (subjectId.HasValue)
            {
                var selectedSub = Subjects.FirstOrDefault(x => x.Id == subjectId.Value);
                SelectedSubjectName = selectedSub?.Name;
            }

            ViewModel = new MyDocumentsViewModel
            {
                Documents = result.Documents.Select(x => new DocumentListItemViewModel
                {
                    Id = x.Id,
                    Slug = x.Slug,
                    Title = x.Title,
                    SubjectCode = x.SubjectCode,
                    SubjectName = x.SubjectName,
                    DocumentTypeId = x.DocumentTypeId,
                    DocumentTypeName = x.DocumentTypeName,
                    AcademicTermName = x.AcademicTermName,
                    Status = x.Status,
                    Visibility = x.Visibility,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    FileCount = x.FileCount,
                    ChunkCount = x.ChunkCount,
                    PreviewText = x.PreviewText,
                    OwnerEmail = x.OwnerEmail,
                    ViewCount = x.ViewCount
                }).ToList(),
                TotalDocuments = result.TotalDocuments,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                PendingReports = reports
            };

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid id, string? q, Guid? subjectId, int page = 1, CancellationToken cancellationToken = default)
        {
            try
            {
                // Fetch title before deleting for the notification payload
                var docInfo = await _documentService.GetAdminDocumentsAsync(null, null, 1, 1, cancellationToken);
                var matchedDoc = ViewModel.Documents.FirstOrDefault(x => x.Id == id);
                string docTitle = matchedDoc?.Title ?? id.ToString();

                await _documentService.DeleteDocumentAsync(id, cancellationToken);

                // Notify all clients viewing this document via SignalR
                await _notificationService.SendDocumentDeletedAsync(id, docTitle, cancellationToken);

                TempData["SuccessMessage"] = "Đã xóa tài liệu khỏi hệ thống thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {Id} by Admin", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tài liệu: " + ex.Message;
            }
            return RedirectToPage("/Admin/Documents", new { tab = "files", q, subjectId, page });
        }

        public async Task<IActionResult> OnPostResolveReportAsync(Guid id, string resolution, CancellationToken cancellationToken)
        {
            try
            {
                await _documentService.ResolveReportAsync(id, resolution, cancellationToken);
                if (resolution.Equals("delete", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["SuccessMessage"] = "Đã xóa tài liệu bị báo cáo và giải quyết các báo cáo liên quan.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Đã bỏ qua báo cáo vi phạm thành công.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving report {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý báo cáo: " + ex.Message;
            }
            return RedirectToPage("/Admin/Documents", new { tab = "reports" });
        }
    }
}
