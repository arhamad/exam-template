using System.Globalization;
using System.Text.RegularExpressions;
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
        Assert.Contains("سؤالًا واحدًا", issue.Message.Arabic);
        Assert.Equal("Add at least one question.", issue.Message.English);
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

        Assert.Contains(ExamLimits.MaxQuestions.ToString(), SingleError(exam).Message.Arabic);
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
        Assert.Contains("(ب)", issue.Message.Arabic);
        // Letters follow the exam, whichever language the interface shows the message in.
        Assert.Contains("(ب)", issue.Message.English);
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

        Assert.Contains("(ج)", SingleWarning(exam).Message.Arabic);
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
    public void English_exams_name_their_own_letters_in_the_clustering_warning()
    {
        var exam = TestExams.Valid();
        exam.Language = ExamLanguage.English;
        exam.Sections[0].Questions.AddRange(
            Enumerable.Range(1, 3).Select(i => TestExams.Mcq($"Question {i}", 5, 2, "One", "Two", "Three", "Four")));

        var message = SingleWarning(exam).Message;

        Assert.Contains("(A)", message.Arabic);
        Assert.Contains("(C)", message.Arabic);
        Assert.Contains("version (A) are option (C)", message.English);
    }

    [Theory]
    [InlineData("All of the above")]
    [InlineData("None of these")]
    [InlineData("Both A and B")]
    public void English_position_dependent_options_warn_when_versions_shuffle_them(string text)
    {
        var exam = TestExams.Valid();
        exam.Language = ExamLanguage.English;
        exam.VersionCount = 2;
        exam.Sections[0].Questions[0].Options[3].Text = text;

        Assert.Equal(nameof(ExamQuestion.KeepOptionOrder), SingleWarning(exam).Field);
    }

    [Fact]
    public void Messages_number_questions_continuously_across_sections()
    {
        var exam = TestExams.TwoSections();
        exam.Sections[1].Questions[0].Text = "";

        var message = SingleError(exam).Message;

        Assert.Contains("السؤال 4", message.Arabic);
        Assert.Contains("question 4", message.English);
    }

    [Fact]
    public void Every_issue_has_an_english_message_without_arabic_text()
    {
        var withoutSections = English(TestExams.Valid());
        withoutSections.Sections.Clear();
        var withoutQuestions = English(TestExams.Valid());
        withoutQuestions.Sections[0].Questions.Clear();

        var issues = new[] { EveryIssue(), withoutSections, withoutQuestions }
            .SelectMany(exam => ExamValidator.Validate(exam).Issues)
            .ToList();

        // Every rule fires at least once. Numbers and letters vary between issues of the same kind.
        var kinds = issues.Select(issue => Regex.Replace(issue.Message.English, @"\d+|\([A-H]\)", "#")).Distinct().ToList();
        Assert.Equal(29, kinds.Count);
        Assert.All(issues, issue =>
        {
            Assert.DoesNotContain(issue.Message.English, IsArabic);
            Assert.Contains(issue.Message.Arabic, IsArabic);
        });
    }

    private static ExamDefinition English(ExamDefinition exam)
    {
        exam.Language = ExamLanguage.English;
        return exam;
    }

    /// <summary>An English exam that breaks every rule the validator checks once questions exist.</summary>
    private static ExamDefinition EveryIssue()
    {
        var exam = English(TestExams.Valid());
        exam.Header.UniversityName = "";
        exam.Header.Semester = new string('x', ExamLimits.MaxHeaderFieldLength + 1);
        exam.VersionCount = ExamLimits.MaxVersions + 1;
        exam.ShuffleSeed = 0;
        exam.Instructions = Enumerable.Repeat(new string('x', ExamLimits.MaxInstructionLength + 1), ExamLimits.MaxInstructions + 1).ToList();

        var noText = TestExams.Mcq("", 0, 0, "One", "Two");
        noText.Note = new string('x', ExamLimits.MaxNoteLength + 1);
        var twoCorrect = TestExams.Mcq("Two correct", 1, 0, "One", "Two");
        twoCorrect.Options[1].IsCorrect = true;
        var noAnswer = TestExams.TrueFalse("No answer", 1, true);
        noAnswer.TrueFalseAnswer = null;

        var questions = exam.Sections[0].Questions;
        questions.AddRange(
        [
            noText,
            TestExams.Mcq(new string('x', ExamLimits.MaxQuestionTextLength + 1), 1.234m, 0, "Only one"),
            TestExams.Mcq("Options", 1, -1, "", new string('x', ExamLimits.MaxOptionTextLength + 1), "Same", "same", "All of the above"),
            twoCorrect,
            noAnswer,
            TestExams.Essay("Lines", 1, ExamLimits.MaxAnswerLines + 1, new string('x', ExamLimits.MaxModelAnswerLength + 1)),
            TestExams.Essay("No model answer", 1, 3, ""),
        ]);
        questions.AddRange(Enumerable.Range(1, 4).Select(i => TestExams.Mcq($"Clustered {i}", 1, 0, "One", "Two")));
        questions.AddRange(Enumerable.Range(1, ExamLimits.MaxQuestions).Select(i => TestExams.TrueFalse($"Statement {i}", 1, true)));

        exam.Sections.Add(new ExamSection
        {
            Title = new string('x', ExamLimits.MaxSectionTitleLength + 1),
            Instruction = new string('x', ExamLimits.MaxInstructionLength + 1),
        });
        exam.Sections.AddRange(Enumerable.Range(0, ExamLimits.MaxSections).Select(_ => new ExamSection()));
        return exam;
    }

    private static bool IsArabic(char character) => character is >= (char)0x0600 and <= (char)0x06FF;

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
