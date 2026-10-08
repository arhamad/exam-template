using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Demo;

/// <summary>Sample exams for the "load sample" action and for tests. Ids are fixed so shuffles are reproducible.</summary>
public static class DemoExams
{
    private static readonly string LongQuestion = string.Join(" ",
        Enumerable.Repeat("هذا نص سؤال طويل جدًا يُستخدم لاختبار التفاف الأسطر وتقسيم الصفحات في ملفات Word وPDF.", 22));

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
        Instructions = [.. ExamDefaults.Arabic.Instructions],
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

    /// <summary>
    /// <see cref="ComputerLogicDesign"/> in English, with the same ids, seed and versions. One note keeps an Arabic term,
    /// so English papers show an Arabic phrase inside English text.
    /// </summary>
    public static ExamDefinition ComputerLogicDesignEnglish()
    {
        var header = ExamDefaults.English.Header("2026/2027");
        header.CourseName = "Computer Logic Design";
        header.CourseCode = "1290";
        header.Semester = "First Semester 1261";

        return new ExamDefinition
        {
            Language = ExamLanguage.English,
            Header = header,
            Instructions = [.. ExamDefaults.English.Instructions],
            VersionCount = 2,
            ShuffleSeed = 12345,
            Sections =
            [
                new ExamSection
                {
                    Id = Id(1),
                    Title = "Part One: Objective Questions",
                    Instruction = "Choose the best answer for each question.",
                    Questions =
                    [
                        Choice(1, 1, 10, "Which of the following is a combinational logic circuit?", correct: 2,
                            ["Register", "Counter", "Full adder", "Flip-flop"], note: "Choose one answer only."),
                        Choice(1, 2, 10, "In an SR flip-flop, what does the input S usually stand for?", correct: 0,
                            ["Set", "Reset", "Select", "Shift", "None of the above"], keepOrder: true),
                        TrueFalse(1, 3, 5, "A JK flip-flop can be used to build counters.", answer: true),
                        TrueFalse(1, 4, 5, "RAM keeps its contents after the power is switched off.", answer: false),
                    ],
                },
                new ExamSection
                {
                    Id = Id(2),
                    Title = "Part Two: Problems and Explanations",
                    Instruction = "Answer clearly and show your working where needed.",
                    Questions =
                    [
                        Essay(2, 1, 15, "Explain the difference between combinational and sequential circuits, giving an example of each.", lines: 5,
                            "The output of a combinational circuit depends only on its current inputs; a sequential circuit also depends on its previous state. Example: a full adder versus a register.",
                            note: "A sequential circuit (دائرة تتابعية – مثل المسجل) has memory."),
                        Essay(2, 2, 20, "Design a circuit for the given logic function, then simplify the function and explain each step.", lines: 8,
                            "First simplify the function with a Karnaugh map, then draw the circuit from the simplified expression, showing the gates and connections.",
                            note: "You may use a Karnaugh map."),
                    ],
                },
                new ExamSection
                {
                    Id = Id(3),
                    Title = "Part Three: Concept Review",
                    Instruction = "Short questions on the core concepts.",
                    Questions =
                    [
                        Choice(3, 1, 15, "Which memory is usually used to store fixed content that can be read?", correct: 1,
                            ["RAM", "ROM", "Register", "Cache"]),
                        Essay(3, 2, 20, "List the main properties of shift registers, then describe one of their practical uses.", lines: 6,
                            "Shift registers move data one bit at a time to the left or right; one use is converting between parallel and serial transmission."),
                    ],
                },
            ],
        };
    }

