using ExamTemplate.Documents.Assets;
using ExamTemplate.Documents.Model;

namespace ExamTemplate.Tests.Template;

public class ExamAssetsTests
{
    [Theory]
    [InlineData(ExamAssets.NotoNaskhArabicRegular)]
    [InlineData(ExamAssets.NotoNaskhArabicBold)]
    [InlineData(ExamAssets.NotoSerifRegular)]
    [InlineData(ExamAssets.NotoSerifBold)]
    [InlineData(ExamAssets.DejaVuSans)]
    public void Fonts_are_embedded_truetype_files(string name) =>
        Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x00 }, ExamAssets.Read(name)[..4]);

    [Theory]
    [InlineData(ExamLanguage.Arabic, "Word.ExamTemplate-ar.docx")]
    [InlineData(ExamLanguage.English, "Word.ExamTemplate-en.docx")]
    public void Each_language_has_an_embedded_word_template(ExamLanguage language, string name)
    {
        Assert.Equal(name, ExamAssets.WordTemplate(language));
        Assert.Equal("PK"u8.ToArray(), ExamAssets.Read(name)[..2]);
    }

    [Fact]
    public void Logo_is_an_embedded_png() =>
        Assert.Equal("PNG", System.Text.Encoding.ASCII.GetString(ExamAssets.Read(ExamAssets.UniversityLogo), 1, 3));

    [Fact]
    public void Missing_resources_fail_with_their_name()
    {
        var error = Assert.Throws<InvalidOperationException>(() => ExamAssets.Read("Fonts.Missing.ttf"));

        Assert.Contains("Fonts.Missing.ttf", error.Message);
    }
}
