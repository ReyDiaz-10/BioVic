using System.Security.Claims;
using System.Text.Encodings.Web;
using BioVic.Application.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BioVic.Api.Auth;

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAuthService authService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : Request.Cookies[SessionAuthenticationDefaults.WebCookieName] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token)) return AuthenticateResult.NoResult();
        var principal = await authService.ValidateSessionAsync(token, Context.RequestAborted);
        if (principal is null) return AuthenticateResult.Fail("La sesión no es válida o expiró.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, principal.UserId.ToString("D")),
            new Claim(ClaimTypes.Name, principal.Username),
            new Claim(ClaimTypes.Role, principal.Role.ToString()),
            new Claim("biovic_session_id", principal.SessionId.ToString("D")),
            new Claim("biovic_session_expires", principal.ExpiresAtUtc.ToString("O"))
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
