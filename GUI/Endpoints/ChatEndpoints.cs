using System.Security.Claims;
using BLL.DTOs.Chat;
using BLL.Interfaces.Chat;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Endpoints;

/// <summary>
/// Minimal API for the chat surface. Routes:
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
        var group = routes.MapGroup("/api/chat").RequireAuthorization("Lecturer,Student");

        // The role check is enforced via the [Authorize] attribute on the original
        // PageModel; mirror it here. Build a policy name that matches roles.
        // (RequireAuthorization with a roles string is the Minimal API equivalent.)
        group = group.RequireAuthorization(p => p.RequireRole("Lecturer", "Student"));

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

        group.MapGet("/documents", async (
            IDocumentService documents,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await documents.GetAllDocumentsAsync(
                query: null,
                subjectId: null,
                page: 1,
                pageSize: 100,
                requesterUserId: userId,
                cancellationToken: ct);

            var approved = result.Documents
                .Where(d => d.Status == "approved" || d.Status == "done")
                .Select(d => new { id = d.Id, title = d.Title, subject = d.SubjectName, status = d.Status })
                .ToList();
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

            var logger = loggerFactory.CreateLogger("ChatEndpoints.Stream");
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.Append("Cache-Control", "no-cache");
            http.Response.Headers.Append("Connection", "keep-alive");

            try
            {
                await foreach (var chunk in chat.StreamMessageAsync(userId, request, ct))
                {
                    await http.Response.WriteAsync($"data: {chunk}\n\n", ct);
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
