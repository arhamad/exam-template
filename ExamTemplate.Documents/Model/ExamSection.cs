namespace ExamTemplate.Documents.Model;

/// <summary>A group of questions. Shuffled versions reorder questions only within their section.</summary>
public sealed class ExamSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Instruction { get; set; } = "";
    public List<ExamQuestion> Questions { get; set; } = [];

    public decimal TotalMarks() => Questions.Sum(question => question.Marks);
}
