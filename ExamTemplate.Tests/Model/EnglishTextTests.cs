using System.Globalization;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Tests.Model;

public class EnglishTextTests
{
    [Theory]
    [InlineData("1", "1 mark")]
    [InlineData("1.00", "1 mark")]
    [InlineData("0.5", "0.5 marks")]
    [InlineData("2", "2 marks")]
    [InlineData("12.25", "12.25 marks")]
    public void Marks_are_singular_only_for_one(string marks, string expected) =>
        Assert.Equal(expected, EnglishText.Marks(decimal.Parse(marks, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData(1, "1 question", "1 error")]
    [InlineData(2, "2 questions", "2 errors")]
    [InlineData(11, "11 questions", "11 errors")]
    public void Counts_are_singular_only_for_one(int count, string questions, string errors)
    {
        Assert.Equal(questions, EnglishText.Questions(count));
        Assert.Equal(errors, EnglishText.Errors(count));
    }

    [Fact]
    public void Type_labels_and_true_false_are_english()
    {
        Assert.Equal("Multiple choice", EnglishText.TypeLabel(QuestionType.MultipleChoice));
        Assert.Equal("True or false", EnglishText.TypeLabel(QuestionType.TrueFalse));
        Assert.Equal("Essay", EnglishText.TypeLabel(QuestionType.Essay));
        Assert.Equal("True", EnglishText.TrueFalse(true));
        Assert.Equal("False", EnglishText.TrueFalse(false));
        Assert.Throws<ArgumentOutOfRangeException>(() => EnglishText.TypeLabel((QuestionType)9));
    }

    [Fact]
    public void English_paper_wording_uses_the_same_counts()
    {
        Assert.Equal(EnglishText.Marks(2.5m), PaperWording.English.Marks(2.5m));
        Assert.Equal(EnglishText.Questions(1), PaperWording.English.Questions(1));
    }
}
