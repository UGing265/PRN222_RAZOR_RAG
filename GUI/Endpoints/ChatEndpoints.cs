using System.Security.Claims;
using BLL.DTOs.Chat;
using BLL.Interfaces.Chat;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace GUI.Endpoints;

/// <summary>
/// Minimal API for the chat surface. Routes
///   GET  /api/chat/sessions
///   GET  /api/chat/sessions/{sessionId}/messages
///   GET  /api/chat/documents
///   POST /api/chat/messages        (non-streaming)
///   POST /api/chat/messages/stream (SSE)
/// </summary>
public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/chat");

        // The role check is enforced via the [Authorize] attribute on the original
        // PageModel; mirror it here. Build a policy name that matches roles.
        // (RequireAuthorization with a roles string is the Minimal API equivalent.)

        group.MapGet("/whoami", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new
            {
                IsAuthenticated = user.Identity?.IsAuthenticated,
                Name = user.Identity?.Name,
                Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                AllClaims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }).RequireAuthorization();

        group.MapGet("/sessions", async (
            IChatService chat,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var sessions = await chat.GetSessionsAsync(userId, ct);
            return Results.Ok(sessions);
        });

        group.MapGet("/sessions/{sessionId:guid}/messages", async (
            Guid sessionId,
            IChatService chat,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var messages = await chat.GetSessionMessagesAsync(userId, sessionId, ct);
            return Results.Ok(messages);
        });

        group.MapDelete("/sessions/{sessionId:guid}", async (
            Guid sessionId,
            IChatService chat,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var success = await chat.DeleteSessionAsync(userId, sessionId, ct);
            if (!success) return Results.NotFound();
            return Results.Ok();
        });

        group.MapPost("/sessions/bulk-delete", async (
            [FromBody] List<Guid> sessionIds,
            IChatService chat,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var success = await chat.DeleteSessionsAsync(userId, sessionIds, ct);
            if (!success) return Results.BadRequest("Invalid session IDs");
            return Results.Ok();
        });

        group.MapGet("/debug-docs", async (
            IDocumentService documents,
            DAL.Interfaces.Documents.IDocumentRepository repo,
            CancellationToken ct) =>
        {
            var raw = await repo.GetDocumentsAsync(null, null, 1, 100, null, null, null, null, null, null, ct);
            return Results.Ok(new
            {
                Count = raw.Count,
                Items = raw.Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Status,
                    d.Visibility,
                    OwnerRoleId = d.OwnerUser?.RoleId,
                    OwnerId = d.OwnerUserId
                })
            });
        });

        group.MapGet("/debug-text-search", async (
            DAL.Data.DBContext context,
            [FromQuery] string q,
            CancellationToken ct) =>
        {
            var matchedChunks = await context.DocumentChunks
                .Where(c => c.Content.Contains(q))
                .Select(c => new
                {
                    c.Id,
                    c.DocumentId,
                    DocTitle = c.Document.Title,
                    ChapterTitle = c.Chapter != null ? c.Chapter.Title : "N/A",
                    c.PageNumber,
                    c.Content
                })
                .ToListAsync(ct);

            return Results.Ok(matchedChunks);
        });

        group.MapGet("/documents", async (
            IDocumentService documents,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            Console.WriteLine("====== [DEBUG] API /api/chat/documents ĐƯỢC GỌI ======");

            if (!TryGetUserId(user, out var userId))
            {
                Console.WriteLine("[DEBUG] Không lấy được UserId từ Cookie. Trả về 401 Unauthorized.");
                return Results.Unauthorized();
            }

            Console.WriteLine($"[DEBUG] Đã lấy được UserId: {userId}. Đang lấy tài liệu từ Database...");

            var result = await documents.GetAllDocumentsAsync(
                null, null, 1, 100, userId, null, null, null, null, null, ct);

            Console.WriteLine($"[DEBUG] Database trả về {result.Documents.Count} tài liệu thô.");

            var approved = result.Documents
                .Where(d => d.Status == "approved" || d.Status == "done" || d.Status == "completed")
                .Select(d => new { id = d.Id, title = d.Title, subject = d.SubjectName, status = d.Status })
                .ToList();

            Console.WriteLine($"[DEBUG] Sau khi lọc trạng thái (completed/approved), còn lại: {approved.Count} tài liệu.");
            Console.WriteLine("====== [DEBUG] KẾT THÚC ======");

            return Results.Ok(approved);
        });

        group.MapPost("/messages", async (
            [FromBody] ChatRequest request,
            IChatService chat,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest(new { error = "Message is required." });

            if (!request.SessionId.HasValue && (request.DocumentIds == null || request.DocumentIds.Count == 0))
                return Results.BadRequest(new { error = "Vui lòng chọn ít nhất một tài liệu để bắt đầu chat." });

            var response = await chat.SendMessageAsync(userId, request, ct);
            return Results.Ok(response);
        });

        group.MapPost("/messages/stream", async (
            [FromBody] ChatRequest request,
            IChatService chat,
            ClaimsPrincipal user,
            HttpContext http,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId))
            {
                http.Response.StatusCode = 401;
                return;
            }
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                http.Response.StatusCode = 400;
                return;
            }

            if (!request.SessionId.HasValue && (request.DocumentIds == null || request.DocumentIds.Count == 0))
            {
                http.Response.ContentType = "text/event-stream";
                await http.Response.WriteAsync($"data: [ERROR] Vui lòng chọn ít nhất một tài liệu (ở menu thả xuống phía trên) để bắt đầu phiên chat.\n\n", ct);
                await http.Response.Body.FlushAsync(ct);
                return;
            }

            var logger = loggerFactory.CreateLogger("ChatEndpoints.Stream");
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.Append("Cache-Control", "no-cache");
            http.Response.Headers.Append("Connection", "keep-alive");

            try
            {
                await foreach (var chunk in chat.StreamMessageAsync(userId, request, ct))
                {
                    var safeChunk = chunk?.Replace("\n", "\\n") ?? string.Empty;
                    await http.Response.WriteAsync($"data: {safeChunk}\n\n", ct);
                    await http.Response.Body.FlushAsync(ct);
                }
                await http.Response.WriteAsync("data: [DONE]\n\n", ct);
                await http.Response.Body.FlushAsync(ct);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Stream cancelled by client.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stream failed.");
                await http.Response.WriteAsync("data: [ERROR] Đã xảy ra lỗi khi xử lý tin nhắn.\n\n", ct);
                await http.Response.Body.FlushAsync(ct);
            }
        });

        return routes;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim, out userId);
    }
}
