using BLL.Interfaces.Auth;
using BLL.Interfaces.Documents;
using GUI.Models.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAuthService _authService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAuthService authService, IDocumentService documentService, ILogger<AdminController> logger)
    {
        _authService = authService;
        _documentService = documentService;
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

    [HttpGet]
    public async Task<IActionResult> Metadata(string? tab, CancellationToken cancellationToken)
    {
        ViewBag.ActiveTab = tab ?? "subjects";
        var model = new AdminMetadataViewModel
        {
            Subjects = await _documentService.GetSubjectsAsync(cancellationToken),
            DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken),
            Languages = await _documentService.GetLanguagesAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(string code, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Mã môn học và tên môn học không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "subjects" });
        }
        try
        {
            await _documentService.CreateSubjectAsync(code, name, cancellationToken);
            TempData["SuccessMessage"] = $"Đã tạo mới môn học '{code.ToUpper()}' thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "subjects" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDocumentType(string name, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Tên loại học liệu không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "documenttypes" });
        }
        try
        {
            await _documentService.CreateDocumentTypeAsync(name, description, cancellationToken);
            TempData["SuccessMessage"] = $"Đã tạo mới loại học liệu '{name}' thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "documenttypes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLanguage(string code, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Mã ngôn ngữ và tên ngôn ngữ không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "languages" });
        }
        try
        {
            await _documentService.CreateLanguageAsync(code, name, cancellationToken);
            TempData["SuccessMessage"] = $"Đã tạo mới ngôn ngữ '{name}' thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "languages" });
    }

    [HttpGet]
    public async Task<IActionResult> Documents(string? tab, string? q, Guid? subjectId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        ViewBag.ActiveTab = tab ?? "files";
        var result = await _documentService.GetAdminDocumentsAsync(q, subjectId, page, pageSize, cancellationToken);
        var reports = await _documentService.GetPendingReportsAsync(cancellationToken);
        
        var viewModel = new MyDocumentsViewModel
        {
            Documents = result.Documents.Select(x => new DocumentListItemViewModel
            {
                Id = x.Id,
                Slug = x.Slug,
                Title = x.Title,
                SubjectName = x.SubjectName,
                MajorName = x.MajorName,
                DocumentTypeId = x.DocumentTypeId,
                DocumentTypeName = x.DocumentTypeName,
                AcademicTerm = x.AcademicTerm,
                Status = x.Status,
                Visibility = x.Visibility,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                FileCount = x.FileCount,
                ChunkCount = x.ChunkCount,
                PreviewText = x.PreviewText,
                OwnerEmail = x.OwnerEmail
            }).ToList(),
            TotalDocuments = result.TotalDocuments,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            PendingReports = reports
        };

        ViewBag.Query = q;
        ViewBag.SelectedSubjectId = subjectId;
        var allSubjects = await _documentService.GetSubjectsAsync(cancellationToken);
        ViewBag.Subjects = allSubjects;
        if (subjectId.HasValue)
        {
            var selectedSub = allSubjects.FirstOrDefault(x => x.Id == subjectId.Value);
            ViewBag.SelectedSubjectName = selectedSub?.Name;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(Guid id, string? q, Guid? subjectId, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            await _documentService.DeleteDocumentAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Đã xóa tài liệu khỏi hệ thống thành công.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {Id} by Admin", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tài liệu: " + ex.Message;
        }
        return RedirectToAction(nameof(Documents), new { tab = "files", q, subjectId, page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveReport(Guid id, string resolution, CancellationToken cancellationToken)
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
        return RedirectToAction(nameof(Documents), new { tab = "reports" });
    }
}
