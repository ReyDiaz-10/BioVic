using BioVic.Infrastructure.Options;

namespace BioVic.Tests;

public sealed class EnvironmentFileLoaderTests
{
    [Fact]
    public void LoadFindsTheEnvironmentFileAtTheSolutionRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"BioVic-Env-Test-{Guid.NewGuid():N}");
        var nestedDirectory = Path.Combine(root, "src", "BioVic.Api", "bin", "Debug", "net10.0");
        var variableName = $"BIOVIC_TEST_{Guid.NewGuid():N}";
        var expectedValue = Guid.NewGuid().ToString("N");

        try
        {
            Directory.CreateDirectory(nestedDirectory);
            File.WriteAllText(Path.Combine(root, "BioVic.sln"), string.Empty);
            File.WriteAllText(Path.Combine(root, ".env"), $"{variableName}={expectedValue}");
            Environment.SetEnvironmentVariable(variableName, null);

            var loadedPath = EnvironmentFileLoader.Load(Path.Combine(nestedDirectory, ".env"));

            Assert.Equal(Path.Combine(root, ".env"), loadedPath);
            Assert.Equal(expectedValue, Environment.GetEnvironmentVariable(variableName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
