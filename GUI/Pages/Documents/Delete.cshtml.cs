using BLL.Interfaces.Documents;
using BLL.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Documents;

[Authorize(Roles = "Lecturer")]
public class DeleteModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(IDocumentService documentService, INotificationService notificationService, ILogger<DeleteModel> logger)
    {
        _documentService = documentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public DeleteDocumentViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var viewData = await _documentService.GetDeleteDocumentViewDataBySlugAsync(slug, userId, cancellationToken);
            if (viewData is null)
            {
                return NotFound();
            }

            ViewModel = new DeleteDocumentViewModel
            {
                Id = viewData.Id,
                Slug = slug,
                Title = viewData.Title,
                FileCount = viewData.FileCount,
                ChunkCount = viewData.ChunkCount
            };

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching delete view data for document: {Slug}", slug);
            TempData["ErrorMessage"] = "Không thể tải thông tin xoá tài liệu.";
            return RedirectToPage("/Documents/Mine");
        }
    }

    public async Task<IActionResult> OnPostAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var canDelete = await _documentService.GetOwnedDocumentDetailsBySlugAsync(slug, userId, cancellationToken);
            if (canDelete is null)
            {
                return NotFound();
            }

            await _documentService.DeleteDocumentAsync(canDelete.Id, cancellationToken);
            // SignalR broadcast is now handled centrally in DocumentService.DeleteDocumentAsync

            TempData["SuccessMessage"] = "Đã xoá tài liệu.";
            return RedirectToPage("/Documents/Mine");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {Slug}", slug);
            TempData["ErrorMessage"] = "Không thể xoá tài liệu: " + ex.Message;
            return RedirectToPage("/Documents/Mine");
        }
    }
}
