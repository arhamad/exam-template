using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Tests.Papers;

public class BidiTextTests
{
    [Theory]
    [InlineData("نص عربي فقط", "R:نص عربي فقط")]
    [InlineData("Set", "L:Set")]
    [InlineData("CS-101", "L:CS-101")]
    [InlineData("2027/01/15", "L:2027/01/15")]
    [InlineData("for (var i = 1; i <= 20; i++) Console.WriteLine(i);", "L:for (var i = 1; i <= 20; i++) Console.WriteLine(i);")]
    [InlineData("–", "R:–")]
    public void Text_in_a_single_direction_stays_whole(string text, string expected) => Assert.Equal(expected, Describe(text));

    [Theory]
    [InlineData("الفصل الأول 1261 – 2026/2027", "R:الفصل الأول |L:1261|R: – |L:2026/2027")]
    [InlineData("النموذج أ – 2026/2027", "R:النموذج أ – |L:2026/2027")]
    [InlineData("1290 – تصميم منطق الحاسوب", "L:1290|R: – تصميم منطق الحاسوب")]
    [InlineData("42.75 علامة", "L:42.75|R: علامة")]
    [InlineData("اكتب/ي برنامجًا يطبع الأعداد من 1 إلى 20،", "R:اكتب/ي برنامجًا يطبع الأعداد من |L:1|R: إلى |L:20|R:،")]
    public void Numbers_and_dates_become_their_own_islands(string text, string expected) => Assert.Equal(expected, Describe(text));

    [Theory]
    [InlineData("اكتب/ي: x_1 + x_2 ≤ 2³ و 5*4*2 = 40.", "R:اكتب/ي: |L:x_1 + x_2 ≤ 2³|R: و |L:5*4*2 = 40|R:.")]
    [InlineData("في القلاب SR (Set = 1)، ما قيمة Q؟", "R:في القلاب |L:SR (Set = 1)|R:، ما قيمة |L:Q|R:؟")]
    public void Formulas_stay_together_and_sentence_punctuation_stays_arabic(string text, string expected) =>
        Assert.Equal(expected, Describe(text));

    [Theory]
    [InlineData("القلاب (Set = 1) مثلًا", "R:القلاب |L:(Set = 1)|R: مثلًا")]
    [InlineData("مقرر الحالات الخاصة (Edge Cases)", "R:مقرر الحالات الخاصة |L:(Edge Cases)")]
    [InlineData("(10 علامات)", "R:(|L:10|R: علامات)")]
    [InlineData("(2.5 علامة)", "R:(|L:2.5|R: علامة)")]
    public void Brackets_join_an_island_only_when_it_closes_them(string text, string expected) => Assert.Equal(expected, Describe(text));

    [Theory]
    [InlineData("Translate: جامعة القدس المفتوحة (QOU).", "L:Translate: |R:جامعة القدس المفتوحة|L: (QOU).")]
    [InlineData("A sequential circuit (دائرة تتابعية – مثل المسجل) has memory.", "L:A sequential circuit (|R:دائرة تتابعية – مثل المسجل|L:) has memory.")]
    [InlineData("See «الفصل 2» first.", "L:See «|R:الفصل 2|L:» first.")]
    [InlineData("2 الفصل and", "L:2 |R:الفصل|L: and")]
    [InlineData("جامعة القدس", "R:جامعة القدس")]
    [InlineData("Version A – 2026/2027 (x_1 ≤ 2³)", "L:Version A – 2026/2027 (x_1 ≤ 2³)")]
    public void In_english_text_arabic_phrases_become_islands(string text, string expected) =>
        Assert.Equal(expected, Describe(text, rightToLeft: false));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Empty_text_has_no_segments(bool rightToLeft) => Assert.Empty(BidiText.Split("", rightToLeft));

    private static string Describe(string text, bool rightToLeft = true) =>
        string.Join("|", BidiText.Split(text, rightToLeft).Select(segment => (segment.IsLeftToRight ? "L:" : "R:") + segment.Text));
}
