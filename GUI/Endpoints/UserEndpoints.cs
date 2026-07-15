using System.Security.Claims;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/user").RequireAuthorization();

        group.MapPut("/profile", async (
            [FromBody] UpdateProfileRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var user = httpContext.User;
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            
            var (success, error) = await authService.UpdateProfileAsync(userId, request.FullName, ct);
            if (!success)
            {
                return Results.BadRequest(new { error });
            }

            if (user.Identity is ClaimsIdentity identity)
            {
                var existingClaim = identity.FindFirst(ClaimTypes.Name);
                if (existingClaim != null)
                {
                    identity.RemoveClaim(existingClaim);
                }
                identity.AddClaim(new Claim(ClaimTypes.Name, request.FullName.Trim()));
                
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));
            }

            return Results.Ok(new { message = "Cập nhật thông tin thành công." });
        });

        group.MapPut("/password", async (
            [FromBody] ChangePasswordRequest request,
            IAuthService authService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            
            var (success, error) = await authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, request.ConfirmPassword, ct);
            if (!success)
            {
                return Results.BadRequest(new { error });
            }

            return Results.Ok(new { message = "Đổi mật khẩu thành công." });
        });

        return routes;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim, out userId);
    }
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
