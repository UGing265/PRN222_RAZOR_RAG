using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Documents;

[Authorize(Roles = "Lecturer")]
public class EditModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IDocumentService documentService, ILogger<EditModel> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [BindProperty]
    public DocumentEditViewModel EditModelData { get; set; } = new();

    public string Slug { get; set; } = string.Empty;

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();
    public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public List<DocumentSourceDto> DocumentSources { get; set; } = new();
    public string SubjectTermMapJson { get; set; } = "{}";

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        Slug = slug;

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var document = await _documentService.GetOwnedDocumentDetailsBySlugAsync(slug, userId, cancellationToken);
            if (document is null)
            {
                return NotFound();
            }

            EditModelData = new DocumentEditViewModel
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                SubjectId = document.SubjectId,
                DocumentTypeId = document.DocumentTypeId,
                AcademicTermId = document.AcademicTermId,
                LanguageId = document.LanguageId,
                Visibility = document.Visibility,
                DocumentSourceId = document.DocumentSourceId
            };

            await PopulateLookupsAsync(cancellationToken, userId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading document for editing: {Slug}", slug);
            TempData["ErrorMessage"] = "Không thể tải thông tin tài liệu.";
            return RedirectToPage("/Documents/Mine");
        }
    }

    public async Task<IActionResult> OnPostAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        Slug = slug;

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(cancellationToken, userId);
            return Page();
        }

        if (EditModelData.SubjectId.HasValue)
        {
            var isAssigned = await _documentService.IsSubjectAssignedToLecturerAsync(userId, EditModelData.SubjectId.Value, cancellationToken);
            if (!isAssigned)
            {
                ModelState.AddModelError("EditModelData.SubjectId", "Bạn không có quyền quản lý học liệu cho môn học này.");
                TempData["ErrorMessage"] = "Bạn không có quyền quản lý học liệu cho môn học này.";
                await PopulateLookupsAsync(cancellationToken, userId);
                return Page();
            }
        }

        var editInput = new DocumentEditInput
        {
            Title = EditModelData.Title,
            Description = EditModelData.Description,
            SubjectId = EditModelData.SubjectId,
            DocumentTypeId = EditModelData.DocumentTypeId,
            AcademicTermId = EditModelData.AcademicTermId,
            LanguageId = EditModelData.LanguageId,
            Visibility = EditModelData.Visibility,
            DocumentSourceId = EditModelData.DocumentSourceId
        };

        try
        {
            await _documentService.UpdateDocumentAsync(EditModelData.Id, userId, editInput, cancellationToken);
            TempData["SuccessMessage"] = "Đã cập nhật thông tin tài liệu.";
            return RedirectToPage("/Documents/Mine");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document not found or access denied for editing: {DocumentId}", EditModelData.Id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {DocumentId}", EditModelData.Id);
            TempData["ErrorMessage"] = "Không thể cập nhật tài liệu: " + ex.Message;
            await PopulateLookupsAsync(cancellationToken, userId);
            return Page();
        }
    }

    private async Task PopulateLookupsAsync(CancellationToken cancellationToken, Guid userId)
    {
        var allSubjects = await _documentService.GetSubjectsByOwnerAsync(userId, cancellationToken);
        Subjects = allSubjects;

        var allTerms = await _documentService.GetAcademicTermsAsync(cancellationToken);
        var subjectTermIds = allSubjects.Where(s => s.AcademicTermId.HasValue).Select(s => s.AcademicTermId.Value).ToHashSet();
        AcademicTerms = allTerms.Where(t => subjectTermIds.Contains(t.Id)).ToList();

        DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        Languages = await _documentService.GetLanguagesAsync(cancellationToken);
        DocumentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);

        var termMap = allSubjects
            .Where(x => x.AcademicTermId.HasValue)
            .ToDictionary(x => x.Id.ToString(), x => x.AcademicTermId!.Value.ToString());
        SubjectTermMapJson = System.Text.Json.JsonSerializer.Serialize(termMap);
    }
}
