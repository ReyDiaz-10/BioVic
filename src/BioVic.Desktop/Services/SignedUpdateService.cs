using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using BioVic.Application.DTOs;

namespace BioVic.Desktop.Services;

public sealed class SignedUpdateService(ClientSettings settings) : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(10) };

    public async Task<bool> CheckAndOfferAsync(Window owner, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.UpdateManifestUrl) || string.IsNullOrWhiteSpace(settings.UpdatePublicKeyPem))
            return false;
        try
        {
            var manifestUri = RequireHttps(settings.UpdateManifestUrl, "manifiesto");
            using var response = await _http.GetAsync(manifestUri, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
            response.EnsureSuccessStatusCode();
            var manifest = await response.Content.ReadFromJsonAsync<UpdateManifestDto>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken)
                ?? throw new InvalidOperationException("El manifiesto de actualización está vacío.");
            if (!UpdateManifestSecurity.Verify(manifest, settings.UpdatePublicKeyPem))
                throw new InvalidOperationException("La firma digital del manifiesto no es válida. La actualización fue bloqueada.");
            var current = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);
            if (!Version.TryParse(manifest.Version, out var available))
                throw new InvalidOperationException("La versión publicada no es válida.");
            if (available <= current) return false;
            var downloadUri = RequireHttps(manifest.DownloadUrl, "instalador");
            if (!downloadUri.Host.Equals(new Uri(settings.ApiUrl).Host, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El instalador no pertenece al servidor configurado de BioVic.");

            var prompt = $"Está disponible BioVic {manifest.Version}.\n\n{manifest.ReleaseNotes}\n\n" +
                         "El instalador y el manifiesto serán verificados antes de ejecutarse.";
            var choice = MessageBox.Show(owner, prompt, manifest.Mandatory ? "Actualización obligatoria" : "Actualización de BioVic",
                MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (choice != MessageBoxResult.OK)
            {
                if (manifest.Mandatory)
                {
                    MessageBox.Show(owner, "Esta versión requiere actualizar BioVic antes de continuar.", "BioVic",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    System.Windows.Application.Current.Shutdown();
                }
                return false;
            }

            var updateDirectory = Path.Combine(Path.GetTempPath(), "BioVic-Updates", manifest.Version);
            Directory.CreateDirectory(updateDirectory);
            var installerPath = Path.Combine(updateDirectory, Path.GetFileName(manifest.FileName));
            using (var installerResponse = await _http.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                installerResponse.EnsureSuccessStatusCode();
                await using var source = await installerResponse.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    81_920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await source.CopyToAsync(target, cancellationToken);
            }
            var info = new FileInfo(installerPath);
            if (info.Length != manifest.SizeBytes) throw new InvalidOperationException("El tamaño de la actualización no coincide.");
            await using var file = File.OpenRead(installerPath);
            var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken));
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(actualHash),
                    Convert.FromHexString(manifest.Sha256)))
                throw new InvalidOperationException("La actualización no superó la verificación SHA-256.");

            Process.Start(new ProcessStartInfo(installerPath)
            {
                UseShellExecute = true,
                Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS"
            });
            System.Windows.Application.Current.Shutdown();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, "BioVic bloqueó la actualización:\n" + ex.Message,
                "Actualización segura", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private static Uri RequireHttps(string? value, string label)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"La dirección del {label} no es HTTPS.");
        return uri;
    }

    public void Dispose() => _http.Dispose();
}

