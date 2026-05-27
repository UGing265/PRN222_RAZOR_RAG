using System.Security.Claims;
using DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GUI.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly DBContext _dbContext;

    public DashboardController(DBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var documentsQuery = _dbContext.Documents.AsNoTracking().Where(x => x.OwnerUserId == userId);

        ViewBag.TotalDocuments = await documentsQuery.CountAsync(cancellationToken);
        ViewBag.TotalChunks = await _dbContext.DocumentChunks.AsNoTracking().CountAsync(x => x.Document.OwnerUserId == userId, cancellationToken);
        ViewBag.TotalFiles = await _dbContext.DocumentFiles.AsNoTracking().CountAsync(x => x.Document.OwnerUserId == userId, cancellationToken);
        ViewBag.ApprovedDocuments = await documentsQuery.CountAsync(x => x.Status == "approved", cancellationToken);
        ViewBag.PendingDocuments = await documentsQuery.CountAsync(x => x.Status == "pending", cancellationToken);
        ViewBag.RejectedDocuments = await documentsQuery.CountAsync(x => x.Status == "rejected", cancellationToken);
        ViewBag.RecentDocuments = await documentsQuery
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Subject,
                x.Status,
                x.UpdatedAt,
                FileCount = x.DocumentFiles.Count,
                ChunkCount = x.DocumentChunks.Count
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        var completedJob = await _dbContext.UploadJobs
            .Where(x => x.OwnerUserId == userId && x.Status == "done" && !x.IsNotified)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (completedJob is not null)
        {
            TempData["SuccessMessage"] = $"Tệp \"{completedJob.FileName}\" đã xử lý xong.";
            completedJob.IsNotified = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        ViewBag.ActiveUploadJobs = await _dbContext.UploadJobs.AsNoTracking()
            .Where(x => x.OwnerUserId == userId && x.Status != "done" && x.Status != "failed")
            .OrderByDescending(x => x.UpdatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return View();
    }
}
