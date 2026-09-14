using ExamTemplateDemo.Models;

namespace ExamTemplateDemo;

public static class DemoData
{
    public static ExamModel Create() => new()
    {
        CourseName = "تصميم منطق الحاسوب",
        CourseCode = "1290",
        Duration = "ساعة ونصف",
        TotalMarks = 100,
        Instructions =
        [
            "تأكد/ي من كتابة بياناتك قبل البدء.",
            "اقرأ/ي السؤال كاملًا قبل الإجابة.",
            "في أسئلة الاختيار من متعدد اختر/ي إجابة واحدة فقط.",
            "اكتب/ي الحل في المساحة المخصصة لكل سؤال."
        ],
        Questions =
        [
            new()
            {
                Number = 1, Type = QuestionType.MultipleChoice, Marks = 10,
                SectionTitle = "القسم الأول: أسئلة موضوعية",
                SectionInstruction = "اختر/ي الإجابة الأكثر صحة لكل سؤال.",
                Text = "أي من العناصر الآتية يُعد دائرة منطقية توافقية؟",
                Note = "اختر إجابة واحدة فقط.",
                Options =
                [
                    new() { Label = "أ", Text = "المسجل" },
                    new() { Label = "ب", Text = "العداد" },
                    new() { Label = "ج", Text = "الجامع الكامل", IsCorrect = true },
                    new() { Label = "د", Text = "القلاب" }
                ]
            },
            new()
            {
                Number = 2, Type = QuestionType.MultipleChoice, Marks = 10,
                Text = "في القلاب SR، ماذا يمثل المدخل S عادةً؟",
                Options =
                [
                    new() { Label = "أ", Text = "Set", IsCorrect = true },
                    new() { Label = "ب", Text = "Reset" },
                    new() { Label = "ج", Text = "Select" },
                    new() { Label = "د", Text = "Shift" },
                    new() { Label = "هـ", Text = "لا شيء مما سبق" }
                ]
            },
            new()
            {
                Number = 3, Type = QuestionType.Essay, Marks = 15, AnswerLines = 5,
                SectionTitle = "القسم الثاني: أسئلة حل وشرح",
                SectionInstruction = "أجب/ي بوضوح، وبيّن/ي خطوات الحل عند الحاجة.",
                Text = "اشرح/ي الفرق بين الدوائر التوافقية والدوائر التتابعية مع ذكر مثال لكل نوع.",
                ModelAnswer = "الدوائر التوافقية يعتمد خرجها على المدخلات الحالية فقط، أما التتابعية فيعتمد خرجها على المدخلات الحالية والحالة السابقة. مثال: الجامع الكامل مقابل المسجل."
            },
            new()
            {
                Number = 4, Type = QuestionType.Essay, Marks = 25, AnswerLines = 8,
                Text = "صمّم/ي دائرة تحقق الدالة المنطقية المعطاة، ثم بسّط/ي الدالة ووضّح/ي خطوات التنفيذ.",
                Note = "يمكن استخدام خريطة كارنوف في الحل.",
                ModelAnswer = "تُبسّط الدالة أولًا باستخدام خريطة كارنوف، ثم تُرسم الدائرة وفق التعبير المبسّط مع توضيح البوابات والتوصيلات."
            },
            new()
            {
                Number = 5, Type = QuestionType.MultipleChoice, Marks = 15,
                SectionTitle = "القسم الثالث: مراجعة المفاهيم",
                SectionInstruction = "أسئلة قصيرة لقياس فهم المفاهيم الأساسية.",
                Text = "أي ذاكرة تُستخدم عادةً لتخزين محتوى ثابت يمكن قراءته؟",
                Options =
                [
                    new() { Label = "أ", Text = "RAM" },
                    new() { Label = "ب", Text = "ROM", IsCorrect = true },
                    new() { Label = "ج", Text = "Register" },
                    new() { Label = "د", Text = "Cache" }
                ]
            },
            new()
            {
                Number = 6, Type = QuestionType.Essay, Marks = 25, AnswerLines = 6,
                Text = "اذكر/ي الخصائص الأساسية لمسجلات الإزاحة، ثم وضّح/ي أحد استخداماتها العملية.",
                ModelAnswer = "تُستخدم مسجلات الإزاحة لنقل البيانات بتاتًا يمينًا أو يسارًا، ومن تطبيقاتها التحويل بين التوازي والتوالي."
            }
        ]
    };
}
