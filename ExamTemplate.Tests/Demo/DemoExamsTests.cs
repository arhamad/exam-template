using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Validation;

namespace ExamTemplate.Tests.Demo;

public class DemoExamsTests
{
    [Fact]
    public void Computer_logic_design_is_valid_and_worth_100_marks()
    {
        var exam = DemoExams.ComputerLogicDesign();

        Assert.Empty(ExamValidator.Validate(exam).Issues);
        Assert.Equal(100m, exam.TotalMarks());
        Assert.Equal(8, exam.QuestionCount());
    }

    [Fact]
    public void English_computer_logic_design_is_valid_and_mirrors_the_arabic_exam()
    {
        var english = DemoExams.ComputerLogicDesignEnglish();
        var arabic = DemoExams.ComputerLogicDesign();

        Assert.Empty(ExamValidator.Validate(english).Issues);
        Assert.Equal(ExamLanguage.English, english.Language);
        Assert.Equal("Theoretical", english.Header.ExamMode);
        Assert.Equal((arabic.TotalMarks(), arabic.QuestionCount(), arabic.VersionCount, arabic.ShuffleSeed),
            (english.TotalMarks(), english.QuestionCount(), english.VersionCount, english.ShuffleSeed));
        Assert.Equal(Ids(arabic), Ids(english));
    }

    [Fact]
    public void Computer_logic_design_is_deterministic() =>
        Assert.Equal(ExamJson.Serialize(DemoExams.ComputerLogicDesign()), ExamJson.Serialize(DemoExams.ComputerLogicDesign()));

    [Fact]
    public void Optional_pair_demo_is_valid_with_six_questions_33_items_and_100_marks()
    {
        var exam = DemoExams.OptionalPair();

        Assert.Empty(ExamValidator.Validate(exam).Issues);
        Assert.Equal(6, exam.MainQuestionCount());
        Assert.Equal(33, exam.QuestionCount());
        Assert.Equal(100m, exam.TotalMarks());
        var first = Assert.Single(exam.Sections, section => section.AlternativeToNext);
        Assert.Equal(4, exam.Sections.IndexOf(first));
        Assert.Equal(first.TotalMarks(), exam.Sections[5].TotalMarks());
    }

    [Fact]
    public void Optional_pair_demo_is_deterministic() =>
        Assert.Equal(ExamJson.Serialize(DemoExams.OptionalPair()), ExamJson.Serialize(DemoExams.OptionalPair()));

    [Fact]
    public void Edge_cases_have_no_validation_errors()
    {
        var report = ExamValidator.Validate(DemoExams.EdgeCases());

        Assert.False(report.HasErrors, string.Join("; ", report.Issues.Select(issue => issue.Message.Arabic)));
    }

    private static IEnumerable<Guid> Ids(ExamDefinition exam) =>
        exam.Sections.SelectMany(section => section.Questions
            .SelectMany(question => question.Options.Select(option => option.Id).Prepend(question.Id))
            .Prepend(section.Id));
}
