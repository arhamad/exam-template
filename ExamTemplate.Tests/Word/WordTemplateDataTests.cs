using System.Collections;
using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Design;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Word;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Word;

public class WordTemplateDataTests
{
    [Fact]
    public void Header_values_and_flags_come_from_the_paper()
    {
        var data = WordTemplateData.Create(Paper(DemoExams.ComputerLogicDesign(), 1), PaperKind.Student);

        Assert.Equal("تصميم منطق الحاسوب", data["CourseName"]);
        Assert.Equal("الفصل الأول 1261 – 2026/2027", data["SemesterLine"]);
        Assert.Equal("ب", data["VersionLetter"]);
        Assert.Equal("النموذج ب", data["FooterVersionText"]);
        Assert.Equal("8 أسئلة", data["QuestionCountText"]);
        Assert.Equal("100 علامة", data["TotalMarksText"]);
        Assert.Equal(true, data["HasExamMode"]);
        Assert.Equal(false, data["HasExamDate"]);
        Assert.Equal(true, data["HasInstructions"]);
    }

    [Fact]
    public void Semester_line_falls_back_to_the_academic_year_and_the_date_is_shown()
    {
        var data = WordTemplateData.Create(Paper(DemoExams.EdgeCases(), 0), PaperKind.Student);

        Assert.Equal("2026/2027", data["SemesterLine"]);
        Assert.Equal(false, data["HasExamMode"]);
        Assert.Equal(true, data["HasExamDate"]);
        Assert.Equal("15/01/2027", data["ExamDateText"]);
    }

