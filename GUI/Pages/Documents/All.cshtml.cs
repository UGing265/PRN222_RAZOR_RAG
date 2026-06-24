using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Documents;

[Authorize(Roles = "Lecturer,Student")]
public class AllModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<AllModel> _logger;

    public AllModel(IDocumentService documentService, ILogger<AllModel> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [BindProperty(Name = "q", SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(Name = "subjectId", SupportsGet = true)]
    public Guid? SubjectId { get; set; }

    [BindProperty(Name = "termId", SupportsGet = true)]
    public Guid? TermId { get; set; }

    [BindProperty(Name = "sortBy", SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(Name = "documentTypeId", SupportsGet = true)]
    public Guid? DocumentTypeId { get; set; }

    [BindProperty(Name = "languageId", SupportsGet = true)]
    public Guid? LanguageId { get; set; }

    [BindProperty(Name = "documentSourceId", SupportsGet = true)]
    public Guid? DocumentSourceId { get; set; }

    [BindProperty(Name = "page", SupportsGet = true)]
    public int PageNum { get; set; } = 1;

    [BindProperty(Name = "isBookmarked", SupportsGet = true)]
    public bool? IsBookmarked { get; set; }

    public AllDocumentsViewModel ViewModel { get; set; } = new();

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();
    public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public List<DocumentSourceDto> DocumentSources { get; set; } = new();
    public string? SelectedSubjectName { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : (Guid?)null;
            var result = await _documentService.GetAllDocumentsAsync(
                Q, SubjectId, PageNum, 6, userId, SortBy, DocumentTypeId, LanguageId, DocumentSourceId, IsBookmarked, cancellationToken);

            ViewModel = new AllDocumentsViewModel
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
                    ViewCount = x.ViewCount,
                    IsBookmarked = x.IsBookmarked
                }).ToList(),
                TotalDocuments = result.TotalDocuments,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                Query = Q,
                SortBy = SortBy
            };

            var allSubjects = await _documentService.GetSubjectsAsync(cancellationToken);
            Subjects = allSubjects;

            var allTerms = await _documentService.GetAcademicTermsAsync(cancellationToken);
            AcademicTerms = allTerms.ToList();

            DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
            Languages = await _documentService.GetLanguagesAsync(cancellationToken);
            DocumentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);

            if (SubjectId.HasValue)
            {
                var selectedSub = allSubjects.FirstOrDefault(x => x.Id == SubjectId.Value);
                SelectedSubjectName = selectedSub?.Code;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading all documents for user");
            TempData["ErrorMessage"] = "Không thể tải danh sách tài liệu.";
            return RedirectToPage("/Index");
        }
    }
}
