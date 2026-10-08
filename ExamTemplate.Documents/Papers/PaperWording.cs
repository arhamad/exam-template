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
        ItemLetters =
        [
            "أ", "ب", "ج", "د", "هـ", "و", "ز", "ح", "ط", "ي", "ك", "ل", "م", "ن",
            "س", "ع", "ف", "ص", "ق", "ر", "ش", "ت", "ث", "خ", "ذ", "ض", "ظ", "غ",
        ],
        QuestionWord = "السؤال",
        ChoiceNoticeFormat = "أجب عن أحد السؤالين الآتيين فقط: {0} أو {1}.",
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
        ItemLetters = [.. "abcdefghijklmnopqrstuvwxyz".Select(letter => letter.ToString())],
        QuestionWord = "Question",
        ChoiceNoticeFormat = "Answer only ONE of the following two questions: {0} or {1}.",
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
    private IReadOnlyList<string> ItemLetters { get; init; } = [];
    private string QuestionWord { get; init; } = "";
    private string ChoiceNoticeFormat { get; init; } = "";

    /// <summary>How many essay parts can be lettered («أ» … «غ», "a" … "z") before a section falls back to numbers.</summary>
    public int ItemLetterCount => ItemLetters.Count;

    public static PaperWording For(ExamLanguage language) => language switch
    {
        ExamLanguage.Arabic => Arabic,
        ExamLanguage.English => English,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    /// <summary>The version letter in Latin script (A–D), for file names.</summary>
    public static char VersionLatin(int index) => (char)('A' + CheckIndex(index, ExamLimits.MaxVersions));

    public string Marks(decimal marks) => IsRightToLeft ? ArabicText.Marks(marks) : EnglishText.Marks(marks);

    public string Questions(int count) => IsRightToLeft ? ArabicText.Questions(count) : EnglishText.Questions(count);

    public string OptionLabel(int index) => OptionLabels[CheckIndex(index, OptionLabels.Count)];

    public string VersionLetter(int index) => VersionLetters[CheckIndex(index, VersionLetters.Count)];

    /// <summary>The letter of an essay part: abjad order in Arabic, lowercase in English, unlike the uppercase option labels.</summary>
    public string ItemLetter(int index) => ItemLetters[CheckIndex(index, ItemLetters.Count)];

    /// <summary>The generated name of the main question at a zero-based position: «السؤال الخامس» or "Question 5".</summary>
    public string SectionName(int index) =>
        $"{QuestionWord} {(IsRightToLeft ? ArabicText.Ordinal(index + 1) : ArabicText.Number(index + 1))}";

    /// <summary>The notice printed before the first of two alternative sections, naming both by position.</summary>
    public string ChoiceNotice(int firstIndex) => string.Format(ChoiceNoticeFormat, SectionName(firstIndex), SectionName(firstIndex + 1));

    /// <summary>«النموذج أ» or "Version A".</summary>
    public string VersionName(string letter) => $"{Version} {letter}";

    public string AnswerKeyBanner(string letter) => $"{AnswerKey} – {VersionName(letter)} – {ExaminersOnly}";

    public string TrueFalse(bool value) => value ? TrueWord : FalseWord;

    private static int CheckIndex(int index, int count) =>
        index >= 0 && index < count ? index : throw new ArgumentOutOfRangeException(nameof(index), index, null);
}
