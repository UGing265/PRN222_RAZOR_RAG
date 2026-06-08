namespace GUI.Components;

/// <summary>
/// View model for the sidebar <see cref="NavViewComponent"/>.
/// One instance per render — populated from <c>User</c> claims and filtered by role.
/// </summary>
public sealed class NavViewModel
{
    public string CurrentPage { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public short RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string Initial => !string.IsNullOrEmpty(FullName) ? FullName.Substring(0, 1).ToUpper() : "?";
    public List<NavLink> NavLinks { get; set; } = new();
    public bool ShowUpgradePromo => IsAuthenticated;
    public bool ShowCreateLibrary => IsAuthenticated && RoleId == 2;
}

public sealed class NavLink
{
    public string Page { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string IconKey { get; set; } = string.Empty;
    public bool Active { get; set; }
}
