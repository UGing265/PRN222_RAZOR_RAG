using System;

namespace DAL.Entities;

public partial class Session
{
    public Guid Id { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string Token { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Guid UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
