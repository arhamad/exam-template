namespace ExamTemplate.Documents.Model;

/// <summary>A group of questions. Shuffled versions reorder questions only within their section.</summary>
public sealed class ExamSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Instruction { get; set; } = "";

    /// <summary>
    /// The student answers this section or the next one, not both. Only one of the two counts toward the total,
    /// and the paper prints a notice before this section. Missing in older files, which read it as false.
    /// </summary>
    public bool AlternativeToNext { get; set; }

    public List<ExamQuestion> Questions { get; set; } = [];

    public decimal TotalMarks() => Questions.Sum(question => question.Marks);
}
