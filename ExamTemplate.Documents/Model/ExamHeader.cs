namespace ExamTemplate.Documents.Model;

/// <summary>Exam information printed in the page header and footer. Values missing from a file read as the Arabic defaults.</summary>
public sealed class ExamHeader
{
    public string UniversityName { get; set; } = ExamDefaults.Arabic.UniversityName;
    public string ExamTitle { get; set; } = ExamDefaults.Arabic.ExamTitle;
    public string Semester { get; set; } = ExamDefaults.Arabic.Semester;
    public string AcademicYear { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string Duration { get; set; } = ExamDefaults.Arabic.Duration;
    public string ExamMode { get; set; } = ExamDefaults.Arabic.ExamMode;
    public DateOnly? ExamDate { get; set; }
}
