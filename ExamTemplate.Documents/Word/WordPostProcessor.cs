using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Documents.Word;

internal static class WordPostProcessor
{
    /// <summary>
    /// Finishes a rendered document: restores the characters protected by <see cref="TemplifyText"/>, then moves
    /// numbers, dates, Latin words and formulas out of right-to-left runs so Word lays them out left to right.
    /// </summary>
    public static void Apply(Stream docx)
    {
        using var document = WordprocessingDocument.Open(docx, isEditable: true);
        var main = document.MainDocumentPart ?? throw new InvalidOperationException("The document has no main part.");

        Finish(main.Document);
        foreach (var header in main.HeaderParts)
            Finish(header.Header);
        foreach (var footer in main.FooterParts)
            Finish(footer.Footer);
    }

    private static void Finish(OpenXmlElement? root)
    {
        if (root is null)
            return;

        foreach (var text in root.Descendants<Text>())
        {
            var restored = TemplifyText.Restore(text.Text);
            if (!ReferenceEquals(restored, text.Text))
                text.Text = restored;
        }

        foreach (var run in root.Descendants<Run>().ToList())
            SplitLeftToRightIslands(run);
    }

    // Word lays out neutral characters (/ = + spaces) of a w:rtl run right to left, so "2026/2027" would read "2027/2026".
    private static void SplitLeftToRightIslands(Run run)
    {
        if (run.RunProperties?.RightToLeftText is null
            || run.ChildElements.Where(child => child is not RunProperties).ToList() is not [Text text])
        {
            return;
        }

        var segments = BidiText.Split(text.Text);
        if (!segments.Any(segment => segment.IsLeftToRight))
            return;

        foreach (var segment in segments)
        {
            var piece = (Run)run.CloneNode(deep: true);
            var pieceText = piece.GetFirstChild<Text>()!;
            pieceText.Text = segment.Text;
            pieceText.Space = SpaceProcessingModeValues.Preserve;
            if (segment.IsLeftToRight)
                piece.RunProperties!.RightToLeftText!.Remove();
            run.InsertBeforeSelf(piece);
        }

        run.Remove();
    }
}
