using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BioVic.Application.DTOs;

public sealed record UpdateManifestDto(
    string Version,
    string FileName,
    string DownloadUrl,
    string Sha256,
    long SizeBytes,
    DateTimeOffset PublishedAtUtc,
    string ReleaseNotes,
    bool Mandatory,
    string Signature);

public static class UpdateManifestSecurity
{
    public static byte[] CreatePayload(UpdateManifestDto manifest)
    {
        var notes = Convert.ToBase64String(Encoding.UTF8.GetBytes(manifest.ReleaseNotes ?? string.Empty));
        var canonical = string.Join('\n', manifest.Version.Trim(), manifest.FileName.Trim(),
            manifest.DownloadUrl.Trim(), manifest.Sha256.Trim().ToUpperInvariant(),
            manifest.SizeBytes.ToString(CultureInfo.InvariantCulture),
            manifest.PublishedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            manifest.Mandatory ? "1" : "0", notes);
        return Encoding.UTF8.GetBytes(canonical);
    }

    public static bool Verify(UpdateManifestDto manifest, string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem) || string.IsNullOrWhiteSpace(manifest.Signature)) return false;
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            return rsa.VerifyData(CreatePayload(manifest), Convert.FromBase64String(manifest.Signature),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException or ArgumentException)
        {
            return false;
        }
    }
}
