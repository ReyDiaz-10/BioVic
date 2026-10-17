using BioVic.Application.Services;

namespace BioVic.Tests;

public sealed class DamerauLevenshteinTests
{
    [Theory]
    [InlineData("monitor", "monitr", 1)]
    [InlineData("urgencias", "urgensias", 1)]
    [InlineData("bomba", "bomba", 0)]
    public void Distance_DetectsCommonTypingErrors(string expected, string typed, int distance)
    {
        Assert.Equal(distance, DamerauLevenshtein.Distance(expected, typed));
    }

    [Fact]
    public void Similarity_NormalizesAccents()
    {
        Assert.Equal(1d, DamerauLevenshtein.Similarity("Oxímetro", "oximetro"), 6);
    }
}
