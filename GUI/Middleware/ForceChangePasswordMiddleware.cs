using System.Security.Claims;
using DAL.Interfaces.Auth;

namespace GUI.Middleware;

public sealed class ForceChangePasswordMiddleware
{
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Auth/ChangePassword",
        "/Auth/Logout",
        "/Auth/Login",
        "/Auth/VerifyEmail",
        "/Auth/GoogleLogin",
        "/Auth/GoogleCallback"
    };

    private static readonly string[] StaticPrefixes =
    {
        "/css", "/js", "/lib", "/images", "/_framework",
        "/favicon", "/uploads", "/signalr"
    };

    private readonly RequestDelegate _next;
    public ForceChangePasswordMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, IAuthRepository authRepo)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            await _next(ctx);
            return;
        }

        var path = ctx.Request.Path.Value ?? "";
        if (AllowList.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || StaticPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Items.TryGetValue("ForceChange.CurrentUser", out var cached)
            || cached is not DAL.Entities.User user)
        {
            var userIdStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                await _next(ctx);
                return;
            }

            user = await authRepo.GetUserByIdAsync(userId);
            if (user != null)
                ctx.Items["ForceChange.CurrentUser"] = user;
        }

        if (user is { MustChangePassword: true })
        {
            ctx.Response.Redirect("/Auth/ChangePassword");
            return;
        }

        await _next(ctx);
    }
}