using System.Security.Claims;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Endpoints;

/// <summary>
/// Minimal API endpoints for Document management:
///   POST /api/documents/upload    — Upload and process a document
///   GET  /api/documents           — List user's documents (with Status for UI dropdown)
///   PUT  /api/documents/{id}/approve — Approve a document (Lecturer only)
/// </summary>
public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/documents");

        // 1. Upload document (triggers indexing pipeline: extract → chunk → embed → save)
        group.MapPost("/upload", async (
            IFormFile file,
            IDocumentService documents,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "File is required." });

            try
            {
                var result = await documents.UploadAndProcessAsync(file, userId, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    title: "Upload failed",
                    statusCode: 500);
            }
        }).RequireAuthorization()
          .DisableAntiforgery();

        // 2. List documents (kèm Status để UI hiển thị dropdown chọn tài liệu
        //    và disable các document chưa Completed/Approved)
        group.MapGet("/", async (
            IDocumentService documents,
            ClaimsPrincipal user,
            [FromQuery] string? query,
            [FromQuery] Guid? subjectId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            var result = await documents.GetAllDocumentsAsync(
                query: query,
                subjectId: subjectId,
                page: page,
                pageSize: pageSize,
                requesterUserId: userId,
                cancellationToken: ct);

            var items = result.Documents.Select(d => new
            {
                id = d.Id,
                title = d.Title,
                status = d.Status,
                subject = d.SubjectName,
                chunkCount = d.ChunkCount,
                createdAt = d.CreatedAt,
                // UI uses this to determine if document is ready for chat
                isReadyForChat = d.Status is "completed" or "approved"
            });

            return Results.Ok(items);
        }).RequireAuthorization();

        // 3. Approve document (Lecturer only)
        group.MapPut("/{id:guid}/approve", async (
            Guid id,
            IDocumentService documents,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();

            // Check role: only Lecturer or Admin can approve
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim is not ("Lecturer" or "Admin"))
            {
                return Results.Forbid();
            }

            try
            {
                await documents.ApproveDocumentAsync(id, userId, ct);
                return Results.Ok(new { message = "Document approved successfully.", documentId = id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        return routes;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim, out userId);
    }
}
