using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GUI.Pages.Documents;

[Authorize(Roles = "Lecturer")]
public class CreateModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IDocumentService documentService, ILogger<CreateModel> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [BindProperty]
    public DocumentCreateViewModel Input { get; set; } = new();

    public List<SubjectDto> Subjects { get; set; } = new();
    public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public List<DocumentSourceDto> DocumentSources { get; set; } = new();
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();
    public string SubjectTermMapJson { get; set; } = "{}";

    private async Task PopulateLookupsAsync(CancellationToken cancellationToken, Guid? userId = null)
    {
        if (userId.HasValue && User.IsInRole("Lecturer"))
        {
            Subjects = await _documentService.GetSubjectsAssignedToLecturerAsync(userId.Value, cancellationToken);
        }
        else
        {
            Subjects = await _documentService.GetSubjectsAsync(cancellationToken);
        }

        DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        Languages = await _documentService.GetLanguagesAsync(cancellationToken);
        DocumentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);
        var terms = await _documentService.GetAcademicTermsAsync(cancellationToken);
        var subjectTermIds = Subjects.Where(s => s.AcademicTermId.HasValue).Select(s => s.AcademicTermId.Value).ToHashSet();
        AcademicTerms = terms.Where(t => subjectTermIds.Contains(t.Id)).ToList();
        SubjectTermMapJson = System.Text.Json.JsonSerializer.Serialize(
            Subjects.Where(s => s.AcademicTermId.HasValue)
                    .ToDictionary(s => s.Id.ToString().ToLowerInvariant(), s => s.AcademicTermId.Value.ToString().ToLowerInvariant())
        );
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await PopulateLookupsAsync(cancellationToken, userId);
        }
        else
        {
            await PopulateLookupsAsync(cancellationToken);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerUserId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(cancellationToken, ownerUserId);
            return Page();
        }

        if (Input.SubjectId.HasValue)
        {
            var isAssigned = await _documentService.IsSubjectAssignedToLecturerAsync(ownerUserId, Input.SubjectId.Value, cancellationToken);
            if (!isAssigned)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền upload học liệu cho môn học này.";
                await PopulateLookupsAsync(cancellationToken, ownerUserId);
                return Page();
            }
        }

        try
        {
            if (Input.UploadFile is null || Input.UploadFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file để tạo tài liệu. Không có file thì sẽ không tạo document.";
                await PopulateLookupsAsync(cancellationToken, ownerUserId);
                return Page();
            }

            var documentInput = new DocumentCreateInput
            {
                OwnerUserId = ownerUserId,
                Title = Input.Title,
                Description = Input.Description,
                SubjectId = Input.SubjectId,
                DocumentTypeId = Input.DocumentTypeId,
                AcademicTermId = Input.AcademicTermId,
                LanguageId = Input.LanguageId,
                Visibility = Input.Visibility,
                DocumentSourceId = Input.DocumentSourceId,
                FileName = Input.UploadFile.FileName,
                FileSizeBytes = Input.UploadFile.Length,
                FileContentType = Input.UploadFile.ContentType
            };

            var savedDocument = await _documentService.CreateDocumentAsync(documentInput, Input.UploadFile, cancellationToken);
            var s3Result = await _documentService.UploadOriginalFileToS3Async(savedDocument.Id, Input.UploadFile, cancellationToken);

            await _documentService.EnqueueUploadJobAsync(ownerUserId, savedDocument.Id, Input.UploadFile.FileName, s3Result.Key, Input.UploadFile.Length, cancellationToken);

            TempData["SuccessMessage"] = "Upload đã được đưa vào hàng đợi xử lý nền.";

            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "XMLHttpRequest")
            {
                return new JsonResult(new
                {
                    success = true,
                    redirectUrl = Url.Page("/Documents/Mine")
                });
            }

            return RedirectToPage("/Documents/Mine");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateLookupsAsync(cancellationToken, ownerUserId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating document");
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo tài liệu. Vui lòng thử lại.");
            await PopulateLookupsAsync(cancellationToken, ownerUserId);
            return Page();
        }
    }
}
