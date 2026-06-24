using System;

namespace BLL.DTOs.Auth;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string? TargetName { get; set; }
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
