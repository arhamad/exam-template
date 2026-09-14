namespace ExamTemplateDemo.Models;

public enum QuestionType
{
    MultipleChoice,
    Essay
}

public sealed class QuestionOption
{
    public required string Label { get; init; }
    public required string Text { get; init; }
    public bool IsCorrect { get; init; }
}

public sealed class ExamQuestion
{
    public int Number { get; init; }
    public QuestionType Type { get; init; }
    public required string Text { get; init; }
    public decimal Marks { get; init; }

    // اختياري: عنوان قسم قبل السؤال. اتركه null إن لم تحتج قسمًا جديدًا.
    public string? SectionTitle { get; init; }
    public string? SectionInstruction { get; init; }

    // اختيار من متعدد: يمكن أن يكون عدد الخيارات أي عدد.
    public List<QuestionOption> Options { get; init; } = [];

    // سؤال حل: عدد أسطر مساحة الحل المطبوعة تحت السؤال.
    public int AnswerLines { get; init; } = 4;
    public string? ModelAnswer { get; init; }
    public string? Note { get; init; }
}

public sealed class ExamModel
{
    public string UniversityName { get; init; } = "جامعة القدس المفتوحة";
    public string ExamTitle { get; init; } = "الامتحان النهائي";
    public string Semester { get; init; } = "الفصل الأول 1261";
    public string AcademicYear { get; init; } = "2026/2027";
    public required string CourseName { get; init; }
    public required string CourseCode { get; init; }
    public string Duration { get; init; } = "ساعة ونصف";
    public string ExamMode { get; init; } = "نظري";
    public decimal TotalMarks { get; init; } = 100;
    public List<string> Instructions { get; init; } = [];
    public List<ExamQuestion> Questions { get; init; } = [];
}
