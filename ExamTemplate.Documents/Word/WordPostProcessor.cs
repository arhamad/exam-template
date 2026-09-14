using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Documents.Word;

internal static class WordPostProcessor
{
    /// <summary>
    /// Finishes a rendered document: restores the characters protected by <see cref="TemplifyText"/>, then gives every
    /// island of the other direction a run of its own. In an Arabic document numbers, dates, Latin words and formulas
    /// leave the right-to-left runs; in an English document Arabic words and phrases get right-to-left runs.
    /// </summary>
    /// <param name="rightToLeft">True for an Arabic document, false for an English one.</param>
    public static void Apply(Stream docx, bool rightToLeft)
    {
        using var document = WordprocessingDocument.Open(docx, isEditable: true);
        var main = document.MainDocumentPart ?? throw new InvalidOperationException("The document has no main part.");

        Finish(main.Document, rightToLeft);
        foreach (var header in main.HeaderParts)
            Finish(header.Header, rightToLeft);
        foreach (var footer in main.FooterParts)
            Finish(footer.Footer, rightToLeft);
    }

    private static void Finish(OpenXmlElement? root, bool rightToLeft)
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
            SplitIslands(run, rightToLeft);
    }

    // Word lays out neutral characters (/ = + spaces) in the direction of their run, so "2026/2027" in a w:rtl run would
    // read "2027/2026", and an Arabic phrase in a left-to-right run would lose its word order around punctuation.
    // Templify writes each line of a multi-line value as its own run, so every run here holds a single line.
    private static void SplitIslands(Run run, bool rightToLeft)
    {
        if (run.ChildElements.Where(child => child is not RunProperties).ToList() is not [Text text])
            return;

        // An Arabic document has Arabic text only in right-to-left runs.
        if (rightToLeft && run.RunProperties?.RightToLeftText is null)
            return;

        var segments = BidiText.Split(text.Text, rightToLeft);
        if (!segments.Any(segment => segment.IsLeftToRight == rightToLeft))
            return;

        foreach (var segment in segments)
        {
            var piece = (Run)run.CloneNode(deep: true);
            var pieceText = piece.GetFirstChild<Text>()!;
            pieceText.Text = segment.Text;
            pieceText.Space = SpaceProcessingModeValues.Preserve;
            if (segment.IsLeftToRight)
                piece.RunProperties?.RightToLeftText?.Remove();
            else
                (piece.RunProperties ??= new RunProperties()).RightToLeftText = new RightToLeftText();
            run.InsertBeforeSelf(piece);
        }

        run.Remove();
    }
}
