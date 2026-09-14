using ExamTemplateDemo.Models;

namespace ExamTemplateDemo.Services;

public static class ExamTemplateMapper
{
    public static Dictionary<string, object> ToTemplateData(ExamModel exam, bool showAnswers)
    {
        return new Dictionary<string, object>
        {
            ["UniversityName"] = exam.UniversityName,
            ["ExamTitle"] = exam.ExamTitle,
            ["Semester"] = exam.Semester,
            ["AcademicYear"] = exam.AcademicYear,
            ["CourseName"] = exam.CourseName,
            ["CourseCode"] = exam.CourseCode,
            ["Duration"] = exam.Duration,
            ["ExamMode"] = exam.ExamMode,
            ["QuestionCount"] = exam.Questions.Count,
            ["TotalMarks"] = exam.TotalMarks,
            ["ShowAnswers"] = showAnswers,
            ["InstructionsBlock"] = BuildInstructions(exam.Instructions),
            ["Questions"] = exam.Questions.Select(MapQuestion).ToList()
        };
    }

    private static Dictionary<string, object> MapQuestion(ExamQuestion q)
    {
        var correct = q.Options.FirstOrDefault(x => x.IsCorrect);

        return new Dictionary<string, object>
        {
            ["Number"] = q.Number,
            ["Type"] = q.Type.ToString(),
            ["TypeLabel"] = q.TypeLabel ?? (q.Type == QuestionType.MultipleChoice ? "اختيار من متعدد" : "إجابة حرة"),
            ["Text"] = q.Text,
            ["Marks"] = q.Marks,
            ["HasSectionTitle"] = !string.IsNullOrWhiteSpace(q.SectionTitle),
            ["SectionTitle"] = q.SectionTitle ?? string.Empty,
            ["SectionInstruction"] = q.SectionInstruction ?? string.Empty,
            ["HasNote"] = !string.IsNullOrWhiteSpace(q.Note),
            ["Note"] = q.Note ?? string.Empty,
            ["OptionsBlock"] = BuildOptions(q.Options),
            ["CorrectAnswer"] = correct is null ? string.Empty : $"{correct.Label}) {correct.Text}",
            ["AnswerBlock"] = BuildAnswerLines(q.AnswerLines),
            ["ModelAnswer"] = q.ModelAnswer ?? string.Empty
        };
    }

    private static string BuildInstructions(IEnumerable<string> instructions) =>
        string.Join("\n", instructions.Select((x, i) => $"{i + 1}. {x}"));

    private static string BuildOptions(IEnumerable<QuestionOption> options) =>
        string.Join("\n", options.Select(x => $"◯ {x.Label}) {x.Text}"));

    private static string BuildAnswerLines(int count)
    {
        count = Math.Clamp(count, 0, 20);
        const string line = "________________________________________________________________________________";
        return string.Join("\n", Enumerable.Repeat(line, count));
    }
}
