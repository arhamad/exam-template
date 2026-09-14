using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ExamTemplate.Documents.Word;

internal static class WordPostProcessor
{
    /// <summary>Restores the characters protected by <see cref="TemplifyText"/> in the body, headers and footers.</summary>
    public static void RestoreProtectedCharacters(Stream docx)
    {
        using var document = WordprocessingDocument.Open(docx, isEditable: true);
        var main = document.MainDocumentPart ?? throw new InvalidOperationException("The document has no main part.");

        Restore(main.Document);
        foreach (var header in main.HeaderParts)
            Restore(header.Header);
        foreach (var footer in main.FooterParts)
            Restore(footer.Footer);
    }

    private static void Restore(OpenXmlElement? root)
    {
        if (root is null)
            return;

        foreach (var text in root.Descendants<Text>())
        {
            var restored = TemplifyText.Restore(text.Text);
            if (!ReferenceEquals(restored, text.Text))
                text.Text = restored;
        }
    }
}
