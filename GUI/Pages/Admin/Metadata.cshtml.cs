using BLL.Interfaces.Documents;
using GUI.Pages.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class MetadataModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<MetadataModel> _logger;

        public MetadataModel(IDocumentService documentService, ILogger<MetadataModel> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        public AdminMetadataViewModel ViewModel { get; set; } = new();
        public string ActiveTab { get; set; } = "subjects";

        public async Task<IActionResult> OnGetAsync(string? tab, CancellationToken cancellationToken)
        {
            ActiveTab = tab ?? "subjects";
            await LoadDataAsync(cancellationToken);
            return Page();
        }

        private async Task LoadDataAsync(CancellationToken cancellationToken)
        {
            ViewModel = new AdminMetadataViewModel
            {
                Subjects = await _documentService.GetSubjectsAsync(cancellationToken),
                DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken),
                Languages = await _documentService.GetLanguagesAsync(cancellationToken),
                DocumentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken),
                AcademicTerms = await _documentService.GetAcademicTermsAsync(cancellationToken)
            };
        }

        public async Task<IActionResult> OnPostCreateSubjectAsync(string code, string name, Guid? academicTermId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Mã môn học và tên môn học không được để trống.";
                return RedirectToPage(new { tab = "subjects" });
            }
            if (!academicTermId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn học kỳ cho môn học.";
                return RedirectToPage(new { tab = "subjects" });
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
            return RedirectToPage(new { tab = "subjects" });
        }

        public async Task<IActionResult> OnPostUpdateSubjectAsync(Guid id, string code, string name, Guid? academicTermId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Mã môn học và tên môn học không được để trống.";
                return RedirectToPage(new { tab = "subjects" });
            }
            if (!academicTermId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn học kỳ cho môn học.";
                return RedirectToPage(new { tab = "subjects" });
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
            return RedirectToPage(new { tab = "subjects" });
        }

        public async Task<IActionResult> OnPostDeleteSubjectAsync(Guid id, CancellationToken cancellationToken)
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
            return RedirectToPage(new { tab = "subjects" });
        }

        public async Task<IActionResult> OnPostCreateDocumentTypeAsync(string name, string? description, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Tên loại học liệu không được để trống.";
                return RedirectToPage(new { tab = "documenttypes" });
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
            return RedirectToPage(new { tab = "documenttypes" });
        }

        public async Task<IActionResult> OnPostUpdateDocumentTypeAsync(Guid id, string name, string? description, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Tên loại học liệu không được để trống.";
                return RedirectToPage(new { tab = "documenttypes" });
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
            return RedirectToPage(new { tab = "documenttypes" });
        }

        public async Task<IActionResult> OnPostDeleteDocumentTypeAsync(Guid id, CancellationToken cancellationToken)
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
            return RedirectToPage(new { tab = "documenttypes" });
        }

        public async Task<IActionResult> OnPostCreateLanguageAsync(string code, string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Mã ngôn ngữ và tên ngôn ngữ không được để trống.";
                return RedirectToPage(new { tab = "languages" });
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
            return RedirectToPage(new { tab = "languages" });
        }

        public async Task<IActionResult> OnPostUpdateLanguageAsync(Guid id, string code, string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Mã ngôn ngữ và tên ngôn ngữ không được để trống.";
                return RedirectToPage(new { tab = "languages" });
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
            return RedirectToPage(new { tab = "languages" });
        }

        public async Task<IActionResult> OnPostDeleteLanguageAsync(Guid id, CancellationToken cancellationToken)
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
            return RedirectToPage(new { tab = "languages" });
        }

        public async Task<IActionResult> OnPostCreateDocumentSourceAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Tên nguồn tài liệu không được để trống.";
                return RedirectToPage(new { tab = "documentsources" });
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
            return RedirectToPage(new { tab = "documentsources" });
        }

        public async Task<IActionResult> OnPostUpdateDocumentSourceAsync(Guid id, string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Tên nguồn tài liệu không được để trống.";
                return RedirectToPage(new { tab = "documentsources" });
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
            return RedirectToPage(new { tab = "documentsources" });
        }

        public async Task<IActionResult> OnPostDeleteDocumentSourceAsync(Guid id, CancellationToken cancellationToken)
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
            return RedirectToPage(new { tab = "documentsources" });
        }

        public async Task<IActionResult> OnPostCreateAcademicTermAsync(string name, int order, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Tên học kỳ không được để trống.";
                return RedirectToPage(new { tab = "academicterms" });
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
            return RedirectToPage(new { tab = "academicterms" });
        }

        public async Task<IActionResult> OnPostUpdateAcademicTermAsync(Guid id, string name, int order, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Tên học kỳ không được để trống.";
                return RedirectToPage(new { tab = "academicterms" });
            }
            try
            {
                var result = await _documentService.UpdateAcademicTermAsync(id, name, order, cancellationToken);
                if (result != null)
                    TempData["SuccessMessage"] = $"Đã cập nhật học kỳ '{name}' thành công.";
                else
                    TempData["ErrorMessage"] = "Không tìm thấy học kỳ này.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage(new { tab = "academicterms" });
        }

        public async Task<IActionResult> OnPostDeleteAcademicTermAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _documentService.DeleteAcademicTermAsync(id, cancellationToken);
                if (success)
                    TempData["SuccessMessage"] = "Đã xóa học kỳ thành công.";
                else
                    TempData["ErrorMessage"] = "Không tìm thấy học kỳ này.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting academic term {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa học kỳ. Đảm bảo dữ liệu không bị ràng buộc.";
            }
            return RedirectToPage(new { tab = "academicterms" });
        }
    }
}
