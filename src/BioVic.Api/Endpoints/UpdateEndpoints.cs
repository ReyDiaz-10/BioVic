using BioVic.Api.Security;
using BioVic.Api.Updates;

namespace BioVic.Api.Endpoints;

public static class UpdateEndpoints
{
    public static IEndpointRouteBuilder MapUpdateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/updates/manifest", async (UpdateReleaseStore store, CancellationToken ct) =>
        {
            var manifest = await store.GetManifestAsync(ct);
            return manifest is null ? (IResult)Results.NotFound() : Results.Ok(manifest);
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapGet("/api/updates/download/{fileName}", async (string fileName, UpdateReleaseStore store,
            CancellationToken ct) =>
        {
            var release = await store.GetInstallerAsync(fileName, ct);
            return release is null
                ? (IResult)Results.NotFound()
                : Results.File(release.Value.Path, "application/vnd.microsoft.portable-executable",
                    release.Value.Manifest.FileName, enableRangeProcessing: true);
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);
        return endpoints;
    }
}