    /// <summary>
    /// Six main questions in the common final-exam shape: objective sections numbered 1–10, essay sections lettered
    /// أ، ب، ج…, a single-essay problem with no label, and a last pair where the student answers one of two questions.
    /// Six questions, 33 items, 100 marks (120 before the pair is counted once).
    /// </summary>
    public static ExamDefinition OptionalPair() => new()
    {
        Header = new ExamHeader
        {
            CourseName = "تصميم منطق الحاسوب",
            CourseCode = "1290",
            Semester = "الفصل الأول 1261",
            AcademicYear = "2026/2027",
            Duration = "ساعتان",
        },
        Instructions = [.. ExamDefaults.Arabic.Instructions, "أجب عن أحد السؤالين الآتيين فقط: السؤال الخامس أو السؤال السادس."],
        VersionCount = 2,
        ShuffleSeed = 2027,
        Sections =
        [
            new ExamSection
            {
                Id = Id(1),
                Title = "السؤال الأول: اختيار من متعدد",
                Instruction = "اختر/ي الإجابة الصحيحة لكل بند.",
                Questions =
                [
                    Choice(1, 1, 2, "أي البوابات الآتية يكون خرجها 1 فقط عندما تكون جميع مداخلها 1؟", correct: 1, ["OR", "AND", "NOT", "XOR"]),
                    Choice(1, 2, 2, "النظام العددي ذو الأساس 16 يُسمّى النظام:", correct: 3, ["الثنائي", "الثماني", "العشري", "السادس عشري"]),
                    Choice(1, 3, 2, "العدد الثنائي 1010 يساوي بالنظام العشري:", correct: 2, ["5", "8", "10", "12"]),
                    Choice(1, 4, 2, "البوابة التي يكون خرجها عكس خرج بوابة AND هي:", correct: 0, ["NAND", "NOR", "XOR", "OR"]),
                    Choice(1, 5, 2, "عدد المتغيرات في خريطة كارنوف ذات 16 خلية هو:", correct: 2, ["2", "3", "4", "5"]),
                    Choice(1, 6, 2, "المُرمِّز (Encoder) ذو 8 مداخل يحتاج من خطوط الخرج:", correct: 1, ["2", "3", "4", "8"]),
                    Choice(1, 7, 2, "أي مما يأتي دائرة تتابعية؟", correct: 3, ["الجامع الكامل", "المُرمِّز", "مُفكِّك الشفرة", "القلاب D"]),
                    Choice(1, 8, 2, "المُجمِّع (Multiplexer) ذو 4 مداخل يحتاج من خطوط الاختيار:", correct: 0, ["2", "1", "3", "4"]),
                    Choice(1, 9, 2, "المتمم الثنائي (2's complement) للعدد 0110 هو:", correct: 3, ["1001", "0110", "1111", "1010"]),
                    Choice(1, 10, 2, "بحسب قانون دي مورغان، (A·B)' يساوي:", correct: 2, ["A'·B'", "A+B", "A'+B'", "(A+B)'"]),
                ],
            },
            new ExamSection
            {
                Id = Id(2),
                Title = "السؤال الثاني: صح أم خطأ",
                Instruction = "ضع/ي علامة أمام الإجابة الصحيحة لكل عبارة.",
                Questions =
                [
                    TrueFalse(2, 1, 2, "تعطي بوابة XOR الخرج 1 عندما يختلف مدخلاها.", answer: true),
                    TrueFalse(2, 2, 2, "الرقم 2 رقم صالح في النظام الثنائي.", answer: false),
                    TrueFalse(2, 3, 2, "يمكن بناء أي دائرة منطقية باستخدام بوابات NAND فقط.", answer: true),
                    TrueFalse(2, 4, 2, "القلاب JK ليس له حالة ممنوعة.", answer: true),
                    TrueFalse(2, 5, 2, "مُفكِّك الشفرة 3 إلى 8 له ثمانية مداخل.", answer: false),
                    TrueFalse(2, 6, 2, "في العداد المتزامن تصل نبضة الساعة إلى جميع القلابات في اللحظة نفسها.", answer: true),
                    TrueFalse(2, 7, 2, "تفقد ذاكرة ROM محتواها عند انقطاع التيار الكهربائي.", answer: false),
                    TrueFalse(2, 8, 2, "تُستخدم خريطة كارنوف لتبسيط الدوال المنطقية.", answer: true),
                    TrueFalse(2, 9, 2, "يستقبل الجامع النصفي حملًا داخلًا (Carry in).", answer: false),
                    TrueFalse(2, 10, 2, "المسجل مجموعة من القلابات تخزّن عدة بتات معًا.", answer: true),
                ],
            },
            new ExamSection
            {
                Id = Id(3),
                Title = "السؤال الثالث: أسئلة قصيرة",
                Instruction = "أجب/ي باختصار عن الفروع الآتية.",
                Questions =
                [
                    Essay(3, 1, 4, "عرّف/ي الدائرة التوافقية.", lines: 3, "دائرة يعتمد خرجها على قيم مداخلها الحالية فقط، دون ذاكرة لحالة سابقة."),
                    Essay(3, 2, 4, "حوّل/ي العدد العشري 25 إلى النظام الثنائي مبيّنًا الخطوات.", lines: 3, "25 = 16 + 8 + 1، أي 11001."),
                    Essay(3, 3, 4, "اكتب/ي جدول الحقيقة لبوابة NOR.", lines: 4, "00 → 1، 01 → 0، 10 → 0، 11 → 0."),
                    Essay(3, 4, 4, "ما الفرق بين المزلاج (Latch) والقلاب (Flip-flop)؟", lines: 3, "المزلاج حساس لمستوى إشارة التمكين، أما القلاب فحساس لحافة نبضة الساعة."),
                    Essay(3, 5, 4, "اذكر/ي استخدامين عمليين للمسجلات.", lines: 3, "التخزين المؤقت للبيانات داخل المعالج، والتحويل بين الإرسال المتوازي والمتوالي."),
                ],
            },
            new ExamSection
            {
                Id = Id(4),
                Title = "السؤال الرابع: مسألة",
                Instruction = "بيّن/ي خطوات الحل كاملة.",
                Questions =
                [
                    Essay(4, 1, 20, "بسّط/ي الدالة F(A, B, C) = Σm(1, 3, 5, 7) باستخدام خريطة كارنوف، ثم ارسم/ي الدائرة المبسّطة.", lines: 10,
                        "تقع الحدود الأربعة في العمود C = 1 كله، فالدالة تُبسَّط إلى F = C، والدائرة سلك مباشر من المدخل C إلى الخرج."),
                ],
            },
            new ExamSection
            {
                Id = Id(5),
                Title = "السؤال الخامس: تصميم دائرة توافقية",
                Instruction = "صمّم/ي جامعًا نصفيًا (Half Adder).",
                AlternativeToNext = true,
                Questions =
                [
                    Essay(5, 1, 7, "اكتب/ي جدول الحقيقة للجامع النصفي.", lines: 5, "A B | S C: 00|00، 01|10، 10|10، 11|01."),
                    Essay(5, 2, 7, "اشتق/ي معادلتي الناتج (Sum) والحمل (Carry).", lines: 4, "S = A ⊕ B، C = A · B."),
                    Essay(5, 3, 6, "ارسم/ي الدائرة باستخدام بوابتي XOR وAND.", lines: 6, "بوابة XOR تعطي S، وبوابة AND تعطي C، ومدخلا كل منهما A وB."),
                ],
            },
            new ExamSection
            {
                Id = Id(6),
                Title = "السؤال السادس: تصميم دائرة تتابعية",
                Instruction = "صمّم/ي عدادًا ثنائيًا متزامنًا يعدّ من 0 إلى 3 باستخدام قلابات D.",
                Questions =
                [
                    Essay(6, 1, 5, "ارسم/ي مخطط الحالات للعداد.", lines: 5, "أربع حالات 00 → 01 → 10 → 11 ثم العودة إلى 00 مع كل نبضة ساعة."),
                    Essay(6, 2, 5, "اكتب/ي جدول الانتقالات (الحالة الحالية والحالة التالية).", lines: 5, "Q1Q0: 00→01، 01→10، 10→11، 11→00."),
                    Essay(6, 3, 5, "اشتق/ي معادلتي مدخلي القلابين D1 وD0.", lines: 4, "D0 = Q0'، D1 = Q1 ⊕ Q0."),
                    Essay(6, 4, 5, "ارسم/ي الدائرة النهائية.", lines: 6, "قلابان D يشتركان في نبضة الساعة، مع بوابة NOT لـ D0 وبوابة XOR لـ D1."),
                ],
            },
        ],
    };

