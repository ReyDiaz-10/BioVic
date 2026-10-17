namespace BioVic.Infrastructure.Options;

public static class EnvironmentFileLoader
{
    public static string? Load(params string[] candidatePaths)
    {
        var exactPaths = candidatePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var path = exactPaths.FirstOrDefault(File.Exists)
            ?? exactPaths.SelectMany(EnumerateParentEnvironmentFiles)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(File.Exists);
        if (path is null) return null;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var separator = line.IndexOf('=');
            if (separator <= 0) continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        return path;
    }

    private static IEnumerable<string> EnumerateParentEnvironmentFiles(string candidatePath)
    {
        var candidateDirectoryPath = Path.GetDirectoryName(candidatePath);
        if (string.IsNullOrWhiteSpace(candidateDirectoryPath)) yield break;

        var candidateDirectory = new DirectoryInfo(candidateDirectoryPath);
        if (File.Exists(Path.Combine(candidateDirectory.FullName, "BioVic.sln"))) yield break;

        var directory = candidateDirectory.Parent;
        for (var depth = 0; depth < 8 && directory is not null; depth++, directory = directory.Parent)
        {
            yield return Path.Combine(directory.FullName, ".env");
            if (File.Exists(Path.Combine(directory.FullName, "BioVic.sln"))) yield break;
        }
    }
}
