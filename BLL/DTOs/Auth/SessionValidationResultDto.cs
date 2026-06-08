using System;

namespace BLL.DTOs.Auth;

public sealed class SessionValidationResultDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? Username { get; init; }
    public bool IsActive { get; init; }
    public bool IsBlocked { get; init; }
    public DateTime ExpiresAt { get; init; }
}
