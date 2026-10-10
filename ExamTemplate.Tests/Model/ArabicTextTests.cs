using System.Globalization;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Tests.Model;

public class ArabicTextTests
{
    [Theory]
    [InlineData("1", "علامة واحدة")]
    [InlineData("2", "علامتان")]
    [InlineData("3", "3 علامات")]
    [InlineData("10", "10 علامات")]
    [InlineData("10.00", "10 علامات")]
    [InlineData("11", "11 علامة")]
    [InlineData("99", "99 علامة")]
    [InlineData("100", "100 علامة")]
    [InlineData("101", "101 علامة")]
    [InlineData("103", "103 علامات")]
    [InlineData("111", "111 علامة")]
    [InlineData("2.5", "2.5 علامة")]
    [InlineData("0.25", "0.25 علامة")]
    public void Marks_follow_arabic_counted_noun_rules(string marks, string expected) =>
        Assert.Equal(expected, ArabicText.Marks(decimal.Parse(marks, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData(1, "سؤال واحد")]
    [InlineData(2, "سؤالان")]
    [InlineData(6, "6 أسئلة")]
    [InlineData(11, "11 سؤالًا")]
    [InlineData(100, "100 سؤال")]
    [InlineData(103, "103 أسئلة")]
    public void Question_counts_follow_arabic_counted_noun_rules(int count, string expected) =>
        Assert.Equal(expected, ArabicText.Questions(count));

    [Theory]
    [InlineData(1, "خطأ واحد")]
    [InlineData(2, "خطآن")]
    [InlineData(7, "7 أخطاء")]
    [InlineData(12, "12 خطأً")]
    [InlineData(100, "100 خطأ")]
    public void Error_counts_follow_arabic_counted_noun_rules(int count, string expected) =>
        Assert.Equal(expected, ArabicText.Errors(count));

    [Theory]
    [InlineData(1, "بند واحد")]
    [InlineData(2, "بندان")]
    [InlineData(3, "3 بنود")]
    [InlineData(33, "33 بندًا")]
    [InlineData(100, "100 بند")]
    public void Item_counts_follow_arabic_counted_noun_rules(int count, string expected) =>
        Assert.Equal(expected, ArabicText.Items(count));

    [Theory]
    [InlineData(1, "الأول")]
    [InlineData(2, "الثاني")]
    [InlineData(5, "الخامس")]
    [InlineData(10, "العاشر")]
    [InlineData(11, "الحادي عشر")]
    [InlineData(12, "الثاني عشر")]
    [InlineData(19, "التاسع عشر")]
    [InlineData(20, "العشرون")]
    [InlineData(21, "21")]
    public void Ordinals_are_words_up_to_twenty_and_numerals_beyond(int number, string expected) =>
        Assert.Equal(expected, ArabicText.Ordinal(number));

    [Theory]
    [InlineData("12.50", "12.5")]
    [InlineData("7", "7")]
    [InlineData("0.25", "0.25")]
    public void Numbers_are_culture_invariant_without_trailing_zeros(string value, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-PS");
        try
        {
            Assert.Equal(expected, ArabicText.Number(decimal.Parse(value, CultureInfo.InvariantCulture)));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(QuestionType.MultipleChoice, "اختيار من متعدد")]
    [InlineData(QuestionType.TrueFalse, "صح أو خطأ")]
    [InlineData(QuestionType.Essay, "سؤال مقالي")]
    public void Question_types_have_arabic_labels(QuestionType type, string expected) =>
        Assert.Equal(expected, ArabicText.TypeLabel(type));

    [Fact]
    public void True_false_answers_have_arabic_words()
    {
        Assert.Equal("صح", ArabicText.TrueFalse(true));
        Assert.Equal("خطأ", ArabicText.TrueFalse(false));
    }
}
