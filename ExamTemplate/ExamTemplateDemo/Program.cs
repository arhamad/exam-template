using ExamTemplateDemo;
using ExamTemplateDemo.Services;

var exam = DemoData.Create();
var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "ExamTemplate.docx");
var outputDir = Path.Combine(AppContext.BaseDirectory, "Output");

var generator = new ExamDocumentGenerator();

// نسخة الطالب: أسئلة + مساحات حل.
generator.Generate(
    exam,
    templatePath,
    Path.Combine(outputDir, "Exam-Student.docx"),
    showAnswers: false);

// مفتاح الإجابة: نفس القالب ونفس البيانات، لكن يعرض الإجابات الصحيحة/النموذجية.
generator.Generate(
    exam,
    templatePath,
    Path.Combine(outputDir, "Exam-AnswerKey.docx"),
    showAnswers: true);

Console.WriteLine("Done.");
