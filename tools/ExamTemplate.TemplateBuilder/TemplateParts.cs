using ExamTemplate.Documents.Model;
using static ExamTemplate.Documents.Design.DesignTokens;
using static ExamTemplate.TemplateBuilder.WordXml;

namespace ExamTemplate.TemplateBuilder;

/// <summary>footer1.xml: course | "Page X of Y" | version and academic year.</summary>
internal sealed class TemplateFooter(ExamLanguage language) : TemplatePart(language)
{
    public string Xml()
    {
        var grid = FooterGrid;
        var table = Table(grid, new TableOptions(TableBorders.TopOnly(Line.Hairline), MarginTopTwips: 40),
            Row(RowOptions.Plain,
                Cell(grid[0], CellOptions.Plain, P("ExamFooter", R("{{CourseCode}} – {{CourseName}}"))),
                Cell(grid[1], CellOptions.Plain, P("ExamFooterCenter", R(Wording.Page), Field("PAGE", "1"), R(Wording.PageOf), Field("NUMPAGES", "1"))),
                Cell(grid[2], CellOptions.Plain, P("ExamFooterEnd", R("{{FooterVersionText}} – {{AcademicYear}}")))));

        // Word needs a paragraph after a table in a header or footer.
        return $"<w:ftr xmlns:w=\"{WordNamespace}\" xmlns:r=\"{RelationshipsNamespace}\">{table}{P("ExamFooterTail")}</w:ftr>";
    }
}

/// <summary>settings.xml: Word 2013+ layout, embedded fonts, no "update fields" prompt.</summary>
internal static class TemplateSettings
{
    public static string Xml() =>
        $"<w:settings xmlns:w=\"{WordNamespace}\">"
        + "<w:zoom w:percent=\"100\"/>"
        + "<w:embedTrueTypeFonts/>"
        + "<w:defaultTabStop w:val=\"720\"/>"
        + "<w:characterSpacingControl w:val=\"doNotCompress\"/>"
        + "<w:compat>"
        + Compatibility("compatibilityMode", "15")
        + Compatibility("overrideTableStyleFontSizeAndJustification", "1")
        + Compatibility("enableOpenTypeFeatures", "1")
        + Compatibility("doNotFlipMirrorIndents", "1")
        + "</w:compat>"
        + "<w:themeFontLang w:val=\"en-US\" w:bidi=\"ar-SA\"/>"
        + "<w:decimalSymbol w:val=\".\"/>"
        + "<w:listSeparator w:val=\",\"/>"
        + "</w:settings>";

    private static string Compatibility(string name, string value) =>
        $"<w:compatSetting w:name=\"{name}\" w:uri=\"http://schemas.microsoft.com/office/word\" w:val=\"{value}\"/>";
}

/// <summary>
/// fontTable.xml. Only the paper's text font is embedded. English templates also list Noto Naskh Arabic, used for
/// Arabic words, without embedding it.
/// </summary>
internal static class TemplateFonts
{
    public static string Xml(ExamLanguage language, string fontRelationshipId, string fontKey) =>
        $"<w:fonts xmlns:w=\"{WordNamespace}\" xmlns:r=\"{RelationshipsNamespace}\">"
        + Font(TextFontFamily(language), $"<w:embedRegular r:id=\"{fontRelationshipId}\" w:fontKey=\"{fontKey}\"/>")
        + (language == ExamLanguage.English ? Font(ArabicFontFamily, embedding: "") : "")
        + $"<w:font w:name=\"{WordBubbleFontFamily}\"><w:panose1 w:val=\"020B0604020202020204\"/><w:charset w:val=\"00\"/>"
        + "<w:family w:val=\"swiss\"/><w:pitch w:val=\"variable\"/></w:font>"
        + "</w:fonts>";

    private static string Font(string name, string embedding) =>
        $"<w:font w:name=\"{name}\"><w:charset w:val=\"00\"/><w:family w:val=\"roman\"/><w:pitch w:val=\"variable\"/>{embedding}</w:font>";
}
