using System.Text;
using BioVic.Infrastructure.Services;
using PdfSharp.Fonts;

namespace BioVic.Tests;

public sealed class PdfRuntimeDiagnosticsTests
{
    [Fact]
    public void CreatesValidProbeDocument()
    {
        GlobalFontSettings.FontResolver = new BioVicFontResolver();

        var pdf = PdfRuntimeDiagnostics.CreateProbeDocument();

        Assert.True(pdf.Length > 100);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
