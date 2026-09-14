namespace ExamTemplate.Documents.Model;

/// <summary>
/// Starting values of an exam in each language. When the author switches the language, fields that still hold
/// the old language's defaults take the new ones (<see cref="ExamEditing.ChangeLanguage"/>).
/// </summary>
public sealed class ExamDefaults
{
    public static ExamDefaults Arabic { get; } = new()
    {
        UniversityName = "جامعة القدس المفتوحة",
        ExamTitle = "الامتحان النهائي",
        Semester = "الفصل الأول",
        Duration = "ساعة ونصف",
        ExamModes = ["نظري", "عملي"],
        Instructions =
        [
            "تأكد/ي من كتابة بياناتك قبل البدء.",
            "اقرأ/ي السؤال كاملًا قبل الإجابة.",
            "في أسئلة الاختيار من متعدد اختر/ي إجابة واحدة فقط.",
            "اكتب/ي الحل في المساحة المخصصة لكل سؤال.",
        ],
        CourseNamePlaceholder = "مثال: تصميم منطق الحاسوب",
        SectionTitlePlaceholder = "مثال: القسم الأول: أسئلة موضوعية",
        SectionInstructionPlaceholder = "مثال: اختر/ي الإجابة الأكثر صحة لكل سؤال.",
    };

    public static ExamDefaults English { get; } = new()
    {
        UniversityName = "Al-Quds Open University",
        ExamTitle = "Final Exam",
        Semester = "First Semester",
        Duration = "1.5 hours",
        ExamModes = ["Theoretical", "Practical"],
        Instructions =
        [
            "Write your details before you start.",
            "Read each question carefully before answering.",
            "For multiple-choice questions, choose one answer only.",
            "Write your answers in the space provided for each question.",
        ],
        CourseNamePlaceholder = "e.g. Computer Logic Design",
        SectionTitlePlaceholder = "e.g. Part One: Objective Questions",
        SectionInstructionPlaceholder = "e.g. Choose the best answer for each question.",
    };

    private ExamDefaults()
    {
    }

    public required string UniversityName { get; init; }
    public required string ExamTitle { get; init; }
    public required string Semester { get; init; }
    public required string Duration { get; init; }

    /// <summary>The exam modes offered in the editor. The same position means the same mode in every language.</summary>
    public required IReadOnlyList<string> ExamModes { get; init; }

    public required IReadOnlyList<string> Instructions { get; init; }
    public required string CourseNamePlaceholder { get; init; }
    public required string SectionTitlePlaceholder { get; init; }
    public required string SectionInstructionPlaceholder { get; init; }

    public string ExamMode => ExamModes[0];

    public static ExamDefaults For(ExamLanguage language) => language switch
    {
        ExamLanguage.Arabic => Arabic,
        ExamLanguage.English => English,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    public ExamHeader Header(string academicYear) => new()
    {
        UniversityName = UniversityName,
        ExamTitle = ExamTitle,
        Semester = Semester,
        AcademicYear = academicYear,
        Duration = Duration,
        ExamMode = ExamMode,
    };
}
