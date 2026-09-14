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

/// <summary>One printable version of an exam: shuffled, numbered, and with all display text prepared for the renderers.</summary>
public sealed record ExamPaper(
    ExamLanguage Language,
    int VersionIndex,
    string VersionLetter,
    char VersionLatin,
    PaperHeader Header,
    IReadOnlyList<string> Instructions,
    IReadOnlyList<PaperSection> Sections,
    int QuestionCount,
    string QuestionCountText,
    decimal TotalMarks,
    string TotalMarksText,
    IReadOnlyList<AnswerSummaryEntry> AnswerSummary);

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

public sealed record PaperSection(
    string Title,
    string Instruction,
    decimal Marks,
    string MarksText,
    IReadOnlyList<PaperQuestion> Questions)
{
    /// <summary>Untitled sections without instructions print no section bar.</summary>
    public bool HasBar => Title.Length > 0 || Instruction.Length > 0;
}

public sealed record PaperQuestion(
    Guid SourceId,
    int Number,
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
    string CorrectAnswerText);

public sealed record PaperOption(string Label, string Text, bool IsCorrect);

/// <summary>An objective question's answer for the key's summary table ("ج", "صح", "True").</summary>
public sealed record AnswerSummaryEntry(int Number, string Answer);
