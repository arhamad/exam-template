using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Papers;

/// <summary>English wording of the editor; the English <see cref="PaperWording"/> uses its counted nouns.</summary>
public static class EnglishText
{
    public static string Marks(decimal marks) => Counted(marks, "mark", "marks");

    public static string Questions(int count) => Counted(count, "question", "questions");

    public static string Errors(int count) => Counted(count, "error", "errors");

    /// <summary>Items of a main question, as counted in the editor ("33 items").</summary>
    public static string Items(int count) => Counted(count, "item", "items");

    public static string TypeLabel(QuestionType type) => type switch
    {
        QuestionType.MultipleChoice => "Multiple choice",
        QuestionType.TrueFalse => "True or false",
        QuestionType.Essay => "Essay",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static string TrueFalse(bool value) => value ? "True" : "False";

    // Singular only for exactly one, so "1.00" is "1 mark" and "0.5" is "0.5 marks".
    private static string Counted(decimal value, string one, string many) => $"{ArabicText.Number(value)} {(value == 1 ? one : many)}";
}
