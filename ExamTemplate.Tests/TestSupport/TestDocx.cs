using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using TriasDev.Templify.Core;

namespace ExamTemplate.Tests.TestSupport;

/// <summary>Builds tiny .docx templates from WordprocessingML snippets and summarizes rendered output.</summary>
internal static class TestDocx
{
    private const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static PlaceholderReplacementOptions StrictOptions => new()
    {
        MissingVariableBehavior = MissingVariableBehavior.ThrowException,
        WarnOnEmptyLoopCollections = false,
    };

    public static MemoryStream Create(string bodyXml)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document($"<w:document xmlns:w=\"{WordNamespace}\"><w:body>{bodyXml}</w:body></w:document>");
        }

        stream.Position = 0;
        return stream;
    }

    public static byte[] Render(string bodyXml, Dictionary<string, object> data, PlaceholderReplacementOptions? options = null)
    {
        using var template = Create(bodyXml);
        using var output = new MemoryStream();

        var result = new DocumentTemplateProcessor(options ?? StrictOptions).ProcessTemplate(template, output, data);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.ErrorMessage);

        return output.ToArray();
    }

    public static Dictionary<string, object> Data(params (string Key, object Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value);

    public static string P(string text) =>
        $"<w:p><w:r><w:t xml:space=\"preserve\">{Escape(text)}</w:t></w:r></w:p>";

    public static string Tbl(int columns, params string[] rows) =>
        $"<w:tbl><w:tblPr/><w:tblGrid>{string.Concat(Enumerable.Repeat("<w:gridCol w:w=\"3000\"/>", columns))}</w:tblGrid>{string.Concat(rows)}</w:tbl>";

    public static string Tr(params string[] cells) =>
        $"<w:tr>{string.Concat(cells.Select(cell => $"<w:tc>{cell}</w:tc>"))}</w:tr>";

    /// <summary>One line per body element: <c>P: text</c> or <c>T: cell | cell / next row</c>; line breaks show as ⏎.</summary>
    public static string Describe(byte[] docx)
    {
        using var document = WordprocessingDocument.Open(new MemoryStream(docx), false);
        var lines = new List<string>();

        foreach (var element in document.MainDocumentPart!.Document!.Body!.Elements())
        {
            switch (element)
            {
                case Paragraph paragraph:
                    lines.Add("P: " + TextOf(paragraph));
                    break;
                case Table table:
                    var rows = table.Elements<TableRow>().Select(row =>
                        string.Join(" | ", row.Elements<TableCell>().Select(cell => string.Join(" ", cell.Elements<Paragraph>().Select(TextOf)))));
                    lines.Add("T: " + string.Join(" / ", rows));
                    break;
            }
        }

        return string.Join("\n", lines);
    }

    private static string TextOf(OpenXmlElement element) => string.Concat(element.Descendants().Select(node => node switch
    {
        Text text => text.Text,
        Break => "⏎",
        _ => "",
    }));

    private static string Escape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
