namespace ExamTemplate.Documents.Papers;

public static class OptionLayoutRules
{
    public const int MaxTwoColumnOptionLength = 30;

    /// <summary>Short single-line options print in two columns (أ ب / ج د); anything longer gets a row each.</summary>
    public static OptionLayout Choose(IReadOnlyList<PaperOption> options) =>
        options.All(option => option.Text.Length <= MaxTwoColumnOptionLength && !option.Text.Contains('\n'))
            ? OptionLayout.TwoColumns
            : OptionLayout.OneColumn;
}
