using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public short RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<UserBookmark> UserBookmarks { get; set; } = new List<UserBookmark>();

    public virtual ICollection<DocumentReport> DocumentReports { get; set; } = new List<DocumentReport>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
