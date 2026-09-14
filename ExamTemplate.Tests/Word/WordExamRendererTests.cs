using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Design;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Documents.Word;

namespace ExamTemplate.Tests.Word;

public class WordExamRendererTests
{
    private static readonly WordExamRenderer Renderer = new();

    public static TheoryData<string, int, PaperKind> Papers => new()
    {
        { nameof(DemoExams.ComputerLogicDesign), 0, PaperKind.Student },
        { nameof(DemoExams.ComputerLogicDesign), 1, PaperKind.AnswerKey },
        { nameof(DemoExams.EdgeCases), 0, PaperKind.Student },
        { nameof(DemoExams.EdgeCases), 1, PaperKind.AnswerKey },
    };

    [Theory]
    [MemberData(nameof(Papers))]
    public void Rendered_documents_are_valid_and_free_of_template_markers(string exam, int version, PaperKind kind)
    {
        using var document = Open(Renderer.Render(Paper(exam, version), kind));

        Assert.Empty(new OpenXmlValidator(FileFormatVersions.Microsoft365).Validate(document).Select(error => error.Description));
        var text = AllText(document);
        Assert.DoesNotContain("{{#", text);
        Assert.DoesNotContain("{{/", text);
        Assert.DoesNotContain(text, character => character is >= (char)0xE000 and <= (char)0xE004);
        Assert.DoesNotContain(Paragraphs(document), paragraph => StyleOf(paragraph) == "TemplateControl");
        Assert.Null(document.MainDocumentPart!.DocumentSettingsPart!.Settings!.GetFirstChild<UpdateFieldsOnOpen>());
    }

    [Fact]
    public void Student_copy_has_student_fields_empty_bubbles_and_answer_lines()
    {
        using var document = Open(Renderer.Render(Paper(nameof(DemoExams.ComputerLogicDesign), 0), PaperKind.Student));
        var text = AllText(document);

        Assert.Contains("اسم الطالب:", text);
        Assert.Contains("تعليمات الامتحان", text);
        Assert.Contains("انتهت الأسئلة", text);
        Assert.DoesNotContain("مفتاح الإجابة", text);
        Assert.DoesNotContain(DesignTokens.FilledBubble, text);
        Assert.DoesNotContain("الإجابة النموذجية", text);
        Assert.Equal(5 + 8 + 6, AnswerLineRows(document));
    }

    [Fact]
    public void Answer_key_marks_every_correct_answer_and_shows_model_answers()
    {
        var paper = Paper(nameof(DemoExams.ComputerLogicDesign), 1);

        using var document = Open(Renderer.Render(paper, PaperKind.AnswerKey));
        var text = AllText(document);

        Assert.Contains("مفتاح الإجابة – النموذج ب", text);
        Assert.Contains("ملخص الإجابات", text);
        Assert.Contains("تُبسّط الدالة أولًا", text);
        Assert.DoesNotContain("اسم الطالب:", text);
        Assert.Equal(paper.AnswerSummary.Count, text.Split(DesignTokens.FilledBubble).Length - 1);
        Assert.Equal(0, AnswerLineRows(document));
    }

    [Fact]
    public void Questions_follow_the_paper_order()
    {
        var paper = Paper(nameof(DemoExams.ComputerLogicDesign), 1);

        using var document = Open(Renderer.Render(paper, PaperKind.Student));

        Assert.Equal(
            paper.Sections.SelectMany(section => section.Questions).Select(question => question.Text),
            TextsOfStyle(document, "ExamQuestionText"));
    }

    [Fact]
    public void Every_option_is_rendered_with_its_label_in_order()
    {
        var paper = Paper(nameof(DemoExams.EdgeCases), 1);

        using var document = Open(Renderer.Render(paper, PaperKind.Student));

        Assert.Equal(
            paper.Sections.SelectMany(section => section.Questions).SelectMany(question => question.Options).Select(option => option.Label + ")"),
            TextsOfStyle(document, "ExamOptionLabel").Where(text => text.Length > 0));
    }

    [Fact]
    public void Markdown_and_brace_characters_typed_by_authors_are_kept_verbatim()
    {
        using var document = Open(Renderer.Render(Paper(nameof(DemoExams.EdgeCases), 0), PaperKind.Student));
        var text = AllText(document);

        Assert.Contains("العبارة **مهمة** و __ليست__ ~~محذوفة~~ و *مائلة*.", text);
        Assert.Contains("x_1 + x_2 ≤ 2³ و 5*4*2 = 40 و {{CourseName}}", text);
    }

    [Fact]
    public void Line_breaks_in_question_text_are_kept()
    {
        using var document = Open(Renderer.Render(Paper(nameof(DemoExams.EdgeCases), 0), PaperKind.Student));

        Assert.Contains("اكتب/ي برنامجًا يطبع الأعداد من 1 إلى 20،\nثم اشرح/ي كل سطر فيه.", TextsOfStyle(document, "ExamQuestionText"));
    }

    [Fact]
    public void Header_and_footer_show_the_course_the_version_and_page_numbers()
    {
        using var document = Open(Renderer.Render(Paper(nameof(DemoExams.ComputerLogicDesign), 1), PaperKind.Student));
        var body = AllText(document);
        var footer = string.Concat(document.MainDocumentPart!.FooterParts.Single().Footer!.Descendants<Text>().Select(text => text.Text));

        Assert.Contains("الفصل الأول 1261 – 2026/2027", body);
        Assert.Contains("عدد الأسئلة: 8 أسئلة", body);
        Assert.Contains("1290 – تصميم منطق الحاسوب", footer);
        Assert.Contains("النموذج ب – 2026/2027", footer);
    }

    private static ExamPaper Paper(string exam, int version) => ExamPaperBuilder.Build(exam switch
    {
        nameof(DemoExams.ComputerLogicDesign) => DemoExams.ComputerLogicDesign(),
        nameof(DemoExams.EdgeCases) => DemoExams.EdgeCases(),
        _ => throw new ArgumentOutOfRangeException(nameof(exam)),
    }, version);

    private static WordprocessingDocument Open(byte[] docx) => WordprocessingDocument.Open(new MemoryStream(docx), false);

    private static IEnumerable<Paragraph> Paragraphs(WordprocessingDocument document) =>
        document.MainDocumentPart!.Document!.Body!.Descendants<Paragraph>();

    private static string? StyleOf(Paragraph paragraph) => paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

    private static string TextOf(OpenXmlElement element) => string.Concat(element.Descendants().Select(node => node switch
    {
        Text text => text.Text,
        Break => "\n",
        _ => "",
    }));

    private static string AllText(WordprocessingDocument document) => string.Join("\n", Paragraphs(document).Select(TextOf));

    private static IEnumerable<string> TextsOfStyle(WordprocessingDocument document, string style) =>
        Paragraphs(document).Where(paragraph => StyleOf(paragraph) == style).Select(TextOf).ToList();

    private static int AnswerLineRows(WordprocessingDocument document) =>
        document.MainDocumentPart!.Document!.Body!.Descendants<TableRowHeight>()
            .Count(height => height.Val?.Value == (uint)DesignTokens.AnswerLinePitchTwips);
}
