using System.Security.Claims;
using System.Text.Encodings.Web;
using BLL.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GUI;

public class BetterAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAuthService _authService;

    public BetterAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuthService authService)
        : base(options, logger, encoder)
    {
        _authService = authService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Read better-auth session token cookie
        if (!Request.Cookies.TryGetValue("better-auth.session_token", out var sessionToken) || string.IsNullOrEmpty(sessionToken))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // 2. Query session and user info from BLL
            var session = await _authService.ValidateSessionTokenAsync(sessionToken);

            if (session == null)
            {
                return AuthenticateResult.Fail("Invalid session token.");
            }

            // 3. Verify session expiration (Better Auth expires_at is stored in UTC or local depending on DB, but generally timestamptz is read as DateTime with UTC kind)
            if (session.ExpiresAt.ToUniversalTime() < DateTime.UtcNow)
            {
                return AuthenticateResult.Fail("Session has expired.");
            }

            if (!session.IsActive || session.IsBlocked)
            {
                return AuthenticateResult.Fail("User account is inactive or blocked.");
            }

            // 4. Build ClaimsPrincipal
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new Claim(ClaimTypes.Email, session.Email),
                new Claim(ClaimTypes.Name, session.FullName),
                new Claim(ClaimTypes.Role, session.RoleName),
                new Claim("role_id", session.RoleId.ToString())
            };

            // Add username claim if present
            if (!string.IsNullOrEmpty(session.Username))
            {
                claims.Add(new Claim("username", session.Username));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during Better Auth session verification.");
            return AuthenticateResult.Fail("Authentication verification failed.");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Auth/Login");
        return Task.CompletedTask;
    }
}
