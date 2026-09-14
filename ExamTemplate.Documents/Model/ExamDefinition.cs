using System.Security.Cryptography;

namespace ExamTemplate.Documents.Model;

/// <summary>An exam as authored in the editor and saved as JSON.</summary>
public sealed class ExamDefinition
{
    public const int CurrentSchemaVersion = 1;

    public static IReadOnlyList<string> DefaultInstructions { get; } =
    [
        "تأكد/ي من كتابة بياناتك قبل البدء.",
        "اقرأ/ي السؤال كاملًا قبل الإجابة.",
        "في أسئلة الاختيار من متعدد اختر/ي إجابة واحدة فقط.",
        "اكتب/ي الحل في المساحة المخصصة لكل سؤال.",
    ];

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public ExamHeader Header { get; set; } = new();
    public List<string> Instructions { get; set; } = [];
    public List<ExamSection> Sections { get; set; } = [];
    public int VersionCount { get; set; } = 1;
    public int ShuffleSeed { get; set; }

    public int QuestionCount() => Sections.Sum(section => section.Questions.Count);

    public decimal TotalMarks() => Sections.Sum(section => section.TotalMarks());

    public static ExamDefinition CreateNew() => CreateNew(DateOnly.FromDateTime(DateTime.Now));

    public static ExamDefinition CreateNew(DateOnly today)
    {
        // The academic year starts in the autumn semester.
        var startYear = today.Month >= 8 ? today.Year : today.Year - 1;

        return new ExamDefinition
        {
            Header = new ExamHeader { AcademicYear = $"{startYear}/{startYear + 1}" },
            Instructions = [.. DefaultInstructions],
            Sections = [new ExamSection()],
            ShuffleSeed = RandomNumberGenerator.GetInt32(1, int.MaxValue),
        };
    }
}
