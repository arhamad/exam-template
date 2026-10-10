using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Papers;

/// <summary>How the items of a main question (a section) are labelled on the paper.</summary>
public enum ItemLabelStyle
{
    /// <summary>1, 2, 3: objective sections, mixed sections, and essay sections with more parts than letters.</summary>
    Numbers,

    /// <summary>أ، ب، ج or a, b, c: a section whose questions are all essays, as parts of one question.</summary>
    Letters,

    /// <summary>No label: a section with a single essay is itself the question.</summary>
    None,
}

/// <summary>
/// The labels of a section's items, shared by the paper builder, the validator and the editor so they always agree.
/// The style depends only on the kinds and number of questions, so shuffled versions keep it; the label depends on
/// the position, so the builder asks for the position after shuffling and the editor for the authored one.
/// </summary>
public sealed class ItemLabels
{
    private readonly PaperWording wording;

    private ItemLabels(ItemLabelStyle style, PaperWording wording)
    {
        Style = style;
        this.wording = wording;
    }

    public ItemLabelStyle Style { get; }

    public static ItemLabels For(ExamSection section, ExamLanguage language) =>
        For(section.Questions.Select(question => question.Type), language);

    public static ItemLabels For(IEnumerable<QuestionType> itemTypes, ExamLanguage language)
    {
        var wording = PaperWording.For(language);
        var types = itemTypes.ToList();
        var allEssays = types.Count > 0 && types.All(type => type == QuestionType.Essay);

        var style = !allEssays ? ItemLabelStyle.Numbers
            : types.Count == 1 ? ItemLabelStyle.None
            : types.Count <= wording.ItemLetterCount ? ItemLabelStyle.Letters
            : ItemLabelStyle.Numbers;

        return new ItemLabels(style, wording);
    }

    /// <summary>"3", "ج", "c" or "" for the item at a zero-based position.</summary>
    public string Label(int index) => Style switch
    {
        ItemLabelStyle.Numbers => ArabicText.Number(index + 1),
        ItemLabelStyle.Letters => wording.ItemLetter(index),
        _ => "",
    };

    /// <summary>The label as printed before the item: "3.", "ج.", "c." or nothing.</summary>
    public string Printed(int index) => Printed(Label(index));

    public static string Printed(string label) => label.Length == 0 ? "" : label + ".";
}
