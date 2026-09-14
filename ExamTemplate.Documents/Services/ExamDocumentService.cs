using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Validation;
using ExamTemplate.Documents.Word;

namespace ExamTemplate.Documents.Services;

public sealed class ExamDocumentService : IExamDocumentService
{
    private readonly WordExamRenderer _word = new();

    public ValidationReport Validate(ExamDefinition exam) => ExamValidator.Validate(exam);

    public IReadOnlyList<ExamPaper> BuildPapers(ExamDefinition exam) => ExamPaperBuilder.BuildAll(exam);

    public RenderedFile RenderWord(ExamPaper paper, PaperKind kind) =>
        new(ExamFileNames.Paper(paper, kind, "docx"), RenderedFile.WordContentType, _word.Render(paper, kind));

    public RenderedFile RenderPdf(ExamPaper paper, PaperKind kind) => throw new NotImplementedException();

    public RenderedFile RenderBundleZip(ExamDefinition exam) => throw new NotImplementedException();
}
