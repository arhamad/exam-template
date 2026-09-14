using ExamTemplate.Documents.Model;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Model;

public class ExamModelTests
{
    [Fact]
    public void CreateNew_starts_with_one_empty_section_and_the_default_instructions()
    {
        var exam = ExamDefinition.CreateNew(new DateOnly(2026, 9, 14));

        var section = Assert.Single(exam.Sections);
        Assert.Empty(section.Questions);
        Assert.Equal(ExamDefinition.DefaultInstructions, exam.Instructions);
        Assert.Equal(1, exam.VersionCount);
        Assert.Equal(ExamDefinition.CurrentSchemaVersion, exam.SchemaVersion);
    }

    [Theory]
    [InlineData(2026, 9, "2026/2027")]
    [InlineData(2027, 1, "2026/2027")]
    [InlineData(2027, 8, "2027/2028")]
    public void CreateNew_derives_the_academic_year_from_the_date(int year, int month, string expected) =>
        Assert.Equal(expected, ExamDefinition.CreateNew(new DateOnly(year, month, 1)).Header.AcademicYear);

    [Fact]
    public void CreateNew_assigns_positive_random_seeds()
    {
        var seeds = Enumerable.Range(0, 20).Select(_ => ExamDefinition.CreateNew(new DateOnly(2026, 9, 1)).ShuffleSeed).ToList();

        Assert.All(seeds, seed => Assert.True(seed > 0));
        Assert.True(seeds.Distinct().Count() > 1);
    }

    [Fact]
    public void Create_multiple_choice_has_four_empty_unmarked_options()
    {
        var question = ExamQuestion.Create(QuestionType.MultipleChoice);

        Assert.Equal(QuestionType.MultipleChoice, question.Type);
        Assert.Equal(4, question.Options.Count);
        Assert.All(question.Options, option => Assert.Equal("", option.Text));
        Assert.DoesNotContain(question.Options, option => option.IsCorrect);
    }

    [Theory]
    [InlineData(QuestionType.TrueFalse)]
    [InlineData(QuestionType.Essay)]
    public void Create_other_types_have_no_options_and_no_answer(QuestionType type)
    {
        var question = ExamQuestion.Create(type);

        Assert.Equal(type, question.Type);
        Assert.Empty(question.Options);
        Assert.Null(question.TrueFalseAnswer);
    }

    [Fact]
    public void DeepClone_copies_content_with_new_ids()
    {
        var original = TestExams.Mcq("سؤال", 7.5m, 1, "أول", "ثانٍ", "ثالث");
        original.Note = "ملاحظة";
        original.KeepOptionOrder = true;

        var clone = original.DeepClone();

        Assert.NotEqual(original.Id, clone.Id);
        Assert.Equal((original.Type, original.Text, original.Marks, original.Note), (clone.Type, clone.Text, clone.Marks, clone.Note));
        Assert.True(clone.KeepOptionOrder);
        Assert.Equal(original.Options.Select(o => (o.Text, o.IsCorrect)), clone.Options.Select(o => (o.Text, o.IsCorrect)));
        Assert.Empty(original.Options.Select(o => o.Id).Intersect(clone.Options.Select(o => o.Id)));

        clone.Options[0].Text = "تغيير";
        Assert.Equal("أول", original.Options[0].Text);
    }

    [Fact]
    public void Totals_sum_marks_per_section_and_for_the_exam()
    {
        var exam = TestExams.TwoSections();

        Assert.Equal(5, exam.QuestionCount());
        Assert.Equal(40m, exam.TotalMarks());
        Assert.Equal(30m, exam.Sections[0].TotalMarks());
        Assert.Equal(10m, exam.Sections[1].TotalMarks());
    }
}
