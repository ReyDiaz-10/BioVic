using System.Globalization;
using System.Security.Claims;
using BioVic.Api.Auth;
using BioVic.Api.Security;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Options;
using BioVic.QrPortal;
using Microsoft.AspNetCore.Antiforgery;

namespace BioVic.Api.Endpoints;

public static class MobilePortalEndpoints
{
    private static readonly TimeSpan BogotaOffset = TimeSpan.FromHours(-5);

    public static IEndpointRouteBuilder MapMobilePortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/e/mobile", async (string? q, HttpContext http, IEquipmentService equipmentService,
            MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            if (http.User.Identity?.IsAuthenticated != true) return Results.Redirect("/e/login");
            var role = GetRole(http);
            var result = await equipmentService.SearchAsync(q, 1, 50, http.ToRequestContext(), ct);
            return Html(renderer.RenderDashboard(result, role, Token(antiforgery, http), q,
                http.Request.Query["message"].ToString()));
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        MapWebLoginEndpoints(endpoints);
        MapOperationalRecordEndpoints(endpoints);
        MapMasterMobileEndpoints(endpoints);
        MapLogoutEndpoints(endpoints);
        return endpoints;
    }

    private static void MapWebLoginEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/e/login", (HttpContext http, QrPortalRenderer renderer, IAntiforgery antiforgery) =>
        {
            if (http.User.Identity?.IsAuthenticated == true) return Results.Redirect("/e/mobile");
            return Html(renderer.RenderWebLogin(Token(antiforgery, http)));
        }).RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/login", async (HttpContext http, IAuthService service, QrPortalRenderer renderer,
            IAntiforgery antiforgery, BioVicOptions options, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var form = await http.Request.ReadFormAsync(ct);
            try
            {
                var session = await service.LoginAsync(new LoginRequest(form["username"].ToString(),
                    form["password"].ToString(), "MOBILE-WEB"), http.ToRequestContext(), ct);
                SetSessionCookie(http, options, session);
                return Results.Redirect("/e/mobile");
            }
            catch (BioVicException exception)
            {
                return Html(renderer.RenderWebLogin(Token(antiforgery, http), exception.Message),
                    StatusCodes.Status401Unauthorized);
            }
        }).RequireRateLimiting(ApiSecurityPolicies.Authentication);
    }

    private static void MapOperationalRecordEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/e/{publicId:guid}/maintenance/new", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
            Html(renderer.RenderMaintenanceForm(await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct),
                GetRole(http), Token(antiforgery, http))))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/{publicId:guid}/maintenance", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var equipment = await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                var request = new AddMaintenanceRequest(
                    EnumValue<MaintenanceType>(form, "type"), Text(form, "workOrderNumber"),
                    RequiredDate(form, "performedAt"), Required(form, "performedBy"), Text(form, "responsible"),
                    Required(form, "activities"), Text(form, "findings"), Text(form, "technicalDiagnosis"),
                    Text(form, "actionsTaken"), Text(form, "sparePartsSummary"), Decimal(form, "cost"),
                    Integer(form, "downtimeMinutes"), EnumValue<MaintenanceResult>(form, "result"),
                    NullableEnum<EquipmentStatus>(form, "resultingStatus"), Date(form, "nextMaintenance"), Text(form, "notes"));
                await service.AddMaintenanceAsync(equipment.Id, request, http.ToRequestContext(), ct);
                return RedirectEquipment(publicId, "Mantenimiento registrado correctamente.");
            }
            catch (BioVicException exception)
            {
                return Html(renderer.RenderMaintenanceForm(equipment, GetRole(http), Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        endpoints.MapGet("/e/{publicId:guid}/calibration/new", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
            Html(renderer.RenderCalibrationForm(await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct),
                GetRole(http), Token(antiforgery, http))))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/{publicId:guid}/calibration", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var equipment = await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                await service.AddCalibrationAsync(equipment.Id, new AddCalibrationRequest(
                    RequiredDate(form, "performedAt"), Date(form, "nextCalibration"), Required(form, "laboratory"),
                    Text(form, "certificateNumber"), EnumValue<CalibrationResult>(form, "result"),
                    Text(form, "traceability"), Text(form, "notes")), http.ToRequestContext(), ct);
                return RedirectEquipment(publicId, "Calibración registrada correctamente.");
            }
            catch (BioVicException exception)
            {
                return Html(renderer.RenderCalibrationForm(equipment, GetRole(http), Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        endpoints.MapGet("/e/{publicId:guid}/failure/new", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
            Html(renderer.RenderFailureForm(await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct),
                GetRole(http), Token(antiforgery, http))))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/{publicId:guid}/failure", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var equipment = await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                await service.AddFailureAsync(equipment.Id, new AddFailureRequest(
                    RequiredDate(form, "reportedAt"), Required(form, "description"), Required(form, "reportedBy"),
                    Text(form, "service"), Text(form, "impact"), Checked(form, "equipmentStopped"),
                    Text(form, "immediateAction"), Text(form, "diagnosis"), Text(form, "solution"),
                    Integer(form, "downtimeMinutes"), Date(form, "closedAt")), http.ToRequestContext(), ct);
                return RedirectEquipment(publicId, "Falla registrada correctamente.");
            }
            catch (BioVicException exception)
            {
                return Html(renderer.RenderFailureForm(equipment, GetRole(http), Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        endpoints.MapGet("/e/{publicId:guid}/incident/new", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
            Html(renderer.RenderIncidentForm(await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct),
                GetRole(http), Token(antiforgery, http))))
            .RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/{publicId:guid}/incident", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var equipment = await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                await service.AddIncidentAsync(equipment.Id, new AddIncidentRequest(
                    RequiredDate(form, "occurredAt"), Required(form, "eventType"), Required(form, "description"),
                    Text(form, "consequence"), Required(form, "reportedBy"), EnumValue<IncidentStatus>(form, "status"),
                    Text(form, "actionsTaken"), Checked(form, "wasReportedExternally"), Text(form, "externalReportNumber"),
                    Date(form, "externalReportDate"), Text(form, "reportedEntity"), Text(form, "conclusions")),
                    http.ToRequestContext(), ct);
                return RedirectEquipment(publicId, "Incidente registrado correctamente.");
            }
            catch (BioVicException exception)
            {
                return Html(renderer.RenderIncidentForm(equipment, GetRole(http), Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.EditorOrMasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);
    }

    private static void MapMasterMobileEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/e/{publicId:guid}/edit", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            var equipment = await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
            var locations = await service.GetLocationsAsync(http.ToRequestContext(), ct);
            return Html(renderer.RenderEquipmentForm(equipment, locations, Token(antiforgery, http)));
        }).RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/{publicId:guid}/edit", async (Guid publicId, HttpContext http,
            IEquipmentService service, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var equipment = await service.GetByPublicIdAsync(publicId, http.ToRequestContext(), ct);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                await service.UpdateAsync(equipment.Id, UpdateEquipment(form), http.ToRequestContext(), ct);
                return RedirectEquipment(publicId, "Equipo actualizado correctamente.");
            }
            catch (BioVicException exception)
            {
                var locations = await service.GetLocationsAsync(http.ToRequestContext(), ct);
                return Html(renderer.RenderEquipmentForm(equipment, locations, Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        endpoints.MapGet("/e/mobile/equipment/new", async (HttpContext http, IEquipmentService service,
            MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
            Html(renderer.RenderEquipmentForm(null, await service.GetLocationsAsync(http.ToRequestContext(), ct),
                Token(antiforgery, http))))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/mobile/equipment/new", async (HttpContext http, IEquipmentService service,
            MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                var created = await service.CreateAsync(CreateEquipment(form), http.ToRequestContext(), ct);
                return RedirectEquipment(created.PublicId, "Equipo creado correctamente.");
            }
            catch (BioVicException exception)
            {
                var locations = await service.GetLocationsAsync(http.ToRequestContext(), ct);
                return Html(renderer.RenderEquipmentForm(null, locations, Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        endpoints.MapGet("/e/mobile/users/new", async (HttpContext http, IEquipmentService equipmentService,
            MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
            Html(renderer.RenderUserForm(await equipmentService.GetLocationsAsync(http.ToRequestContext(), ct),
                Token(antiforgery, http))))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/mobile/users/new", async (HttpContext http, IEquipmentService equipmentService,
            IUserAdminService userService, MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                var role = EnumValue<UserRole>(form, "role");
                if (role == UserRole.MasterAdministrator)
                    throw new ValidationException("No se puede crear otro Administrador Maestro.");
                var locationIds = form["locationIds"].Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
                    .Where(id => id != Guid.Empty).Distinct().ToList();
                await userService.CreateAsync(new CreateUserRequest(Required(form, "firstName"), Required(form, "lastName"),
                    Required(form, "username"), Required(form, "password"), Text(form, "email"), role,
                    Checked(form, "hasAllLocations"), locationIds), http.ToRequestContext(), ct);
                return Results.Redirect("/e/mobile?message=" + Uri.EscapeDataString("Usuario creado correctamente."));
            }
            catch (BioVicException exception)
            {
                var locations = await equipmentService.GetLocationsAsync(http.ToRequestContext(), ct);
                return Html(renderer.RenderUserForm(locations, Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);

        endpoints.MapGet("/e/mobile/locations/new", (HttpContext http, MobilePortalRenderer renderer, IAntiforgery antiforgery) =>
            Html(renderer.RenderLocationForm(Token(antiforgery, http))))
            .RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedRead);

        endpoints.MapPost("/e/mobile/locations/new", async (HttpContext http, IEquipmentService service,
            MobilePortalRenderer renderer, IAntiforgery antiforgery, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                await service.CreateLocationAsync(new CreateLocationRequest(Required(form, "institution"), Required(form, "site"),
                    Text(form, "building"), Text(form, "floor"), Required(form, "service"), Text(form, "area"),
                    Text(form, "room"), Text(form, "specificLocation")), http.ToRequestContext(), ct);
                return Results.Redirect("/e/mobile?message=" + Uri.EscapeDataString("Ubicación creada correctamente."));
            }
            catch (BioVicException exception)
            {
                return Html(renderer.RenderLocationForm(Token(antiforgery, http), exception.Message),
                    StatusCodes.Status400BadRequest);
            }
        }).RequireAuthorization(SessionAuthenticationDefaults.MasterPolicy)
            .RequireRateLimiting(ApiSecurityPolicies.AuthenticatedWrite);
    }

    private static void MapLogoutEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/e/{publicId:guid}/logout", async (Guid publicId, HttpContext http, IAuthService service,
            IAntiforgery antiforgery, BioVicOptions options, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            await LogoutAsync(http, service, options, ct);
            return Results.Redirect($"/e/{publicId:D}");
        });

        endpoints.MapPost("/e/mobile/logout", async (HttpContext http, IAuthService service,
            IAntiforgery antiforgery, BioVicOptions options, CancellationToken ct) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            await LogoutAsync(http, service, options, ct);
            return Results.Redirect("/e/login");
        });
    }

    private static async Task LogoutAsync(HttpContext http, IAuthService service, BioVicOptions options, CancellationToken ct)
    {
        var rawToken = http.Request.Cookies[SessionAuthenticationDefaults.WebCookieName] ?? string.Empty;
        if (rawToken.Length > 0) await service.LogoutAsync(rawToken, http.ToRequestContext(), ct);
        http.Response.Cookies.Delete(SessionAuthenticationDefaults.WebCookieName, new CookieOptions
        {
            Path = "/e", HttpOnly = true, SameSite = SameSiteMode.Strict,
            Secure = options.RequireHttps || http.Request.IsHttps
        });
    }

    private static void SetSessionCookie(HttpContext http, BioVicOptions options, AuthSessionDto session) =>
        http.Response.Cookies.Append(SessionAuthenticationDefaults.WebCookieName, session.Token, new CookieOptions
        {
            Path = "/e", HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Strict,
            Secure = options.RequireHttps || http.Request.IsHttps, Expires = session.ExpiresAtUtc
        });

    private static CreateEquipmentRequest CreateEquipment(IFormCollection form) => new(
        Required(form, "internalCode"), Required(form, "name"), Text(form, "equipmentType"), Text(form, "category"),
        Text(form, "description"), Text(form, "manufacturer"), Text(form, "brand"), Text(form, "model"),
        Text(form, "serialNumber"), Text(form, "inventoryNumber"), Text(form, "assetCode"),
        RequiredGuid(form, "locationId"), Text(form, "responsibleName"), Text(form, "responsiblePosition"),
        EnumValue<EquipmentStatus>(form, "status"), EnumValue<Criticality>(form, "criticality"),
        Checked(form, "requiresCalibration"), Date(form, "nextMaintenance"), Date(form, "nextCalibration"),
        Text(form, "publicOperationalNotes"));

    private static UpdateEquipmentRequest UpdateEquipment(IFormCollection form) => new(
        Required(form, "internalCode"), Required(form, "name"), Text(form, "equipmentType"), Text(form, "category"),
        Text(form, "description"), Text(form, "manufacturer"), Text(form, "brand"), Text(form, "model"),
        Text(form, "serialNumber"), Text(form, "inventoryNumber"), Text(form, "assetCode"),
        RequiredGuid(form, "locationId"), Text(form, "responsibleName"), Text(form, "responsiblePosition"),
        EnumValue<EquipmentStatus>(form, "status"), EnumValue<Criticality>(form, "criticality"),
        Checked(form, "requiresCalibration"), Date(form, "nextMaintenance"), Date(form, "nextCalibration"),
        Text(form, "publicOperationalNotes"), Required(form, "changeReason"));

    private static UserRole GetRole(HttpContext http)
    {
        if (Enum.TryParse<UserRole>(http.User.FindFirstValue(ClaimTypes.Role), out var role)) return role;
        throw new ForbiddenException("La sesión no contiene un rol válido.");
    }

    private static string Token(IAntiforgery antiforgery, HttpContext http) =>
        antiforgery.GetAndStoreTokens(http).RequestToken ?? string.Empty;

    private static IResult Html(string html, int statusCode = StatusCodes.Status200OK) =>
        Results.Content(html, "text/html; charset=utf-8", statusCode: statusCode);

    private static IResult RedirectEquipment(Guid publicId, string message) =>
        Results.Redirect($"/e/{publicId:D}?message={Uri.EscapeDataString(message)}");

    private static string Required(IFormCollection form, string name)
    {
        var value = form[name].ToString().Trim();
        if (value.Length == 0) throw new ValidationException($"El campo {name} es obligatorio.");
        return value;
    }

    private static string? Text(IFormCollection form, string name)
    {
        var value = form[name].ToString().Trim();
        return value.Length == 0 ? null : value;
    }

    private static bool Checked(IFormCollection form, string name) => form[name].Count > 0;

    private static Guid RequiredGuid(IFormCollection form, string name) =>
        Guid.TryParse(form[name].ToString(), out var value) ? value : throw new ValidationException("Seleccione una ubicación válida.");

    private static TEnum EnumValue<TEnum>(IFormCollection form, string name) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(form[name].ToString(), true, out var value) && Enum.IsDefined(value)
            ? value : throw new ValidationException($"El valor de {name} no es válido.");

    private static TEnum? NullableEnum<TEnum>(IFormCollection form, string name) where TEnum : struct, Enum
    {
        var raw = form[name].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return Enum.TryParse<TEnum>(raw, true, out var value) && Enum.IsDefined(value)
            ? value : throw new ValidationException($"El valor de {name} no es válido.");
    }

    private static DateTimeOffset RequiredDate(IFormCollection form, string name) =>
        Date(form, name) ?? throw new ValidationException($"La fecha {name} es obligatoria.");

    private static DateTimeOffset? Date(IFormCollection form, string name)
    {
        var raw = form[name].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var local))
            throw new ValidationException($"La fecha {name} no es válida.");
        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), BogotaOffset);
    }

    private static int? Integer(IFormCollection form, string name)
    {
        var raw = form[name].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value : throw new ValidationException($"El valor de {name} no es válido.");
    }

    private static decimal? Decimal(IFormCollection form, string name)
    {
        var raw = form[name].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value : throw new ValidationException($"El valor de {name} no es válido.");
    }
}
