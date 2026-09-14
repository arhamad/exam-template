using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Documents.Pdf;

internal static class PdfText
{
    private const char LeftToRightIsolate = (char)0x2066;
    private const char RightToLeftIsolate = (char)0x2067;
    private const char PopDirectionalIsolate = (char)0x2069;

    /// <summary>
    /// Wraps the islands of the other direction in Unicode isolates, LRI … PDI in Arabic text and RLI … PDI in English text,
    /// so the PDF text engine keeps their order.
    /// </summary>
    public static string Isolate(string text, bool rightToLeft = true)
    {
        var segments = BidiText.Split(text, rightToLeft);
        if (!segments.Any(IsIsland))
            return text;

        var isolate = rightToLeft ? LeftToRightIsolate : RightToLeftIsolate;
        return string.Concat(segments.Select(segment => IsIsland(segment)
            ? $"{isolate}{segment.Text}{PopDirectionalIsolate}"
            : segment.Text));

        bool IsIsland(BidiSegment segment) => segment.IsLeftToRight == rightToLeft;
    }
}
