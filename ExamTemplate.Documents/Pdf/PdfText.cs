using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Documents.Pdf;

internal static class PdfText
{
    private const char LeftToRightIsolate = (char)0x2066;
    private const char PopDirectionalIsolate = (char)0x2069;

    /// <summary>Wraps left-to-right islands in Unicode isolates (LRI … PDI) so the PDF text engine keeps their order.</summary>
    public static string Isolate(string text)
    {
        var segments = BidiText.Split(text);
        if (!segments.Any(segment => segment.IsLeftToRight))
            return text;

        return string.Concat(segments.Select(segment => segment.IsLeftToRight
            ? $"{LeftToRightIsolate}{segment.Text}{PopDirectionalIsolate}"
            : segment.Text));
    }
}
