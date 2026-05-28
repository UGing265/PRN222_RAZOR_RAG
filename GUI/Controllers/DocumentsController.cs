using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using GUI.Models.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
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

    [HttpGet]
    [Authorize(Roles = "Admin,Lecturer")]
    public IActionResult Create()
    {
        return View(new DocumentCreateViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Lecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
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
                return View(model);
            }

            var documentInput = new DocumentCreateInput
            {
                OwnerUserId = ownerUserId,
                Title = model.Title,
                Description = model.Description,
                Subject = model.Subject,
                School = model.School,
                Department = model.Department,
                Language = model.Language,
                Visibility = model.Visibility,
                SourceType = model.SourceType,
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
                    redirectUrl = Url.RouteUrl("dashboard")
                });
            }

            return RedirectToRoute("dashboard")!;
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating document");
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo tài liệu. Vui lòng thử lại.");
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

            var document = await _documentService.GetDocumentBySlugAsync(slug, userId, cancellationToken);
            if (document is null)
            {
                return NotFound();
            }

            var documentWithFiles = await _documentService.GetDocumentWithFilesBySlugAsync(slug, cancellationToken);
            if (documentWithFiles is null)
            {
                return NotFound();
            }

            var documentDetails = await _documentService.GetDocumentDetailsAsync(documentWithFiles.Id, chunkPage, chunkPageSize, cancellationToken);
            if (documentDetails is null)
            {
                return NotFound();
            }

            var viewModel = new DocumentDetailsViewModel
            {
                Id = documentDetails.Id,
                Slug = slug,
                Title = documentDetails.Title,
                Subject = documentDetails.Subject,
                School = documentDetails.School,
                Department = documentDetails.Department,
                Visibility = documentDetails.Visibility,
                Language = documentDetails.Language,
                Description = documentDetails.Description,
                Status = documentDetails.Status,
                TotalChunks = documentDetails.TotalChunks,
                TotalChapters = documentDetails.TotalChapters,
                ApprovedAt = documentDetails.ApprovedAt,
                FileCount = documentDetails.FileCount,
                ChunkPage = chunkPage,
                ChunkPageSize = chunkPageSize,
                TotalChunkPages = Math.Max(1, (int)Math.Ceiling(documentDetails.Chunks.Count / (double)Math.Clamp(chunkPageSize, 8, 10))),
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
            return StatusCode(500, "Không thể tải chi tiết tài liệu.");
        }
    }

    [HttpGet("{slug}/delete")]
    [Authorize(Roles = "Admin,Lecturer")]
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
    [Authorize(Roles = "Admin,Lecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string slug, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var canDelete = await _documentService.GetOwnedDocumentBySlugAsync(slug, userId, cancellationToken);
        if (canDelete is null)
        {
            return NotFound();
        }

        await _documentService.DeleteDocumentAsync(canDelete.Id, cancellationToken);

        TempData["SuccessMessage"] = "Đã xoá tài liệu.";
        return RedirectToAction(nameof(MyDocuments));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin,Lecturer,Student")]
    public async Task<IActionResult> AllDocuments(string? q = null, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId) ? parsedUserId : (Guid?)null;
            var result = await _documentService.GetAllDocumentsAsync(q, page, pageSize, userId, cancellationToken);

            var viewModel = new AllDocumentsViewModel
            {
                Documents = result.Documents.Select(x => new DocumentListItemViewModel
                {
                    Id = x.Id,
                    Slug = x.Slug,
                    Title = x.Title,
                    Subject = x.Subject,
                    Status = x.Status,
                    Visibility = x.Visibility,
                    School = x.School,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    FileCount = x.FileCount,
                    ChunkCount = x.ChunkCount,
                    PreviewText = x.PreviewText,
                    OwnerEmail = x.OwnerEmail
                }).ToList(),
                TotalDocuments = result.TotalDocuments,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                Query = q
            };

            ViewBag.Query = q;
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
    [Authorize(Roles = "Lecturer,Student")]
    public async Task<IActionResult> MyDocuments(string? q = null, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _documentService.GetMyDocumentsAsync(userId, q, page, pageSize, cancellationToken);

            var viewModel = new MyDocumentsViewModel
            {
                Documents = result.Documents.Select(x => new DocumentListItemViewModel
                {
                    Id = x.Id,
                    Slug = x.Slug,
                    Title = x.Title,
                    Subject = x.Subject,
                    Status = x.Status,
                    Visibility = x.Visibility,
                    School = x.School,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    FileCount = x.FileCount,
                    ChunkCount = x.ChunkCount,
                    PreviewText = x.PreviewText,
                    OwnerEmail = x.OwnerEmail
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
