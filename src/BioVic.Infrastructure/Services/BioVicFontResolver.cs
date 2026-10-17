using PdfSharp.Fonts;

namespace BioVic.Infrastructure.Services;

/// <summary>
/// Supplies PDFsharp with font bytes explicitly. PDFsharp Core 6.2 does not
/// depend on the Windows font-selection APIs and requires a resolver in
/// production scenarios.
/// </summary>
public sealed class BioVicFontResolver : IFontResolver
{
    private const string RegularFace = "BioVicSans-Regular";
    private const string BoldFace = "BioVicSans-Bold";
    private readonly string _regularPath;
    private readonly string _boldPath;

    public BioVicFontResolver()
    {
        var directories = FontDirectories().Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _regularPath = FindFont(directories,
            "arial.ttf", "segoeui.ttf", "calibri.ttf", "DejaVuSans.ttf", "LiberationSans-Regular.ttf")
            ?? throw new InvalidOperationException("BioVic no encontró una fuente TrueType compatible para generar el PDF.");
        _boldPath = FindFont(directories,
            "arialbd.ttf", "segoeuib.ttf", "calibrib.ttf", "DejaVuSans-Bold.ttf", "LiberationSans-Bold.ttf")
            ?? _regularPath;
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(bold ? BoldFace : RegularFace, false, italic);

    public byte[]? GetFont(string faceName) => faceName switch
    {
        RegularFace => File.ReadAllBytes(_regularPath),
        BoldFace => File.ReadAllBytes(_boldPath),
        _ => null
    };

    private static IEnumerable<string> FontDirectories()
    {
        var windowsFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (!string.IsNullOrWhiteSpace(windowsFonts)) yield return windowsFonts;
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        yield return Path.Combine(AppContext.BaseDirectory, "fonts");
        yield return "/usr/share/fonts/truetype/dejavu";
        yield return "/usr/share/fonts/truetype/liberation2";
    }

    private static string? FindFont(IEnumerable<string> directories, params string[] fileNames)
    {
        foreach (var directory in directories)
            foreach (var fileName in fileNames)
            {
                var path = Path.Combine(directory, fileName);
                if (File.Exists(path)) return path;
            }
        return null;
    }
}
