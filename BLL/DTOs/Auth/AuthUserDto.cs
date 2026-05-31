namespace BLL.DTOs.Auth;

public sealed class AuthUserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public short RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
}
