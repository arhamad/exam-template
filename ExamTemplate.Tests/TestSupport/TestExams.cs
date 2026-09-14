using ExamTemplate.Documents.Model;

namespace ExamTemplate.Tests.TestSupport;

internal static class TestExams
{
    /// <summary>A small exam with one question of each type and no validation issues.</summary>
    public static ExamDefinition Valid() => new()
    {
        Header = new ExamHeader
        {
            CourseName = "تصميم منطق الحاسوب",
            CourseCode = "1290",
            AcademicYear = "2026/2027",
        },
        Instructions = ["اقرأ/ي السؤال كاملًا قبل الإجابة."],
        VersionCount = 1,
        ShuffleSeed = 12345,
        Sections =
        [
            new ExamSection
            {
                Title = "القسم الأول",
                Questions =
                [
                    Mcq("أي من العناصر الآتية يُعد دائرة منطقية توافقية؟", 10, 2, "المسجل", "العداد", "الجامع الكامل", "القلاب"),
                    TrueFalse("يمكن استخدام القلاب JK لبناء العدادات.", 5, true),
                    Essay("اشرح/ي الفرق بين الدوائر التوافقية والتتابعية.", 15, 6, "التوافقية تعتمد على المدخلات الحالية فقط."),
                ],
            },
        ],
    };

    /// <summary><see cref="Valid"/> plus a second section with two true/false questions (5 questions, 40 marks).</summary>
    public static ExamDefinition TwoSections()
    {
        var exam = Valid();
        exam.Sections.Add(new ExamSection
        {
            Title = "القسم الثاني",
            Questions = [TrueFalse("تحتفظ ذاكرة RAM بمحتواها بعد انقطاع التيار.", 5, false), TrueFalse("ROM ذاكرة للقراءة فقط.", 5, true)],
        });
        return exam;
    }

    public static ExamQuestion Mcq(string text, decimal marks, int correctIndex, params string[] options) => new()
    {
        Type = QuestionType.MultipleChoice,
        Text = text,
        Marks = marks,
        Options = options.Select((option, index) => new ExamOption { Text = option, IsCorrect = index == correctIndex }).ToList(),
    };

    public static ExamQuestion TrueFalse(string text, decimal marks, bool answer) => new()
    {
        Type = QuestionType.TrueFalse,
        Text = text,
        Marks = marks,
        TrueFalseAnswer = answer,
    };

    public static ExamQuestion Essay(string text, decimal marks, int lines, string modelAnswer) => new()
    {
        Type = QuestionType.Essay,
        Text = text,
        Marks = marks,
        AnswerLines = lines,
        ModelAnswer = modelAnswer,
    };
}
