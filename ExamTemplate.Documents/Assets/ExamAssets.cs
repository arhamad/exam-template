using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Assets;

/// <summary>Fonts, logo and Word templates embedded in this assembly.</summary>
public static class ExamAssets
{
    public const string NotoNaskhArabicRegular = "Fonts.NotoNaskhArabic-Regular.ttf";
    public const string NotoNaskhArabicBold = "Fonts.NotoNaskhArabic-Bold.ttf";
    public const string NotoSerifRegular = "Fonts.NotoSerif-Regular.ttf";
    public const string NotoSerifBold = "Fonts.NotoSerif-Bold.ttf";
    public const string DejaVuSans = "Fonts.DejaVuSans.ttf";
    public const string UniversityLogo = "Images.university-logo.png";

    public static byte[] Read(string name)
    {
        using var stream = typeof(ExamAssets).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// The template file of a language, e.g. <c>ExamTemplate-ar.docx</c>. A hyphen rather than <c>.ar.docx</c>:
    /// MSBuild would read ".ar" as a culture and move the file into a satellite assembly.
    /// </summary>
    public static string WordTemplateFileName(ExamLanguage language) => language switch
    {
        ExamLanguage.Arabic => "ExamTemplate-ar.docx",
        ExamLanguage.English => "ExamTemplate-en.docx",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    public static string WordTemplate(ExamLanguage language) => "Word." + WordTemplateFileName(language);

    /// <summary>The regular weight of a paper's text font, which the Word template embeds.</summary>
    public static string TextFontRegular(ExamLanguage language) =>
        language == ExamLanguage.English ? NotoSerifRegular : NotoNaskhArabicRegular;
}
