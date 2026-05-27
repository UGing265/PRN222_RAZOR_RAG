using BLL.Interfaces.Documents;
using DAL.Entities;
using GUI.Models.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GUI.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly DAL.Data.DBContext _dbContext;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, DAL.Data.DBContext dbContext, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new DocumentCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentCreateViewModel model, CancellationToken cancellationToken)
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

            var createdDocument = new Document
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                Title = model.Title,
                Description = model.Description,
                Subject = model.Subject,
                School = model.School,
                Department = model.Department,
                Language = model.Language,
                Visibility = model.Visibility,
                SourceType = model.SourceType,
                Status = "approved",
                TotalChunks = 0,
                TotalChapters = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow
            };

            var savedDocument = await _documentService.CreateDocumentAsync(createdDocument, cancellationToken);
            var s3Result = await _documentService.UploadOriginalFileToS3Async(savedDocument.Id, model.UploadFile, cancellationToken);

            var uploadJob = new UploadJob
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                DocumentId = savedDocument.Id,
                StoragePath = s3Result.Key,
                FileName = model.UploadFile.FileName,
                FileSizeBytes = model.UploadFile.Length,
                Status = "pending",
                ProgressPercent = 0,
                Message = "Đang chờ xử lý",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.UploadJobs.Add(uploadJob);
            await _dbContext.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] = "Upload đã được đưa vào hàng đợi xử lý nền.";

            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.RouteUrl("dashboard")
                });
            }

            return RedirectToRoute("dashboard");
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

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, int chunkPage = 1, int chunkPageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _documentService.GetDocumentWithFilesAsync(id, cancellationToken);
            if (document is null)
            {
                return NotFound();
            }

            var orderedChunks = document.DocumentChunks?.OrderBy(x => x.ChunkOrder).ToList() ?? [];
            var totalChunks = orderedChunks.Count;
            chunkPageSize = Math.Clamp(chunkPageSize, 8, 10);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalChunks / (double)chunkPageSize));
            chunkPage = Math.Clamp(chunkPage, 1, totalPages);
            var pageChunks = orderedChunks.Skip((chunkPage - 1) * chunkPageSize).Take(chunkPageSize).ToList();

            var chapters = document.DocumentChapters?
                .OrderBy(x => x.ChapterOrder)
                .Select(x => new DocumentChapterViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Summary = x.Summary,
                    ChapterOrder = x.ChapterOrder,
                    StartChunkIndex = x.StartChunkIndex ?? 0,
                    EndChunkIndex = x.EndChunkIndex ?? 0,
                    IsAiGenerated = x.IsAiGenerated,
                    ConfidenceScore = x.ConfidenceScore
                })
                .ToList() ?? [];

            var viewModel = new DocumentDetailsViewModel
            {
                Id = document.Id,
                Title = document.Title,
                Subject = document.Subject,
                School = document.School,
                Department = document.Department,
                Visibility = document.Visibility,
                Language = document.Language,
                Description = document.Description,
                Status = document.Status,
                TotalChunks = document.TotalChunks,
                TotalChapters = document.TotalChapters,
                ApprovedAt = document.ApprovedAt,
                FileCount = document.DocumentFiles?.Count ?? 0,
                ChunkPage = chunkPage,
                ChunkPageSize = chunkPageSize,
                TotalChunkPages = totalPages,
                Files = document.DocumentFiles?.ToList() ?? [],
                Chapters = chapters,
                Chunks = pageChunks.Select(x => new DocumentChunkViewModel
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
            _logger.LogError(ex, "Error while loading document details for {DocumentId}", id);
            return StatusCode(500, "Không thể tải chi tiết tài liệu.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var document = await _dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var viewModel = new DeleteDocumentViewModel
        {
            Id = document.Id,
            Title = document.Title,
            FileCount = await _dbContext.DocumentFiles.CountAsync(x => x.DocumentId == id, cancellationToken),
            ChunkCount = await _dbContext.DocumentChunks.CountAsync(x => x.DocumentId == id, cancellationToken)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var document = await _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var jobs = await _dbContext.UploadJobs.Where(x => x.DocumentId == id).ToListAsync(cancellationToken);
        _dbContext.UploadJobs.RemoveRange(jobs);

        await _documentService.DeleteDocumentAssetsAsync(id, cancellationToken);

        var files = await _dbContext.DocumentFiles.Where(x => x.DocumentId == id).ToListAsync(cancellationToken);
        _dbContext.DocumentFiles.RemoveRange(files);

        var chunks = await _dbContext.DocumentChunks.Where(x => x.DocumentId == id).ToListAsync(cancellationToken);
        _dbContext.DocumentChunks.RemoveRange(chunks);

        _dbContext.Documents.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Đã xoá tài liệu.";
        return RedirectToAction(nameof(MyDocuments));
    }

    [HttpGet]
    public async Task<IActionResult> MyDocuments(string? q = null, int page = 1, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        try
        {
            pageSize = Math.Clamp(pageSize, 6, 12);

            var baseQuery = _dbContext.Documents
                .AsNoTracking()
                .Include(x => x.DocumentFiles)
                .Include(x => x.DocumentChunks)
                .Where(x => x.OwnerUserId == userId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                baseQuery = baseQuery.Where(x => x.Title.Contains(q) || (x.Subject != null && x.Subject.Contains(q)) || (x.School != null && x.School.Contains(q)));
            }

            baseQuery = baseQuery.OrderByDescending(x => x.UpdatedAt);

            var totalDocuments = await baseQuery.CountAsync(cancellationToken);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalDocuments / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var documents = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new GUI.Models.Documents.DocumentListItemViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Subject = x.Subject,
                    Status = x.Status,
                    Visibility = x.Visibility,
                    School = x.School,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    FileCount = x.DocumentFiles.Count,
                    ChunkCount = x.DocumentChunks.Count,
                    PreviewText = x.DocumentChunks
                        .OrderBy(c => c.ChunkOrder)
                        .Select(c => c.Content)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var viewModel = new MyDocumentsViewModel
            {
                Documents = documents,
                TotalDocuments = totalDocuments,
                PendingDocuments = await _dbContext.Documents.CountAsync(x => x.OwnerUserId == userId && x.Status == "pending", cancellationToken),
                ApprovedDocuments = await _dbContext.Documents.CountAsync(x => x.OwnerUserId == userId && x.Status == "approved", cancellationToken),
                RejectedDocuments = await _dbContext.Documents.CountAsync(x => x.OwnerUserId == userId && x.Status == "rejected", cancellationToken),
                TotalFiles = await _dbContext.DocumentFiles.CountAsync(x => x.Document.OwnerUserId == userId, cancellationToken),
                TotalChunks = await _dbContext.DocumentChunks.CountAsync(x => x.Document.OwnerUserId == userId, cancellationToken),
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            ViewBag.Query = q;
            ViewBag.ActiveUploadJobs = await _dbContext.UploadJobs.AsNoTracking()
                .Where(x => x.OwnerUserId == userId && x.Status != "done" && x.Status != "failed")
                .OrderByDescending(x => x.UpdatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

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
