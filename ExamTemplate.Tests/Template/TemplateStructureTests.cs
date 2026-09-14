using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ExamTemplate.Documents.Assets;
using ExamTemplate.Documents.Design;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.TemplateBuilder;
using TriasDev.Templify.Core;

namespace ExamTemplate.Tests.Template;

/// <summary>Enforces the Word layout rules on the generated templates: Arabic right to left, English left to right.</summary>
public class TemplateStructureTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly Dictionary<ExamLanguage, Lazy<byte[]>> Templates = Enum.GetValues<ExamLanguage>()
        .ToDictionary(language => language, language => new Lazy<byte[]>(() => WordTemplateBuilder.Build(language)));

    public static TheoryData<ExamLanguage> Languages => new() { ExamLanguage.Arabic, ExamLanguage.English };

    [Theory]
    [MemberData(nameof(Languages))]
    public void Template_passes_open_xml_validation(ExamLanguage language)
    {
        using var document = Open(Template(language));

        var errors = new OpenXmlValidator(FileFormatVersions.Microsoft365)
            .Validate(document)
            .Select(error => $"{error.Part?.Uri} {error.Path?.XPath}: {error.Description}");

        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Every_table_spans_exactly_the_text_width_without_table_alignment(ExamLanguage language)
    {
        var tables = Parts(language).SelectMany(part => part.Descendants(W + "tbl")).ToList();

        Assert.NotEmpty(tables);
        Assert.All(tables, table =>
        {
            var properties = table.Element(W + "tblPr")!;
            var grid = table.Element(W + "tblGrid")!.Elements(W + "gridCol").Sum(column => (int)column.Attribute(W + "w")!);
            Assert.Equal(DesignTokens.TextWidthTwips, grid);
            Assert.Equal(DesignTokens.TextWidthTwips, (int)properties.Element(W + "tblW")!.Attribute(W + "w")!);
            Assert.Null(properties.Element(W + "jc"));
        });
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Each_row_fills_its_table_grid(ExamLanguage language)
    {
        foreach (var table in Parts(language).SelectMany(part => part.Descendants(W + "tbl")))
        {
            var columns = table.Element(W + "tblGrid")!.Elements(W + "gridCol").Count();
            Assert.All(table.Elements(W + "tr"), row => Assert.Equal(columns, row.Elements(W + "tc").Sum(cell =>
                (int?)cell.Element(W + "tcPr")?.Element(W + "gridSpan")?.Attribute(W + "val") ?? 1)));
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void No_east_asian_font_hints_anywhere(ExamLanguage language) =>
        Assert.DoesNotContain(Parts(language).SelectMany(part => part.Descendants()), element => element.Attribute(W + "hint") is not null);

    [Theory]
    [MemberData(nameof(Languages))]
    public void Paragraphs_never_use_left_alignment(ExamLanguage language) =>
        Assert.DoesNotContain(Parts(language).SelectMany(part => part.Descendants(W + "jc")), jc => (string?)jc.Attribute(W + "val") == "left");

    [Theory]
    [MemberData(nameof(Languages))]
    public void Sizes_and_bold_are_always_paired_with_their_complex_script_versions(ExamLanguage language)
    {
        foreach (var runProperties in Parts(language).SelectMany(part => part.Descendants(W + "rPr")))
        {
            Assert.Equal((string?)runProperties.Element(W + "sz")?.Attribute(W + "val"), (string?)runProperties.Element(W + "szCs")?.Attribute(W + "val"));
            Assert.Equal(runProperties.Element(W + "b") is null, runProperties.Element(W + "bCs") is null);
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Latin_slots_use_the_text_font_and_complex_scripts_use_the_arabic_font(ExamLanguage language)
    {
        var declarations = Parts(language).SelectMany(part => part.Descendants(W + "rFonts")).ToList();

        Assert.NotEmpty(declarations);
        Assert.All(declarations, fonts =>
        {
            var ascii = (string?)fonts.Attribute(W + "ascii");
            Assert.Equal(ascii, (string?)fonts.Attribute(W + "hAnsi"));
            Assert.Equal(ascii, (string?)fonts.Attribute(W + "eastAsia"));

            // The answer bubbles use Arial for every script.
            var expected = ascii == DesignTokens.WordBubbleFontFamily
                ? (DesignTokens.WordBubbleFontFamily, DesignTokens.WordBubbleFontFamily)
                : (DesignTokens.TextFontFamily(language), DesignTokens.ArabicFontFamily);
            Assert.Equal(expected, (ascii, (string?)fonts.Attribute(W + "cs")));
        });
    }

    [Fact]
    public void Arabic_template_lays_out_paragraphs_tables_runs_and_the_section_right_to_left()
    {
        var parts = Parts(ExamLanguage.Arabic);

        Assert.All(parts.SelectMany(part => part.Descendants(W + "tbl")), table => Assert.NotNull(table.Element(W + "tblPr")!.Element(W + "bidiVisual")));
        Assert.All(parts[1].Descendants(W + "style").Where(style => (string?)style.Attribute(W + "type") == "paragraph"),
            style => Assert.NotNull(style.Element(W + "pPr")?.Element(W + "bidi")));
        Assert.All(parts[0].Descendants(W + "r").Where(run => run.Element(W + "t") is not null),
            run => Assert.NotNull(run.Element(W + "rPr")?.Element(W + "rtl")));
        Assert.NotNull(Section(ExamLanguage.Arabic).Element(W + "bidi"));
    }

    [Fact]
    public void English_template_has_no_right_to_left_markup()
    {
        var names = Parts(ExamLanguage.English).SelectMany(part => part.Descendants()).Select(element => element.Name.LocalName).ToHashSet();

        Assert.DoesNotContain("bidi", names);
        Assert.DoesNotContain("rtl", names);
        Assert.DoesNotContain("bidiVisual", names);
    }

    [Fact]
    public void English_template_contains_no_arabic_characters()
    {
        var xml = string.Concat(Parts(ExamLanguage.English).Select(part => part.ToString()));

        Assert.DoesNotContain(xml, character => character is >= (char)0x0600 and <= (char)0x06FF);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Settings_use_word_2013_layout_embed_fonts_and_never_prompt_for_field_updates(ExamLanguage language)
    {
        using var document = Open(Template(language));
        var settings = XDocument.Load(document.MainDocumentPart!.DocumentSettingsPart!.GetStream());

        var compatibilityMode = settings.Descendants(W + "compatSetting").Single(setting => (string?)setting.Attribute(W + "name") == "compatibilityMode");
        Assert.Equal("15", (string?)compatibilityMode.Attribute(W + "val"));
        Assert.NotNull(settings.Root!.Element(W + "embedTrueTypeFonts"));
        Assert.Null(settings.Root.Element(W + "updateFields"));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Only_the_text_font_is_embedded_as_an_obfuscated_copy_of_the_bundled_font(ExamLanguage language)
    {
        using var document = Open(Template(language));
        var fontTablePart = document.MainDocumentPart!.FontTablePart!;
        var fonts = XDocument.Load(fontTablePart.GetStream()).Descendants(W + "font").ToList();

        var embedded = Assert.Single(fonts, font => font.Element(W + "embedRegular") is not null);
        Assert.Equal(DesignTokens.TextFontFamily(language), (string?)embedded.Attribute(W + "name"));
        Assert.Contains(fonts, font => (string?)font.Attribute(W + "name") == DesignTokens.ArabicFontFamily);

        var embed = embedded.Element(W + "embedRegular")!;
        var part = (FontPart)fontTablePart.GetPartById((string)embed.Attribute(Relationships + "id")!);
        using var stream = part.GetStream();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var key = Guid.Parse((string)embed.Attribute(W + "fontKey")!);

        Assert.Equal("application/vnd.openxmlformats-officedocument.obfuscatedFont", part.ContentType);
        Assert.Equal(ExamAssets.Read(ExamAssets.TextFontRegular(language)), FontEmbedding.Obfuscate(buffer.ToArray(), key));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Page_is_a4_with_the_designed_margins(ExamLanguage language)
    {
        var section = Section(language);

        Assert.Equal(W + "sectPr", section.Name);
        Assert.Equal(DesignTokens.PageWidthTwips, (int)section.Element(W + "pgSz")!.Attribute(W + "w")!);
        Assert.Equal(DesignTokens.PageHeightTwips, (int)section.Element(W + "pgSz")!.Attribute(W + "h")!);
        var margins = section.Element(W + "pgMar")!;
        Assert.Equal(DesignTokens.MarginTopTwips, (int)margins.Attribute(W + "top")!);
        Assert.Equal(DesignTokens.MarginSideTwips, (int)margins.Attribute(W + "right")!);
        Assert.Equal(DesignTokens.MarginSideTwips, (int)margins.Attribute(W + "left")!);
        Assert.NotNull(section.Element(W + "footerReference"));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Footer_shows_page_x_of_y_in_the_paper_language(ExamLanguage language)
    {
        using var document = Open(Template(language));
        var footer = XDocument.Load(document.MainDocumentPart!.FooterParts.Single().GetStream());
        var wording = PaperWording.For(language);

        var fields = footer.Descendants(W + "fldSimple").Select(field => ((string)field.Attribute(W + "instr")!).Trim()).ToList();
        var text = string.Concat(footer.Descendants(W + "t").Select(element => element.Value));

        Assert.Equal(new[] { "PAGE", "NUMPAGES" }, fields);
        Assert.Contains($"{wording.Page}1{wording.PageOf}1", text);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Templify_accepts_the_template(ExamLanguage language)
    {
        using var stream = new MemoryStream(Template(language));

        var result = new DocumentTemplateProcessor().ValidateTemplate(stream);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Embedded_template_matches_the_builder_output(ExamLanguage language)
    {
        using var committed = Open(ExamAssets.Read(ExamAssets.WordTemplate(language)));
        using var fresh = Open(Template(language));

        var committedParts = committed.GetAllParts().ToDictionary(part => part.Uri.ToString(), ReadBytes);
        var freshParts = fresh.GetAllParts().ToDictionary(part => part.Uri.ToString(), ReadBytes);

        Assert.Equal(freshParts.Keys.Order(), committedParts.Keys.Order());
        Assert.All(freshParts, pair => Assert.True(pair.Value.AsSpan().SequenceEqual(committedParts[pair.Key]),
            $"{pair.Key} of {ExamAssets.WordTemplateFileName(language)} differs; run: dotnet run --project tools/ExamTemplate.TemplateBuilder"));
    }

    private static byte[] Template(ExamLanguage language) => Templates[language].Value;

    private static WordprocessingDocument Open(byte[] bytes) => WordprocessingDocument.Open(new MemoryStream(bytes), false);

    private static byte[] ReadBytes(OpenXmlPart part)
    {
        using var stream = part.GetStream();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static XElement Section(ExamLanguage language) => Parts(language)[0].Descendants(W + "body").Single().Elements().Last();

    /// <summary>Main document, styles and footer XML, in that order.</summary>
    private static List<XDocument> Parts(ExamLanguage language)
    {
        using var document = Open(Template(language));
        var main = document.MainDocumentPart!;
        return new OpenXmlPart[] { main, main.StyleDefinitionsPart! }
            .Concat(main.FooterParts)
            .Select(part => XDocument.Load(part.GetStream()))
            .ToList();
    }
}
