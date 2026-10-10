using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Papers;

public enum PaperKind
{
    Student,
    AnswerKey,
}

public enum OptionLayout
{
    None,
    OneColumn,
    TwoColumns,
}

/// <summary>One printable version of an exam: shuffled, labelled, and with all display text prepared for the renderers.</summary>
/// <param name="MainQuestionCount">The number of sections, each one a main question; what the header prints as the question count.</param>
/// <param name="TotalMarks">The marks a student can earn: one section of each alternative pair counts.</param>
/// <param name="AnswerSummary">The key's answer table, one group per section that has objective items.</param>
public sealed record ExamPaper(
    ExamLanguage Language,
    int VersionIndex,
    string VersionLetter,
    char VersionLatin,
    PaperHeader Header,
    IReadOnlyList<string> Instructions,
    IReadOnlyList<PaperSection> Sections,
    int MainQuestionCount,
    string QuestionCountText,
    decimal TotalMarks,
    string TotalMarksText,
    IReadOnlyList<AnswerSummaryGroup> AnswerSummary);

public sealed record PaperHeader(
    string UniversityName,
    string ExamTitle,
    string Semester,
    string AcademicYear,
    string CourseName,
    string CourseCode,
    string Duration,
    string ExamMode,
    string ExamDateText);

/// <param name="Name">The generated name by position, «السؤال الأول» or "Question 1"; the summary uses it when the title is empty.</param>
/// <param name="ChoiceNotice">«أجب عن أحد السؤالين…» before the first section of an alternative pair, otherwise empty.</param>
public sealed record PaperSection(
    string Name,
    string Title,
    string Instruction,
    string ChoiceNotice,
    decimal Marks,
    string MarksText,
    IReadOnlyList<PaperQuestion> Questions)
{
    /// <summary>Untitled sections without instructions print no section bar.</summary>
    public bool HasBar => Title.Length > 0 || Instruction.Length > 0;

    public bool HasChoiceNotice => ChoiceNotice.Length > 0;

    public string SummaryHeading => Title.Length > 0 ? Title : Name;
}

/// <param name="Label">"3", "ج", "c", or "" for a lone essay; restarts in every section (<see cref="ItemLabels"/>).</param>
public sealed record PaperQuestion(
    Guid SourceId,
    string Label,
    QuestionType Type,
    string Text,
    string Note,
    decimal Marks,
    string MarksText,
    IReadOnlyList<PaperOption> Options,
    OptionLayout OptionLayout,
    bool? TrueFalseAnswer,
    int AnswerLines,
    string ModelAnswer,
    string CorrectAnswerText)
{
    /// <summary>The label as printed before the item: "3.", "ج.", "c." or nothing.</summary>
    public string PrintedLabel => ItemLabels.Printed(Label);
}

public sealed record PaperOption(string Label, string Text, bool IsCorrect);

/// <summary>The objective items of one section in the key's summary table, under the section's title or generated name.</summary>
public sealed record AnswerSummaryGroup(string Heading, IReadOnlyList<AnswerSummaryEntry> Entries);

/// <summary>An objective item's answer for the key's summary table ("ج", "صح", "True").</summary>
public sealed record AnswerSummaryEntry(string Label, string Answer);
