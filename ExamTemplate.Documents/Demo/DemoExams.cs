using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Demo;

/// <summary>Sample exams for the "load sample" action and for tests. Ids are fixed so shuffles are reproducible.</summary>
public static class DemoExams
{
    public static ExamDefinition ComputerLogicDesign() => new()
    {
        Header = new ExamHeader
        {
            CourseName = "تصميم منطق الحاسوب",
            CourseCode = "1290",
            Semester = "الفصل الأول 1261",
            AcademicYear = "2026/2027",
            Duration = "ساعة ونصف",
        },
        Instructions = [.. ExamDefinition.DefaultInstructions],
        VersionCount = 2,
        ShuffleSeed = 12345,
        Sections =
        [
            new ExamSection
            {
                Id = Id(1),
                Title = "القسم الأول: أسئلة موضوعية",
                Instruction = "اختر/ي الإجابة الأكثر صحة لكل سؤال.",
                Questions =
                [
                    Choice(1, 1, 10, "أي من العناصر الآتية يُعد دائرة منطقية توافقية؟", correct: 2,
                        ["المسجل", "العداد", "الجامع الكامل", "القلاب"], note: "اختر/ي إجابة واحدة فقط."),
                    Choice(1, 2, 10, "في القلاب SR، ماذا يمثل المدخل S عادةً؟", correct: 0,
                        ["Set", "Reset", "Select", "Shift", "لا شيء مما سبق"], keepOrder: true),
                    TrueFalse(1, 3, 5, "يمكن استخدام القلاب JK لبناء العدادات.", answer: true),
                    TrueFalse(1, 4, 5, "تحتفظ ذاكرة RAM بمحتواها بعد انقطاع التيار الكهربائي.", answer: false),
                ],
            },
            new ExamSection
            {
                Id = Id(2),
                Title = "القسم الثاني: أسئلة حل وشرح",
                Instruction = "أجب/ي بوضوح، وبيّن/ي خطوات الحل عند الحاجة.",
                Questions =
                [
                    Essay(2, 1, 15, "اشرح/ي الفرق بين الدوائر التوافقية والدوائر التتابعية مع ذكر مثال لكل نوع.", lines: 5,
                        "الدوائر التوافقية يعتمد خرجها على المدخلات الحالية فقط، أما التتابعية فيعتمد خرجها على المدخلات الحالية والحالة السابقة. مثال: الجامع الكامل مقابل المسجل."),
                    Essay(2, 2, 20, "صمّم/ي دائرة تحقق الدالة المنطقية المعطاة، ثم بسّط/ي الدالة ووضّح/ي خطوات التنفيذ.", lines: 8,
                        "تُبسّط الدالة أولًا باستخدام خريطة كارنوف، ثم تُرسم الدائرة وفق التعبير المبسّط مع توضيح البوابات والتوصيلات.",
                        note: "يمكن استخدام خريطة كارنوف في الحل."),
                ],
            },
            new ExamSection
            {
                Id = Id(3),
                Title = "القسم الثالث: مراجعة المفاهيم",
                Instruction = "أسئلة قصيرة لقياس فهم المفاهيم الأساسية.",
                Questions =
                [
                    Choice(3, 1, 15, "أي ذاكرة تُستخدم عادةً لتخزين محتوى ثابت يمكن قراءته؟", correct: 1,
                        ["RAM", "ROM", "Register", "Cache"]),
                    Essay(3, 2, 20, "اذكر/ي الخصائص الأساسية لمسجلات الإزاحة، ثم وضّح/ي أحد استخداماتها العملية.", lines: 6,
                        "تنقل مسجلات الإزاحة البيانات بتًا بعد بت يمينًا أو يسارًا، ومن تطبيقاتها التحويل بين الإرسال المتوازي والمتوالي."),
                ],
            },
        ],
    };

    private static Guid Id(int section, int question = 0, int option = 0) =>
        new($"00000000-0000-{section:D4}-{question:D4}-{option:D12}");

    private static ExamQuestion Choice(int section, int number, decimal marks, string text, int correct, string[] options,
        string note = "", bool keepOrder = false) => new()
    {
        Id = Id(section, number),
        Type = QuestionType.MultipleChoice,
        Text = text,
        Marks = marks,
        Note = note,
        KeepOptionOrder = keepOrder,
        Options = options
            .Select((option, index) => new ExamOption { Id = Id(section, number, index + 1), Text = option, IsCorrect = index == correct })
            .ToList(),
    };

    private static ExamQuestion TrueFalse(int section, int number, decimal marks, string text, bool answer) => new()
    {
        Id = Id(section, number),
        Type = QuestionType.TrueFalse,
        Text = text,
        Marks = marks,
        TrueFalseAnswer = answer,
    };

    private static ExamQuestion Essay(int section, int number, decimal marks, string text, int lines, string modelAnswer,
        string note = "") => new()
    {
        Id = Id(section, number),
        Type = QuestionType.Essay,
        Text = text,
        Marks = marks,
        Note = note,
        AnswerLines = lines,
        ModelAnswer = modelAnswer,
    };
}
