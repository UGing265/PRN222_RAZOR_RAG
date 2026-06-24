using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Documents;

[Authorize(Roles = "Lecturer,Student")]
public class DetailsModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IDocumentService documentService, ILogger<DetailsModel> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    public string Slug { get; set; } = string.Empty;

    [BindProperty(Name = "chunkPage", SupportsGet = true)]
    public int ChunkPage { get; set; } = 1;

    [BindProperty(Name = "chunkPageSize", SupportsGet = true)]
    public int ChunkPageSize { get; set; } = 4;

    public DocumentDetailsViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        Slug = slug;

        try
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            string cookieKey = $"ViewedDoc_{userId}_{slug}";
            bool hasViewed = Request.Cookies.ContainsKey(cookieKey);
            bool isAdmin = User.IsInRole("Admin");

            var documentDetails = await _documentService.GetDocumentDetailsBySlugAsync(slug, userId, ChunkPage, ChunkPageSize, !hasViewed && ChunkPage == 1, isAdmin, cancellationToken);
            if (documentDetails is null)
            {
                return NotFound();
            }

            if (!hasViewed && documentDetails.OwnerUserId != userId)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(24),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append(cookieKey, "1", cookieOptions);
            }

            ViewModel = new DocumentDetailsViewModel
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
                IsBookmarked = documentDetails.IsBookmarked,
                ChunkPage = ChunkPage,
                ChunkPageSize = ChunkPageSize,
                TotalChunkPages = Math.Max(1, (int)Math.Ceiling(documentDetails.TotalChunks / (double)Math.Clamp(ChunkPageSize, 4, 10))),
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
                    Content = x.Content ?? string.Empty,
                    WordCount = (x.Content ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
                    ChunkHash = x.ChunkHash,
                    HasEmbedding = x.HasEmbedding
                }).ToList()
            };

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading document details for {Slug}", slug);
            TempData["ErrorMessage"] = "Không thể tải chi tiết tài liệu.";
            return RedirectToPage("/Documents/All");
        }
    }

    public async Task<IActionResult> OnPostReportAsync(string slug, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Lý do báo cáo không được để trống.";
            return RedirectToPage("/Documents/Details", new { slug });
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

        return RedirectToPage("/Documents/Details", new { slug });
    }
}