    /// <summary>Rendering stress cases: very long text, eight long options, 20 answer lines, mixed scripts and markdown-like characters.</summary>
    public static ExamDefinition EdgeCases() => new()
    {
        Header = new ExamHeader
        {
            CourseName = "مقرر الحالات الخاصة (Edge Cases)",
            CourseCode = "CS-101",
            Semester = "",
            AcademicYear = "2026/2027",
            Duration = "ساعتان",
            ExamMode = "",
            ExamDate = new DateOnly(2027, 1, 15),
        },
        Instructions = [.. ExamDefaults.Arabic.Instructions, "اكتب/ي الرموز كما هي: x_1 + x_2 ≤ 2³ و 5*4*2 = 40 و {{CourseName}}."],
        VersionCount = 2,
        ShuffleSeed = 2026,
        Sections =
        [
            new ExamSection
            {
                Id = Id(1),
                Questions =
                [
                    Choice(1, 1, 5, LongQuestion, correct: 7,
                        Enumerable.Range(1, 8).Select(number => $"الخيار الطويل رقم {number} الذي يتجاوز ثلاثين حرفًا بوضوح").ToArray()),
                    Choice(1, 2, 2.5m, "في القلاب SR (Set = 1)، ما قيمة الخرج Q عندما x_1 = 0؟", correct: 1, ["0", "1", "Q'", "غير معرّف"]),
                    TrueFalse(1, 3, 1, "العبارة **مهمة** و __ليست__ ~~محذوفة~~ و *مائلة*.", answer: false),
                    Essay(1, 4, 20, "اكتب/ي برنامجًا يطبع الأعداد من 1 إلى 20،\nثم اشرح/ي كل سطر فيه.", lines: 20,
                        "for (var i = 1; i <= 20; i++) Console.WriteLine(i);"),
                    Essay(1, 5, 7.25m, "سؤال بلا أسطر إجابة؛ تُكتب الإجابة في دفتر منفصل.", lines: 0, "إجابة مختصرة."),
                ],
            },
            new ExamSection
            {
                Id = Id(2),
                Title = "قسم له عنوان فقط",
                Questions =
                [
                    TrueFalse(2, 1, 3, "الرقم 2026 عدد زوجي.", answer: true),
                    Choice(2, 2, 4, "اختر/ي الإجابة الصحيحة:", correct: 2, ["الأولى", "الثانية", "الثالثة"]),
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
