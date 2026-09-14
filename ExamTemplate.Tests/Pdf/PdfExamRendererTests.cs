using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Pdf;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Pdf;

public class PdfExamRendererTests
{
    private static readonly PdfExamRenderer Renderer = new();

    // Fail instead of silently drawing empty boxes when a character is missing from the bundled fonts.
    static PdfExamRendererTests() => QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = true;

    public static TheoryData<string, int, PaperKind> Papers => new()
    {
        { nameof(DemoExams.ComputerLogicDesign), 0, PaperKind.Student },
        { nameof(DemoExams.ComputerLogicDesign), 1, PaperKind.AnswerKey },
        { nameof(DemoExams.EdgeCases), 0, PaperKind.Student },
        { nameof(DemoExams.EdgeCases), 1, PaperKind.AnswerKey },
        { nameof(DemoExams.ComputerLogicDesignEnglish), 0, PaperKind.Student },
        { nameof(DemoExams.ComputerLogicDesignEnglish), 1, PaperKind.AnswerKey },
    };

    [Theory]
    [MemberData(nameof(Papers))]
    public void Every_paper_renders_to_pdf_with_all_glyphs_available(string exam, int version, PaperKind kind)
    {
        var pdf = Renderer.Render(Paper(exam, version), kind);

        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }

    [Theory]
    [InlineData(nameof(DemoExams.ComputerLogicDesign))]
    [InlineData(nameof(DemoExams.ComputerLogicDesignEnglish))]
    public void Demo_exam_prints_on_a_few_pages(string exam)
    {
        var pages = Renderer.RenderPageImages(Paper(exam, 0), PaperKind.Student, dpi: 24);

        Assert.InRange(pages.Count, 2, 4);
        Assert.All(pages, page => Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, page[..4]));
    }

    [Fact]
    public void Long_exams_flow_across_many_pages()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions.AddRange(Enumerable.Range(1, 150).Select(i => TestExams.TrueFalse($"العبارة رقم {i} صحيحة.", 0.5m, i % 2 == 0)));

        var pages = Renderer.RenderPageImages(ExamPaperBuilder.Build(exam, 0), PaperKind.AnswerKey, dpi: 12);

        Assert.True(pages.Count > 4, $"expected more than 4 pages, got {pages.Count}");
    }

    private static ExamPaper Paper(string exam, int version) => ExamPaperBuilder.Build(exam switch
    {
        nameof(DemoExams.ComputerLogicDesign) => DemoExams.ComputerLogicDesign(),
        nameof(DemoExams.ComputerLogicDesignEnglish) => DemoExams.ComputerLogicDesignEnglish(),
        nameof(DemoExams.EdgeCases) => DemoExams.EdgeCases(),
        _ => throw new ArgumentOutOfRangeException(nameof(exam)),
    }, version);
}
