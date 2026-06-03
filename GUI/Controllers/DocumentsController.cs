using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using GUI.Models.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GUI.Controllers;

[Authorize]
[Route("documents")]
public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    private async Task PopulateLookupsAsync(CancellationToken cancellationToken)
    {

        var subjects = await _documentService.GetSubjectsAsync(cancellationToken);
        ViewBag.Subjects = subjects;
        ViewBag.DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        ViewBag.Languages = await _documentService.GetLanguagesAsync(cancellationToken);
        ViewBag.DocumentSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);
        ViewBag.AcademicTerms = await _documentService.GetAcademicTermsAsync(cancellationToken);
        ViewBag.SubjectTermMap = System.Text.Json.JsonSerializer.Serialize(subjects.Where(s => s.AcademicTermId.HasValue).ToDictionary(s => s.Id.ToString().ToLowerInvariant(), s => s.AcademicTermId.Value.ToString().ToLowerInvariant()));
    }

    [HttpGet]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateLookupsAsync(cancellationToken);
        return View(new DocumentCreateViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Lecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(CancellationToken.None);
            return View(model);
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerUserId))
        {
            return Unauthorized();
        }

        try
        {
            if (model.UploadFile is null || model.UploadFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file để tạo tài liệu. Không có file thì sẽ không tạo document.";
                await PopulateLookupsAsync(CancellationToken.None);
                return View(model);
            }

            var documentInput = new DocumentCreateInput
            {
                OwnerUserId = ownerUserId,
                Title = model.Title,
                Description = model.Description,
                SubjectId = model.SubjectId,

                DocumentTypeId = model.DocumentTypeId,
                AcademicTermId = model.AcademicTermId,
                LanguageId = model.LanguageId,
                Visibility = model.Visibility,
                DocumentSourceId = model.DocumentSourceId,
                FileName = model.UploadFile.FileName,
                FileSizeBytes = model.UploadFile.Length,
                FileContentType = model.UploadFile.ContentType
            };

            var savedDocument = await _documentService.CreateDocumentAsync(documentInput, model.UploadFile, CancellationToken.None);
            var s3Result = await _documentService.UploadOriginalFileToS3Async(savedDocument.Id, model.UploadFile, CancellationToken.None);

            await _documentService.EnqueueUploadJobAsync(ownerUserId, savedDocument.Id, model.UploadFile.FileName, s3Result.Key, model.UploadFile.Length, CancellationToken.None);

            TempData["SuccessMessage"] = "Upload đã được đưa vào hàng đợi xử lý nền.";

            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action(nameof(MyDocuments), "Documents")
                });
            }

            return RedirectToAction(nameof(MyDocuments));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateLookupsAsync(CancellationToken.None);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating document");
            ModelState.AddModelError(string.Empty, "CÃ³ lá»—i xáº£y ra khi táº¡o tÃ i liá»‡u. Vui lÃ²ng thá»­ láº¡i.");
            await PopulateLookupsAsync(CancellationToken.None);
            return View(model);
        }
    }

    [HttpGet("{slug}")]
    [Authorize(Roles = "Admin,Lecturer,Student")]
    public async Task<IActionResult> Details(string slug, int chunkPage = 1, int chunkPageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            string cookieKey = $"ViewedDoc_{userId}_{slug}";
            bool hasViewed = Request.Cookies.ContainsKey(cookieKey);
            bool isAdmin = User.IsInRole("Admin");

            var documentDetails = await _documentService.GetDocumentDetailsBySlugAsync(slug, userId, chunkPage, chunkPageSize, !hasViewed && chunkPage == 1, isAdmin, cancellationToken);
            if (documentDetails is null)
            {
                return NotFound();
            }

            if (!hasViewed && documentDetails.OwnerUserId != userId)
            {
                var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(24),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
                };
                Response.Cookies.Append(cookieKey, "1", cookieOptions);
            }

            var viewModel = new DocumentDetailsViewModel
            {
                Id = documentDetails.Id,
                OwnerUserId = documentDetails.OwnerUserId,
                Slug = slug,
                Title = documentDetails.Title,
                SubjectName = documentDetails.SubjectName,
                SubjectCode = documentDetails.SubjectCode,


                DocumentTypeId = documentDetails.DocumentTypeId,
                DocumentTypeName = documentDetails.DocumentTypeName,
                AcademicTermName = documentDetails.AcademicTermName,
                Visibility = documentDetails.Visibility,
                LanguageId = documentDetails.LanguageId,
                LanguageCode = documentDetails.LanguageCode,
                LanguageName = documentDetails.LanguageName,
                DocumentSourceName = documentDetails.DocumentSourceName,
                Description = documentDetails.Description,
                Status = documentDetails.Status,
                TotalChunks = documentDetails.TotalChunks,
                TotalChapters = documentDetails.TotalChapters,
                ViewCount = documentDetails.ViewCount,
                DownloadCount = documentDetails.DownloadCount,
                ApprovedAt = documentDetails.ApprovedAt,
                FileCount = documentDetails.FileCount,
                ChunkPage = chunkPage,
                ChunkPageSize = chunkPageSize,
                TotalChunkPages = Math.Max(1, (int)Math.Ceiling(documentDetails.TotalChunks / (double)Math.Clamp(chunkPageSize, 8, 10))),
                Files = documentDetails.Files.Select(file => new DocumentFileViewModel
                {
                    Id = file.Id,
                    OriginalFilename = file.OriginalFilename,
                    MimeType = file.MimeType,
                    FileSizeBytes = file.FileSizeBytes,
                    ExtractionStatus = file.ExtractionStatus,
                    CreatedAt = file.CreatedAt
                }).ToList(),
                Chapters = documentDetails.Chapters.Select(x => new DocumentChapterViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Summary = x.Summary,
                    ChapterOrder = x.ChapterOrder,
                    StartChunkIndex = x.StartChunkIndex ?? 0,
                    EndChunkIndex = x.EndChunkIndex ?? 0,
                    IsAiGenerated = x.IsAiGenerated,
                    ConfidenceScore = x.ConfidenceScore
                }).ToList(),
                Chunks = documentDetails.Chunks.Select(x => new DocumentChunkViewModel
                {
                    ChunkOrder = x.ChunkOrder,
                    Content = x.Content,
                    WordCount = x.Content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
                    ChunkHash = x.ChunkHash
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading document details for {Slug}", slug);
            return StatusCode(500, "KhÃ´ng thá»ƒ táº£i chi tiáº¿t tÃ i liá»‡u.");
        }
    }

    [HttpGet("{slug}/delete")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Delete(string slug, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var viewData = await _documentService.GetDeleteDocumentViewDataBySlugAsync(slug, userId, cancellationToken);
        if (viewData is null)
        {
            return NotFound();
        }

        var viewModel = new DeleteDocumentViewModel
        {
            Id = viewData.Id,
            Slug = slug,
            Title = viewData.Title,
            FileCount = viewData.FileCount,
            ChunkCount = viewData.ChunkCount
        };

        return View(viewModel);
    }

    [HttpPost("{slug}/delete")]
    [Authorize(Roles = "Lecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string slug, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var canDelete = await _documentService.GetOwnedDocumentDetailsBySlugAsync(slug, userId, cancellationToken);
        if (canDelete is null)
        {
            return NotFound();
        }

        await _documentService.DeleteDocumentAsync(canDelete.Id, cancellationToken);

        TempData["SuccessMessage"] = "Đã xoá tài liệu.";
        return RedirectToAction(nameof(MyDocuments));
    }

    [HttpGet("{slug}/edit")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Edit(string slug, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var document = await _documentService.GetOwnedDocumentDetailsBySlugAsync(slug, userId, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        await PopulateLookupsAsync(cancellationToken);

        var viewModel = new DocumentEditViewModel
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

        return View(viewModel);
    }

    [HttpPost("{slug}/edit")]
    [Authorize(Roles = "Lecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string slug, DocumentEditViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(cancellationToken);
            return View(model);
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var editInput = new DocumentEditInput
        {
            Title = model.Title,
            Description = model.Description,
            SubjectId = model.SubjectId,

            DocumentTypeId = model.DocumentTypeId,
            AcademicTermId = model.AcademicTermId,
            LanguageId = model.LanguageId,
            Visibility = model.Visibility,
            DocumentSourceId = model.DocumentSourceId
        };

        try
        {
            await _documentService.UpdateDocumentAsync(model.Id, userId, editInput, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Đã cập nhật thông tin tài liệu.";
        return RedirectToAction(nameof(MyDocuments));
    }

    [HttpPost("{slug}/report")]
    [Authorize(Roles = "Lecturer,Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(string slug, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Lý do báo cáo không được để trống.";
            return RedirectToAction(nameof(Details), new { slug });
        }
        
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var document = await _documentService.GetDocumentDetailsBySlugAsync(slug, userId, 1, 10, false, false, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        try
        {
            await _documentService.ReportDocumentAsync(document.Id, userId, reason, cancellationToken);
            TempData["SuccessMessage"] = "Báo cáo tài liệu thành công. Ban quản trị sẽ sớm xử lý.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting document {Slug}", slug);
            TempData["ErrorMessage"] = "Không thể gửi báo cáo: " + ex.Message;
        }

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpGet("all")]
    [Authorize(Roles = "Lecturer,Student")]
    public async Task<IActionResult> AllDocuments(string? q = null, Guid? subjectId = null, Guid? termId = null, string? sortBy = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : (Guid?)null;
            var result = await _documentService.GetAllDocumentsAsync(q, subjectId, page, pageSize, userId, sortBy, documentTypeId, languageId, documentSourceId, cancellationToken);

            var viewModel = new AllDocumentsViewModel
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
                Query = q,
                SortBy = sortBy
            };

            ViewBag.Query = q;
            ViewBag.SelectedSubjectId = subjectId;
            ViewBag.SelectedTermId = termId;
            ViewBag.SelectedSortBy = sortBy;
            ViewBag.SelectedTypeId = documentTypeId;
            ViewBag.SelectedLangId = languageId;
            ViewBag.SelectedSourceId = documentSourceId;

            var allSubjects = await _documentService.GetSubjectsAsync(cancellationToken);
            ViewBag.Subjects = allSubjects;
            var allTerms = await _documentService.GetAcademicTermsAsync(cancellationToken);
            ViewBag.AcademicTerms = allTerms;
            var allTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
            ViewBag.DocumentTypes = allTypes;
            var allLangs = await _documentService.GetLanguagesAsync(cancellationToken);
            ViewBag.Languages = allLangs;
            var allSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);
            ViewBag.DocumentSources = allSources;
            if (subjectId.HasValue)
            {
                var selectedSub = allSubjects.FirstOrDefault(x => x.Id == subjectId.Value);
                ViewBag.SelectedSubjectName = selectedSub?.Code;
            }
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading all documents for user");
            TempData["ErrorMessage"] = "Không thể tải danh sách tài liệu.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> MyDocuments(string? q = null, Guid? subjectId = null, Guid? termId = null, string? sortBy = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _documentService.GetMyDocumentsAsync(userId, q, subjectId, termId, sortBy, documentTypeId, languageId, documentSourceId, page, pageSize, cancellationToken);

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
                PendingDocuments = result.PendingDocuments,
                ApprovedDocuments = result.ApprovedDocuments,
                RejectedDocuments = result.RejectedDocuments,
                TotalFiles = result.TotalFiles,
                TotalChunks = result.TotalChunks,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            viewModel.ActiveUploadJobs = result.ActiveUploadJobs.Select(x => new UploadJobViewModel
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

            ViewBag.Query = q;
            ViewBag.SelectedSubjectId = subjectId;
            ViewBag.SelectedTermId = termId;
            ViewBag.SelectedSortBy = sortBy;
            ViewBag.SelectedTypeId = documentTypeId;
            ViewBag.SelectedLangId = languageId;
            ViewBag.SelectedSourceId = documentSourceId;

            var allSubjects = await _documentService.GetSubjectsByOwnerAsync(userId, cancellationToken);
            ViewBag.Subjects = allSubjects;
            var allTerms = await _documentService.GetAcademicTermsAsync(cancellationToken);
            ViewBag.AcademicTerms = allTerms;
            var allTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
            ViewBag.DocumentTypes = allTypes;
            var allLangs = await _documentService.GetLanguagesAsync(cancellationToken);
            ViewBag.Languages = allLangs;
            var allSources = await _documentService.GetDocumentSourcesAsync(cancellationToken);
            ViewBag.DocumentSources = allSources;
            
            if (subjectId.HasValue)
            {
                var selectedSub = allSubjects.FirstOrDefault(x => x.Id == subjectId.Value);
                ViewBag.SelectedSubjectName = selectedSub?.Code;
            }

            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "XMLHttpRequest")
            {
                return PartialView("_UploadJobs", viewModel.ActiveUploadJobs);
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading my documents for user {UserId}", userId);
            TempData["ErrorMessage"] = "Không thể tải danh sách tài liệu của bạn.";
            return RedirectToAction("Index", "Home");
        }
    }
}
