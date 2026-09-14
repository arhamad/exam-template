namespace ExamTemplate.Documents.Assets;

/// <summary>Fonts, logo and Word template embedded in this assembly.</summary>
public static class ExamAssets
{
    public const string NotoNaskhArabicRegular = "Fonts.NotoNaskhArabic-Regular.ttf";
    public const string NotoNaskhArabicBold = "Fonts.NotoNaskhArabic-Bold.ttf";
    public const string DejaVuSans = "Fonts.DejaVuSans.ttf";
    public const string UniversityLogo = "Images.university-logo.png";
    public const string WordTemplate = "Word.ExamTemplate.docx";

    public static byte[] Read(string name)
    {
        using var stream = typeof(ExamAssets).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
