using System.Globalization;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Validation;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Validation;

public class ExamValidatorTests
{
    [Fact]
    public void Valid_exam_has_no_issues() => Assert.Empty(ExamValidator.Validate(TestExams.Valid()).Issues);

    [Theory]
    [InlineData(nameof(ExamHeader.UniversityName))]
    [InlineData(nameof(ExamHeader.ExamTitle))]
    [InlineData(nameof(ExamHeader.CourseName))]
    [InlineData(nameof(ExamHeader.CourseCode))]
    [InlineData(nameof(ExamHeader.AcademicYear))]
    [InlineData(nameof(ExamHeader.Duration))]
    public void Required_header_fields_must_not_be_blank(string field)
    {
        var exam = TestExams.Valid();
        typeof(ExamHeader).GetProperty(field)!.SetValue(exam.Header, "  ");

        Assert.Equal(field, SingleError(exam).Field);
    }

    [Fact]
    public void Header_field_longer_than_the_limit_is_an_error()
    {
        var exam = TestExams.Valid();
        exam.Header.CourseName = new string('م', ExamLimits.MaxHeaderFieldLength + 1);

        Assert.Equal(nameof(ExamHeader.CourseName), SingleError(exam).Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Version_count_must_be_between_one_and_four(int count)
    {
        var exam = TestExams.Valid();
        exam.VersionCount = count;

        Assert.Equal(nameof(ExamDefinition.VersionCount), SingleError(exam).Field);
    }

    [Fact]
    public void Shuffle_seed_must_be_positive()
    {
        var exam = TestExams.Valid();
        exam.ShuffleSeed = 0;

        Assert.Equal(nameof(ExamDefinition.ShuffleSeed), SingleError(exam).Field);
    }

    [Fact]
    public void Instruction_longer_than_the_limit_is_an_error()
    {
        var exam = TestExams.Valid();
        exam.Instructions.Add(new string('ت', ExamLimits.MaxInstructionLength + 1));

        Assert.Equal(nameof(ExamDefinition.Instructions), SingleError(exam).Field);
    }

    [Fact]
    public void Exam_without_sections_is_an_error()
    {
        var exam = TestExams.Valid();
        exam.Sections.Clear();

        Assert.Equal(nameof(ExamDefinition.Sections), SingleError(exam).Field);
    }

    [Fact]
    public void Exam_without_questions_reports_a_single_error()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions.Clear();

        var issue = SingleError(exam);

        Assert.Equal(nameof(ExamDefinition.Sections), issue.Field);
        Assert.Contains("سؤالًا واحدًا", issue.Message);
    }

    [Fact]
    public void Empty_section_is_an_error_when_the_exam_has_questions()
    {
        var exam = TestExams.Valid();
        var empty = new ExamSection { Title = "قسم فارغ" };
        exam.Sections.Add(empty);

        Assert.Equal(empty.Id, SingleError(exam).SectionId);
    }

    [Fact]
    public void Question_limit_is_enforced()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions = Enumerable.Range(1, ExamLimits.MaxQuestions + 1)
            .Select(i => TestExams.TrueFalse($"عبارة {i}", 0.5m, true))
            .ToList();

        Assert.Contains(ExamLimits.MaxQuestions.ToString(), SingleError(exam).Message);
    }

    [Fact]
    public void Question_text_is_required()
    {
        var exam = TestExams.Valid();
        var question = exam.Sections[0].Questions[1];
        question.Text = " ";

        var issue = SingleError(exam);

        Assert.Equal(nameof(ExamQuestion.Text), issue.Field);
        Assert.Equal(question.Id, issue.QuestionId);
        Assert.Equal(exam.Sections[0].Id, issue.SectionId);
    }

    [Fact]
    public void Question_text_longer_than_the_limit_is_an_error()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Text = new string('س', ExamLimits.MaxQuestionTextLength + 1);

