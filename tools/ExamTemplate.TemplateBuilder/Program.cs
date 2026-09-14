using ExamTemplate.TemplateBuilder;

// Usage (from the repository root): dotnet run --project tools/ExamTemplate.TemplateBuilder [output.docx]
var output = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("ExamTemplate.Documents", "Word", "ExamTemplate.docx"));

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllBytes(output, WordTemplateBuilder.Build());
Console.WriteLine($"Wrote {output}");
