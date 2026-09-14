using System.IO.Compression;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Services;
using ExamTemplate.Documents.Validation;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Services;

public class ExamDocumentServiceTests
{
    private readonly ExamDocumentService _service = new();

    [Fact]
    public void Validate_reports_errors()
    {
        var exam = TestExams.Valid();
        exam.Header.CourseCode = "";

        Assert.True(_service.Validate(exam).HasErrors);
    }

    [Fact]
    public void BuildPapers_returns_one_paper_per_version()
    {
        var exam = TestExams.Valid();
        exam.VersionCount = 3;

        Assert.Equal(new[] { "أ", "ب", "ج" }, _service.BuildPapers(exam).Select(paper => paper.VersionLetter));
    }

    [Fact]
    public void RenderWord_returns_a_docx_named_after_the_course_version_and_kind()
    {
        var paper = _service.BuildPapers(TestExams.Valid())[0];

        var file = _service.RenderWord(paper, PaperKind.AnswerKey);

        Assert.Equal("1290-A-key.docx", file.FileName);
        Assert.Equal(RenderedFile.WordContentType, file.ContentType);
        Assert.Equal("PK"u8.ToArray(), file.Content[..2]);
    }

    [Fact]
    public void RenderPdf_returns_a_pdf_named_after_the_course_version_and_kind()
    {
        var paper = _service.BuildPapers(TestExams.Valid())[0];

        var file = _service.RenderPdf(paper, PaperKind.Student);

        Assert.Equal("1290-A-exam.pdf", file.FileName);
        Assert.Equal(RenderedFile.PdfContentType, file.ContentType);
        Assert.Equal("%PDF"u8.ToArray(), file.Content[..4]);
    }

    [Fact]
    public void Bundle_contains_both_formats_of_every_paper_and_the_exam_json()
    {
        var exam = TestExams.Valid();
        exam.VersionCount = 2;

        var bundle = _service.RenderBundleZip(exam);

        Assert.Equal("1290-exam-bundle.zip", bundle.FileName);
        Assert.Equal(RenderedFile.ZipContentType, bundle.ContentType);
        using var zip = new ZipArchive(new MemoryStream(bundle.Content));
        Assert.Equal(
            new[]
            {
                "1290-A-exam.docx", "1290-A-exam.pdf", "1290-A-key.docx", "1290-A-key.pdf",
                "1290-B-exam.docx", "1290-B-exam.pdf", "1290-B-key.docx", "1290-B-key.pdf",
                "1290-exam.json",
            },
            zip.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal));

        using var json = new StreamReader(zip.GetEntry("1290-exam.json")!.Open());
        Assert.Equal("1290", ExamJson.Deserialize(json.ReadToEnd()).Header.CourseCode);
    }

    [Fact]
    public void Bundle_refuses_an_exam_with_errors()
    {
        var exam = TestExams.Valid();
        exam.Header.CourseName = "";

        Assert.Throws<ExamValidationException>(() => _service.RenderBundleZip(exam));
    }
}
