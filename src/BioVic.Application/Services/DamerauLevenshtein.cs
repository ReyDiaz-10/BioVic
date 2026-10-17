namespace BioVic.Application.Services;

public static class DamerauLevenshtein
{
    public static int Distance(string source, string target)
    {
        source ??= string.Empty;
        target ??= string.Empty;

        var matrix = new int[source.Length + 1, target.Length + 1];
        for (var i = 0; i <= source.Length; i++) matrix[i, 0] = i;
        for (var j = 0; j <= target.Length; j++) matrix[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);

                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                {
                    matrix[i, j] = Math.Min(matrix[i, j], matrix[i - 2, j - 2] + cost);
                }
            }
        }

        return matrix[source.Length, target.Length];
    }

    public static double Similarity(string source, string target)
    {
        var normalizedSource = TextNormalizer.Normalize(source);
        var normalizedTarget = TextNormalizer.Normalize(target);
        var maximumLength = Math.Max(normalizedSource.Length, normalizedTarget.Length);
        return maximumLength == 0 ? 1d : 1d - (double)Distance(normalizedSource, normalizedTarget) / maximumLength;
    }
}
