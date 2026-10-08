using System.Globalization;
using ExamTemplate.Documents.Design;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Documents.Word;

/// <summary>
/// Builds the values for the placeholders, loops and conditions in ExamTemplate.docx.
/// Every flag is precomputed so the template needs no <c>and</c>/<c>or</c>, and no value is null
/// (Templify treats a null property on a loop item as a missing variable).
/// </summary>
internal static class WordTemplateData
{
    public static Dictionary<string, object> Create(ExamPaper paper, PaperKind kind)
    {
        var isAnswerKey = kind == PaperKind.AnswerKey;
        var wording = PaperWording.For(paper.Language);
        var header = paper.Header;
        var semesterLine = header.Semester.Length > 0 ? $"{header.Semester} – {header.AcademicYear}" : header.AcademicYear;

        return new Dictionary<string, object>
        {
            ["UniversityName"] = Text(header.UniversityName),
            ["ExamTitle"] = Text(header.ExamTitle),
            ["SemesterLine"] = Text(semesterLine),
            ["AcademicYear"] = Text(header.AcademicYear),
            ["CourseName"] = Text(header.CourseName),
            ["CourseCode"] = Text(header.CourseCode),
            ["Duration"] = Text(header.Duration),
            ["ExamMode"] = Text(header.ExamMode),
            ["HasExamMode"] = header.ExamMode.Length > 0,
            ["ExamDateText"] = header.ExamDateText,
            ["HasExamDate"] = header.ExamDateText.Length > 0,
            ["VersionLetter"] = paper.VersionLetter,
            ["FooterVersionText"] = wording.VersionName(paper.VersionLetter),
            ["QuestionCountText"] = paper.QuestionCountText,
            ["TotalMarksText"] = paper.TotalMarksText,
            ["IsAnswerKey"] = isAnswerKey,
            ["HasAnswerSummary"] = isAnswerKey && paper.AnswerSummary.Count > 0,
            ["SummaryRows"] = SummaryRows(paper.AnswerSummary.SelectMany(group => group.Entries).ToList()),
            ["HasInstructions"] = paper.Instructions.Count > 0,
            ["Instructions"] = paper.Instructions.Select((instruction, index) => new InstructionItem(Number(index + 1), Text(instruction))).ToList(),
            ["Sections"] = paper.Sections.Select(section => Section(section, isAnswerKey)).ToList(),
            ["EndText"] = isAnswerKey ? wording.EndOfAnswerKey : wording.EndOfQuestions,
            ["EndNote"] = isAnswerKey ? "" : wording.GoodLuck,
        };
    }

    private static SectionItem Section(PaperSection section, bool isAnswerKey) => new(
        Text(section.Title),
        section.Title.Length > 0,
        Text(section.Instruction),
        section.Instruction.Length > 0,
        section.HasBar,
        section.MarksText,
        section.Questions.Select(question => Question(question, isAnswerKey)).ToList());

    private static QuestionItem Question(PaperQuestion question, bool isAnswerKey)
    {
        // The answer key fills the bubble of the correct option; the student copy shows empty bubbles.
        var options = question.Options
            .Select(option => new OptionItem(Bubble(isAnswerKey && option.IsCorrect), $"{option.Label})", Text(option.Text)))
            .ToList();

        var isEssay = question.Type == QuestionType.Essay;
        var showAnswerLines = isEssay && !isAnswerKey && question.AnswerLines > 0;
        var twoColumns = question.OptionLayout == OptionLayout.TwoColumns;

        return new QuestionItem(
            question.Label,
            Text(question.Text),
            Text(question.Note),
            question.Note.Length > 0,
            question.MarksText,
            question.Type == QuestionType.TrueFalse,
            Bubble(isAnswerKey && question.TrueFalseAnswer == true),
            Bubble(isAnswerKey && question.TrueFalseAnswer == false),
            question.OptionLayout == OptionLayout.OneColumn,
            question.OptionLayout == OptionLayout.OneColumn ? options : [],
            twoColumns,
            twoColumns ? Pairs(options) : [],
            showAnswerLines,
            showAnswerLines ? Enumerable.Range(1, question.AnswerLines).ToList() : [],
            isEssay && isAnswerKey && question.ModelAnswer.Length > 0,
            Text(question.ModelAnswer));
    }

    private static List<OptionRowItem> Pairs(List<OptionItem> options) =>
        options.Chunk(2)
            .Select(pair => pair.Length == 2
                ? new OptionRowItem(pair[0].Bubble, pair[0].Label, pair[0].Text, pair[1].Bubble, pair[1].Label, pair[1].Text)
                : new OptionRowItem(pair[0].Bubble, pair[0].Label, pair[0].Text, "", "", ""))
            .ToList();

    private static List<SummaryRowItem> SummaryRows(IReadOnlyList<AnswerSummaryEntry> entries) =>
        entries.Chunk(DesignTokens.SummaryPairsPerRow)
            .Select(row =>
            {
                string NumberAt(int index) => index < row.Length ? row[index].Label : "";
                string AnswerAt(int index) => index < row.Length ? row[index].Answer : "";
                return new SummaryRowItem(
                    NumberAt(0), AnswerAt(0), NumberAt(1), AnswerAt(1), NumberAt(2), AnswerAt(2),
                    NumberAt(3), AnswerAt(3), NumberAt(4), AnswerAt(4));
            })
            .ToList();

    private static string Text(string value) => TemplifyText.Protect(value);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bubble(bool filled) => filled ? DesignTokens.FilledBubble : DesignTokens.EmptyBubble;
}

internal sealed record InstructionItem(string Number, string Text);

internal sealed record SummaryRowItem(
    string N1, string A1, string N2, string A2, string N3, string A3, string N4, string A4, string N5, string A5);

internal sealed record SectionItem(
    string Title,
    bool HasTitle,
    string Instruction,
    bool HasInstruction,
    bool HasBar,
    string MarksText,
    List<QuestionItem> Questions);

internal sealed record QuestionItem(
    string Number,
    string Text,
    string Note,
    bool HasNote,
    string MarksText,
    bool IsTrueFalse,
    string TrueBubble,
    string FalseBubble,
    bool ShowOptionsOneColumn,
    List<OptionItem> Options,
    bool ShowOptionsTwoColumns,
    List<OptionRowItem> OptionRows,
    bool ShowAnswerLines,
    List<int> AnswerLines,
    bool ShowModelAnswer,
    string ModelAnswer);

internal sealed record OptionItem(string Bubble, string Label, string Text);

internal sealed record OptionRowItem(string Bubble1, string Label1, string Text1, string Bubble2, string Label2, string Text2);
