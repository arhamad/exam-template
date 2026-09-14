using ExamTemplate.Documents.Assets;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ExamTemplate.Documents.Pdf;

/// <summary>Lays out a paper as PDF with QuestPDF, mirroring the Word template's design. Thread-safe.</summary>
internal sealed class PdfExamRenderer
{
    private const string FailureMessage = "تعذّر إنشاء ملف PDF. حاول مرة أخرى، وإن تكررت المشكلة فتواصل مع الدعم الفني.";

    private readonly byte[] _logo = ExamAssets.Read(ExamAssets.UniversityLogo);

    public PdfExamRenderer() => QuestPdfSetup.EnsureInitialized();

    /// <exception cref="ExamRenderException">The PDF could not be generated.</exception>
    public byte[] Render(ExamPaper paper, PaperKind kind)
    {
        try
        {
            return CreateDocument(paper, kind).GeneratePdf();
        }
        catch (Exception exception)
        {
            throw new ExamRenderException(FailureMessage, exception);
        }
    }

    /// <summary>One PNG per page; used for visual checks.</summary>
    public IReadOnlyList<byte[]> RenderPageImages(ExamPaper paper, PaperKind kind, int dpi) =>
        CreateDocument(paper, kind)
            .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = dpi })
            .ToList();

    private IDocument CreateDocument(ExamPaper paper, PaperKind kind)
    {
        var title = kind == PaperKind.AnswerKey ? "مفتاح الإجابة" : "نسخة الطالب";
        return Document.Create(container => new ExamPdfLayout(paper, kind, _logo).Compose(container))
            .WithMetadata(new DocumentMetadata
            {
                Title = $"{paper.Header.CourseName} – {paper.Header.ExamTitle} – النموذج {paper.VersionLetter} – {title}",
                Author = paper.Header.UniversityName,
                Subject = paper.Header.ExamTitle,
            });
    }
}
