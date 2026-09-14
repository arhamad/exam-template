using System.Text.Json;
using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Validation;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Papers;

public class ExamPaperBuilderTests
{
    [Fact]
    public void Version_zero_keeps_the_authored_order()
    {
        var exam = Demo();

        var paper = ExamPaperBuilder.Build(exam, 0);

        for (var index = 0; index < exam.Sections.Count; index++)
            Assert.Equal(exam.Sections[index].Questions.Select(q => q.Id), paper.Sections[index].Questions.Select(q => q.SourceId));
        foreach (var question in Questions(paper))
            Assert.Equal(Source(exam, question).Options.Select(o => o.Text), question.Options.Select(o => o.Text));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Later_versions_shuffle_questions_only_within_their_sections(int version)
    {
        var exam = Demo();

        var paper = ExamPaperBuilder.Build(exam, version);

        Assert.Equal(exam.Sections.Select(s => s.Title), paper.Sections.Select(s => s.Title));
        for (var index = 0; index < exam.Sections.Count; index++)
        {
            var authored = exam.Sections[index].Questions.Select(q => q.Id).ToList();
            var shuffled = paper.Sections[index].Questions.Select(q => q.SourceId).ToList();
            Assert.Equal(authored.Order(), shuffled.Order());
            Assert.NotEqual(authored, shuffled);
        }
    }

    [Fact]
    public void Questions_are_numbered_continuously_after_shuffling()
    {
        var paper = ExamPaperBuilder.Build(Demo(), 2);

        Assert.Equal(Enumerable.Range(1, 8), Questions(paper).Select(q => q.Number));
        Assert.Equal(8, paper.QuestionCount);
    }

    [Fact]
    public void Same_seed_builds_identical_papers() =>
        Assert.Equal(Snapshot(ExamPaperBuilder.Build(Demo(), 1)), Snapshot(ExamPaperBuilder.Build(Demo(), 1)));

    [Fact]
    public void Different_seeds_build_different_papers()
    {
        var other = Demo();
        other.ShuffleSeed = 54321;

        Assert.NotEqual(Snapshot(ExamPaperBuilder.Build(Demo(), 1)), Snapshot(ExamPaperBuilder.Build(other, 1)));
    }

    [Fact]
    public void Kept_option_order_is_never_shuffled()
    {
        var exam = Demo();
        var kept = exam.Sections[0].Questions[1];
        Assert.True(kept.KeepOptionOrder);

        foreach (var paper in ExamPaperBuilder.BuildAll(exam))
            Assert.Equal(kept.Options.Select(o => o.Text), Find(paper, kept.Id).Options.Select(o => o.Text));
    }

    [Fact]
    public void Shuffled_options_keep_their_correct_answer_and_are_relabelled()
    {
        var exam = Demo();

        foreach (var paper in ExamPaperBuilder.BuildAll(exam))
        {
            foreach (var question in Questions(paper).Where(q => q.Type == QuestionType.MultipleChoice))
            {
                var source = Source(exam, question);
                var correct = Assert.Single(question.Options, o => o.IsCorrect);
                Assert.Equal(source.Options.Single(o => o.IsCorrect).Text, correct.Text);
                Assert.Equal($"{correct.Label}) {correct.Text}", question.CorrectAnswerText);
                Assert.Equal(Enumerable.Range(0, question.Options.Count).Select(PaperWording.Arabic.OptionLabel), question.Options.Select(o => o.Label));
                Assert.Equal(source.Options.Select(o => o.Text).Order(), question.Options.Select(o => o.Text).Order());
            }
        }
    }

    [Fact]
    public void True_false_and_essay_questions_keep_their_details()
    {
        var paper = ExamPaperBuilder.Build(Demo(), 3);

        Assert.All(Questions(paper).Where(q => q.Type == QuestionType.TrueFalse), question =>
        {
            Assert.Empty(question.Options);
            Assert.Equal(OptionLayout.None, question.OptionLayout);
            Assert.Equal(ArabicText.TrueFalse(question.TrueFalseAnswer!.Value), question.CorrectAnswerText);
        });

        var essay = Find(paper, new Guid("00000000-0000-0002-0002-000000000000"));
        Assert.Equal(8, essay.AnswerLines);
        Assert.StartsWith("تُبسّط الدالة", essay.ModelAnswer);
        Assert.Equal("يمكن استخدام خريطة كارنوف في الحل.", essay.Note);
        Assert.Equal("", essay.CorrectAnswerText);
    }

    [Fact]
    public void Adding_a_question_to_one_section_does_not_reshuffle_the_others()
    {
        var before = ExamPaperBuilder.Build(Demo(), 1);
        var exam = Demo();
        exam.Sections[1].Questions.Add(TestExams.Essay("سؤال إضافي", 5, 4, "إجابة نموذجية"));

        var after = ExamPaperBuilder.Build(exam, 1);

        foreach (var index in new[] { 0, 2 })
            Assert.Equal(Order(before.Sections[index]), Order(after.Sections[index]));
    }

    [Fact]
    public void Marks_and_totals_are_prepared_as_arabic_text()
    {
        var paper = ExamPaperBuilder.Build(Demo(), 0);

        Assert.Equal(100m, paper.TotalMarks);
        Assert.Equal("100 علامة", paper.TotalMarksText);
        Assert.Equal("8 أسئلة", paper.QuestionCountText);
        Assert.Equal("30 علامة", paper.Sections[0].MarksText);
        Assert.Equal("10 علامات", paper.Sections[0].Questions[0].MarksText);
    }

    [Fact]
    public void Answer_summary_lists_objective_questions_in_paper_order()
    {
        var paper = ExamPaperBuilder.Build(Demo(), 1);
        var objective = Questions(paper).Where(q => q.Type != QuestionType.Essay).ToList();

        Assert.Equal(objective.Select(q => q.Number), paper.AnswerSummary.Select(entry => entry.Number));
        foreach (var (entry, question) in paper.AnswerSummary.Zip(objective))
        {
            var expected = question.Type == QuestionType.TrueFalse
                ? ArabicText.TrueFalse(question.TrueFalseAnswer!.Value)
                : question.Options.Single(o => o.IsCorrect).Label;
            Assert.Equal(expected, entry.Answer);
        }
    }

    [Fact]
    public void Short_options_use_two_columns_and_long_or_multi_line_options_use_one()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions.Add(TestExams.Mcq("خيارات طويلة", 5, 0, new string('خ', OptionLayoutRules.MaxTwoColumnOptionLength + 1), "قصير"));
        exam.Sections[0].Questions.Add(TestExams.Mcq("خيار من سطرين", 5, 0, "سطر أول\nسطر ثانٍ", "قصير"));

        var questions = ExamPaperBuilder.Build(exam, 0).Sections[0].Questions;

        Assert.Equal(
            new[] { OptionLayout.TwoColumns, OptionLayout.None, OptionLayout.None, OptionLayout.OneColumn, OptionLayout.OneColumn },
            questions.Select(q => q.OptionLayout));
    }

