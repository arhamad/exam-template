namespace ExamTemplate.Documents.Validation;

public enum IssueSeverity
{
    Error,
    Warning,
}

/// <param name="Field">Property name of the offending field (e.g. <c>CourseName</c>, <c>Text</c>, <c>CorrectOption</c>).</param>
public sealed record ValidationIssue(
    IssueSeverity Severity,
    string Message,
    string Field,
    Guid? SectionId = null,
    Guid? QuestionId = null,
    Guid? OptionId = null);

public sealed record ValidationReport(IReadOnlyList<ValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(issue => issue.Severity == IssueSeverity.Error);
}
