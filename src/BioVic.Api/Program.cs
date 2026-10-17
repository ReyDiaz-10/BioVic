using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BioVic.Api.Auth;
using BioVic.Api.Endpoints;
using BioVic.Api.Logging;
using BioVic.Api.Middleware;
using BioVic.Api.Security;
using BioVic.Api.Updates;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Domain.Enums;
using BioVic.Infrastructure;
using BioVic.Infrastructure.Data;
using BioVic.Infrastructure.Options;
using BioVic.Infrastructure.Services;
using BioVic.QrPortal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using PdfSharp.Fonts;

var environmentFile = EnvironmentFileLoader.Load(
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(AppContext.BaseDirectory, ".env"));
Environment.SetEnvironmentVariable("BIOVIC_ENV_FILE_STATUS", environmentFile is null ? "not-found" : "loaded");

GlobalFontSettings.FontResolver = new BioVicFontResolver();

var configuredEnvironment = Environment.GetEnvironmentVariable("BIOVIC_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environments.Development;
configuredEnvironment = string.IsNullOrWhiteSpace(configuredEnvironment)
    ? Environments.Development
    : configuredEnvironment.Trim();
Environment.SetEnvironmentVariable("BIOVIC_ENVIRONMENT", configuredEnvironment);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = configuredEnvironment
});
builder.Configuration.AddEnvironmentVariables();
var configuredHostingProvider = (builder.Configuration["BIOVIC_HOSTING_PROVIDER"] ?? "standalone").Trim();
var isAzureAppService = configuredHostingProvider.Equals("azure-app-service", StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrWhiteSpace(builder.Configuration["WEBSITE_INSTANCE_ID"]);
var configuredAllowedHosts = builder.Configuration["BIOVIC_ALLOWED_HOSTS"];
if (!string.IsNullOrWhiteSpace(configuredAllowedHosts))
    builder.Configuration["AllowedHosts"] = configuredAllowedHosts;

var maxRequestBodyBytes = ParsePositiveInt(builder.Configuration["BIOVIC_MAX_REQUEST_BODY_BYTES"], 1_048_576);
var documentMaxBytes = ParsePositiveInt(builder.Configuration["BIOVIC_DOCUMENT_MAX_BYTES"], 20_971_520);
builder.WebHost.ConfigureKestrel(server =>
{
    server.AddServerHeader = false;
    server.Limits.MaxRequestBodySize = Math.Max(maxRequestBodyBytes, documentMaxBytes + 1_048_576L);
    server.Limits.MaxRequestHeaderCount = 64;
    server.Limits.MaxRequestHeadersTotalSize = 32_768;
    server.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(20);
    server.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

var configuredLogPath = builder.Configuration["BIOVIC_LOG_PATH"] ?? "logs";
var logDirectory = Path.IsPathRooted(configuredLogPath)
    ? Path.GetFullPath(configuredLogPath)
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredLogPath));
builder.Logging.AddProvider(new RollingFileLoggerProvider(logDirectory));
var configuredDataProtectionPath = builder.Configuration["BIOVIC_DATA_PROTECTION_PATH"] ?? "data-protection";
var dataProtectionDirectory = Path.IsPathRooted(configuredDataProtectionPath)
    ? Path.GetFullPath(configuredDataProtectionPath)
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredDataProtectionPath));
Directory.CreateDirectory(dataProtectionDirectory);
builder.Services.AddDataProtection()
    .SetApplicationName("BioVic.Api")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory));
builder.Services.AddBioVicInfrastructure(builder.Configuration);
builder.Services.AddScoped<IEquipmentDocumentService, EquipmentDocumentService>();
builder.Services.AddScoped<IRecoveryService, RecoveryService>();
builder.Services.AddSingleton<UpdateReleaseStore>();
builder.Services.AddSingleton<QrPortalRenderer>();
builder.Services.AddSingleton<MobilePortalRenderer>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = 1;
    if (isAzureAppService)
    {
        // En App Service la aplicación solo recibe tráfico a través del front-end de Azure.
        // Sus direcciones internas pueden cambiar, por eso no se fijan IP privadas del proxy.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        return;
    }

    var trustedProxyIps = builder.Configuration["BIOVIC_TRUSTED_PROXY_IPS"] ?? "127.0.0.1;::1";
    foreach (var rawIp in trustedProxyIps.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (IPAddress.TryParse(rawIp, out var address) && !options.KnownProxies.Contains(address))
            options.KnownProxies.Add(address);
    }
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = false;
    options.Preload = false;
});
builder.Services.AddAntiforgery(options =>
{
    options.FormFieldName = "__RequestVerificationToken";
    options.Cookie.Name = "BioVic.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction()
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ApiSecurityPolicies.Authentication, context =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        }));
    options.AddPolicy(ApiSecurityPolicies.AuthenticatedRead, context =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 240,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        }));
    options.AddPolicy(ApiSecurityPolicies.AuthenticatedWrite, context =>
        RateLimitPartition.GetFixedWindowLimiter(GetAuthenticatedPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = "Demasiadas solicitudes",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Espere un momento antes de volver a intentarlo."
        }, cancellationToken);
    };
});
builder.Services.AddAuthentication(SessionAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationDefaults.Scheme, _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SessionAuthenticationDefaults.EditorOrMasterPolicy, policy =>
        policy.RequireRole(UserRole.Editor.ToString(), UserRole.MasterAdministrator.ToString()));
    options.AddPolicy(SessionAuthenticationDefaults.MasterPolicy, policy =>
        policy.RequireRole(UserRole.MasterAdministrator.ToString()));
});

var urls = builder.Configuration["BIOVIC_API_URLS"] ?? "http://127.0.0.1:5180";
builder.WebHost.UseUrls(urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

var app = builder.Build();
var biovicOptions = app.Services.GetRequiredService<BioVicOptions>();
ProductionConfigurationValidator.ValidateAndThrow(biovicOptions);

try
{
    PdfRuntimeDiagnostics.Validate();
    app.Logger.LogInformation("Diagnóstico de PDFsharp completado correctamente.");
}
catch (Exception exception)
{
    app.Logger.LogCritical(exception, "BioVic.Api no puede iniciar porque falló el diagnóstico de generación PDF.");
    throw;
}

app.UseForwardedHeaders();
if (biovicOptions.RequireHttps)
{
    if (app.Environment.IsProduction()) app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    if (context.Request.Path.StartsWithSegments("/e") || context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
    }
    if (context.Request.Path.StartsWithSegments("/e"))
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'; upgrade-insecure-requests";
        context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
    }
    await next();
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();
app.MapBioVicEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
    await audit.AppendAsync(new AuditEntryRequest(RequestContext.Anonymous(Environment.MachineName, "LOCAL", "1.0.0"),
        "APPLICATION_STARTED", "Application", null, null, "BioVic API inició correctamente."));
}

await app.RunAsync();

static int ParsePositiveInt(string? value, int fallback) =>
    int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;

static string GetClientPartition(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static string GetAuthenticatedPartition(HttpContext context) =>
    context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
    ?? GetClientPartition(context);
