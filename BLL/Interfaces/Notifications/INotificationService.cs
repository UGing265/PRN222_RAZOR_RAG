using System;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Interfaces.Notifications;

public interface INotificationService
{
    Task SendForceLogoutAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SendRoleChangedAsync(Guid userId, string newRole, CancellationToken cancellationToken = default);
    Task SendDocumentStatusUpdatedAsync(Guid documentId, string title, string status, Guid ownerId, CancellationToken cancellationToken = default);
    Task SendUploadProgressAsync(string jobId, int progressPercent, string statusMessage, Guid ownerId, CancellationToken cancellationToken = default);
    Task SendMetadataUpdatedAsync(string metadataType, string actionType, object data, CancellationToken cancellationToken = default);
    Task SendSubjectsAssignedUpdatedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SendReportsUpdatedAsync(CancellationToken cancellationToken = default);
}
