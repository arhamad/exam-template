using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Papers;

public class ItemLabelsTests
{
    [Fact]
    public void Objective_and_mixed_sections_use_numbers()
    {
        var objective = ItemLabels.For([QuestionType.MultipleChoice, QuestionType.TrueFalse, QuestionType.MultipleChoice], ExamLanguage.Arabic);
        var mixed = ItemLabels.For([QuestionType.Essay, QuestionType.MultipleChoice], ExamLanguage.English);

        Assert.Equal(ItemLabelStyle.Numbers, objective.Style);
        Assert.Equal(new[] { "1", "2", "3" }, Enumerable.Range(0, 3).Select(objective.Label));
        Assert.Equal(ItemLabelStyle.Numbers, mixed.Style);
        Assert.Equal(new[] { "1.", "2." }, Enumerable.Range(0, 2).Select(mixed.Printed));
    }

    [Theory]
    [InlineData(ExamLanguage.Arabic, "أ", "ب", "ج")]
    [InlineData(ExamLanguage.English, "a", "b", "c")]
    public void Essay_only_sections_use_letters_in_the_exam_language(ExamLanguage language, string first, string second, string third)
    {
        var labels = ItemLabels.For([QuestionType.Essay, QuestionType.Essay, QuestionType.Essay], language);

        Assert.Equal(ItemLabelStyle.Letters, labels.Style);
        Assert.Equal(new[] { first, second, third }, Enumerable.Range(0, 3).Select(labels.Label));
        Assert.Equal($"{second}.", labels.Printed(1));
    }

    [Fact]
    public void A_section_with_a_single_essay_has_no_label()
    {
        var labels = ItemLabels.For([QuestionType.Essay], ExamLanguage.Arabic);

        Assert.Equal(ItemLabelStyle.None, labels.Style);
        Assert.Equal("", labels.Label(0));
        Assert.Equal("", labels.Printed(0));
    }

    [Fact]
    public void A_section_with_a_single_objective_question_is_numbered()
    {
        var labels = ItemLabels.For([QuestionType.TrueFalse], ExamLanguage.Arabic);

        Assert.Equal(ItemLabelStyle.Numbers, labels.Style);
        Assert.Equal("1", labels.Label(0));
    }

    [Theory]
    [InlineData(ExamLanguage.Arabic, 28, ItemLabelStyle.Letters)]
    [InlineData(ExamLanguage.Arabic, 29, ItemLabelStyle.Numbers)]
    [InlineData(ExamLanguage.English, 26, ItemLabelStyle.Letters)]
    [InlineData(ExamLanguage.English, 27, ItemLabelStyle.Numbers)]
    public void Sections_with_more_essays_than_letters_fall_back_to_numbers(ExamLanguage language, int count, ItemLabelStyle expected)
    {
        var labels = ItemLabels.For(Enumerable.Repeat(QuestionType.Essay, count), language);

        Assert.Equal(expected, labels.Style);
        Assert.Equal(expected == ItemLabelStyle.Numbers ? count.ToString() : PaperWording.For(language).ItemLetter(count - 1), labels.Label(count - 1));
    }

    [Fact]
    public void Empty_sections_use_numbers_without_throwing() =>
        Assert.Equal(ItemLabelStyle.Numbers, ItemLabels.For([], ExamLanguage.Arabic).Style);

    [Fact]
    public void Printed_labels_end_with_a_dot_or_are_empty()
    {
        Assert.Equal("3.", ItemLabels.Printed("3"));
        Assert.Equal("أ.", ItemLabels.Printed("أ"));
        Assert.Equal("", ItemLabels.Printed(""));
    }

    [Fact]
    public void A_section_is_labelled_from_its_questions()
    {
        var section = TestExams.Valid().Sections[0];

        Assert.Equal(ItemLabelStyle.Numbers, ItemLabels.For(section, ExamLanguage.Arabic).Style);

        section.Questions.RemoveAll(question => question.Type != QuestionType.Essay);
        section.Questions.Add(TestExams.Essay("فرع ثانٍ", 5, 3, "إجابة"));

        Assert.Equal(ItemLabelStyle.Letters, ItemLabels.For(section, ExamLanguage.Arabic).Style);
    }
}
