using BioVic.Infrastructure.Options;
using BioVic.Infrastructure.Services;

namespace BioVic.Tests;

public sealed class QrAccessServiceTests
{
    [Fact]
    public void LoopbackConfigurationIsNotReadyForRemoteAccess()
    {
        var service = CreateService("http://127.0.0.1:5180", "http://127.0.0.1:5180/e");

        var result = service.GetAccessInfo(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.False(result.IsRemoteAccessReady);
        Assert.Contains(result.Warnings, warning => warning.Contains("localhost", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, warning => warning.Contains("solo escucha", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LanConfigurationBuildsTheExactEquipmentUrl()
    {
        var publicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var service = CreateService("http://0.0.0.0:5180", "http://192.168.1.50:5180/e");

        var result = service.GetAccessInfo(publicId);

        Assert.True(result.IsRemoteAccessReady);
        Assert.False(result.UsesHttps);
        Assert.Equal("http://192.168.1.50:5180/e/22222222-2222-2222-2222-222222222222", result.Url);
        Assert.Contains(result.Warnings, warning => warning.Contains("red privada", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HttpsDnsConfigurationIsReadyWithoutTransportWarning()
    {
        var service = CreateService("http://0.0.0.0:5180", "https://biovic.institucion.local/e");

        var result = service.GetAccessInfo(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        Assert.True(result.IsRemoteAccessReady);
        Assert.True(result.UsesHttps);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void HttpsReverseProxyIsReadyWithPrivateKestrelListener()
    {
        var service = new QrAccessService(new BioVicOptions
        {
            ApiUrls = "http://127.0.0.1:5180",
            QrBaseUrl = "https://biovic.institucion.org/e",
            ReverseProxyEnabled = true
        });

        var result = service.GetAccessInfo(Guid.Parse("55555555-5555-5555-5555-555555555555"));

        Assert.True(result.IsRemoteAccessReady);
        Assert.True(result.UsesHttps);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ConfigurationWithoutPortalPathIsRejected()
    {
        var service = CreateService("http://0.0.0.0:5180", "http://192.168.1.50:5180");

        var result = service.GetAccessInfo(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        Assert.False(result.IsRemoteAccessReady);
        Assert.Contains(result.Warnings, warning => warning.Contains("terminar en /e", StringComparison.OrdinalIgnoreCase));
    }

    private static QrAccessService CreateService(string apiUrls, string qrBaseUrl) =>
        new(new BioVicOptions { ApiUrls = apiUrls, QrBaseUrl = qrBaseUrl });
}
