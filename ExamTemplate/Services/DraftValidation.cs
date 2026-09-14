using ExamTemplate.Documents.Validation;

namespace ExamTemplate.Services;

/// <summary>A validation report indexed by what each issue points at, so every editor finds its own issues quickly.</summary>
public sealed class DraftValidation
{
    private readonly ILookup<Guid, ValidationIssue> byQuestion;
    private readonly ILookup<Guid, ValidationIssue> bySection;
    private readonly ILookup<string, ValidationIssue> byExamField;

    public DraftValidation(ValidationReport report)
    {
        Report = report;
        byQuestion = report.Issues.Where(issue => issue.QuestionId is not null).ToLookup(issue => issue.QuestionId!.Value);
        bySection = report.Issues.Where(issue => issue is { SectionId: not null, QuestionId: null }).ToLookup(issue => issue.SectionId!.Value);
        byExamField = report.Issues.Where(issue => issue is { SectionId: null, QuestionId: null }).ToLookup(issue => issue.Field);
        ErrorCount = report.Issues.Count(issue => issue.Severity == IssueSeverity.Error);
        WarningCount = report.Issues.Count - ErrorCount;
    }

    public static DraftValidation Empty { get; } = new(new ValidationReport([]));

    public ValidationReport Report { get; }

    public bool HasErrors => ErrorCount > 0;

    public int ErrorCount { get; }

    public int WarningCount { get; }

    /// <summary>Issues of one question (including its options).</summary>
    public IEnumerable<ValidationIssue> ForQuestion(Guid questionId) => byQuestion[questionId];

    /// <summary>Issues of the section itself, without its questions' issues.</summary>
    public IEnumerable<ValidationIssue> ForSection(Guid sectionId) => bySection[sectionId];

    /// <summary>Exam-level issues: header fields, <c>Instructions</c>, <c>Sections</c>, <c>VersionCount</c>, <c>ShuffleSeed</c>.</summary>
    public IEnumerable<ValidationIssue> ForField(string field) => byExamField[field];
}
