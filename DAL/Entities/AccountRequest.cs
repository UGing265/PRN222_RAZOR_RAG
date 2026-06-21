using System;

namespace DAL.Entities;

public partial class AccountRequest
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public short RoleId { get; set; }
    public string Status { get; set; } = "pending";
    public string? VerificationToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Role Role { get; set; } = null!;
}
