using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using TriasDev.Templify.Core;
using static ExamTemplate.Tests.TestSupport.TestDocx;

namespace ExamTemplate.Tests.Word;

/// <summary>Pins down the TriasDev.Templify 1.7.0 behaviours the exam template relies on.</summary>
public class TemplifyCapabilityTests
{
    public sealed record OptionItem(string Label, string Text);

    public sealed record QuestionItem(string Number, string Text, bool HasNote, string Note, List<OptionItem> Options);

    public sealed record SectionItem(string Title, List<QuestionItem> Questions);

    [Fact]
    public void Nested_paragraph_loops_expand_and_marker_paragraphs_disappear()
    {
        var body = P("{{#foreach s in Sections}}") + P("Section {{s.Title}}")
            + P("{{#foreach q in s.Questions}}") + P("Q{{q.Number}} {{q.Text}}") + P("{{/foreach}}")
            + P("{{/foreach}}");
        var data = Data(("Sections", new List<SectionItem> { new("A", [Question("1", "x"), Question("2", "y")]), new("B", [Question("3", "z")]) }));

        Assert.Equal("P: Section A\nP: Q1 x\nP: Q2 y\nP: Section B\nP: Q3 z", Describe(Render(body, data)));
    }

    [Fact]
    public void Row_loops_work_inside_tables_repeated_by_an_outer_loop()
    {
        var body = P("{{#foreach q in Questions}}")
            + Tbl(2, Tr(P("{{#foreach o in q.Options}}"), P("")), Tr(P("{{o.Label}}"), P("{{o.Text}}")), Tr(P("{{/foreach}}"), P("")))
            + P("spacer {{q.Number}}")
            + P("{{/foreach}}");
        var data = Data(("Questions", new List<QuestionItem>
        {
            Question("1", "x", [new("أ", "a"), new("ب", "b")]),
            Question("2", "y", [new("أ", "c")]),
        }));

        Assert.Equal("T: أ | a / ب | b\nP: spacer 1\nT: أ | c\nP: spacer 2", Describe(Render(body, data)));
    }

    [Fact]
    public void Conditionals_work_inside_a_table_cell()
    {
        var body = P("{{#foreach q in Questions}}")
            + Tbl(1, Tr(P("{{q.Text}}") + P("{{#if q.HasNote}}") + P("Note: {{q.Note}}") + P("{{/if}}")))
            + P("-")
            + P("{{/foreach}}");
        var data = Data(("Questions", new List<QuestionItem> { Question("1", "with note", note: "n"), Question("2", "without note") }));

        Assert.Equal("T: with note Note: n\nP: -\nT: without note\nP: -", Describe(Render(body, data)));
    }

    [Theory]
    [InlineData(true, "T: KEY\nP: after")]
    [InlineData(false, "T: STUDENT\nP: after")]
    public void If_else_chooses_between_whole_tables(bool isAnswerKey, string expected)
    {
        var body = P("{{#if IsAnswerKey}}") + Tbl(1, Tr(P("KEY"))) + P("{{#else}}") + Tbl(1, Tr(P("STUDENT"))) + P("{{/if}}") + P("after");

        Assert.Equal(expected, Describe(Render(body, Data(("IsAnswerKey", isAnswerKey)))));
    }

    [Fact]
    public void Root_values_are_visible_inside_loops()
    {
        var body = P("{{#foreach q in Questions}}") + P("{{CourseCode}}-{{q.Number}}") + P("{{/foreach}}");
        var data = Data(("CourseCode", "1290"), ("Questions", new List<QuestionItem> { Question("1", "x"), Question("2", "y") }));

        Assert.Equal("P: 1290-1\nP: 1290-2", Describe(Render(body, data)));
    }

    [Fact]
    public void Empty_row_loop_leaves_the_static_rows()
    {
        var body = Tbl(1, Tr(P("Header")), Tr(P("{{#foreach i in Items}}")), Tr(P("{{i}}")), Tr(P("{{/foreach}}"))) + P("after");

        Assert.Equal("T: Header\nP: after", Describe(Render(body, Data(("Items", new List<string>())))));
    }

    [Fact]
    public void Newlines_become_breaks_and_the_placeholder_run_formatting_is_kept()
    {
        const string body = "<w:p><w:r><w:rPr><w:b/><w:bCs/><w:rtl/></w:rPr><w:t>{{Text}}</w:t></w:r></w:p>";

        var output = Render(body, Data(("Text", "سطر أول\nسطر ثانٍ")));

        Assert.Equal("P: سطر أول⏎سطر ثانٍ", Describe(output));
        using var document = WordprocessingDocument.Open(new MemoryStream(output), false);
        var textRuns = document.MainDocumentPart!.Document!.Body!.Descendants<Run>().Where(run => run.GetFirstChild<Text>() is not null).ToList();
        Assert.Equal(2, textRuns.Count);
        Assert.All(textRuns, run =>
        {
            Assert.NotNull(run.RunProperties?.BoldComplexScript);
            Assert.NotNull(run.RunProperties?.RightToLeftText);
        });
    }

    [Fact]
    public void Markdown_mangles_raw_values_but_private_use_characters_pass_through()
    {
        Assert.NotEqual("P: 5*4*2 = 40", Describe(Render(P("{{Text}}"), Data(("Text", "5*4*2 = 40")))));

        var star = (char)0xE000;
        var (open, close) = ((char)0xE003, (char)0xE004);
        var protectedText = $"5{star}4{star}2 {open}{open}CourseCode{close}{close}";

        var output = Render(P("{{Text}}"), Data(("Text", protectedText), ("CourseCode", "1290")));

        Assert.Equal("P: " + protectedText, Describe(output));
    }

    [Fact]
    public void Missing_values_fail_in_strict_mode() =>
        Assert.ThrowsAny<Exception>(() => Render(P("{{Missing}}"), Data(("Other", "x"))));

    [Fact]
    public void ValidateTemplate_reports_unmatched_markers()
    {
        using var template = Create(P("{{#if HasNote}}") + P("note"));

        var result = new DocumentTemplateProcessor(StrictOptions).ValidateTemplate(template);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Type == ValidationErrorType.UnmatchedConditionalStart);
    }

    private static QuestionItem Question(string number, string text, List<OptionItem>? options = null, string note = "") =>
        new(number, text, note.Length > 0, note, options ?? []);
}
