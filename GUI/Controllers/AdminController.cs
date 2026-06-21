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
    public async Task<IActionResult> CreateUser(string fullName, string email, string password, short roleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin để tạo tài khoản.";
            return RedirectToAction(nameof(Users));
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

        return RedirectToAction(nameof(Users));
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

    [HttpGet]
    public async Task<IActionResult> PendingRequests(CancellationToken cancellationToken)
    {
        var requests = await _authService.GetPendingRequestsAsync(cancellationToken);
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var verificationUrlFormat = Url.Action("Verify", "Auth", new { token = "TOKEN_PLACEHOLDER" }, Request.Scheme);
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

        return RedirectToAction(nameof(PendingRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(Guid id, CancellationToken cancellationToken)
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
        return RedirectToAction(nameof(PendingRequests));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken cancellationToken)
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
            Languages = await _documentService.GetLanguagesAsync(cancellationToken),
            DocumentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken),
            AcademicTerms = await _documentService.GetAcademicTermsAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(string code, string name, Guid? academicTermId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Mã môn học và tên môn học không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "subjects" });
        }
        if (!academicTermId.HasValue)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn học kỳ cho môn học.";
            return RedirectToAction(nameof(Metadata), new { tab = "subjects" });
        }
        try
        {
            await _documentService.CreateSubjectAsync(code, name, academicTermId, cancellationToken);
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
    public async Task<IActionResult> UpdateSubject(Guid id, string code, string name, Guid? academicTermId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Mã môn học và tên môn học không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "subjects" });
        }
        if (!academicTermId.HasValue)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn học kỳ cho môn học.";
            return RedirectToAction(nameof(Metadata), new { tab = "subjects" });
        }
        try
        {
            await _documentService.UpdateSubjectAsync(id, code, name, academicTermId, cancellationToken);
            TempData["SuccessMessage"] = $"Đã cập nhật môn học '{code.ToUpper()}' thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "subjects" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubject(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _documentService.DeleteSubjectAsync(id, cancellationToken);
            if (result)
                TempData["SuccessMessage"] = "Đã xóa môn học thành công.";
            else
                TempData["ErrorMessage"] = "Không tìm thấy môn học.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {Id}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa môn học. Đảm bảo môn học không bị ràng buộc dữ liệu.";
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
    public async Task<IActionResult> UpdateDocumentType(Guid id, string name, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Tên loại học liệu không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "documenttypes" });
        }
        try
        {
            var result = await _documentService.UpdateDocumentTypeAsync(id, name, description, cancellationToken);
            if (result != null)
                TempData["SuccessMessage"] = $"Đã cập nhật loại học liệu '{name}' thành công.";
            else
                TempData["ErrorMessage"] = "Không tìm thấy loại học liệu này.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "documenttypes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocumentType(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _documentService.DeleteDocumentTypeAsync(id, cancellationToken);
            if (success)
                TempData["SuccessMessage"] = "Đã xóa loại học liệu thành công.";
            else
                TempData["ErrorMessage"] = "Không tìm thấy loại học liệu này.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document type {Id}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa loại học liệu. Đảm bảo dữ liệu không bị ràng buộc.";
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLanguage(Guid id, string code, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Mã ngôn ngữ và tên ngôn ngữ không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "languages" });
        }
        try
        {
            var result = await _documentService.UpdateLanguageAsync(id, code, name, cancellationToken);
            if (result != null)
                TempData["SuccessMessage"] = $"Đã cập nhật ngôn ngữ '{name}' thành công.";
            else
                TempData["ErrorMessage"] = "Không tìm thấy ngôn ngữ này.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "languages" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _documentService.DeleteLanguageAsync(id, cancellationToken);
            if (success)
                TempData["SuccessMessage"] = "Đã xóa ngôn ngữ thành công.";
            else
                TempData["ErrorMessage"] = "Không tìm thấy ngôn ngữ này.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting language {Id}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa ngôn ngữ. Đảm bảo dữ liệu không bị ràng buộc.";
        }
        return RedirectToAction(nameof(Metadata), new { tab = "languages" });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDocumentSource(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Tên nguồn tài liệu không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "documentsources" });
        }
        try
        {
            await _documentService.CreateDocumentSourceAsync(name, cancellationToken);
            TempData["SuccessMessage"] = $"Đã tạo mới nguồn tài liệu '{name}' thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "documentsources" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDocumentSource(Guid id, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Tên nguồn tài liệu không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "documentsources" });
        }
        try
        {
            var result = await _documentService.UpdateDocumentSourceAsync(id, name, cancellationToken);
            if (result != null)
                TempData["SuccessMessage"] = $"Đã cập nhật nguồn tài liệu '{name}' thành công.";
            else
                TempData["ErrorMessage"] = "Không tìm thấy nguồn tài liệu này.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "documentsources" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocumentSource(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _documentService.DeleteDocumentSourceAsync(id, cancellationToken);
            if (success)
                TempData["SuccessMessage"] = "Đã xóa nguồn tài liệu thành công.";
            else
                TempData["ErrorMessage"] = "Không tìm thấy nguồn tài liệu này.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document source {Id}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa nguồn tài liệu. Đảm bảo dữ liệu không bị ràng buộc.";
        }
        return RedirectToAction(nameof(Metadata), new { tab = "documentsources" });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAcademicTerm(string name, int order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Tên học kỳ không được để trống.";
            return RedirectToAction(nameof(Metadata), new { tab = "academicterms" });
        }
        try
        {
            await _documentService.CreateAcademicTermAsync(name, order, cancellationToken);
            TempData["SuccessMessage"] = $"Đã tạo mới học kỳ '{name}' thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Metadata), new { tab = "academicterms" });
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

    [HttpGet]
    public async Task<IActionResult> AssignSubjects(Guid userId, CancellationToken cancellationToken)
    {
        var users = await _authService.GetAllUsersAsync(cancellationToken);
        var user = users.FirstOrDefault(u => u.Id == userId);
        if (user is null || user.RoleId != 2)
        {
            return NotFound();
        }

        var allSubjects = await _documentService.GetSubjectsAsync(cancellationToken);
        var assignedSubjects = await _documentService.GetSubjectsAssignedToLecturerAsync(userId, cancellationToken);
        var subjectLecturerMap = await _documentService.GetSubjectLecturerMapAsync(cancellationToken);

        ViewBag.Lecturer = user;
        ViewBag.AssignedSubjectIds = assignedSubjects.Select(s => s.Id).ToList();
        ViewBag.SubjectLecturerMap = subjectLecturerMap;

        return View(allSubjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubjects(Guid userId, List<Guid> subjectIds, CancellationToken cancellationToken)
    {
        var users = await _authService.GetAllUsersAsync(cancellationToken);
        var user = users.FirstOrDefault(u => u.Id == userId);
        if (user is null || user.RoleId != 2)
        {
            return NotFound();
        }

        if (subjectIds != null && subjectIds.Any())
        {
            var lecturerMap = await _documentService.GetSubjectLecturerMapAsync(cancellationToken);
            foreach (var subId in subjectIds)
            {
                if (lecturerMap.TryGetValue(subId, out var info) && info.UserId != userId)
                {
                    TempData["ErrorMessage"] = "Không thể phân công: Một hoặc nhiều môn học đã được phân công cho giảng viên khác.";
                    return RedirectToAction(nameof(Users));
                }
            }
        }

        await _documentService.AssignSubjectsToLecturerAsync(userId, subjectIds ?? new List<Guid>(), cancellationToken);
        TempData["SuccessMessage"] = $"Đã cập nhật danh sách môn học phân công cho giảng viên '{user.FullName}' thành công.";
        return RedirectToAction(nameof(Users));
    }
}

