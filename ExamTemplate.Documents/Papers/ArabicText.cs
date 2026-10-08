using System.Globalization;
using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Papers;

/// <summary>Arabic wording of the editor; the Arabic <see cref="PaperWording"/> uses its counted nouns.</summary>
public static class ArabicText
{
    /// <summary>Formats a number with Western digits and "." whatever the current culture.</summary>
    public static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Marks(decimal marks) =>
        Counted(marks, one: "علامة واحدة", two: "علامتان", few: "علامات", many: "علامة", hundreds: "علامة");

    public static string Questions(int count) =>
        Counted(count, one: "سؤال واحد", two: "سؤالان", few: "أسئلة", many: "سؤالًا", hundreds: "سؤال");

    public static string Errors(int count) =>
        Counted(count, one: "خطأ واحد", two: "خطآن", few: "أخطاء", many: "خطأً", hundreds: "خطأ");

    /// <summary>Items of a main question, as counted in the editor («33 بندًا»).</summary>
    public static string Items(int count) =>
        Counted(count, one: "بند واحد", two: "بندان", few: "بنود", many: "بندًا", hundreds: "بند");

    private static readonly string[] Ordinals =
    [
        "الأول", "الثاني", "الثالث", "الرابع", "الخامس", "السادس", "السابع", "الثامن", "التاسع", "العاشر",
        "الحادي عشر", "الثاني عشر", "الثالث عشر", "الرابع عشر", "الخامس عشر", "السادس عشر", "السابع عشر", "الثامن عشر", "التاسع عشر", "العشرون",
    ];

    /// <summary>The masculine ordinal («الخامس») for 1–20, as in «السؤال الخامس»; the numeral beyond that.</summary>
    public static string Ordinal(int number) =>
        number >= 1 && number <= Ordinals.Length ? Ordinals[number - 1] : Number(number);

    public static string TypeLabel(QuestionType type) => type switch
    {
        QuestionType.MultipleChoice => "اختيار من متعدد",
        QuestionType.TrueFalse => "صح أو خطأ",
        QuestionType.Essay => "سؤال مقالي",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static string TrueFalse(bool value) => value ? "صح" : "خطأ";

    // Arabic counted nouns: 1 and 2 have their own forms, 3–10 take the plural,
    // 11–99 the singular accusative, and round hundreds (or fractions) the singular.
    private static string Counted(decimal value, string one, string two, string few, string many, string hundreds)
    {
        if (value != decimal.Truncate(value))
            return $"{Number(value)} {hundreds}";

        var whole = (long)value;
        if (whole == 1)
            return one;
        if (whole == 2)
            return two;

        var noun = (whole % 100) switch
        {
            >= 3 and <= 10 => few,
            >= 11 and <= 99 => many,
            _ => hundreds,
        };
        return $"{Number(value)} {noun}";
    }
}
