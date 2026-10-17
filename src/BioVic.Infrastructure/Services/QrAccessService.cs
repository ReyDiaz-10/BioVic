using System.Net;
using BioVic.Application.DTOs;
using BioVic.Infrastructure.Options;

namespace BioVic.Infrastructure.Services;

public sealed class QrAccessService(BioVicOptions options)
{
    public QrAccessInfoDto GetAccessInfo(Guid publicId)
    {
        var warnings = new List<string>();
        var configuredBaseUrl = options.QrBaseUrl.Trim();
        var hasValidBaseUrl = Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri)
            && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps);

        if (!hasValidBaseUrl || baseUri is null)
        {
            warnings.Add("BIOVIC_QR_BASE_URL debe contener una dirección HTTP o HTTPS válida.");
            return new(configuredBaseUrl, false, false, warnings);
        }

        var hasRemoteHost = IsRemoteHost(baseUri.Host);
        if (!hasRemoteHost)
            warnings.Add("BIOVIC_QR_BASE_URL usa localhost o una dirección local del propio computador. Configure la IP LAN o el DNS del servidor.");

        var hasPortalPath = baseUri.AbsolutePath.TrimEnd('/').EndsWith("/e", StringComparison.OrdinalIgnoreCase);
        if (!hasPortalPath)
            warnings.Add("BIOVIC_QR_BASE_URL debe terminar en /e para dirigir al portal protegido de BioVic.");

        var usesHttps = baseUri.Scheme == Uri.UriSchemeHttps;
        var hasRemoteListener = HasRemoteListener(options.ApiUrls);
        var hasReachablePortal = hasRemoteListener || options.ReverseProxyEnabled;
        if (!hasReachablePortal)
            warnings.Add("BIOVIC_API_URLS solo escucha conexiones locales. Configure http://0.0.0.0:5180 para aceptar conexiones de la red privada.");
        else if (options.ReverseProxyEnabled && !hasRemoteListener && !usesHttps)
            warnings.Add("El proxy inverso está habilitado, pero BIOVIC_QR_BASE_URL todavía no usa HTTPS.");

        if (!usesHttps)
            warnings.Add("El portal usa HTTP. Utilícelo únicamente dentro de una red privada confiable; para acceso institucional o remoto configure HTTPS.");

        var url = $"{configuredBaseUrl.TrimEnd('/')}/{publicId:D}";
        return new(url, hasRemoteHost && hasPortalPath && hasReachablePortal, usesHttps, warnings);
    }

    private static bool IsRemoteHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return false;
        if (!IPAddress.TryParse(host, out var address)) return true;
        return !IPAddress.IsLoopback(address)
            && !address.Equals(IPAddress.Any)
            && !address.Equals(IPAddress.IPv6Any)
            && !address.Equals(IPAddress.None);
    }

    private static bool HasRemoteListener(string apiUrls)
    {
        foreach (var rawUrl in apiUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawUrl.Contains("://*:", StringComparison.Ordinal) || rawUrl.Contains("://+:", StringComparison.Ordinal))
                return true;
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri)) continue;
            if (uri.Host is "0.0.0.0" or "::" or "[::]") return true;
            if (IsRemoteHost(uri.Host)) return true;
        }

        return false;
    }
}
