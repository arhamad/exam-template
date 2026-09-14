using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Validation;

namespace ExamTemplate.Documents.Services;

public sealed class ExamDocumentService : IExamDocumentService
{
    public ValidationReport Validate(ExamDefinition exam) => ExamValidator.Validate(exam);

    public IReadOnlyList<ExamPaper> BuildPapers(ExamDefinition exam) => ExamPaperBuilder.BuildAll(exam);

    public RenderedFile RenderWord(ExamPaper paper, PaperKind kind) => throw new NotImplementedException();

    public RenderedFile RenderPdf(ExamPaper paper, PaperKind kind) => throw new NotImplementedException();

    public RenderedFile RenderBundleZip(ExamDefinition exam) => throw new NotImplementedException();
}
