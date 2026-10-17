using System.Globalization;
using BioVic.Api.Auth;
using BioVic.Api.Security;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BioVic.Api.Endpoints;

public static class EquipmentDocumentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/equipment/{equipmentId:guid}/documents").RequireAuthorization();

        group.MapGet("/", async (Guid equipmentId, bool? includeDeleted, HttpContext http,
                IEquipmentDocumentService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(equipmentId, http.ToRequestContext(), includeDeleted == true, ct)))
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        group.MapPost("/", async (Guid equipmentId, HttpContext http, IEquipmentDocumentService service,
                CancellationToken ct) =>
            await UploadAsync(equipmentId, null, http, service, ct))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        group.MapPost("/{documentId:guid}/versions", async (Guid equipmentId, Guid documentId, HttpContext http,
                IEquipmentDocumentService service, CancellationToken ct) =>
            await UploadAsync(equipmentId, documentId, http, service, ct))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        group.MapGet("/{documentId:guid}/download", async (Guid equipmentId, Guid documentId, HttpContext http,
            IEquipmentDocumentService service, CancellationToken ct) =>
        {
            var file = await service.DownloadAsync(equipmentId, documentId, http.ToRequestContext(), ct);
            return Results.File(file.Content, file.ContentType, file.FileName);
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        group.MapDelete("/{documentId:guid}", async (Guid equipmentId, Guid documentId,
            [FromBody] ArchiveDocumentRequest request, HttpContext http, IEquipmentDocumentService service, CancellationToken ct) =>
        {
            await service.ArchiveAsync(equipmentId, documentId, request, http.ToRequestContext(), ct);
            return Results.NoContent();
        }).RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        group.MapPost("/{documentId:guid}/restore", async (Guid equipmentId, Guid documentId,
            RestoreDocumentRequest request, HttpContext http, IEquipmentDocumentService service, CancellationToken ct) =>
            Results.Ok(await service.RestoreAsync(equipmentId, documentId, request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        return endpoints;
    }

    private static async Task<IResult> UploadAsync(Guid equipmentId, Guid? supersedesDocumentId, HttpContext http,
        IEquipmentDocumentService service, CancellationToken ct)
    {
        if (!http.Request.HasFormContentType) throw new ValidationException("La solicitud debe contener un archivo.");
        var form = await http.Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file") ?? throw new ValidationException("Seleccione un archivo.");
        var documentType = form["documentType"].ToString();
        var description = Empty(form["description"].ToString());
        var documentDate = Date(form["documentDate"].ToString(), "fecha del documento");
        var expiresAt = Date(form["expiresAt"].ToString(), "fecha de vencimiento");
        var approved = bool.TryParse(form["approvedForAiKnowledge"].ToString(), out var parsed) && parsed;
        await using var stream = file.OpenReadStream();
        var result = await service.UploadAsync(equipmentId,
            new(file.FileName, documentType, description, documentDate, expiresAt, approved, supersedesDocumentId),
            stream, http.ToRequestContext(), ct);
        return Results.Created($"/api/equipment/{equipmentId:D}/documents/{result.Id:D}", result);
    }

    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateTimeOffset? Date(string? raw, string label)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value : throw new ValidationException($"La {label} no es válida.");
    }
}

