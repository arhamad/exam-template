using System.Globalization;
using ExamTemplate.Documents.Assets;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Services;
using TriasDev.Templify.Core;

namespace ExamTemplate.Documents.Word;

/// <summary>Fills the embedded Word template with a paper. Thread-safe; one instance can serve all requests.</summary>
internal sealed class WordExamRenderer
{
    private const string FailureMessage = "تعذّر إنشاء ملف Word. حاول مرة أخرى، وإن تكررت المشكلة فتواصل مع الدعم الفني.";

    private readonly byte[] _template = ExamAssets.Read(ExamAssets.WordTemplate);

    private readonly DocumentTemplateProcessor _processor = new(new PlaceholderReplacementOptions
    {
        // A template/data mismatch is a bug: fail loudly instead of printing "{{Name}}".
        MissingVariableBehavior = MissingVariableBehavior.ThrowException,
        Culture = CultureInfo.InvariantCulture,
        EnableNewlineSupport = true,
        // Word updates PAGE/NUMPAGES on its own; forcing an update would show a prompt when the file opens.
        UpdateFieldsOnOpen = UpdateFieldsOnOpenMode.Never,
        WarnOnEmptyLoopCollections = false,
    });

    /// <exception cref="ExamRenderException">The template could not be filled.</exception>
    public byte[] Render(ExamPaper paper, PaperKind kind)
    {
        using var template = new MemoryStream(_template, writable: false);
        using var output = new MemoryStream();

        try
        {
            var result = _processor.ProcessTemplate(template, output, WordTemplateData.Create(paper, kind));
            if (!result.IsSuccess)
                throw new ExamRenderException(FailureMessage, new InvalidOperationException(result.ErrorMessage));
            if (result.MissingVariables.Count > 0)
                throw new ExamRenderException(FailureMessage, new InvalidOperationException($"Missing template values: {string.Join(", ", result.MissingVariables)}"));

            WordPostProcessor.Apply(output, PaperWording.For(paper.Language).IsRightToLeft);
        }
        catch (Exception exception) when (exception is not ExamRenderException)
        {
            throw new ExamRenderException(FailureMessage, exception);
        }

        return output.ToArray();
    }
}
