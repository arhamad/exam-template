using ExamTemplate.Documents.Model;

namespace ExamTemplate.Components.Shared;

/// <summary>The HTML direction and language of fields holding exam content, so English exams are typed left to right.</summary>
public static class ExamContent
{
    public static string Dir(ExamLanguage language) => language == ExamLanguage.English ? "ltr" : "rtl";

    public static string Lang(ExamLanguage language) => language == ExamLanguage.English ? "en" : "ar";
}
