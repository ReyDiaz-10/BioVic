using System.Security.Cryptography;
using BioVic.Application.DTOs;

namespace BioVic.Tests;

public sealed class UpdateManifestSecurityTests
{
    [Fact]
    public void ValidRsaSignatureIsAcceptedAndTamperingIsRejected()
    {
        using var rsa = RSA.Create(2048);
        var unsigned = new UpdateManifestDto("1.2.3", "BioVic-Setup-win-x64.exe",
            "https://biovic.example/api/updates/download/BioVic-Setup-win-x64.exe",
            new string('A', 64), 123456, new DateTimeOffset(2026, 8, 29, 1, 2, 3, TimeSpan.Zero),
            "Mejoras verificadas", false, string.Empty);
        var signature = Convert.ToBase64String(rsa.SignData(UpdateManifestSecurity.CreatePayload(unsigned),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        var signed = unsigned with { Signature = signature };

        Assert.True(UpdateManifestSecurity.Verify(signed, rsa.ExportSubjectPublicKeyInfoPem()));
        Assert.False(UpdateManifestSecurity.Verify(signed with { SizeBytes = signed.SizeBytes + 1 },
            rsa.ExportSubjectPublicKeyInfoPem()));
    }
}
