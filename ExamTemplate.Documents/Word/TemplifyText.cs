namespace ExamTemplate.Documents.Word;

/// <summary>
/// Templify always applies markdown (<c>* _ ~</c>) to values, and braces could be read as placeholders.
/// User text is protected by swapping those characters for private-use characters before rendering;
/// <see cref="WordPostProcessor"/> swaps them back in the finished document.
/// </summary>
internal static class TemplifyText
{
    private const string Plain = "*_~{}";
    private static readonly string Protected = new(new[] { (char)0xE000, (char)0xE001, (char)0xE002, (char)0xE003, (char)0xE004 });

    public static string Protect(string text) => Swap(text, Plain, Protected);

    public static string Restore(string text) => Swap(text, Protected, Plain);

    private static string Swap(string text, string from, string to)
    {
        if (text.AsSpan().IndexOfAny(from) < 0)
            return text;

        var characters = text.ToCharArray();
        for (var index = 0; index < characters.Length; index++)
        {
            var position = from.IndexOf(characters[index]);
            if (position >= 0)
                characters[index] = to[position];
        }

        return new string(characters);
    }
}
