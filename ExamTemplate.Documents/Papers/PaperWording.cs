using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Papers;

/// <summary>
/// The fixed words printed on a paper, in the exam's language. Labels are stored exactly as printed, including any
/// colon and trailing space, so the Word template and the PDF layout print the same text.
/// </summary>
public sealed class PaperWording
{
    public static PaperWording Arabic { get; } = new()
    {
        Language = ExamLanguage.Arabic,
        Course = "المقرر: ",
        CourseCode = "رقم المقرر: ",
        Duration = "مدة الامتحان: ",
        QuestionCount = "عدد الأسئلة: ",
        TotalMarks = "العلامة الكلية: ",
        Version = "النموذج",
        ExamMode = "نوع الامتحان: ",
        Date = "التاريخ: ",
        StudentName = "اسم الطالب:",
        StudentId = "الرقم الجامعي:",
        StudentGroup = "الشعبة:",
        AnswerSummary = "ملخص الإجابات",
        Instructions = "تعليمات الامتحان",
        Note = "ملاحظة: ",
        ModelAnswer = "الإجابة النموذجية:",
        StudentCopy = "نسخة الطالب",
        AnswerKey = "مفتاح الإجابة",
        ExaminersOnly = "للمصحّح فقط",
        EndOfQuestions = "انتهت الأسئلة",
        EndOfAnswerKey = "انتهى مفتاح الإجابة",
        GoodLuck = " – مع تمنياتنا لكم بالتوفيق",
        Page = "صفحة ",
        PageOf = " من ",
        LogoDescription = "شعار الجامعة",
        TrueWord = ArabicText.TrueFalse(true),
        FalseWord = ArabicText.TrueFalse(false),
        OptionLabels = ["أ", "ب", "ج", "د", "هـ", "و", "ز", "ح"],
        VersionLetters = ["أ", "ب", "ج", "د"],
    };

    public static PaperWording English { get; } = new()
    {
        Language = ExamLanguage.English,
        Course = "Course: ",
        CourseCode = "Course code: ",
        Duration = "Duration: ",
        QuestionCount = "Questions: ",
        TotalMarks = "Total marks: ",
        Version = "Version",
        ExamMode = "Exam type: ",
        Date = "Date: ",
        StudentName = "Name:",
        StudentId = "Student ID:",
        StudentGroup = "Section:",
        AnswerSummary = "Answer summary",
        Instructions = "Exam instructions",
        Note = "Note: ",
        ModelAnswer = "Model answer:",
        StudentCopy = "Student copy",
        AnswerKey = "Answer key",
        ExaminersOnly = "for examiners only",
        EndOfQuestions = "End of questions",
        EndOfAnswerKey = "End of answer key",
        GoodLuck = " – Good luck",
        Page = "Page ",
        PageOf = " of ",
        LogoDescription = "University logo",
        TrueWord = "True",
        FalseWord = "False",
        OptionLabels = ["A", "B", "C", "D", "E", "F", "G", "H"],
        VersionLetters = ["A", "B", "C", "D"],
    };

    private PaperWording()
    {
    }

    public required ExamLanguage Language { get; init; }
    public required string Course { get; init; }
    public required string CourseCode { get; init; }
    public required string Duration { get; init; }
    public required string QuestionCount { get; init; }
    public required string TotalMarks { get; init; }

    /// <summary>The word above the version letter in the header box.</summary>
    public required string Version { get; init; }

    public required string ExamMode { get; init; }
    public required string Date { get; init; }
    public required string StudentName { get; init; }
    public required string StudentId { get; init; }
    public required string StudentGroup { get; init; }
    public required string AnswerSummary { get; init; }
    public required string Instructions { get; init; }
    public required string Note { get; init; }
    public required string ModelAnswer { get; init; }
    public required string StudentCopy { get; init; }
    public required string AnswerKey { get; init; }
    public required string ExaminersOnly { get; init; }
    public required string EndOfQuestions { get; init; }
    public required string EndOfAnswerKey { get; init; }

    /// <summary>Printed after <see cref="EndOfQuestions"/> on the student copy.</summary>
    public required string GoodLuck { get; init; }

    /// <summary>"Page " before the page number; <see cref="PageOf"/> goes between it and the page count.</summary>
    public required string Page { get; init; }

    public required string PageOf { get; init; }
    public required string LogoDescription { get; init; }

    public bool IsRightToLeft => Language == ExamLanguage.Arabic;

    private string TrueWord { get; init; } = "";
    private string FalseWord { get; init; } = "";
    private IReadOnlyList<string> OptionLabels { get; init; } = [];
    private IReadOnlyList<string> VersionLetters { get; init; } = [];

    public static PaperWording For(ExamLanguage language) => language switch
    {
        ExamLanguage.Arabic => Arabic,
        ExamLanguage.English => English,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    /// <summary>The version letter in Latin script (A–D), for file names.</summary>
    public static char VersionLatin(int index) => (char)('A' + CheckIndex(index, ExamLimits.MaxVersions));

    public string Marks(decimal marks) => IsRightToLeft ? ArabicText.Marks(marks) : Counted(marks, "mark", "marks");

    public string Questions(int count) => IsRightToLeft ? ArabicText.Questions(count) : Counted(count, "question", "questions");

    public string OptionLabel(int index) => OptionLabels[CheckIndex(index, OptionLabels.Count)];

    public string VersionLetter(int index) => VersionLetters[CheckIndex(index, VersionLetters.Count)];

    /// <summary>«النموذج أ» or "Version A".</summary>
    public string VersionName(string letter) => $"{Version} {letter}";

    public string AnswerKeyBanner(string letter) => $"{AnswerKey} – {VersionName(letter)} – {ExaminersOnly}";

    public string TrueFalse(bool value) => value ? TrueWord : FalseWord;

    private static string Counted(decimal value, string one, string many) => $"{ArabicText.Number(value)} {(value == 1 ? one : many)}";

    private static int CheckIndex(int index, int count) =>
        index >= 0 && index < count ? index : throw new ArgumentOutOfRangeException(nameof(index), index, null);
}
