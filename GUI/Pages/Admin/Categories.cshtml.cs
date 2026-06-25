using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using BLL.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CategoriesModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CategoriesModel> _logger;

    public CategoriesModel(IDocumentService documentService, INotificationService notificationService, ILogger<CategoriesModel> logger)
    {
        _documentService = documentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public List<DocumentSourceDto> DocumentSources { get; set; } = new();
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Subjects = await _documentService.GetSubjectsAsync(cancellationToken);
        AcademicTerms = await _documentService.GetAcademicTermsAsync(cancellationToken);
        Languages = await _documentService.GetLanguagesAsync(cancellationToken);
        DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        DocumentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);
        return Page();
    }

    private void SetError(string message) => TempData["ErrorMessage"] = message;
    private void SetSuccess(string message) => TempData["SuccessMessage"] = message;

    private async Task<IActionResult> ExecuteActionAsync(Func<Task> action, string successMessage, string actionType = "Update", object? data = null)
    {
        try
        {
            await action();
            SetSuccess(successMessage);
            await _notificationService.SendMetadataUpdatedAsync("Metadata", actionType, data ?? new { }, default);
        }
        catch (InvalidOperationException ex)
        {
            SetError(ex.Message);
        }
        return RedirectToPage("/Admin/Categories");
    }

    // SUBJECTS
    public Task<IActionResult> OnPostCreateSubjectAsync(string code, string name, Guid? academicTermId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã môn học, tên môn học không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            () => _documentService.CreateSubjectAsync(code, name, academicTermId, ct),
            $"Đã tạo mới môn học '{code.ToUpper()}' thành công."
        );
    }

    public Task<IActionResult> OnPostUpdateSubjectAsync(Guid id, string code, string name, Guid? academicTermId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã môn học, tên môn học không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            async () => {
                var res = await _documentService.UpdateSubjectAsync(id, code, name, academicTermId, ct);
                if (res == null) throw new InvalidOperationException("Không tìm thấy môn học.");
            },
            $"Đã cập nhật môn học '{code.ToUpper()}' thành công."
        );
    }

    public Task<IActionResult> OnPostDeleteSubjectAsync(Guid id, CancellationToken ct)
    {
        return ExecuteActionAsync(
            async () => {
                var ok = await _documentService.DeleteSubjectAsync(id, ct);
                if (!ok) throw new InvalidOperationException("Không tìm thấy hoặc không thể xóa môn học (đang bị ràng buộc dữ liệu).");
            },
            "Đã xóa môn học thành công.",
            "Delete",
            new { entityId = id, entityType = "Subject" }
        );
    }

    // DOCUMENT TYPES
    public Task<IActionResult> OnPostCreateDocumentTypeAsync(string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên loại học liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            () => _documentService.CreateDocumentTypeAsync(name, description, ct),
            $"Đã tạo mới loại học liệu '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostUpdateDocumentTypeAsync(Guid id, string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên loại học liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            async () => {
                var res = await _documentService.UpdateDocumentTypeAsync(id, name, description, ct);
                if (res == null) throw new InvalidOperationException("Không tìm thấy loại học liệu.");
            },
            $"Đã cập nhật loại học liệu '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostDeleteDocumentTypeAsync(Guid id, CancellationToken ct)
    {
        return ExecuteActionAsync(
            async () => {
                var ok = await _documentService.DeleteDocumentTypeAsync(id, ct);
                if (!ok) throw new InvalidOperationException("Không tìm thấy hoặc không thể xóa loại học liệu (đang bị ràng buộc dữ liệu).");
            },
            "Đã xóa loại học liệu thành công.",
            "Delete",
            new { entityId = id, entityType = "DocumentType" }
        );
    }

    // LANGUAGES
    public Task<IActionResult> OnPostCreateLanguageAsync(string code, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã ngôn ngữ và tên ngôn ngữ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            () => _documentService.CreateLanguageAsync(code, name, ct),
            $"Đã tạo mới ngôn ngữ '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostUpdateLanguageAsync(Guid id, string code, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã ngôn ngữ và tên ngôn ngữ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            async () => {
                var res = await _documentService.UpdateLanguageAsync(id, code, name, ct);
                if (res == null) throw new InvalidOperationException("Không tìm thấy ngôn ngữ.");
            },
            $"Đã cập nhật ngôn ngữ '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostDeleteLanguageAsync(Guid id, CancellationToken ct)
    {
        return ExecuteActionAsync(
            async () => {
                var ok = await _documentService.DeleteLanguageAsync(id, ct);
                if (!ok) throw new InvalidOperationException("Không tìm thấy hoặc không thể xóa ngôn ngữ (đang bị ràng buộc dữ liệu).");
            },
            "Đã xóa ngôn ngữ thành công.",
            "Delete",
            new { entityId = id, entityType = "Language" }
        );
    }

    // DOCUMENT SOURCES
    public Task<IActionResult> OnPostCreateDocumentSourceAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên nguồn tài liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            () => _documentService.CreateDocumentSourceAsync(name, ct),
            $"Đã tạo mới nguồn tài liệu '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostUpdateDocumentSourceAsync(Guid id, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên nguồn tài liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            async () => {
                var res = await _documentService.UpdateDocumentSourceAsync(id, name, ct);
                if (res == null) throw new InvalidOperationException("Không tìm thấy nguồn tài liệu.");
            },
            $"Đã cập nhật nguồn tài liệu '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostDeleteDocumentSourceAsync(Guid id, CancellationToken ct)
    {
        return ExecuteActionAsync(
            async () => {
                var ok = await _documentService.DeleteDocumentSourceAsync(id, ct);
                if (!ok) throw new InvalidOperationException("Không tìm thấy hoặc không thể xóa nguồn tài liệu (đang bị ràng buộc dữ liệu).");
            },
            "Đã xóa nguồn tài liệu thành công.",
            "Delete",
            new { entityId = id, entityType = "DocumentSource" }
        );
    }

    // ACADEMIC TERMS
    public Task<IActionResult> OnPostCreateAcademicTermAsync(string name, int order, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên học kỳ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            () => _documentService.CreateAcademicTermAsync(name, order, ct),
            $"Đã tạo mới học kỳ '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostUpdateAcademicTermAsync(Guid id, string name, int order, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên học kỳ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Categories"));
        }
        return ExecuteActionAsync(
            async () => {
                var res = await _documentService.UpdateAcademicTermAsync(id, name, order, ct);
                if (res == null) throw new InvalidOperationException("Không tìm thấy học kỳ.");
            },
            $"Đã cập nhật học kỳ '{name}' thành công."
        );
    }

    public Task<IActionResult> OnPostDeleteAcademicTermAsync(Guid id, CancellationToken ct)
    {
        return ExecuteActionAsync(
            async () => {
                var ok = await _documentService.DeleteAcademicTermAsync(id, ct);
                if (!ok) throw new InvalidOperationException("Không tìm thấy hoặc không thể xóa học kỳ (đang bị ràng buộc dữ liệu).");
            },
            "Đã xóa học kỳ thành công.",
            "Delete",
            new { entityId = id, entityType = "AcademicTerm" }
        );
    }
}
