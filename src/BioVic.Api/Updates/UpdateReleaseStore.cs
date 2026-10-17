using System.Security.Cryptography;
using System.Text.Json;
using BioVic.Application.DTOs;

namespace BioVic.Api.Updates;

public sealed class UpdateReleaseStore(IConfiguration configuration)
{
    private readonly string _root = ResolveRoot(configuration["BIOVIC_UPDATE_PATH"]);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UpdateManifestDto? _cached;
    private DateTime _cachedManifestWriteUtc;

    public async Task<UpdateManifestDto?> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, "manifest.json");
        if (!File.Exists(path)) return null;
        var writeUtc = File.GetLastWriteTimeUtc(path);
        if (_cached is not null && writeUtc == _cachedManifestWriteUtc) return _cached;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            writeUtc = File.GetLastWriteTimeUtc(path);
            if (_cached is not null && writeUtc == _cachedManifestWriteUtc) return _cached;
            await using var stream = File.OpenRead(path);
            var manifest = await JsonSerializer.DeserializeAsync<UpdateManifestDto>(stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken)
                ?? throw new InvalidOperationException("El manifiesto de actualización está vacío.");
            ValidateName(manifest.FileName);
            if (!Version.TryParse(manifest.Version, out _) || manifest.SizeBytes <= 0 || manifest.Sha256.Length != 64)
                throw new InvalidOperationException("El manifiesto de actualización no es válido.");
            var installerPath = Path.Combine(_root, manifest.FileName);
            if (!File.Exists(installerPath)) throw new FileNotFoundException("El instalador publicado no existe.");
            var info = new FileInfo(installerPath);
            if (info.Length != manifest.SizeBytes) throw new InvalidOperationException("El tamaño del instalador no coincide con el manifiesto.");
            await using var installer = File.OpenRead(installerPath);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(installer, cancellationToken));
            if (!hash.Equals(manifest.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El instalador no coincide con el SHA-256 publicado.");
            _cached = manifest;
            _cachedManifestWriteUtc = writeUtc;
            return manifest;
        }
        finally { _gate.Release(); }
    }

    public async Task<(string Path, UpdateManifestDto Manifest)?> GetInstallerAsync(string fileName,
        CancellationToken cancellationToken = default)
    {
        ValidateName(fileName);
        var manifest = await GetManifestAsync(cancellationToken);
        if (manifest is null || !manifest.FileName.Equals(fileName, StringComparison.Ordinal)) return null;
        return (Path.Combine(_root, fileName), manifest);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name)
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidOperationException("Nombre de instalador no válido.");
    }

    private static string ResolveRoot(string? configured)
    {
        var value = string.IsNullOrWhiteSpace(configured)
            ? (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"))
                ? Path.Combine("data", "updates") : "/home/data/biovic-updates")
            : configured.Trim();
        var root = Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(AppContext.BaseDirectory, value));
        Directory.CreateDirectory(root);
        return root;
    }
}
