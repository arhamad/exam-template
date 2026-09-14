using ExamTemplate.Documents.Model;

namespace ExamTemplate.Components.Shared;

/// <summary>
/// The HTML direction and language of fields holding exam content, so English exams are typed left to right whatever the
/// interface language.
/// </summary>
public static class ExamContent
{
    private const char FirstStrongIsolate = (char)0x2068;
    private const char PopDirectionalIsolate = (char)0x2069;

    public static string Dir(ExamLanguage language) => language == ExamLanguage.English ? "ltr" : "rtl";

    public static string Lang(ExamLanguage language) => language == ExamLanguage.English ? "en" : "ar";

    /// <summary>
    /// Keeps exam text in its own direction inside interface text where markup (<c>&lt;bdi&gt;</c>) isn't allowed, such
    /// as <c>&lt;option&gt;</c>.
    /// </summary>
    public static string Isolate(string text) => $"{FirstStrongIsolate}{text}{PopDirectionalIsolate}";
}
