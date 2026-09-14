using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Validation;

namespace ExamTemplate.Documents.Services;

/// <summary>Validates exams and renders their papers. Stateless and safe to share between requests.</summary>
public interface IExamDocumentService
{
    ValidationReport Validate(ExamDefinition exam);

    /// <summary>One paper per version.</summary>
    /// <exception cref="ExamValidationException">The exam has validation errors.</exception>
    IReadOnlyList<ExamPaper> BuildPapers(ExamDefinition exam);

    /// <exception cref="ExamRenderException">The Word document could not be generated.</exception>
    RenderedFile RenderWord(ExamPaper paper, PaperKind kind);

    /// <exception cref="ExamRenderException">The PDF could not be generated.</exception>
    RenderedFile RenderPdf(ExamPaper paper, PaperKind kind);

    /// <summary>A zip with the student copy and answer key of every version in Word and PDF, plus the exam JSON.</summary>
    /// <exception cref="ExamValidationException">The exam has validation errors.</exception>
    /// <exception cref="ExamRenderException">A document could not be generated.</exception>
    RenderedFile RenderBundleZip(ExamDefinition exam);
}
