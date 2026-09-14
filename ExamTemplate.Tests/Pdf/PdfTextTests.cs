using ExamTemplate.Documents.Pdf;

namespace ExamTemplate.Tests.Pdf;

public class PdfTextTests
{
    private static readonly string Lri = ((char)0x2066).ToString();
    private static readonly string Pdi = ((char)0x2069).ToString();

    [Fact]
    public void Left_to_right_islands_are_wrapped_in_isolates() =>
        Assert.Equal($"الفصل الأول {Lri}1261{Pdi} – {Lri}2026/2027{Pdi}", PdfText.Isolate("الفصل الأول 1261 – 2026/2027"));

    [Fact]
    public void Formulas_inside_arabic_sentences_are_isolated_whole() =>
        Assert.Equal($"قيمة {Lri}x_1 + x_2 ≤ 2³{Pdi} هي", PdfText.Isolate("قيمة x_1 + x_2 ≤ 2³ هي"));

    [Fact]
    public void Arabic_only_text_is_unchanged() => Assert.Equal("نص عربي فقط", PdfText.Isolate("نص عربي فقط"));

    [Fact]
    public void Left_to_right_text_is_isolated_as_a_whole() => Assert.Equal($"{Lri}5*4*2 = 40{Pdi}", PdfText.Isolate("5*4*2 = 40"));

    [Fact]
    public void Empty_text_stays_empty() => Assert.Equal("", PdfText.Isolate(""));
}