        Assert.Equal(nameof(ExamQuestion.Text), SingleError(exam).Field);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("100.5")]
    [InlineData("2.125")]
    public void Marks_must_be_positive_at_most_100_with_two_decimals(string marks)
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Marks = decimal.Parse(marks, CultureInfo.InvariantCulture);

        Assert.Equal(nameof(ExamQuestion.Marks), SingleError(exam).Field);
    }

    [Fact]
    public void Marks_with_two_decimals_are_valid()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Marks = 12.25m;

        Assert.Empty(ExamValidator.Validate(exam).Issues);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void Multiple_choice_needs_two_to_eight_options(int count)
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0] = TestExams.Mcq("سؤال", 10, 0, Enumerable.Range(1, count).Select(i => $"خيار {i}").ToArray());

        Assert.Equal(nameof(ExamQuestion.Options), SingleError(exam).Field);
    }

    [Fact]
    public void Empty_option_is_an_error_that_identifies_the_option()
    {
        var exam = TestExams.Valid();
        var option = exam.Sections[0].Questions[0].Options[1];
        option.Text = "";

        var issue = SingleError(exam);

        Assert.Equal(option.Id, issue.OptionId);
        Assert.Contains("(ب)", issue.Message);
    }

    [Fact]
    public void Multiple_choice_without_a_correct_option_is_an_error()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Options.ForEach(option => option.IsCorrect = false);

        Assert.Equal("CorrectOption", SingleError(exam).Field);
    }

    [Fact]
    public void Multiple_choice_with_two_correct_options_is_an_error()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Options[0].IsCorrect = true;

        Assert.Equal("CorrectOption", SingleError(exam).Field);
    }

    [Fact]
    public void True_false_answer_is_required()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[1].TrueFalseAnswer = null;

        Assert.Equal(nameof(ExamQuestion.TrueFalseAnswer), SingleError(exam).Field);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void Answer_lines_must_be_between_zero_and_twenty(int lines)
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[2].AnswerLines = lines;

        Assert.Equal(nameof(ExamQuestion.AnswerLines), SingleError(exam).Field);
    }

    [Fact]
    public void Zero_answer_lines_are_allowed()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[2].AnswerLines = 0;

        Assert.Empty(ExamValidator.Validate(exam).Issues);
    }

    [Fact]
    public void Duplicate_options_are_a_warning()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Options[3].Text = " الجامع الكامل ";

        Assert.Equal(nameof(ExamQuestion.Options), SingleWarning(exam).Field);
    }

    [Fact]
    public void Essay_without_a_model_answer_is_a_warning()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[2].ModelAnswer = "";

        Assert.Equal(nameof(ExamQuestion.ModelAnswer), SingleWarning(exam).Field);
    }

    [Fact]
    public void Position_dependent_options_warn_when_versions_shuffle_them()
    {
        var exam = TestExams.Valid();
        exam.VersionCount = 2;
        exam.Sections[0].Questions[0].Options[3].Text = "جميع ما سبق";

        Assert.Equal(nameof(ExamQuestion.KeepOptionOrder), SingleWarning(exam).Field);
    }

    [Fact]
    public void Position_dependent_options_are_fine_when_their_order_is_kept()
    {
        var exam = TestExams.Valid();
        exam.VersionCount = 2;
        exam.Sections[0].Questions[0].Options[3].Text = "جميع ما سبق";
        exam.Sections[0].Questions[0].KeepOptionOrder = true;

        Assert.Empty(ExamValidator.Validate(exam).Issues);
    }

    [Fact]
    public void Position_dependent_options_are_fine_with_a_single_version()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Options[3].Text = "جميع ما سبق";

        Assert.Empty(ExamValidator.Validate(exam).Issues);
    }

    [Fact]
    public void Correct_answers_clustered_on_one_letter_are_a_warning()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions.AddRange(
            Enumerable.Range(1, 3).Select(i => TestExams.Mcq($"سؤال {i}", 5, 2, "أول", "ثانٍ", "ثالث", "رابع")));

        Assert.Contains("(ج)", SingleWarning(exam).Message);
    }

    [Fact]
    public void Evenly_spread_correct_answers_do_not_warn()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions.AddRange(
            new[] { 0, 1, 3 }.Select(correct => TestExams.Mcq($"سؤال {correct}", 5, correct, "أول", "ثانٍ", "ثالث", "رابع")));

        Assert.Empty(ExamValidator.Validate(exam).Issues);
    }

    [Fact]
    public void Messages_number_questions_continuously_across_sections()
    {
        var exam = TestExams.TwoSections();
        exam.Sections[1].Questions[0].Text = "";

        Assert.Contains("السؤال 4", SingleError(exam).Message);
    }

    private static ValidationIssue SingleError(ExamDefinition exam)
    {
        var report = ExamValidator.Validate(exam);
        Assert.True(report.HasErrors);
        return Assert.Single(report.Issues);
    }

    private static ValidationIssue SingleWarning(ExamDefinition exam)
    {
        var report = ExamValidator.Validate(exam);
        Assert.False(report.HasErrors);
        var issue = Assert.Single(report.Issues);
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        return issue;
    }
}
