using System.Text.RegularExpressions;

namespace ExamTemplate.Documents.Model;

/// <summary>ASCII file names for downloads and zip entries.</summary>
public static partial class ExamFileNames
{
    /// <summary>The course code reduced to ASCII letters, digits and single dashes; "exam" when nothing is left.</summary>
    public static string Stem(string courseCode)
    {
        var stem = NonAlphanumeric().Replace(courseCode, "-").Trim('-');
        return stem.Length == 0 ? "exam" : stem;
    }

    public static string Json(ExamDefinition exam) => $"{Stem(exam.Header.CourseCode)}-exam.json";

    public static string Bundle(ExamDefinition exam) => $"{Stem(exam.Header.CourseCode)}-exam-bundle.zip";

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
