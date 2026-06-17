using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace GUI.Components;

/// <summary>
/// Renders the role-aware sidebar. Backed by <see cref="IMemoryCache"/> with a
/// per-user, per-page key so role changes invalidate on the next render and
/// active-link state is always correct for the current page.
/// </summary>
/// <remarks>
/// Cache key: <c>nav-{userId}-{roleId}-{page}</c>. TTL: 1 hour. Stale-on-page
/// is acceptable because a key change (different page) creates a new entry;
/// stale-on-role would leak across role changes, but role lives in the same
/// user claim so the key naturally shifts when role changes.
/// </remarks>
public sealed class NavViewComponent : ViewComponent
{
    private const string GuestKey = "anon";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly ILogger<NavViewComponent> _logger;

    public NavViewComponent(IMemoryCache cache, ILogger<NavViewComponent> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public IViewComponentResult Invoke(string? userId = null)
    {
        var page = ViewContext.RouteData.Values["page"]?.ToString() ?? string.Empty;
        var isAuthenticated = HttpContext.User?.Identity?.IsAuthenticated == true;

        short roleId = 0;
        string fullName = "Người dùng";
        string email = string.Empty;
        string cacheUser = GuestKey;

        if (isAuthenticated)
        {
            var principal = HttpContext.User!;
            var roleIdClaim = principal.FindFirstValue("role_id");
            _ = short.TryParse(roleIdClaim, out roleId);
            _logger.LogInformation("NavViewComponent: isAuthenticated={IsAuthenticated}, userId={UserId}, roleIdClaim={RoleIdClaim}, parsedRoleId={ParsedRoleId}", 
                isAuthenticated, userId, roleIdClaim, roleId);
            fullName = principal.FindFirstValue(ClaimTypes.Name) ?? fullName;
            email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            cacheUser = !string.IsNullOrWhiteSpace(userId)
                ? userId
                : principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? GuestKey;
        }

        var cacheKey = $"nav-{cacheUser}-{roleId}-{page}";

        if (_cache.TryGetValue(cacheKey, out NavViewModel? cached) && cached is not null)
        {
            _logger.LogDebug("Nav cache HIT key={Key}", cacheKey);
            // Active state is page-specific and lives inside the cached model;
            // refresh it for the current page before returning.
            UpdateActiveState(cached, page);
            return View(cached);
        }

        _logger.LogDebug("Nav cache MISS key={Key}", cacheKey);
        var model = BuildModel(page, isAuthenticated, roleId, fullName, email);
        _cache.Set(cacheKey, model, CacheTtl);
        return View(model);
    }

    private static NavViewModel BuildModel(string page, bool isAuth, short roleId, string fullName, string email)
    {
        var model = new NavViewModel
        {
            CurrentPage = page,
            IsAuthenticated = isAuth,
            RoleId = roleId,
            FullName = fullName,
            Email = email,
            RoleLabel = roleId switch
            {
                1 => "Admin",
                2 => "Giảng viên",
                3 => "Sinh viên",
                _ => "Khách"
            }
        };

        if (!isAuth)
        {
            model.NavLinks.Add(new NavLink { Page = "/Auth/Login", Label = "Đăng Nhập", IconKey = "login" });
            model.NavLinks.Add(new NavLink { Page = "/Auth/Register", Label = "Đăng Ký", IconKey = "register" });
        }
        else if (roleId == 1)
        {
            model.NavLinks.Add(new NavLink { Page = "/Admin/Documents", Label = "Quản Lý Tài Liệu", IconKey = "doc" });
            model.NavLinks.Add(new NavLink { Page = "/Admin/Users", Label = "Quản Lý Thành Viên", IconKey = "users" });
            model.NavLinks.Add(new NavLink { Page = "/Admin/Metadata/Subjects/Index", Label = "Quản Lý Danh Mục", IconKey = "metadata" });
        }
        else
        {
            model.NavLinks.Add(new NavLink { Page = "/Documents/All", Label = "Khám Phá", IconKey = "discover" });
            if (roleId == 2 || roleId == 3)
            {
                model.NavLinks.Add(new NavLink { Page = "/Chat/Index", Label = "Trò Chuyện AI", IconKey = "chat" });
                model.NavLinks.Add(new NavLink { Page = "/Documents/Compare", Label = "So Sánh", IconKey = "library" });
            }
            if (roleId == 2)
            {
                model.NavLinks.Add(new NavLink { Page = "/Documents/Mine", Label = "Thư Viện", IconKey = "library" });
                model.NavLinks.Add(new NavLink { Page = "/Documents/Create", Label = "Tải Lên", IconKey = "upload" });
            }
        }

        UpdateActiveState(model, page);
        return model;
    }

    private static void UpdateActiveState(NavViewModel model, string page)
    {
        foreach (var link in model.NavLinks)
        {
            link.Active = !string.IsNullOrEmpty(page) &&
                          page.Equals(link.Page, StringComparison.OrdinalIgnoreCase);
        }
    }
}
