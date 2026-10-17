using BioVic.Application.Contracts;
using BioVic.Domain.Entities;
using BioVic.Infrastructure.Data;
using BioVic.Infrastructure.Options;
using BioVic.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BioVic.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBioVicInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var environmentName = configuration["BIOVIC_ENVIRONMENT"] ?? "Development";
        var isProduction = environmentName.Equals("Production", StringComparison.OrdinalIgnoreCase);
        var options = new BioVicOptions
        {
            EnvironmentName = environmentName,
            HostingProvider = (configuration["BIOVIC_HOSTING_PROVIDER"] ?? "standalone").Trim().ToLowerInvariant(),
            AllowedHosts = configuration["BIOVIC_ALLOWED_HOSTS"] ?? "localhost;127.0.0.1",
            RequireHttps = ParseBool(configuration["BIOVIC_REQUIRE_HTTPS"], isProduction),
            ReverseProxyEnabled = ParseBool(configuration["BIOVIC_REVERSE_PROXY_ENABLED"], false),
            TrustedProxyIps = configuration["BIOVIC_TRUSTED_PROXY_IPS"] ?? "127.0.0.1;::1",
            SetupEnabled = ParseBool(configuration["BIOVIC_SETUP_ENABLED"], !isProduction),
            SetupToken = configuration["BIOVIC_SETUP_TOKEN"] ?? string.Empty,
            MaxRequestBodyBytes = ParseInt(configuration["BIOVIC_MAX_REQUEST_BODY_BYTES"], 1_048_576),
            DatabaseProvider = (configuration["BIOVIC_DB_PROVIDER"] ?? "sqlite").Trim().ToLowerInvariant(),
            DatabaseConnection = configuration["BIOVIC_DB_CONNECTION"] ?? "Data Source=data/biovic.db",
            BackupEnabled = ParseBool(configuration["BIOVIC_BACKUP_ENABLED"], true),
            BackupPath = configuration["BIOVIC_BACKUP_PATH"] ?? "backups",
            LogPath = configuration["BIOVIC_LOG_PATH"] ?? "logs",
            DataProtectionPath = configuration["BIOVIC_DATA_PROTECTION_PATH"] ?? "data-protection",
            BackupIntervalHours = ParseInt(configuration["BIOVIC_BACKUP_INTERVAL_HOURS"], 24),
            BackupRetentionDays = ParseInt(configuration["BIOVIC_BACKUP_RETENTION_DAYS"], 30),
            SerialMustBeUnique = ParseBool(configuration["BIOVIC_SERIAL_MUST_BE_UNIQUE"], true),
            SessionHours = ParseInt(configuration["BIOVIC_SESSION_HOURS"], 8),
            FailedLoginLimit = ParseInt(configuration["BIOVIC_FAILED_LOGIN_LIMIT"], 5),
            LockoutMinutes = ParseInt(configuration["BIOVIC_LOCKOUT_MINUTES"], 15),
            ApiUrls = configuration["BIOVIC_API_URLS"] ?? "http://127.0.0.1:5180",
            QrBaseUrl = configuration["BIOVIC_QR_BASE_URL"] ?? "http://127.0.0.1:5180/e"
        };

        services.AddSingleton(options);
        services.AddDbContext<BioVicDbContext>(dbOptions => ConfigureDatabase(dbOptions, options));
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEquipmentOutputService, EquipmentOutputService>();
        services.AddSingleton<QrAccessService>();
        services.AddScoped<IAdminNotificationService, AdminNotificationService>();
        services.AddScoped<LocationAccessService>();
        services.AddHttpClient();
        services.AddScoped<DemoDataSeeder>();
        services.AddHostedService<SqliteBackupHostedService>();
        return services;
    }

    private static void ConfigureDatabase(DbContextOptionsBuilder dbOptions, BioVicOptions options)
    {
        if (options.DatabaseProvider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            dbOptions.UseSqlite(DatabaseInitializer.NormalizeSqliteConnection(options.DatabaseConnection));
            return;
        }

        if (options.DatabaseProvider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            dbOptions.UseSqlServer(options.DatabaseConnection, sqlServer =>
                sqlServer.MigrationsAssembly("BioVic.Infrastructure.SqlServerMigrations").EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null));
            return;
        }

        throw new InvalidOperationException(
            $"El proveedor de base de datos '{options.DatabaseProvider}' no es compatible. Use sqlite o sqlserver.");
    }

    private static bool ParseBool(string? value, bool fallback) => bool.TryParse(value, out var parsed) ? parsed : fallback;
    private static int ParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
}
