using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Services;

// Renders the demo exams (every version, student copy and answer key) for visual checks.
// Usage (from the repository root): dotnet run --project tools/ExamTemplate.Samples -- [outputDir] [docx|pdf|all]
var output = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("artifacts", "samples"));
var formats = args.Length > 1 ? args[1] : "all";
Directory.CreateDirectory(output);

var service = new ExamDocumentService();
(string Name, ExamDefinition Exam)[] exams = [("logic", DemoExams.ComputerLogicDesign()), ("edge", DemoExams.EdgeCases())];

foreach (var (name, exam) in exams)
{
    foreach (var paper in service.BuildPapers(exam))
    {
        foreach (var kind in Enum.GetValues<PaperKind>())
        {
            if (formats is "docx" or "all")
                Write(name, service.RenderWord(paper, kind));
            if (formats is "pdf" or "all")
                Write(name, service.RenderPdf(paper, kind));
        }
    }
}

void Write(string prefix, RenderedFile file)
{
    var path = Path.Combine(output, $"{prefix}-{file.FileName}");
    File.WriteAllBytes(path, file.Content);
    Console.WriteLine(path);
}
