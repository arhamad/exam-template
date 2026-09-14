using System.Globalization;
using ExamTemplateDemo.Models;
using TriasDev.Templify;

namespace ExamTemplateDemo.Services;

public sealed class ExamDocumentGenerator
{
    public void Generate(ExamModel exam, string templatePath, string outputPath, bool showAnswers)
    {
        var options = new PlaceholderReplacementOptions
        {
            // مهم لأن عدد الأسئلة ومساحات الحل تغير عدد الصفحات.
            // يطلب من Word تحديث PAGE / NUMPAGES عند فتح الملف.
            UpdateFieldsOnOpen = UpdateFieldsOnOpenMode.Auto,
            Culture = CultureInfo.GetCultureInfo("ar-PS")
        };

        var processor = new DocumentTemplateProcessor(options);
        var data = ExamTemplateMapper.ToTemplateData(exam, showAnswers);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var templateStream = File.OpenRead(templatePath);
        using var outputStream = File.Create(outputPath);

        var result = processor.ProcessTemplate(templateStream, outputStream, data);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.ErrorMessage ?? "Template processing failed.");

        if (result.MissingVariables.Count > 0)
            Console.WriteLine("Missing template variables: " + string.Join(", ", result.MissingVariables));

        Console.WriteLine($"Generated: {outputPath} | replacements: {result.ReplacementCount}");
    }
}
