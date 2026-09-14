using ExamTemplate.Documents.Assets;

namespace ExamTemplate.Tests.Template;

public class ExamAssetsTests
{
    [Theory]
    [InlineData(ExamAssets.NotoNaskhArabicRegular)]
    [InlineData(ExamAssets.NotoNaskhArabicBold)]
    [InlineData(ExamAssets.DejaVuSans)]
    public void Fonts_are_embedded_truetype_files(string name) =>
        Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x00 }, ExamAssets.Read(name)[..4]);

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
