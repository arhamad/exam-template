using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Validation;

/// <summary>Generation was requested for an exam that still has validation errors.</summary>
public sealed class ExamValidationException(ValidationReport report)
    : ExamUserException(new LocalizedText(
        "الامتحان يحتوي على أخطاء يجب إصلاحها قبل التوليد.",
        "The exam has errors that must be fixed before generating."))
{
    public ValidationReport Report { get; } = report;
}
