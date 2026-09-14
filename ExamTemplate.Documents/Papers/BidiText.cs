using System.Text;

namespace ExamTemplate.Documents.Papers;

/// <param name="IsLeftToRight">True for Latin, digits, formulas and dates embedded in Arabic text.</param>
public sealed record BidiSegment(string Text, bool IsLeftToRight);

/// <summary>
/// Splits Arabic text into right-to-left text and left-to-right islands (numbers, dates, Latin words, formulas),
/// so renderers can lay each island out left to right instead of letting the surrounding direction reorder it
/// (e.g. "2026/2027" shown as "2027/2026", or "5*4*2 = 40" as "40 = 2*4*5").
/// </summary>
public static class BidiText
{
    // Characters that may continue an island: spaces plus formula and code punctuation.
    private const string IslandJoiners = "+-*/=<>≤≥≠±×÷^%.,:;()[]{}'\"_#&|!?@$~";

    // Spaces and sentence punctuation at the end of an island belong to the surrounding Arabic sentence.
    private const string TrailingPunctuation = ".,:;!?";

    private const string OpeningBrackets = "([{";
    private const string ClosingBrackets = ")]}";

    public static IReadOnlyList<BidiSegment> Split(string text)
    {
        if (text.Length == 0)
            return [];

        if (!text.Any(IsRightToLeft))
            return [new BidiSegment(text, text.Any(StartsIsland))];

        var segments = new List<BidiSegment>();
        var buffer = new StringBuilder();
        var inIsland = false;

        foreach (var character in text)
        {
            if (!inIsland)
            {
                if (StartsIsland(character))
                {
                    // Opening brackets typed right before the island join it; CloseIsland gives back any it doesn't close.
                    var openingBrackets = TakeTrailingOpeningBrackets(buffer);
                    Add(segments, buffer.ToString(), isLeftToRight: false);
                    buffer.Clear().Append(openingBrackets).Append(character);
                    inIsland = true;
                }
                else
                {
                    buffer.Append(character);
                }
            }
            else if (StartsIsland(character) || IsIslandJoiner(character))
            {
                buffer.Append(character);
            }
            else
            {
                CloseIsland(segments, buffer);
                buffer.Append(character);
                inIsland = false;
            }
        }

        if (inIsland)
            CloseIsland(segments, buffer);

        Add(segments, buffer.ToString(), isLeftToRight: false);
        return segments;
    }

    // Moves the island held in the buffer into the segments; what belongs to the Arabic text stays in the buffer.
    private static void CloseIsland(List<BidiSegment> segments, StringBuilder buffer)
    {
        var island = buffer.ToString();
        buffer.Clear();

        var end = island.Length;
        while (end > 0 && (char.IsWhiteSpace(island[end - 1]) || TrailingPunctuation.Contains(island[end - 1])))
            end--;

        var start = 0;
        while (start < end)
        {
            var bracket = OpeningBrackets.IndexOf(island[start]);
            if (bracket < 0 || island.IndexOf(ClosingBrackets[bracket], start + 1, end - start - 1) >= 0)
                break;
            start++;
        }

        Add(segments, island[..start], isLeftToRight: false);
        Add(segments, island[start..end], isLeftToRight: true);
        buffer.Append(island, end, island.Length - end);
    }

    private static string TakeTrailingOpeningBrackets(StringBuilder buffer)
    {
        var count = 0;
        while (count < buffer.Length && OpeningBrackets.Contains(buffer[buffer.Length - 1 - count]))
            count++;

        var brackets = buffer.ToString(buffer.Length - count, count);
        buffer.Length -= count;
        return brackets;
    }

    private static void Add(List<BidiSegment> segments, string text, bool isLeftToRight)
    {
        if (text.Length == 0)
            return;

        if (segments.Count > 0 && segments[^1].IsLeftToRight == isLeftToRight)
            segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
        else
            segments.Add(new BidiSegment(text, isLeftToRight));
    }

    // Hebrew, Arabic, Syriac, Thaana, N'Ko and the Arabic presentation forms, plus the right-to-left mark.
    private static bool IsRightToLeft(char character) =>
        (int)character is (>= 0x0590 and <= 0x08FF) or (>= 0xFB1D and <= 0xFDFF) or (>= 0xFE70 and <= 0xFEFF) or 0x200F;

    // Latin letters, digits (including ² and ³) and the left-to-right mark.
    private static bool StartsIsland(char character) =>
        !IsRightToLeft(character) && (char.IsLetterOrDigit(character) || char.IsNumber(character) || character == (char)0x200E);

    private static bool IsIslandJoiner(char character) => char.IsWhiteSpace(character) || IslandJoiners.Contains(character);
}
