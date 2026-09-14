using ExamTemplate.Documents.Assets;
using ExamTemplate.Documents.Model;
using ExamTemplate.TemplateBuilder;

// Usage (from the repository root): dotnet run --project tools/ExamTemplate.TemplateBuilder [outputDirectory]
// Writes one template per exam language (ExamTemplate-ar.docx, ExamTemplate-en.docx), by default into ExamTemplate.Documents/Word.
var directory = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("ExamTemplate.Documents", "Word"));
Directory.CreateDirectory(directory);

foreach (var language in Enum.GetValues<ExamLanguage>())
{
    var output = Path.Combine(directory, ExamAssets.WordTemplateFileName(language));
    File.WriteAllBytes(output, WordTemplateBuilder.Build(language));
    Console.WriteLine($"Wrote {output}");
}
