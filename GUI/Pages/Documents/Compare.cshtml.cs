using BLL.DTOs.Chat;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GUI.Pages.Documents;

[Authorize]
public class CompareModel : PageModel
{
    private readonly ICompareService _compareService;
    private readonly IDocumentService _documentService;

    public CompareModel(ICompareService compareService, IDocumentService documentService)
    {
        _compareService = compareService;
        _documentService = documentService;
    }

    // --- Legacy Mode Properties ---
    [BindProperty]
    public IFormFile? File1 { get; set; }

    [BindProperty]
    public IFormFile? File2 { get; set; }

    public CompareResult? LegacyResult { get; set; }

    // --- DB Mode Properties ---
    [BindProperty]
    public List<Guid> SelectedDocumentIds { get; set; } = new();

    [BindProperty]
    public string? Question { get; set; }

    public ComparisonResultDto? DbResult { get; set; }

    public List<SelectListItem> AvailableDocuments { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDocumentsAsync();
    }

    public async Task<IActionResult> OnPostLegacyAsync()
    {
        await LoadDocumentsAsync(); // Reload UI

        if (File1 == null || File2 == null)
        {
            ErrorMessage = "Vui lòng chọn cả hai tệp để so sánh.";
            return Page();
        }

        var ext1 = System.IO.Path.GetExtension(File1.FileName).ToLowerInvariant();
        var ext2 = System.IO.Path.GetExtension(File2.FileName).ToLowerInvariant();

        var allowedExtensions = new[] { ".txt", ".md", ".pdf", ".docx", ".pptx" };
        if (!allowedExtensions.Contains(ext1) || !allowedExtensions.Contains(ext2))
        {
            ErrorMessage = "Định dạng tệp không được hỗ trợ. Vui lòng chọn .txt, .md, .pdf, .docx, .pptx.";
            return Page();
        }

        try
        {
            LegacyResult = await _compareService.CompareFilesAsync(File1, File2);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Đã xảy ra lỗi trong quá trình so sánh: " + ex.Message;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDbAsync()
    {
        await LoadDocumentsAsync(); // Reload UI

        if (SelectedDocumentIds == null || SelectedDocumentIds.Count < 2)
        {
            ErrorMessage = "Vui lòng chọn ít nhất 2 tài liệu từ hệ thống để so sánh.";
            return Page();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            DbResult = await _compareService.CompareDocumentsAsync(SelectedDocumentIds, Question, userId);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Đã xảy ra lỗi trong quá trình phân tích: " + ex.Message;
        }

        return Page();
    }

    private async Task LoadDocumentsAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var myDocs = await _documentService.GetMyDocumentsAsync(userId, null, null, null, null, null, null, null, 1, 100);
            AvailableDocuments = myDocs.Documents
                .Where(d => d.Status == "completed" || d.Status == "approved")
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Title
                }).ToList();
        }
    }
}
