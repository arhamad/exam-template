using System.Security;
using System.Text;
using ExamTemplate.Documents.Design;

namespace ExamTemplate.TemplateBuilder;

internal enum Align
{
    Start,
    Center,

    /// <summary>The visual left edge of a right-to-left paragraph.</summary>
    End,
}

/// <summary>A border line; <see cref="None"/> removes the border.</summary>
internal sealed record Line(int Size, string Color)
{
    public static readonly Line None = new(0, "auto");
    public static readonly Line Hairline = new(DesignTokens.HairlineBorder, DesignTokens.RuleColor);
    public static readonly Line LightHairline = new(DesignTokens.HairlineBorder, DesignTokens.LightRuleColor);
    public static readonly Line Medium = new(DesignTokens.MediumBorder, DesignTokens.TextColor);
    public static readonly Line Heavy = new(DesignTokens.HeavyBorder, DesignTokens.TextColor);

    public string Xml(string side) => Size == 0
        ? $"<w:{side} w:val=\"nil\"/>"
        : $"<w:{side} w:val=\"single\" w:sz=\"{Size}\" w:space=\"0\" w:color=\"{Color}\"/>";
}

internal static class TableBorders
{
    public static string None => Of(Line.None, Line.None, Line.None, Line.None, Line.None);

    public static string Box(Line line) => Of(line, line, line, Line.None, Line.None);

    public static string Grid(Line line) => Of(line, line, line, line, line);

    public static string TopAndBottom(Line line) => Of(line, line, Line.None, Line.None, Line.None);

    public static string TopOnly(Line line) => Of(line, Line.None, Line.None, Line.None, Line.None);

    public static string BottomOnly(Line line) => Of(Line.None, line, Line.None, Line.None, Line.None);

    private static string Of(Line top, Line bottom, Line sides, Line insideHorizontal, Line insideVertical) =>
        "<w:tblBorders>" + top.Xml("top") + sides.Xml("left") + bottom.Xml("bottom") + sides.Xml("right")
        + insideHorizontal.Xml("insideH") + insideVertical.Xml("insideV") + "</w:tblBorders>";
}

internal static class CellBorders
{
    public static string Box(Line line) => Of(line, line, line);

    public static string BottomOnly(Line line) => Of(Line.None, line, Line.None);

    private static string Of(Line top, Line bottom, Line sides) =>
        "<w:tcBorders>" + top.Xml("top") + sides.Xml("left") + bottom.Xml("bottom") + sides.Xml("right") + "</w:tcBorders>";
}

internal sealed record TableOptions(string Borders, int MarginTopTwips = 0, int MarginBottomTwips = 0);

internal sealed record RowOptions(bool CantSplit = false, int ExactHeightTwips = 0)
{
    public static readonly RowOptions Plain = new();
    public static readonly RowOptions KeepTogether = new(CantSplit: true);
}

internal sealed record CellOptions(int GridSpan = 1, string? Fill = null, string? Borders = null, int PaddingTwips = 0, string VerticalAlign = "top")
{
    public static readonly CellOptions Plain = new();
    public static readonly CellOptions Middle = new(VerticalAlign: "center");
}

/// <summary>Small helpers that emit WordprocessingML snippets. Formatting lives in named styles, not on runs.</summary>
internal static class WordXml
{
    public const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public const string RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private const string DrawingNamespace = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private const string DrawingMainNamespace = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private const string PictureNamespace = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    private const long EmuPerCentimeter = 360000;

    public static string Document(string body) =>
        $"<w:document xmlns:w=\"{WordNamespace}\" xmlns:r=\"{RelationshipsNamespace}\" xmlns:wp=\"{DrawingNamespace}\" "
        + $"xmlns:a=\"{DrawingMainNamespace}\" xmlns:pic=\"{PictureNamespace}\"><w:body>{body}</w:body></w:document>";

    public static string P(string style, params string[] content) =>
        $"<w:p><w:pPr><w:pStyle w:val=\"{style}\"/></w:pPr>{string.Concat(content)}</w:p>";

    /// <summary>A right-to-left text run.</summary>
    public static string R(string text) => Run(text, characterStyle: null);

    /// <summary>A right-to-left text run with a character style.</summary>
    public static string R(string text, string characterStyle) => Run(text, characterStyle);

    /// <summary>A Templify marker (<c>{{#if}}</c>, <c>{{#foreach}}</c>, …) alone in its own paragraph, as Templify requires.</summary>
    public static string Control(string marker) => P("TemplateControl", R(marker));

    public static string Field(string instruction, string cachedResult) =>
        $"<w:fldSimple w:instr=\" {instruction} \"><w:r><w:rPr><w:rtl/></w:rPr><w:t>{cachedResult}</w:t></w:r></w:fldSimple>";

