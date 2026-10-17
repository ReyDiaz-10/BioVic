using System.Diagnostics;
using System.Security.Claims;
using BioVic.Application.DTOs;
using BioVic.Domain.Enums;

namespace BioVic.Api.Auth;

public static class RequestContextFactory
{
    public static RequestContext ToRequestContext(this HttpContext httpContext)
    {
        var nameIdentifier = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = httpContext.User.FindFirstValue(ClaimTypes.Role);
        var computerName = Limit(httpContext.Request.Headers["X-BioVic-Computer"].FirstOrDefault(), 255, "UNKNOWN");
        var version = FileVersionInfo.GetVersionInfo(typeof(RequestContextFactory).Assembly.Location).ProductVersion ?? "1.0.0";
        if (Guid.TryParse(nameIdentifier, out var userId) && Enum.TryParse<UserRole>(roleValue, out var role))
        {
            return new(userId, httpContext.User.Identity?.Name ?? "UNKNOWN", role,
                httpContext.User.FindFirstValue("biovic_session_id") ?? "UNKNOWN", computerName,
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "LOCAL", Limit(version, 40, "1.0.0"));
        }

        return RequestContext.Anonymous(computerName,
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "LOCAL", Limit(version, 40, "1.0.0"));
    }

    private static string Limit(string? value, int maxLength, string fallback)
    {
        var result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return result[..Math.Min(result.Length, maxLength)];
    }
}
