using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Services;

/// <summary>
/// The interface language and its wording, injected into every component as <c>Ui</c>. Each text is written in both
/// languages where it is used: <c>Ui.Text("الأسئلة", "Questions")</c>.
/// </summary>
/// <remarks>
/// Set once per request by <c>App</c> and once per circuit by <c>Routes</c>, from the language cookie. Switching the
/// language reloads the page, so it never changes while a circuit is running.
/// </remarks>
public sealed class UiText
{
    public UiLanguage Language { get; private set; }

    public bool IsEnglish => Language == UiLanguage.English;

    /// <summary>The interface's HTML <c>dir</c>.</summary>
    public string Dir => IsEnglish ? "ltr" : "rtl";

    /// <summary>The interface's HTML <c>lang</c>.</summary>
    public string Lang => UiLanguageCookie.Code(Language);

    /// <summary>For the browser's <c>toLocaleString</c>: the Gregorian calendar and Western digits in both languages.</summary>
    public string DateLocale => IsEnglish ? "en-GB" : "ar-u-ca-gregory-nu-latn";

    /// <summary>The language a new exam starts in; it can be changed on the details page.</summary>
    public ExamLanguage NewExamLanguage => IsEnglish ? ExamLanguage.English : ExamLanguage.Arabic;

    public void Use(UiLanguage language) =>
        Language = Enum.IsDefined(language) ? language : throw new ArgumentOutOfRangeException(nameof(language), language, null);

    public string Text(string arabic, string english) => IsEnglish ? english : arabic;

    public string Text(LocalizedText text) => Text(text.Arabic, text.English);

    public string Questions(int count) => IsEnglish ? EnglishText.Questions(count) : ArabicText.Questions(count);

    public string Marks(decimal marks) => IsEnglish ? EnglishText.Marks(marks) : ArabicText.Marks(marks);

    public string Errors(int count) => IsEnglish ? EnglishText.Errors(count) : ArabicText.Errors(count);

    public string Items(int count) => IsEnglish ? EnglishText.Items(count) : ArabicText.Items(count);

    public string TypeLabel(QuestionType type) => IsEnglish ? EnglishText.TypeLabel(type) : ArabicText.TypeLabel(type);

    public string TrueFalse(bool value) => IsEnglish ? EnglishText.TrueFalse(value) : ArabicText.TrueFalse(value);

    /// <summary>Joins list items with the language's comma («، » or ", ").</summary>
    public string Join(IEnumerable<string> items) => string.Join(IsEnglish ? ", " : "، ", items);
}