    /// <summary>A full-width right-to-left table with a fixed grid.</summary>
    public static string Table(IReadOnlyList<int> grid, TableOptions options, params string[] rows)
    {
        var width = grid.Sum();
        if (width != DesignTokens.TextWidthTwips)
            throw new InvalidOperationException($"Table grid sums to {width} twips instead of {DesignTokens.TextWidthTwips}.");

        var cellMargins = "<w:tblCellMar>"
            + $"<w:top w:w=\"{options.MarginTopTwips}\" w:type=\"dxa\"/><w:left w:w=\"{DesignTokens.CellMarginTwips}\" w:type=\"dxa\"/>"
            + $"<w:bottom w:w=\"{options.MarginBottomTwips}\" w:type=\"dxa\"/><w:right w:w=\"{DesignTokens.CellMarginTwips}\" w:type=\"dxa\"/>"
            + "</w:tblCellMar>";

        return "<w:tbl><w:tblPr><w:bidiVisual/>"
            + $"<w:tblW w:w=\"{width}\" w:type=\"dxa\"/><w:tblInd w:w=\"0\" w:type=\"dxa\"/>"
            + options.Borders + "<w:tblLayout w:type=\"fixed\"/>" + cellMargins + "</w:tblPr>"
            + $"<w:tblGrid>{string.Concat(grid.Select(column => $"<w:gridCol w:w=\"{column}\"/>"))}</w:tblGrid>"
            + string.Concat(rows) + "</w:tbl>";
    }

    public static string Row(RowOptions options, params string[] cells)
    {
        var properties = (options.CantSplit ? "<w:cantSplit/>" : "")
            + (options.ExactHeightTwips > 0 ? $"<w:trHeight w:val=\"{options.ExactHeightTwips}\" w:hRule=\"exact\"/>" : "");
        var rowProperties = properties.Length > 0 ? $"<w:trPr>{properties}</w:trPr>" : "";
        return $"<w:tr>{rowProperties}{string.Concat(cells)}</w:tr>";
    }

    /// <summary>A row holding a Templify row-loop marker in its first cell; Templify removes the whole row.</summary>
    public static string ControlRow(IReadOnlyList<int> grid, string marker) =>
        Row(RowOptions.Plain, grid.Select((width, index) => Cell(width, CellOptions.Plain, index == 0 ? Control(marker) : P("TemplateControl"))).ToArray());

    public static string Cell(int width, CellOptions options, params string[] content)
    {
        var properties = new StringBuilder($"<w:tcW w:w=\"{width}\" w:type=\"dxa\"/>");
        if (options.GridSpan > 1)
            properties.Append($"<w:gridSpan w:val=\"{options.GridSpan}\"/>");
        if (options.Borders is not null)
            properties.Append(options.Borders);
        if (options.Fill is not null)
            properties.Append($"<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"{options.Fill}\"/>");
        if (options.PaddingTwips > 0)
        {
            var padding = options.PaddingTwips;
            properties.Append($"<w:tcMar><w:top w:w=\"{padding}\" w:type=\"dxa\"/><w:left w:w=\"{padding}\" w:type=\"dxa\"/>"
                + $"<w:bottom w:w=\"{padding}\" w:type=\"dxa\"/><w:right w:w=\"{padding}\" w:type=\"dxa\"/></w:tcMar>");
        }

        properties.Append($"<w:vAlign w:val=\"{options.VerticalAlign}\"/>");
        return $"<w:tc><w:tcPr>{properties}</w:tcPr>{string.Concat(content)}</w:tc>";
    }

    /// <summary>An inline picture sized by width, keeping the pixel aspect ratio.</summary>
    public static string InlineImage(string relationshipId, double widthCm, int pixelWidth, int pixelHeight, string description)
    {
        var width = (long)Math.Round(widthCm * EmuPerCentimeter);
        var height = width * pixelHeight / pixelWidth;

        return "<w:r><w:rPr><w:noProof/></w:rPr><w:drawing>"
            + "<wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\">"
            + $"<wp:extent cx=\"{width}\" cy=\"{height}\"/>"
            + $"<wp:docPr id=\"1\" name=\"Logo\" descr=\"{SecurityElement.Escape(description)}\"/>"
            + "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"1\"/></wp:cNvGraphicFramePr>"
            + $"<a:graphic><a:graphicData uri=\"{PictureNamespace}\"><pic:pic>"
            + "<pic:nvPicPr><pic:cNvPr id=\"1\" name=\"university-logo.png\"/><pic:cNvPicPr/></pic:nvPicPr>"
            + $"<pic:blipFill><a:blip r:embed=\"{relationshipId}\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>"
            + $"<pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"{width}\" cy=\"{height}\"/></a:xfrm>"
            + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></pic:spPr>"
            + "</pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r>";
    }

    private static string Run(string text, string? characterStyle)
    {
        var style = characterStyle is null ? "" : $"<w:rStyle w:val=\"{characterStyle}\"/>";
        return $"<w:r><w:rPr>{style}<w:rtl/></w:rPr><w:t xml:space=\"preserve\">{SecurityElement.Escape(text)}</w:t></w:r>";
    }
}
