using System;

namespace DAL.Entities;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Action { get; set; } = null!;

    public string TargetTable { get; set; } = null!;

    public Guid TargetId { get; set; }

    public string? IpAddress { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
