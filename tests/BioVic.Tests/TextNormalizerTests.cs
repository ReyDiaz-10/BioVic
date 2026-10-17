using BioVic.Application.Services;

namespace BioVic.Tests;

public sealed class TextNormalizerTests
{
    [Fact]
    public void Normalize_RemovesAccentsCaseAndRepeatedSpaces()
    {
        Assert.Equal("MONITOR MULTIPARAMETRICO UCI", TextNormalizer.Normalize("  Mónitor   multiparamétrico-UCI "));
    }

    [Fact]
    public void JoinNormalized_IgnoresEmptyValues()
    {
        Assert.Equal("DESFIBRILADOR URGENCIAS", TextNormalizer.JoinNormalized("Desfibrilador", null, "", "Urgencias"));
    }
}
