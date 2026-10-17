using BioVic.Infrastructure.Options;

namespace BioVic.Tests;

public sealed class ProductionConfigurationValidatorTests
{
    [Fact]
    public void ValidIisConfigurationPassesProductionValidation()
    {
        var errors = ProductionConfigurationValidator.Validate(ValidOptions());

        Assert.Empty(errors);
    }

    [Fact]
    public void PublicHttpListenerAndInsecureQrAreRejected()
    {
        var options = ValidOptions(
            apiUrls: "http://0.0.0.0:5180",
            qrBaseUrl: "http://192.168.1.20:5180/e",
            allowedHosts: "*",
            requireHttps: false,
            reverseProxyEnabled: false);

        var errors = ProductionConfigurationValidator.Validate(options);

        Assert.Contains(errors, error => error.Contains("HTTPS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("proxy", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("comodines", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnabledProductionSetupRequiresStrongToken()
    {
        var options = ValidOptions(setupEnabled: true, setupToken: "corto");

        var errors = ProductionConfigurationValidator.Validate(options);

        Assert.Contains(errors, error => error.Contains("32 caracteres", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidAzureAppServiceConfigurationPassesProductionValidation()
    {
        var options = ValidOptions(
            apiUrls: "http://0.0.0.0:8080",
            hostingProvider: "azure-app-service",
            databaseProvider: "sqlserver",
            databaseConnection: "Server=tcp:biovic.database.windows.net,1433;Initial Catalog=biovic;User ID=biovic-admin;Password=Prueba-no-real-123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
            backupPath: "backups",
            logPath: "/home/biovic/logs",
            dataProtectionPath: "/home/biovic/keys");

        var errors = ProductionConfigurationValidator.Validate(options);

        Assert.Empty(errors);
    }

    [Fact]
    public void AzureAppServiceRejectsSqliteEphemeralPathsAndWrongListener()
    {
        var options = ValidOptions(
            apiUrls: "http://127.0.0.1:5180",
            hostingProvider: "azure-app-service",
            databaseConnection: "Data Source=data/biovic.db",
            backupPath: "backups",
            logPath: "logs",
            dataProtectionPath: "keys");

        var errors = ProductionConfigurationValidator.Validate(options);

        Assert.Contains(errors, error => error.Contains("0.0.0.0:8080", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("sqlserver", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("LOG_PATH", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("DATA_PROTECTION_PATH", StringComparison.OrdinalIgnoreCase));
    }

    private static BioVicOptions ValidOptions(
        string apiUrls = "http://127.0.0.1:5180",
        string qrBaseUrl = "https://biovic.institucion.org/e",
        string allowedHosts = "biovic.institucion.org",
        bool requireHttps = true,
        bool reverseProxyEnabled = true,
        bool setupEnabled = false,
        string setupToken = "",
        string hostingProvider = "iis",
        string databaseProvider = "sqlite",
        string databaseConnection = "Data Source=data/biovic.db",
        string backupPath = "backups",
        string logPath = "logs",
        string dataProtectionPath = "data-protection") => new()
    {
        EnvironmentName = "Production",
        HostingProvider = hostingProvider,
        AllowedHosts = allowedHosts,
        RequireHttps = requireHttps,
        ReverseProxyEnabled = reverseProxyEnabled,
        SetupEnabled = setupEnabled,
        SetupToken = setupToken,
        DatabaseProvider = databaseProvider,
        DatabaseConnection = databaseConnection,
        ApiUrls = apiUrls,
        QrBaseUrl = qrBaseUrl,
        BackupEnabled = true,
        BackupPath = backupPath,
        LogPath = logPath,
        DataProtectionPath = dataProtectionPath,
        SessionHours = 8
    };
}
