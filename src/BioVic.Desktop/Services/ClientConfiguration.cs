using System.IO;
using System.Text.Json;
namespace BioVic.Desktop.Services;

public sealed record ClientSettings(string ApiUrl, string? UpdateManifestUrl, string? UpdatePublicKeyPem);

public static class ClientConfiguration
{
    public static ClientSettings Load()
    {
        var environmentValue = Environment.GetEnvironmentVariable("BIOVIC_API_URL");
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            var api = Validate(environmentValue, "BIOVIC_API_URL");
            return new(api, Environment.GetEnvironmentVariable("BIOVIC_UPDATE_MANIFEST_URL"),
                Environment.GetEnvironmentVariable("BIOVIC_UPDATE_PUBLIC_KEY_PEM"));
        }

        foreach (var path in CandidatePaths())
        {
            if (!File.Exists(path)) continue;
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("apiUrl", out var value)
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                var api = Validate(value.GetString()!, path);
                return new(api, Text(document.RootElement, "updateManifestUrl"),
                    Text(document.RootElement, "updatePublicKeyPem"));
            }
        }

        return new("http://127.0.0.1:5180", null, null);
    }

    public static string LoadApiUrl() => Load().ApiUrl;

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim() : null;

    private static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "biovic.client.json");
        var currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "biovic.client.json");
        if (!currentDirectoryPath.Equals(Path.Combine(AppContext.BaseDirectory, "biovic.client.json"), StringComparison.OrdinalIgnoreCase))
            yield return currentDirectoryPath;
    }

    private static string Validate(string value, string source)
    {
        var normalized = value.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"La dirección de BioVic.Api configurada en {source} no es válida.");
        }

        return normalized;
    }
}
