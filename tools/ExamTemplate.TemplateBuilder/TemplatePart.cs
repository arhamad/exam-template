using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.TemplateBuilder;

/// <summary>
/// A template part written for one exam language. Its fixed words come from <see cref="PaperWording"/>, and these helpers
/// give its runs, tables and fields the language's direction, so the layout code reads the same for both languages.
/// </summary>
internal abstract class TemplatePart(ExamLanguage language)
{
    protected ExamLanguage Language => language;

    protected PaperWording Wording { get; } = PaperWording.For(language);

    protected string R(string text) => WordXml.Run(text, characterStyle: null, Wording.IsRightToLeft);

    protected string R(string text, string characterStyle) => WordXml.Run(text, characterStyle, Wording.IsRightToLeft);

    /// <summary>A Templify marker (<c>{{#if}}</c>, <c>{{#foreach}}</c>, …) alone in its own paragraph, as Templify requires.</summary>
    protected string Control(string marker) => WordXml.P("TemplateControl", R(marker));

    /// <summary>A row holding a Templify row-loop marker in its first cell; Templify removes the whole row.</summary>
    protected string ControlRow(IReadOnlyList<int> grid, string marker) =>
        WordXml.Row(RowOptions.Plain, grid
            .Select((width, index) => WordXml.Cell(width, CellOptions.Plain, index == 0 ? Control(marker) : WordXml.P("TemplateControl")))
            .ToArray());

    protected string Table(IReadOnlyList<int> grid, TableOptions options, params string[] rows) =>
        WordXml.Table(grid, options, Wording.IsRightToLeft, rows);

    protected string Field(string instruction, string cachedResult) => WordXml.Field(instruction, cachedResult, Wording.IsRightToLeft);
}
