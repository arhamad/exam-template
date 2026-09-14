namespace ExamTemplate.Documents.Model;

/// <summary>Exam information printed in the page header and footer.</summary>
public sealed class ExamHeader
{
    public string UniversityName { get; set; } = "جامعة القدس المفتوحة";
    public string ExamTitle { get; set; } = "الامتحان النهائي";
    public string Semester { get; set; } = "الفصل الأول";
    public string AcademicYear { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string Duration { get; set; } = "ساعة ونصف";
    public string ExamMode { get; set; } = "نظري";
    public DateOnly? ExamDate { get; set; }
}
