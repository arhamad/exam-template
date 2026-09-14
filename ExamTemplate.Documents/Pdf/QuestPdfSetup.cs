using ExamTemplate.Documents.Assets;
using QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace ExamTemplate.Documents.Pdf;

internal static class QuestPdfSetup
{
    private static readonly Lazy<bool> Initialization = new(Initialize);

    public static void EnsureInitialized() => _ = Initialization.Value;

    private static bool Initialize()
    {
        // The Community license covers publicly funded and non-profit academic institutions.
        Settings.License = LicenseType.Community;

        // Only the bundled fonts, so PDFs look the same on developer machines and servers.
        Settings.UseEnvironmentFonts = false;
        foreach (var font in new[] { ExamAssets.NotoNaskhArabicRegular, ExamAssets.NotoNaskhArabicBold, ExamAssets.DejaVuSans })
            FontManager.RegisterFont(new MemoryStream(ExamAssets.Read(font)));

        return true;
    }
}
