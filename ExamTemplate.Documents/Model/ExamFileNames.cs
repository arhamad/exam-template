using System.Text.RegularExpressions;
using ExamTemplate.Documents.Papers;

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

    /// <summary>e.g. <c>1290-A-exam.docx</c> for a student copy or <c>1290-B-key.pdf</c> for an answer key.</summary>
    public static string Paper(ExamPaper paper, PaperKind kind, string extension) =>
        $"{Stem(paper.Header.CourseCode)}-{paper.VersionLatin}-{(kind == PaperKind.Student ? "exam" : "key")}.{extension}";

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
