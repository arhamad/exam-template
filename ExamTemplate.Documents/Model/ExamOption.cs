namespace ExamTemplate.Documents.Model;

public sealed class ExamOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
}
