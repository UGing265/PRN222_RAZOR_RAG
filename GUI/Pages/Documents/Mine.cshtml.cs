using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Documents;

[Authorize(Roles = "Lecturer")]
public class MineModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<MineModel> _logger;

    public MineModel(IDocumentService documentService, ILogger<MineModel> logger)
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

    public MyDocumentsViewModel ViewModel { get; set; } = new();

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();
    public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public List<DocumentSourceDto> DocumentSources { get; set; } = new();
    public string? SelectedSubjectName { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _documentService.GetMyDocumentsAsync(
                userId, Q, SubjectId, TermId, SortBy, DocumentTypeId, LanguageId, DocumentSourceId, PageNum, 6, cancellationToken);

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
                PendingDocuments = result.PendingDocuments,
                ApprovedDocuments = result.ApprovedDocuments,
                RejectedDocuments = result.RejectedDocuments,
                TotalFiles = result.TotalFiles,
                TotalChunks = result.TotalChunks,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            ViewModel.ActiveUploadJobs = result.ActiveUploadJobs.Select(x => new UploadJobViewModel
            {
                Id = x.Id,
                DocumentId = x.DocumentId,
                FileName = x.FileName,
                FileSizeBytes = x.FileSizeBytes,
                Status = x.Status,
                ProgressPercent = x.ProgressPercent,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList();

            var allSubjects = await _documentService.GetSubjectsByOwnerAsync(userId, cancellationToken);
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

            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "XMLHttpRequest")
            {
                return Partial("_UploadJobs", ViewModel.ActiveUploadJobs);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading my documents for user {UserId}", userId);
            TempData["ErrorMessage"] = "Không thể tải danh sách tài liệu của bạn.";
            return RedirectToPage("/Index");
        }
    }
}
