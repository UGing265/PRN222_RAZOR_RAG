using System;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces.Notifications;
using GUI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GUI.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<SystemHub> _hubContext;

    public SignalRNotificationService(IHubContext<SystemHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendForceLogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveForceLogout", cancellationToken);
    }

    public async Task SendRoleChangedAsync(Guid userId, string newRole, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveRoleChanged", newRole, cancellationToken);
    }

    public async Task SendDocumentStatusUpdatedAsync(Guid documentId, string title, string status, Guid ownerId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"User_{ownerId}").SendAsync("ReceiveDocumentStatusUpdated", new { documentId, title, status }, cancellationToken);
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveDocumentListUpdated", cancellationToken);
        await _hubContext.Clients.Group("Role_Lecturer").SendAsync("ReceiveDocumentListUpdated", cancellationToken);
    }

    public async Task SendDocumentDeletedAsync(Guid documentId, string title, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveDocumentDeleted", new { documentId, title }, cancellationToken);
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveDocumentListUpdated", cancellationToken);
    }

    public async Task SendDocumentUpdatedAsync(Guid documentId, string title, string visibility, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveDocumentUpdated", new { documentId, title, visibility, ownerUserId }, cancellationToken);
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveDocumentListUpdated", cancellationToken);
    }

    public async Task SendNewPublicDocumentAvailableAsync(Guid documentId, string title, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNewPublicDocument", new { documentId, title }, cancellationToken);
    }

    public async Task SendAuditLogCreatedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveAuditLogCreated", cancellationToken);
    }

    public async Task SendUploadProgressAsync(string jobId, int progressPercent, string statusMessage, Guid ownerId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"User_{ownerId}").SendAsync("ReceiveUploadProgress", new { jobId, progressPercent, statusMessage }, cancellationToken);
    }

    public async Task SendMetadataUpdatedAsync(string metadataType, string actionType, object data, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveMetadataUpdated", new { metadataType, actionType, data }, cancellationToken);
    }

    public async Task SendSubjectsAssignedUpdatedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveSubjectsAssignedUpdated", cancellationToken);
    }

    public async Task SendReportsUpdatedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveReportsUpdated", cancellationToken);
    }

    public async Task SendBookmarkUpdatedAsync(Guid documentId, Guid userId, bool isBookmarked, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveBookmarkUpdated", new { documentId, isBookmarked }, cancellationToken);
    }

    public async Task SendLibraryRefreshAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveLibraryRefresh", cancellationToken);
    }

    public async Task SendDocumentListUpdatedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveDocumentListUpdated", cancellationToken);
    }

    public async Task SendTokenUsageUpdatedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveTokenUsageUpdated", cancellationToken);
    }

    public async Task SendProfileUpdatedAsync(Guid userId, string newFullName, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveProfileUpdated", new { newFullName }, cancellationToken);
    }

    public async Task SendUserListUpdatedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveUserListUpdated", cancellationToken);
    }
}
