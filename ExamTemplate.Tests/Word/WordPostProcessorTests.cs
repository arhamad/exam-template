using System.Security;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTemplate.Documents.Word;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Word;

public class WordPostProcessorTests
{
    [Fact]
    public void Protected_characters_are_restored()
    {
        var runs = Runs(Apply(RunXml(TemplifyText.Protect("x_1 * 2 ~ {y}"), rtl: false)));

        Assert.Equal(new[] { ("x_1 * 2 ~ {y}", false) }, runs);
    }

    [Fact]
    public void Left_to_right_islands_move_into_runs_without_the_rtl_flag()
    {
        var runs = Runs(Apply(RunXml("الفصل الأول 1261 – 2026/2027", rtl: true)));

        Assert.Equal(new[] { ("الفصل الأول ", true), ("1261", false), (" – ", true), ("2026/2027", false) }, runs);
    }

    [Fact]
    public void Split_runs_keep_the_original_formatting()
    {
        var docx = Apply(RunXml("العلامة 42.75 علامة", rtl: true, bold: true));

        using var document = WordprocessingDocument.Open(new MemoryStream(docx), false);
        var runs = document.MainDocumentPart!.Document!.Body!.Descendants<Run>().ToList();

        Assert.Equal(3, runs.Count);
        Assert.All(runs, run =>
        {
            Assert.NotNull(run.RunProperties?.Bold);
            Assert.NotNull(run.RunProperties?.BoldComplexScript);
        });
    }

    [Fact]
    public void Restored_characters_stay_inside_their_island()
    {
        var runs = Runs(Apply(RunXml(TemplifyText.Protect("قيمة x_1 = 0؟"), rtl: true)));

        Assert.Equal(new[] { ("قيمة ", true), ("x_1 = 0", false), ("؟", true) }, runs);
    }

    [Fact]
    public void Arabic_only_runs_and_runs_without_the_rtl_flag_are_not_split()
    {
        var runs = Runs(Apply(RunXml("نص عربي فقط", rtl: true) + RunXml("Set 2026/2027 نص", rtl: false)));

        Assert.Equal(new[] { ("نص عربي فقط", true), ("Set 2026/2027 نص", false) }, runs);
    }

    private static string RunXml(string text, bool rtl, bool bold = false) =>
        $"<w:p><w:r><w:rPr>{(bold ? "<w:b/><w:bCs/>" : "")}{(rtl ? "<w:rtl/>" : "")}</w:rPr><w:t xml:space=\"preserve\">{SecurityElement.Escape(text)}</w:t></w:r></w:p>";

    private static byte[] Apply(string bodyXml)
    {
        using var docx = TestDocx.Create(bodyXml);
        WordPostProcessor.Apply(docx);
        return docx.ToArray();
    }

    private static List<(string Text, bool IsRightToLeft)> Runs(byte[] docx)
    {
        using var document = WordprocessingDocument.Open(new MemoryStream(docx), false);
        return document.MainDocumentPart!.Document!.Body!.Descendants<Run>()
            .Select(run => (string.Concat(run.Elements<Text>().Select(text => text.Text)), run.RunProperties?.RightToLeftText is not null))
            .ToList();
    }
}
