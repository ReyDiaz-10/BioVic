using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace BioVic.Infrastructure.Services;

/// <summary>
/// Verifies at API startup that PDFsharp can resolve BioVic fonts, draw text
/// using the supported alignment mode, and serialize a valid PDF document.
/// </summary>
public static class PdfRuntimeDiagnostics
{
    public static void Validate() => _ = CreateProbeDocument();

    public static byte[] CreateProbeDocument()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;

        using (var graphics = XGraphics.FromPdfPage(page))
        {
            var title = new XFont("BioVic Sans", 14, XFontStyleEx.Bold);
            var body = new XFont("BioVic Sans", 9, XFontStyleEx.Regular);
            graphics.DrawString("BioVic · Diagnóstico PDF", title, XBrushes.Black,
                new XRect(40, 40, page.Width.Point - 80, 24), XStringFormats.TopLeft);
            graphics.DrawString("Fuentes y alineación disponibles.", body, XBrushes.Black,
                new XRect(40, 70, page.Width.Point - 80, 18), XStringFormats.TopLeft);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        var result = stream.ToArray();
        if (result.Length < 5 || Encoding.ASCII.GetString(result, 0, 5) != "%PDF-")
            throw new InvalidOperationException("PDFsharp no produjo un documento PDF válido.");

        return result;
    }
}
