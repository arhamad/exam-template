using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ExamTemplate.Documents.Assets;

namespace ExamTemplate.TemplateBuilder;

/// <summary>Generates the exam .docx template consumed by the Word renderer.</summary>
public static class WordTemplateBuilder
{
    // Fixed so that rebuilding produces byte-identical parts.
    private static readonly Guid FontKey = new("5E2C8A71-94B3-4D0F-A6C1-3B7E9D24F108");

    /// <exception cref="InvalidOperationException">The generated package fails Open XML validation.</exception>
    public static byte[] Build()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            WriteXml(main.AddNewPart<StyleDefinitionsPart>("rIdStyles"), TemplateStyles.Xml());
            WriteXml(main.AddNewPart<DocumentSettingsPart>("rIdSettings"), TemplateSettings.Xml());

            var fontTable = main.AddNewPart<FontTablePart>("rIdFontTable");
            var font = fontTable.AddFontPart(FontPartType.FontOdttf, "rIdNotoNaskhArabic");
            WriteBytes(font, FontEmbedding.Obfuscate(ExamAssets.Read(ExamAssets.NotoNaskhArabicRegular), FontKey));
            WriteXml(fontTable, TemplateFonts.Xml(fontTable.GetIdOfPart(font), FontEmbedding.FontKey(FontKey)));

            var footer = main.AddNewPart<FooterPart>("rIdFooter");
            WriteXml(footer, TemplateFooter.Xml());

            var logo = main.AddImagePart(ImagePartType.Png, "rIdLogo");
            WriteBytes(logo, ExamAssets.Read(ExamAssets.UniversityLogo));

            WriteXml(main, TemplateBody.Xml(main.GetIdOfPart(logo), main.GetIdOfPart(footer)));
        }

        var template = stream.ToArray();
        ThrowIfInvalid(template);
        return template;
    }

    private static void WriteXml(OpenXmlPart part, string xml)
    {
        using var writer = new StreamWriter(part.GetStream(FileMode.Create, FileAccess.Write), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        writer.Write(xml);
    }

    private static void WriteBytes(OpenXmlPart part, byte[] content)
    {
        using var source = new MemoryStream(content);
        part.FeedData(source);
    }

    private static void ThrowIfInvalid(byte[] template)
    {
        using var document = WordprocessingDocument.Open(new MemoryStream(template), false);
        var errors = new OpenXmlValidator(FileFormatVersions.Microsoft365)
            .Validate(document)
            .Select(error => $"{error.Part?.Uri} {error.Path?.XPath}: {error.Description}")
            .ToList();

        if (errors.Count > 0)
            throw new InvalidOperationException("The generated template is not valid Open XML:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }
}
