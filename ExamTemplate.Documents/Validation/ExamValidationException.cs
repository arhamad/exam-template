namespace ExamTemplate.Documents.Validation;

/// <summary>Generation was requested for an exam that still has validation errors.</summary>
public sealed class ExamValidationException(ValidationReport report)
    : Exception("الامتحان يحتوي على أخطاء يجب إصلاحها قبل التوليد.")
{
    public ValidationReport Report { get; } = report;
}
