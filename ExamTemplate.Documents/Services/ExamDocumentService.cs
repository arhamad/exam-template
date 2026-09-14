using System.IO.Compression;
using System.Text;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Pdf;
using ExamTemplate.Documents.Validation;
using ExamTemplate.Documents.Word;

namespace ExamTemplate.Documents.Services;

public sealed class ExamDocumentService : IExamDocumentService
{
    private readonly WordExamRenderer _word = new();
    private readonly PdfExamRenderer _pdf = new();

    public ValidationReport Validate(ExamDefinition exam) => ExamValidator.Validate(exam);

    public IReadOnlyList<ExamPaper> BuildPapers(ExamDefinition exam) => ExamPaperBuilder.BuildAll(exam);

    public RenderedFile RenderWord(ExamPaper paper, PaperKind kind) =>
        new(ExamFileNames.Paper(paper, kind, "docx"), RenderedFile.WordContentType, _word.Render(paper, kind));

    public RenderedFile RenderPdf(ExamPaper paper, PaperKind kind) =>
        new(ExamFileNames.Paper(paper, kind, "pdf"), RenderedFile.PdfContentType, _pdf.Render(paper, kind));

    public RenderedFile RenderBundleZip(ExamDefinition exam)
    {
        var papers = BuildPapers(exam);

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var paper in papers)
            {
                foreach (var kind in Enum.GetValues<PaperKind>())
                {
                    Add(zip, RenderWord(paper, kind));
                    Add(zip, RenderPdf(paper, kind));
                }
            }

            // The exam file lets anyone regenerate exactly the same versions later.
            Add(zip, new RenderedFile(ExamFileNames.Json(exam), RenderedFile.JsonContentType, Encoding.UTF8.GetBytes(ExamJson.Serialize(exam))));
        }

        return new RenderedFile(ExamFileNames.Bundle(exam), RenderedFile.ZipContentType, buffer.ToArray());
    }

    private static void Add(ZipArchive zip, RenderedFile file)
    {
        using var entry = zip.CreateEntry(file.FileName, CompressionLevel.Optimal).Open();
        entry.Write(file.Content);
    }
}
