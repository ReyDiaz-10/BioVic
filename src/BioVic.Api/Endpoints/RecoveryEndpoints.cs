using BioVic.Api.Auth;
using BioVic.Api.Security;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;

namespace BioVic.Api.Endpoints;

public static class RecoveryEndpoints
{
    public static IEndpointRouteBuilder MapRecoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/recovery")
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);

        group.MapGet("/", async (HttpContext http, IRecoveryService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(http.ToRequestContext(), ct)))
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        group.MapPost("/equipment/{id:guid}", async (Guid id, RestoreDeletedItemRequest request,
            HttpContext http, IRecoveryService service, CancellationToken ct) =>
        {
            await service.RestoreEquipmentAsync(id, request, http.ToRequestContext(), ct);
            return Results.NoContent();
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        group.MapPost("/users/{id:guid}", async (Guid id, RestoreDeletedItemRequest request,
            HttpContext http, IRecoveryService service, CancellationToken ct) =>
        {
            await service.RestoreUserAsync(id, request, http.ToRequestContext(), ct);
            return Results.NoContent();
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        group.MapPost("/locations/{id:guid}", async (Guid id, RestoreDeletedItemRequest request,
            HttpContext http, IRecoveryService service, CancellationToken ct) =>
        {
            await service.RestoreLocationAsync(id, request, http.ToRequestContext(), ct);
            return Results.NoContent();
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        return endpoints;
    }
}

