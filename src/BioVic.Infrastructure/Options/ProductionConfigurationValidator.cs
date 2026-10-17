using System.Net;
using Microsoft.Data.SqlClient;

namespace BioVic.Infrastructure.Options;

public static class ProductionConfigurationValidator
{
    public static void ValidateAndThrow(BioVicOptions options)
    {
        if (!options.EnvironmentName.Equals("Production", StringComparison.OrdinalIgnoreCase)) return;

        var errors = Validate(options);
        if (errors.Count == 0) return;

        throw new InvalidOperationException(
            "BioVic no iniciará porque la configuración de producción es insegura o incompleta:" +
            Environment.NewLine + "- " + string.Join(Environment.NewLine + "- ", errors));
    }

    public static IReadOnlyList<string> Validate(BioVicOptions options)
    {
        var errors = new List<string>();
        var isAzureAppService = options.HostingProvider.Equals(
            "azure-app-service",
            StringComparison.OrdinalIgnoreCase);

        var isKnownHostingProvider = options.HostingProvider.Equals("standalone", StringComparison.OrdinalIgnoreCase)
            || options.HostingProvider.Equals("iis", StringComparison.OrdinalIgnoreCase)
            || isAzureAppService;
        if (!isKnownHostingProvider)
            errors.Add("BIOVIC_HOSTING_PROVIDER debe ser standalone, iis o azure-app-service.");
        if (!options.RequireHttps)
            errors.Add("BIOVIC_REQUIRE_HTTPS debe ser true.");
        if (!options.ReverseProxyEnabled)
            errors.Add("BIOVIC_REVERSE_PROXY_ENABLED debe ser true; publique la API detrás de IIS o un proxy equivalente.");
        if (options.SessionHours > 24)
            errors.Add("BIOVIC_SESSION_HOURS no puede superar 24 horas en producción.");
        var usesSqlite = options.DatabaseProvider.Equals("sqlite", StringComparison.OrdinalIgnoreCase);
        var usesSqlServer = options.DatabaseProvider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase);
        if (usesSqlite && !options.BackupEnabled)
            errors.Add("BIOVIC_BACKUP_ENABLED debe ser true mientras se use SQLite.");
        if (usesSqlite && string.IsNullOrWhiteSpace(options.BackupPath))
            errors.Add("BIOVIC_BACKUP_PATH es obligatorio.");
        if (string.IsNullOrWhiteSpace(options.LogPath))
            errors.Add("BIOVIC_LOG_PATH es obligatorio.");
        if (string.IsNullOrWhiteSpace(options.DataProtectionPath))
            errors.Add("BIOVIC_DATA_PROTECTION_PATH es obligatorio.");
        if (!usesSqlite && !usesSqlServer)
            errors.Add("BIOVIC_DB_PROVIDER debe ser sqlite o sqlserver.");

        if (!Uri.TryCreate(options.QrBaseUrl, UriKind.Absolute, out var qrUri)
            || qrUri.Scheme != Uri.UriSchemeHttps
            || !qrUri.AbsolutePath.TrimEnd('/').EndsWith("/e", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("BIOVIC_QR_BASE_URL debe ser una URL HTTPS pública que termine en /e.");
        }

        var allowedHosts = options.AllowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (allowedHosts.Length == 0 || allowedHosts.Any(host => host is "*" or "0.0.0.0" or "::"))
            errors.Add("BIOVIC_ALLOWED_HOSTS debe contener el dominio público exacto; no se permiten comodines globales.");
        else if (qrUri is not null && !allowedHosts.Any(host => MatchesHost(host, qrUri.Host)))
            errors.Add("El host de BIOVIC_QR_BASE_URL también debe aparecer en BIOVIC_ALLOWED_HOSTS.");

        if (isAzureAppService)
        {
            if (!HasAzureListener(options.ApiUrls))
                errors.Add("En Azure App Service BIOVIC_API_URLS debe escuchar en http://0.0.0.0:8080.");

            if (!usesSqlServer)
                errors.Add("En Azure App Service BIOVIC_DB_PROVIDER debe ser sqlserver; SQLite no es seguro sobre almacenamiento compartido.");
            else
                ValidateAzureSqlConnection(options.DatabaseConnection, errors);

            ValidateAzurePersistentPath(options.LogPath, "BIOVIC_LOG_PATH", errors);
            ValidateAzurePersistentPath(options.DataProtectionPath, "BIOVIC_DATA_PROTECTION_PATH", errors);
        }
        else if (HasPublicListener(options.ApiUrls))
        {
            errors.Add("Fuera de Azure App Service, BIOVIC_API_URLS debe escuchar solamente en 127.0.0.1 o localhost; IIS o el proxy publicará HTTPS.");
        }

        if (options.SetupEnabled && options.SetupToken.Length < 32)
            errors.Add("Si BIOVIC_SETUP_ENABLED=true, BIOVIC_SETUP_TOKEN debe tener al menos 32 caracteres aleatorios.");

        return errors;
    }

    private static bool MatchesHost(string configuredHost, string actualHost)
    {
        var host = configuredHost.Trim();
        if (host.StartsWith("*.", StringComparison.Ordinal))
            return actualHost.EndsWith(host[1..], StringComparison.OrdinalIgnoreCase);
        return host.Equals(actualHost, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasPublicListener(string apiUrls)
    {
        foreach (var rawUrl in apiUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawUrl.Contains("://*:", StringComparison.Ordinal) || rawUrl.Contains("://+:", StringComparison.Ordinal))
                return true;
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)) return true;
            if (uri.Host is "0.0.0.0" or "::" or "[::]") return true;
            if (IPAddress.TryParse(uri.Host, out var address))
            {
                if (!IPAddress.IsLoopback(address)) return true;
            }
            else if (!uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAzureListener(string apiUrls)
    {
        var urls = apiUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (urls.Length != 1 || !Uri.TryCreate(urls[0], UriKind.Absolute, out var uri)) return false;

        return uri.Scheme == Uri.UriSchemeHttp
            && uri.Host == "0.0.0.0"
            && uri.Port == 8080;
    }

    private static void ValidateAzureSqlConnection(string connectionString, ICollection<string> errors)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource) || string.IsNullOrWhiteSpace(builder.InitialCatalog))
                errors.Add("BIOVIC_DB_CONNECTION debe indicar el servidor y la base de datos de Azure SQL.");
            if (builder.TrustServerCertificate)
                errors.Add("BIOVIC_DB_CONNECTION debe usar TrustServerCertificate=False.");
            var encryptionMode = builder.Encrypt.ToString();
            if (!encryptionMode.Equals("True", StringComparison.OrdinalIgnoreCase)
                && !encryptionMode.Equals("Mandatory", StringComparison.OrdinalIgnoreCase)
                && !encryptionMode.Equals("Strict", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("BIOVIC_DB_CONNECTION debe usar Encrypt=True.");
            }
        }
        catch (ArgumentException)
        {
            errors.Add("BIOVIC_DB_CONNECTION no es una cadena de conexión de Azure SQL válida.");
        }
    }

    private static void ValidateAzurePersistentPath(string path, string settingName, ICollection<string> errors)
    {
        if (!IsAzurePersistentPath(path))
            errors.Add($"En Azure, {settingName} debe ser una ruta absoluta dentro de /home.");
    }

    private static bool IsAzurePersistentPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = path.Trim().Replace('\\', '/');
        return normalized.Equals("/home", StringComparison.Ordinal)
            || normalized.StartsWith("/home/", StringComparison.Ordinal);
    }
}
