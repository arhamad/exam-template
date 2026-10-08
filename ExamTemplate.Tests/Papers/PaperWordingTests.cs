using System.Globalization;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Tests.Papers;

public class PaperWordingTests
{
    [Theory]
    [InlineData(ExamLanguage.Arabic, true)]
    [InlineData(ExamLanguage.English, false)]
    public void Each_language_has_its_own_wording_and_direction(ExamLanguage language, bool rightToLeft)
    {
        var wording = PaperWording.For(language);

        Assert.Equal(language, wording.Language);
        Assert.Equal(rightToLeft, wording.IsRightToLeft);
    }

    [Fact]
    public void Undefined_language_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperWording.For((ExamLanguage)9));

    [Theory]
    [InlineData("1", "1 mark")]
    [InlineData("1.00", "1 mark")]
    [InlineData("0.5", "0.5 marks")]
    [InlineData("2", "2 marks")]
    [InlineData("2.5", "2.5 marks")]
    [InlineData("100", "100 marks")]
    public void English_marks_are_singular_only_for_one(string marks, string expected) =>
        Assert.Equal(expected, PaperWording.English.Marks(decimal.Parse(marks, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData(1, "1 question")]
    [InlineData(8, "8 questions")]
    public void English_question_counts_are_singular_only_for_one(int count, string expected) =>
        Assert.Equal(expected, PaperWording.English.Questions(count));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(11)]
    [InlineData(103)]
    public void Arabic_counts_follow_the_arabic_counted_noun_rules(int count)
    {
        Assert.Equal(ArabicText.Marks(count), PaperWording.Arabic.Marks(count));
        Assert.Equal(ArabicText.Questions(count), PaperWording.Arabic.Questions(count));
    }

    [Fact]
    public void Option_labels_follow_abjad_order_in_arabic_and_the_alphabet_in_english()
    {
        Assert.Equal(new[] { "أ", "ب", "ج", "د", "هـ", "و", "ز", "ح" }, Enumerable.Range(0, 8).Select(PaperWording.Arabic.OptionLabel));
        Assert.Equal(new[] { "A", "B", "C", "D", "E", "F", "G", "H" }, Enumerable.Range(0, 8).Select(PaperWording.English.OptionLabel));
    }

    [Theory]
    [InlineData(ExamLanguage.Arabic)]
    [InlineData(ExamLanguage.English)]
    public void Letters_beyond_the_supported_range_throw(ExamLanguage language)
    {
        var wording = PaperWording.For(language);

        Assert.Throws<ArgumentOutOfRangeException>(() => wording.OptionLabel(8));
        Assert.Throws<ArgumentOutOfRangeException>(() => wording.VersionLetter(4));
    }

    [Fact]
    public void Item_letters_follow_abjad_order_in_arabic_and_lowercase_letters_in_english()
    {
        Assert.Equal(new[] { "أ", "ب", "ج", "د", "هـ", "و", "ز", "ح", "ط", "ي", "ك", "ل" }, Enumerable.Range(0, 12).Select(PaperWording.Arabic.ItemLetter));
        Assert.Equal(28, PaperWording.Arabic.ItemLetterCount);
        Assert.Equal("غ", PaperWording.Arabic.ItemLetter(27));
        Assert.Equal("abcdefghijklmnopqrstuvwxyz", string.Concat(Enumerable.Range(0, 26).Select(PaperWording.English.ItemLetter)));
        Assert.Equal(26, PaperWording.English.ItemLetterCount);
    }

    [Fact]
    public void Item_letters_beyond_the_supported_range_throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperWording.Arabic.ItemLetter(28));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperWording.English.ItemLetter(26));
    }

    [Fact]
    public void Section_names_use_ordinal_words_in_arabic_and_numerals_in_english()
    {
        Assert.Equal("السؤال الأول", PaperWording.Arabic.SectionName(0));
        Assert.Equal("السؤال الخامس", PaperWording.Arabic.SectionName(4));
        Assert.Equal("السؤال العشرون", PaperWording.Arabic.SectionName(19));
        Assert.Equal("السؤال 21", PaperWording.Arabic.SectionName(20));
        Assert.Equal("Question 1", PaperWording.English.SectionName(0));
        Assert.Equal("Question 5", PaperWording.English.SectionName(4));
    }

    [Fact]
    public void Choice_notice_names_the_two_sections_of_a_pair()
    {
        Assert.Equal("أجب عن أحد السؤالين الآتيين فقط: السؤال الخامس أو السؤال السادس.", PaperWording.Arabic.ChoiceNotice(4));
        Assert.Equal("Answer only ONE of the following two questions: Question 2 or Question 3.", PaperWording.English.ChoiceNotice(1));
    }

    [Fact]
    public void English_item_letters_and_names_contain_no_arabic_characters()
    {
        var texts = Enumerable.Range(0, PaperWording.English.ItemLetterCount).Select(PaperWording.English.ItemLetter)
            .Concat(Enumerable.Range(0, ExamLimits.MaxSections).Select(PaperWording.English.SectionName))
            .Append(PaperWording.English.ChoiceNotice(0));

        Assert.All(texts, text => Assert.DoesNotContain(text, character => character is >= (char)0x0600 and <= (char)0x06FF));
    }

    [Fact]
    public void Versions_have_letters_in_the_exam_language_and_latin_letters_for_file_names()
    {
        Assert.Equal(new[] { "أ", "ب", "ج", "د" }, Enumerable.Range(0, 4).Select(PaperWording.Arabic.VersionLetter));
        Assert.Equal(new[] { "A", "B", "C", "D" }, Enumerable.Range(0, 4).Select(PaperWording.English.VersionLetter));
        Assert.Equal("ABCD", string.Concat(Enumerable.Range(0, 4).Select(PaperWording.VersionLatin)));
    }

    [Fact]
    public void Version_names_and_answer_key_banners()
    {
        Assert.Equal("النموذج ب", PaperWording.Arabic.VersionName("ب"));
        Assert.Equal("مفتاح الإجابة – النموذج ب – للمصحّح فقط", PaperWording.Arabic.AnswerKeyBanner("ب"));
        Assert.Equal("Version B", PaperWording.English.VersionName("B"));
        Assert.Equal("Answer key – Version B – for examiners only", PaperWording.English.AnswerKeyBanner("B"));
    }

    [Fact]
    public void True_false_answers_are_worded_in_the_exam_language()
    {
        Assert.Equal(("صح", "خطأ"), (PaperWording.Arabic.TrueFalse(true), PaperWording.Arabic.TrueFalse(false)));
        Assert.Equal(("True", "False"), (PaperWording.English.TrueFalse(true), PaperWording.English.TrueFalse(false)));
    }

    [Fact]
    public void English_labels_contain_no_arabic_characters()
    {
        var labels = typeof(PaperWording).GetProperties()
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => (string)property.GetValue(PaperWording.English)!)
            .ToList();

        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.DoesNotContain(label, character => character is >= (char)0x0600 and <= (char)0x06FF));
    }
}