    [Fact]
    public void Text_is_trimmed_normalized_and_stripped_of_reserved_characters()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0].Text = "  السطر الأول\r\nالسطر\uE001 الثاني\r ";
        exam.Instructions = ["  ", " تعليمة واحدة "];

        var paper = ExamPaperBuilder.Build(exam, 0);

        Assert.Equal("السطر الأول\nالسطر الثاني", paper.Sections[0].Questions[0].Text);
        Assert.Equal(new[] { "تعليمة واحدة" }, paper.Instructions);
    }

    [Fact]
    public void Header_is_copied_with_the_exam_date_formatted()
    {
        var exam = TestExams.Valid();
        exam.Header.ExamDate = new DateOnly(2027, 1, 15);

        var header = ExamPaperBuilder.Build(exam, 0).Header;

        Assert.Equal("15/01/2027", header.ExamDateText);
        Assert.Equal("1290", header.CourseCode);
        Assert.Equal("جامعة القدس المفتوحة", header.UniversityName);
    }

    [Fact]
    public void Missing_exam_date_is_empty() =>
        Assert.Equal("", ExamPaperBuilder.Build(TestExams.Valid(), 0).Header.ExamDateText);

    [Fact]
    public void Section_bar_is_needed_only_with_a_title_or_instruction()
    {
        var exam = TestExams.TwoSections();
        exam.Sections[1].Title = "";

        var paper = ExamPaperBuilder.Build(exam, 0);

        Assert.True(paper.Sections[0].HasBar);
        Assert.False(paper.Sections[1].HasBar);
    }

    [Fact]
    public void Exam_with_errors_cannot_be_built()
    {
        var exam = TestExams.Valid();
        exam.Header.CourseCode = "";

        var error = Assert.Throws<ExamValidationException>(() => ExamPaperBuilder.BuildAll(exam));

        Assert.True(error.Report.HasErrors);
    }

    [Fact]
    public void Warnings_do_not_block_building()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[2].ModelAnswer = "";

        Assert.Single(ExamPaperBuilder.BuildAll(exam));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Version_index_must_be_within_the_version_count(int index) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ExamPaperBuilder.Build(Demo(versions: 4), index));

    [Fact]
    public void BuildAll_returns_one_paper_per_version()
    {
        var papers = ExamPaperBuilder.BuildAll(Demo(versions: 3));

        Assert.Equal(new[] { "أ", "ب", "ج" }, papers.Select(p => p.VersionLetter));
        Assert.Equal("ABC", string.Concat(papers.Select(p => p.VersionLatin)));
    }

    [Fact]
    public void English_papers_use_english_letters_and_wording()
    {
        var exam = TestExams.Valid();
        exam.Language = ExamLanguage.English;
        exam.VersionCount = 2;

        var paper = ExamPaperBuilder.Build(exam, 1);
        var choice = Questions(paper).Single(question => question.Type == QuestionType.MultipleChoice);
        var trueFalse = Questions(paper).Single(question => question.Type == QuestionType.TrueFalse);

        Assert.Equal(ExamLanguage.English, paper.Language);
        Assert.Equal(("B", 'B'), (paper.VersionLetter, paper.VersionLatin));
        Assert.Equal("3 questions", paper.QuestionCountText);
        Assert.Equal("30 marks", paper.TotalMarksText);
        Assert.Equal(new[] { "A", "B", "C", "D" }, choice.Options.Select(option => option.Label));
        Assert.Equal("10 marks", choice.MarksText);
        Assert.Equal("True", trueFalse.CorrectAnswerText);
        Assert.Contains(new AnswerSummaryEntry(trueFalse.Number, "True"), paper.AnswerSummary);
    }

    private static ExamDefinition Demo(int versions = 4)
    {
        var exam = DemoExams.ComputerLogicDesign();
        exam.VersionCount = versions;
        return exam;
    }

    private static IEnumerable<PaperQuestion> Questions(ExamPaper paper) => paper.Sections.SelectMany(section => section.Questions);

    private static PaperQuestion Find(ExamPaper paper, Guid sourceId) => Questions(paper).Single(q => q.SourceId == sourceId);

    private static ExamQuestion Source(ExamDefinition exam, PaperQuestion question) =>
        exam.Sections.SelectMany(section => section.Questions).Single(q => q.Id == question.SourceId);

    private static string Order(PaperSection section) =>
        string.Join(" | ", section.Questions.Select(q => $"{q.SourceId}: {string.Join(", ", q.Options.Select(o => o.Text))}"));

    private static string Snapshot(ExamPaper paper) => JsonSerializer.Serialize(paper);
}
