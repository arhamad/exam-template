using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ExamTemplate.Documents.Assets;
using ExamTemplate.Documents.Design;
using ExamTemplate.TemplateBuilder;
using TriasDev.Templify.Core;

namespace ExamTemplate.Tests.Template;

/// <summary>Enforces the Word layout rules for Arabic right-to-left documents on the generated template.</summary>
public class TemplateStructureTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly Lazy<byte[]> Template = new(WordTemplateBuilder.Build);

    [Fact]
    public void Template_passes_open_xml_validation()
    {
        using var document = Open(Template.Value);

        var errors = new OpenXmlValidator(FileFormatVersions.Microsoft365)
            .Validate(document)
            .Select(error => $"{error.Part?.Uri} {error.Path?.XPath}: {error.Description}");

        Assert.Empty(errors);
    }

    [Fact]
    public void Every_table_spans_exactly_the_text_width_without_table_alignment()
    {
        var tables = Parts().SelectMany(part => part.Descendants(W + "tbl")).ToList();

        Assert.NotEmpty(tables);
        Assert.All(tables, table =>
        {
            var properties = table.Element(W + "tblPr")!;
            var grid = table.Element(W + "tblGrid")!.Elements(W + "gridCol").Sum(column => (int)column.Attribute(W + "w")!);
            Assert.Equal(DesignTokens.TextWidthTwips, grid);
            Assert.Equal(DesignTokens.TextWidthTwips, (int)properties.Element(W + "tblW")!.Attribute(W + "w")!);
            Assert.NotNull(properties.Element(W + "bidiVisual"));
            Assert.Null(properties.Element(W + "jc"));
        });
    }

    [Fact]
    public void Each_row_fills_its_table_grid()
    {
        foreach (var table in Parts().SelectMany(part => part.Descendants(W + "tbl")))
        {
            var columns = table.Element(W + "tblGrid")!.Elements(W + "gridCol").Count();
            Assert.All(table.Elements(W + "tr"), row => Assert.Equal(columns, row.Elements(W + "tc").Sum(cell =>
                (int?)cell.Element(W + "tcPr")?.Element(W + "gridSpan")?.Attribute(W + "val") ?? 1)));
        }
    }

    [Fact]
    public void No_east_asian_font_hints_anywhere() =>
        Assert.DoesNotContain(Parts().SelectMany(part => part.Descendants()), element => element.Attribute(W + "hint") is not null);

    [Fact]
    public void Right_to_left_paragraphs_never_use_left_alignment()
    {
        var properties = Parts().SelectMany(part => part.Descendants(W + "pPr")).Where(pPr => pPr.Element(W + "bidi") is not null);

        Assert.DoesNotContain(properties, pPr => (string?)pPr.Element(W + "jc")?.Attribute(W + "val") == "left");
    }

    [Fact]
    public void Sizes_and_bold_are_always_paired_with_their_complex_script_versions()
    {
        foreach (var runProperties in Parts().SelectMany(part => part.Descendants(W + "rPr")))
        {
            Assert.Equal((string?)runProperties.Element(W + "sz")?.Attribute(W + "val"), (string?)runProperties.Element(W + "szCs")?.Attribute(W + "val"));
            Assert.Equal(runProperties.Element(W + "b") is null, runProperties.Element(W + "bCs") is null);
        }
    }

    [Fact]
    public void Every_font_declaration_names_all_four_scripts()
    {
        foreach (var fonts in Parts().SelectMany(part => part.Descendants(W + "rFonts")))
        {
            var ascii = (string?)fonts.Attribute(W + "ascii");
            Assert.NotNull(ascii);
            Assert.Equal(ascii, (string?)fonts.Attribute(W + "hAnsi"));
            Assert.Equal(ascii, (string?)fonts.Attribute(W + "eastAsia"));
            Assert.Equal(ascii, (string?)fonts.Attribute(W + "cs"));
        }
    }

    [Fact]
    public void Settings_use_word_2013_layout_embed_fonts_and_never_prompt_for_field_updates()
    {
        using var document = Open(Template.Value);
        var settings = XDocument.Load(document.MainDocumentPart!.DocumentSettingsPart!.GetStream());

        var compatibilityMode = settings.Descendants(W + "compatSetting").Single(setting => (string?)setting.Attribute(W + "name") == "compatibilityMode");
        Assert.Equal("15", (string?)compatibilityMode.Attribute(W + "val"));
        Assert.NotNull(settings.Root!.Element(W + "embedTrueTypeFonts"));
        Assert.Null(settings.Root.Element(W + "updateFields"));
    }

    [Fact]
    public void Arabic_font_is_embedded_as_an_obfuscated_copy_of_the_bundled_font()
    {
        using var document = Open(Template.Value);
        var fontTablePart = document.MainDocumentPart!.FontTablePart!;
        var font = XDocument.Load(fontTablePart.GetStream()).Descendants(W + "font")
            .Single(element => (string?)element.Attribute(W + "name") == DesignTokens.FontFamily);
        var embed = font.Element(W + "embedRegular")!;
        XNamespace relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var part = (FontPart)fontTablePart.GetPartById((string)embed.Attribute(relationships + "id")!);
        using var stream = part.GetStream();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var key = Guid.Parse((string)embed.Attribute(W + "fontKey")!);

        Assert.Equal("application/vnd.openxmlformats-officedocument.obfuscatedFont", part.ContentType);
        Assert.Equal(ExamAssets.Read(ExamAssets.NotoNaskhArabicRegular), FontEmbedding.Obfuscate(buffer.ToArray(), key));
    }

    [Fact]
    public void Page_is_a4_with_a_right_to_left_section_and_the_designed_margins()
    {
        var body = Parts().First().Descendants(W + "body").Single();
        var section = body.Elements().Last();

        Assert.Equal(W + "sectPr", section.Name);
        Assert.NotNull(section.Element(W + "bidi"));
        Assert.Equal(DesignTokens.PageWidthTwips, (int)section.Element(W + "pgSz")!.Attribute(W + "w")!);
        Assert.Equal(DesignTokens.PageHeightTwips, (int)section.Element(W + "pgSz")!.Attribute(W + "h")!);
        var margins = section.Element(W + "pgMar")!;
        Assert.Equal(DesignTokens.MarginTopTwips, (int)margins.Attribute(W + "top")!);
        Assert.Equal(DesignTokens.MarginSideTwips, (int)margins.Attribute(W + "right")!);
        Assert.Equal(DesignTokens.MarginSideTwips, (int)margins.Attribute(W + "left")!);
        Assert.NotNull(section.Element(W + "footerReference"));
    }

    [Fact]
    public void Footer_shows_page_x_of_y()
    {
        using var document = Open(Template.Value);
        var footer = XDocument.Load(document.MainDocumentPart!.FooterParts.Single().GetStream());

        var fields = footer.Descendants(W + "fldSimple").Select(field => ((string)field.Attribute(W + "instr")!).Trim()).ToList();

        Assert.Equal(new[] { "PAGE", "NUMPAGES" }, fields);
    }

    [Fact]
    public void Templify_accepts_the_template()
    {
        using var stream = new MemoryStream(Template.Value);

        var result = new DocumentTemplateProcessor().ValidateTemplate(stream);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    [Fact]
    public void Embedded_template_matches_the_builder_output()
    {
        using var committed = Open(ExamAssets.Read(ExamAssets.WordTemplate));
        using var fresh = Open(Template.Value);

        var committedParts = committed.GetAllParts().ToDictionary(part => part.Uri.ToString(), ReadBytes);
        var freshParts = fresh.GetAllParts().ToDictionary(part => part.Uri.ToString(), ReadBytes);

        Assert.Equal(freshParts.Keys.Order(), committedParts.Keys.Order());
        Assert.All(freshParts, pair => Assert.True(pair.Value.AsSpan().SequenceEqual(committedParts[pair.Key]),
            $"{pair.Key} differs; run: dotnet run --project tools/ExamTemplate.TemplateBuilder"));
    }

    private static WordprocessingDocument Open(byte[] bytes) => WordprocessingDocument.Open(new MemoryStream(bytes), false);

    private static byte[] ReadBytes(OpenXmlPart part)
    {
        using var stream = part.GetStream();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>Main document, styles and footer XML, in that order.</summary>
    private static IEnumerable<XDocument> Parts()
    {
        using var document = Open(Template.Value);
        var main = document.MainDocumentPart!;
        return new OpenXmlPart[] { main, main.StyleDefinitionsPart! }
            .Concat(main.FooterParts)
            .Select(part => XDocument.Load(part.GetStream()))
            .ToList();
    }
}
