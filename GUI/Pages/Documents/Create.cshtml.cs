using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GUI.Pages.Documents;

[Authorize(Roles = "Lecturer")]
[RequestSizeLimit(104857600)] // 100 MB
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

    public async Task<IActionResult> OnGetRefreshSubjectsAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var subjects = await _documentService.GetSubjectsAssignedToLecturerAsync(userId, cancellationToken);
        var documentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        var languages = await _documentService.GetLanguagesAsync(cancellationToken);
        var documentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);
        
        var terms = await _documentService.GetAcademicTermsAsync(cancellationToken);
        var subjectTermIds = subjects.Where(s => s.AcademicTermId.HasValue).Select(s => s.AcademicTermId.Value).ToHashSet();
        var academicTerms = terms.Where(t => subjectTermIds.Contains(t.Id)).ToList();

        var subjectTermMap = subjects.Where(s => s.AcademicTermId.HasValue)
            .ToDictionary(s => s.Id.ToString().ToLowerInvariant(), s => s.AcademicTermId.Value.ToString().ToLowerInvariant());

        return new JsonResult(new
        {
            subjects = subjects.Select(s => new { id = s.Id.ToString().ToLowerInvariant(), code = s.Code, termId = s.AcademicTermId?.ToString().ToLowerInvariant() }),
            academicTerms = academicTerms.Select(t => new { id = t.Id.ToString().ToLowerInvariant(), name = t.Name }),
            documentTypes = documentTypes.Select(dt => new { id = dt.Id.ToString().ToLowerInvariant(), name = dt.Name }),
            languages = languages.Select(l => new { id = l.Id.ToString().ToLowerInvariant(), name = l.Name }),
            documentSources = documentSources.Select(ds => new { id = ds.Id.ToString().ToLowerInvariant(), name = ds.Name }),
            subjectTermMap = subjectTermMap
        });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerUserId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            if (Request.Headers.TryGetValue("X-Requested-With", out var xrw1) && xrw1 == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(e => !string.IsNullOrEmpty(e)).ToList();
                return new JsonResult(new { success = false, errors });
            }
            await PopulateLookupsAsync(cancellationToken, ownerUserId);
            return Page();
        }

        if (Input.SubjectId.HasValue)
        {
            var isAssigned = await _documentService.IsSubjectAssignedToLecturerAsync(ownerUserId, Input.SubjectId.Value, cancellationToken);
            if (!isAssigned)
            {
                if (Request.Headers.TryGetValue("X-Requested-With", out var xrw2) && xrw2 == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, errors = new[] { "Bạn không có quyền upload học liệu cho môn học này." } });
                }
                TempData["ErrorMessage"] = "Bạn không có quyền upload học liệu cho môn học này.";
                await PopulateLookupsAsync(cancellationToken, ownerUserId);
                return Page();
            }
        }

        try
        {
            const long maxFileSize = 104857600; // 100 MB

            if (Input.UploadFile is null || Input.UploadFile.Length == 0)
            {
                if (Request.Headers.TryGetValue("X-Requested-With", out var xrw3) && xrw3 == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, errors = new[] { "Vui lòng chọn file để tạo tài liệu." } });
                }
                TempData["ErrorMessage"] = "Vui lòng chọn file để tạo tài liệu. Không có file thì sẽ không tạo document.";
                await PopulateLookupsAsync(cancellationToken, ownerUserId);
                return Page();
            }

            if (Input.UploadFile.Length > maxFileSize)
            {
                var sizeMb = Input.UploadFile.Length / 1024.0 / 1024.0;
                var errors = new[] { $"File '{Input.UploadFile.FileName}' ({sizeMb:0.0} MB) vượt quá giới hạn 100 MB. Vui lòng chọn file nhỏ hơn." };
                if (Request.Headers.TryGetValue("X-Requested-With", out var xrw) && xrw == "XMLHttpRequest")
                    return new JsonResult(new { success = false, errors });
                TempData["ErrorMessage"] = errors[0];
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
            if (Request.Headers.TryGetValue("X-Requested-With", out var xrw4) && xrw4 == "XMLHttpRequest")
            {
                return new JsonResult(new { success = false, errors = new[] { ex.Message } });
            }
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateLookupsAsync(cancellationToken, ownerUserId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating document");
            if (Request.Headers.TryGetValue("X-Requested-With", out var xrw5) && xrw5 == "XMLHttpRequest")
            {
                return new JsonResult(new { success = false, errors = new[] { "Có lỗi xảy ra khi tạo tài liệu. Vui lòng thử lại." } });
            }
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo tài liệu. Vui lòng thử lại.");
            await PopulateLookupsAsync(cancellationToken, ownerUserId);
            return Page();
        }
    }
}