    [Fact]
    public void Student_copy_has_empty_bubbles_answer_lines_and_no_answers()
    {
        var data = WordTemplateData.Create(Paper(DemoExams.ComputerLogicDesign(), 0), PaperKind.Student);
        var questions = Questions(data);

        Assert.Equal(false, data["IsAnswerKey"]);
        Assert.Equal(false, data["HasAnswerSummary"]);
        Assert.Equal(" – مع تمنياتنا لكم بالتوفيق", data["EndNote"]);
        Assert.DoesNotContain(DesignTokens.FilledBubble, Bubbles(questions));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, questions.First(question => question.ShowAnswerLines).AnswerLines);
        Assert.DoesNotContain(questions, question => question.ShowModelAnswer);
    }

    [Fact]
    public void Answer_key_fills_exactly_the_correct_bubbles_and_shows_model_answers()
    {
        var paper = Paper(DemoExams.ComputerLogicDesign(), 1);

        var data = WordTemplateData.Create(paper, PaperKind.AnswerKey);
        var questions = Questions(data);

        Assert.Equal(true, data["IsAnswerKey"]);
        Assert.Equal(true, data["HasAnswerSummary"]);
        Assert.Equal("", data["EndNote"]);
        Assert.Equal(paper.AnswerSummary.Count, Bubbles(questions).Count(bubble => bubble == DesignTokens.FilledBubble));
        Assert.DoesNotContain(questions, question => question.ShowAnswerLines);
        Assert.Equal(3, questions.Count(question => question.ShowModelAnswer));
    }

    [Fact]
    public void True_false_bubbles_mark_the_right_answer_in_the_key()
    {
        var questions = Questions(WordTemplateData.Create(Paper(DemoExams.ComputerLogicDesign(), 0), PaperKind.AnswerKey));

        var trueStatement = questions.Single(question => question.IsTrueFalse && question.Text.StartsWith("يمكن"));
        var falseStatement = questions.Single(question => question.IsTrueFalse && question.Text.StartsWith("تحتفظ"));

        Assert.Equal((DesignTokens.FilledBubble, DesignTokens.EmptyBubble), (trueStatement.TrueBubble, trueStatement.FalseBubble));
        Assert.Equal((DesignTokens.EmptyBubble, DesignTokens.FilledBubble), (falseStatement.TrueBubble, falseStatement.FalseBubble));
    }

    [Fact]
    public void Short_options_are_paired_into_rows_leaving_the_last_cell_empty_for_odd_counts()
    {
        var questions = Questions(WordTemplateData.Create(Paper(DemoExams.ComputerLogicDesign(), 0), PaperKind.Student));

        var fiveOptions = questions.Single(question => question.Text.StartsWith("في القلاب"));

        Assert.True(fiveOptions.ShowOptionsTwoColumns);
        Assert.False(fiveOptions.ShowOptionsOneColumn);
        Assert.Equal(3, fiveOptions.OptionRows.Count);
        Assert.Equal(("أ)", "Set", "ب)", "Reset"), (fiveOptions.OptionRows[0].Label1, fiveOptions.OptionRows[0].Text1, fiveOptions.OptionRows[0].Label2, fiveOptions.OptionRows[0].Text2));
        Assert.Equal(("هـ)", "", ""), (fiveOptions.OptionRows[2].Label1, fiveOptions.OptionRows[2].Label2, fiveOptions.OptionRows[2].Bubble2));
    }

    [Fact]
    public void Long_options_get_one_row_each()
    {
        var questions = Questions(WordTemplateData.Create(Paper(DemoExams.EdgeCases(), 0), PaperKind.Student));

        var eightOptions = questions.Single(question => question.Options.Count == 8);

        Assert.True(eightOptions.ShowOptionsOneColumn);
        Assert.False(eightOptions.ShowOptionsTwoColumns);
        Assert.Equal("ح)", eightOptions.Options[7].Label);
    }

    [Fact]
    public void Answer_summary_is_split_into_rows_of_five_padded_with_blanks()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions.AddRange(Enumerable.Range(1, 6).Select(i => TestExams.TrueFalse($"عبارة {i}", 1, i % 2 == 0)));

        var rows = (List<SummaryRowItem>)WordTemplateData.Create(Paper(exam, 0), PaperKind.AnswerKey)["SummaryRows"];

        Assert.Equal(2, rows.Count);
        Assert.Equal(("1", "ج", "2", "صح"), (rows[0].N1, rows[0].A1, rows[0].N2, rows[0].A2));
        Assert.Equal(("9", "صح", "", ""), (rows[1].N3, rows[1].A3, rows[1].N4, rows[1].A5));
    }

    [Fact]
    public void User_text_is_protected_from_templify_markdown()
    {
        var instructions = (List<InstructionItem>)WordTemplateData.Create(Paper(DemoExams.EdgeCases(), 0), PaperKind.Student)["Instructions"];

        var last = instructions[^1];

        Assert.Equal("5", last.Number);
        Assert.DoesNotContain(last.Text, character => character is '*' or '_' or '~' or '{' or '}');
        Assert.Equal("اكتب/ي الرموز كما هي: x_1 + x_2 ≤ 2³ و 5*4*2 = 40 و {{CourseName}}.", TemplifyText.Restore(last.Text));
    }

    [Theory]
    [InlineData(PaperKind.Student)]
    [InlineData(PaperKind.AnswerKey)]
    public void No_value_is_null_because_templify_treats_null_loop_values_as_missing(PaperKind kind)
    {
        var data = WordTemplateData.Create(Paper(DemoExams.EdgeCases(), 1), kind);

        Assert.All(data, pair => AssertNoNulls(pair.Value, pair.Key));
    }

    private static ExamPaper Paper(ExamDefinition exam, int version) => ExamPaperBuilder.Build(exam, version);

    private static List<QuestionItem> Questions(Dictionary<string, object> data) =>
        ((List<SectionItem>)data["Sections"]).SelectMany(section => section.Questions).ToList();

    private static IEnumerable<string> Bubbles(IEnumerable<QuestionItem> questions) =>
        questions.SelectMany(question => question.Options.Select(option => option.Bubble)
            .Concat(question.OptionRows.SelectMany(row => new[] { row.Bubble1, row.Bubble2 }))
            .Concat(question.IsTrueFalse ? [question.TrueBubble, question.FalseBubble] : []))
        .Where(bubble => bubble.Length > 0);

    private static void AssertNoNulls(object? value, string path)
    {
        Assert.True(value is not null, $"{path} is null");
        if (value is string or bool or int)
            return;

        if (value is IEnumerable items)
        {
            var index = 0;
            foreach (var item in items)
                AssertNoNulls(item, $"{path}[{index++}]");
            return;
        }

        foreach (var property in value!.GetType().GetProperties())
            AssertNoNulls(property.GetValue(value), $"{path}.{property.Name}");
    }
}
