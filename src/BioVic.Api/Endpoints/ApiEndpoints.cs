using System.Security.Claims;
using BioVic.Api.Auth;
using BioVic.Api.Security;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Infrastructure.Services;
using BioVic.QrPortal;
using BioVic.Domain.Enums;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace BioVic.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapBioVicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Ok(new { name = "BioVic API", version = "1.0.0", status = "ready" }));
        endpoints.MapGet("/health", (QrAccessService qrAccess, IHostEnvironment environment) =>
        {
            if (environment.IsProduction())
                return Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow });

            var qr = qrAccess.GetAccessInfo(Guid.Empty);
            return Results.Ok(new
            {
                status = "healthy",
                utc = DateTimeOffset.UtcNow,
                startupInstance = Environment.GetEnvironmentVariable("BIOVIC_STARTUP_INSTANCE") ?? "manual",
                processId = Environment.ProcessId,
                configurationFile = Environment.GetEnvironmentVariable("BIOVIC_ENV_FILE_STATUS") ?? "unknown",
                pdf = "ready",
                qrRemoteAccess = qr.IsRemoteAccessReady
            });
        });

        var setup = endpoints.MapGroup("/api/setup");
        setup.MapGet("/status", async (IAuthService service, CancellationToken ct) =>
            Results.Ok(await service.GetSetupStatusAsync(ct)));
        setup.MapPost("/master", async (CreateMasterRequest request, HttpContext http, IHostEnvironment environment,
                BioVic.Infrastructure.Options.BioVicOptions options, IAuthService service, CancellationToken ct) =>
            {
                SetupSecurity.EnsureAllowed(http, environment, options);
                return Results.Ok(await service.CreateMasterAsync(request, http.ToRequestContext(), ct));
            })
            .RequireRateLimiting(ApiSecurityPolicies.Authentication);

        var auth = endpoints.MapGroup("/api/auth");
        auth.MapPost("/login", async (LoginRequest request, HttpContext http, IAuthService service, CancellationToken ct) =>
            Results.Ok(await service.LoginAsync(request, http.ToRequestContext(), ct)))
            .RequireRateLimiting(ApiSecurityPolicies.Authentication);
        auth.MapPost("/logout", async (HttpContext http, IAuthService service, CancellationToken ct) =>
        {
            var header = http.Request.Headers.Authorization.ToString();
            var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
            await service.LogoutAsync(token, http.ToRequestContext(), ct);
            return Results.NoContent();
        }).RequireAuthorization();
        auth.MapGet("/master-profile", async (HttpContext http, IAuthService service, CancellationToken ct) =>
            Results.Ok(await service.GetMasterProfileAsync(http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);
        auth.MapPut("/master-profile", async (UpdateMasterProfileRequest request, HttpContext http, IAuthService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateMasterProfileAsync(request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.Authentication);

        var equipment = endpoints.MapGroup("/api/equipment").RequireAuthorization();
        equipment.MapGet("/search", async (string? q, int? page, int? pageSize, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Ok(await service.SearchAsync(q, page ?? 1, pageSize ?? 25, http.ToRequestContext(), ct)));
        equipment.MapGet("/{id:guid}", async (Guid id, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, http.ToRequestContext(), ct)));
        equipment.MapPost("/", async (CreateEquipmentRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Created($"/api/equipment/{request.InternalCode}", await service.CreateAsync(request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy);
        equipment.MapPut("/{id:guid}", async (Guid id, UpdateEquipmentRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateAsync(id, request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);
        equipment.MapDelete("/{id:guid}", async (Guid id, [FromBody] DeleteEquipmentRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
        { await service.DeleteAsync(id, request, http.ToRequestContext(), ct); return Results.NoContent(); })
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);
        equipment.MapPost("/{id:guid}/maintenance", async (Guid id, AddMaintenanceRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Created($"/api/equipment/{id}/maintenance", await service.AddMaintenanceAsync(id, request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy);
        equipment.MapPost("/{id:guid}/calibrations", async (Guid id, AddCalibrationRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Created($"/api/equipment/{id}/calibrations", await service.AddCalibrationAsync(id, request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy);
        equipment.MapPost("/{id:guid}/failures", async (Guid id, AddFailureRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Created($"/api/equipment/{id}/failures", await service.AddFailureAsync(id, request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy);
        equipment.MapPost("/{id:guid}/incidents", async (Guid id, AddIncidentRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Created($"/api/equipment/{id}/incidents", await service.AddIncidentAsync(id, request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy);
        equipment.MapGet("/{id:guid}/qr/access", async (Guid id, HttpContext http, IEquipmentOutputService service, CancellationToken ct) =>
            Results.Ok(await service.GetQrAccessInfoAsync(id, http.ToRequestContext(), ct)));
        equipment.MapGet("/{id:guid}/qr", async (Guid id, HttpContext http, IEquipmentOutputService service, CancellationToken ct) =>
            Results.File(await service.CreateQrPngAsync(id, http.ToRequestContext(), ct), "image/png", $"BioVic-QR-{id:N}.png"))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy);
        equipment.MapGet("/{id:guid}/life-sheet.pdf", async (Guid id, HttpContext http, IEquipmentOutputService service, CancellationToken ct) =>
            Results.File(await service.CreateLifeSheetPdfAsync(id, http.ToRequestContext(), ct), "application/pdf", $"Hoja-de-vida-{id:N}.pdf"))
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);
        equipment.MapPost("/{id:guid}/ai", async (Guid id, AiQuestionRequest request, HttpContext http, IEquipmentOutputService service, CancellationToken ct) =>
            Results.Ok(await service.AskAsync(id, request, http.ToRequestContext(), ct)));
        equipment.MapPost("/{id:guid}/status", async (Guid id, ChangeEquipmentStatusRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
        {
            await service.ChangeStatusAsync(id, request, http.ToRequestContext(), ct);
            return Results.NoContent();
        }).RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy);

        var locations = endpoints.MapGroup("/api/locations").RequireAuthorization();
        locations.MapGet("/", async (HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Ok(await service.GetLocationsAsync(http.ToRequestContext(), ct)));
        locations.MapPost("/", async (CreateLocationRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Created("/api/locations", await service.CreateLocationAsync(request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);
        locations.MapPut("/{id:guid}", async (Guid id, UpdateLocationRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateLocationAsync(id, request, http.ToRequestContext(), ct)))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);
        locations.MapDelete("/{id:guid}", async (Guid id, [FromBody] DeleteLocationRequest request, HttpContext http, IEquipmentService service, CancellationToken ct) =>
        { await service.DeleteLocationAsync(id, request, http.ToRequestContext(), ct); return Results.NoContent(); })
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);

        endpoints.MapGet("/api/public/equipment/{publicId:guid}", async (Guid publicId, HttpContext http, IEquipmentService service, CancellationToken ct) =>
            Results.Ok(await service.GetPublicAsync(publicId, http.ToRequestContext(), ct)))
            .RequireAuthorization()
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);
        endpoints.MapGet("/e/{publicId:guid}", async (Guid publicId, HttpContext http, IEquipmentService service,
            IEquipmentDocumentService documentService,
            QrPortalRenderer loginRenderer, MobilePortalRenderer mobileRenderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            var antiforgeryToken = antiforgery.GetAndStoreTokens(http).RequestToken ?? string.Empty;
            if (http.User.Identity?.IsAuthenticated != true)
                return Results.Content(loginRenderer.RenderLogin(publicId, antiforgeryToken), "text/html; charset=utf-8");
            try
            {
                var data = await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
                var roleValue = http.User.FindFirstValue(ClaimTypes.Role);
                if (!Enum.TryParse<UserRole>(roleValue, out var role))
                    throw new ForbiddenException("La sesión no contiene un rol válido.");
                var documents = await documentService.ListAsync(data.Id, http.ToRequestContext(), false, ct);
                return Results.Content(mobileRenderer.RenderEquipment(data, documents, role, antiforgeryToken,
                    http.Request.Query["message"].ToString()), "text/html; charset=utf-8");
            }
            catch (ForbiddenException exception)
            {
                return Results.Content(loginRenderer.RenderLogin(publicId, antiforgeryToken, exception.Message), "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status403Forbidden);
            }
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);
        endpoints.MapPost("/e/{publicId:guid}/login", async (Guid publicId, HttpContext http, IAuthService service,
            QrPortalRenderer renderer, IAntiforgery antiforgery, BioVic.Infrastructure.Options.BioVicOptions options, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var form = await http.Request.ReadFormAsync(ct);
            try
            {
                var session = await service.LoginAsync(new LoginRequest(form["username"].ToString(), form["password"].ToString(), "QR-WEB"), http.ToRequestContext(), ct);
                http.Response.Cookies.Append(SessionAuthenticationDefaults.WebCookieName, session.Token, new CookieOptions
                {
                    Path = "/e",
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = options.RequireHttps || http.Request.IsHttps,
                    Expires = session.ExpiresAtUtc
                });
                return Results.Redirect($"/e/{publicId:D}");
            }
            catch (BioVicException exception)
            {
                var antiforgeryToken = antiforgery.GetAndStoreTokens(http).RequestToken ?? string.Empty;
                return Results.Content(renderer.RenderLogin(publicId, antiforgeryToken, exception.Message), "text/html; charset=utf-8", statusCode: StatusCodes.Status401Unauthorized);
            }
        })
            .RequireRateLimiting(ApiSecurityPolicies.Authentication);
        endpoints.MapPost("/e/logout", async (HttpContext http, IAuthService service,
            IAntiforgery antiforgery, BioVic.Infrastructure.Options.BioVicOptions options, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var token = http.Request.Cookies[SessionAuthenticationDefaults.WebCookieName] ?? string.Empty;
            if (token.Length > 0) await service.LogoutAsync(token, http.ToRequestContext(), ct);
            http.Response.Cookies.Delete(SessionAuthenticationDefaults.WebCookieName, new CookieOptions
            {
                Path = "/e",
                Secure = options.RequireHttps || http.Request.IsHttps,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict
            });
            return Results.Redirect("/e/login");
        });
        endpoints.MapGet("/e/{publicId:guid}/hoja-de-vida.pdf", async (Guid publicId, HttpContext http, IEquipmentOutputService service, CancellationToken ct) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
                return (IResult)Results.Redirect($"/e/{publicId:D}");
            return (IResult)Results.File(await service.CreatePublicLifeSheetPdfAsync(publicId, http.ToRequestContext(), ct),
                "application/pdf", $"Hoja-de-vida-{publicId:N}.pdf");
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);
        endpoints.MapGet("/e/{publicId:guid}/documents/{documentId:guid}", async (Guid publicId, Guid documentId,
            HttpContext http, IEquipmentService equipmentService, IEquipmentDocumentService documentService, CancellationToken ct) =>
        {
            if (http.User.Identity?.IsAuthenticated != true) return (IResult)Results.Redirect($"/e/{publicId:D}");
            var equipmentData = await equipmentService.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
            var file = await documentService.DownloadAsync(equipmentData.Id, documentId, http.ToRequestContext(), ct);
            return (IResult)Results.File(file.Content, file.ContentType, file.FileName);
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        var admin = endpoints.MapGroup("/api/admin").RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy);
        admin.MapGet("/dashboard", async (IDashboardService service, CancellationToken ct) => Results.Ok(await service.GetAsync(ct)));
        admin.MapGet("/audit", async (int? page, int? pageSize, IAuditService service, CancellationToken ct) =>
            Results.Ok(await service.QueryAsync(page ?? 1, pageSize ?? 50, ct)));
        admin.MapGet("/audit/integrity", async (IAuditService service, CancellationToken ct) => Results.Ok(await service.VerifyIntegrityAsync(ct)));
        admin.MapGet("/users", async (IUserAdminService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)));
        admin.MapPost("/users", async (CreateUserRequest request, HttpContext http, IUserAdminService service, CancellationToken ct) =>
            Results.Created("/api/admin/users", await service.CreateAsync(request, http.ToRequestContext(), ct)));
        admin.MapPut("/users/{id:guid}/access", async (Guid id, UpdateUserAccessRequest request, HttpContext http, IUserAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateAccessAsync(id, request, http.ToRequestContext(), ct)));
        admin.MapDelete("/users/{id:guid}", async (Guid id, [FromBody] DeleteUserRequest request, HttpContext http, IUserAdminService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, request, http.ToRequestContext(), ct);
            return Results.NoContent();
        });
        admin.MapGet("/notifications", async (HttpContext http, IAdminNotificationService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(http.ToRequestContext(), ct)));
        admin.MapPost("/notifications/{id:guid}/snooze", async (Guid id, SnoozeNotificationRequest request, HttpContext http, IAdminNotificationService service, CancellationToken ct) =>
        { await service.SnoozeAsync(id, request, http.ToRequestContext(), ct); return Results.NoContent(); });
        admin.MapPost("/notifications/{id:guid}/resolve", async (Guid id, HttpContext http, IAdminNotificationService service, CancellationToken ct) =>
        { await service.ResolveAsync(id, http.ToRequestContext(), ct); return Results.NoContent(); });
        admin.MapPost("/demo/seed", async (HttpContext http, IHostEnvironment environment, DemoDataSeeder seeder, CancellationToken ct) =>
        {
            if (!environment.IsDevelopment()) return Results.NotFound();
            var count = await seeder.SeedAsync(http.ToRequestContext(), ct);
            return Results.Ok(new { equipmentCreated = count, fictitious = true });
        });

        endpoints.MapMobilePortalEndpoints();
        endpoints.MapEquipmentDocumentEndpoints();
        endpoints.MapRecoveryEndpoints();
        endpoints.MapUpdateEndpoints();

        return endpoints;
    }
}
