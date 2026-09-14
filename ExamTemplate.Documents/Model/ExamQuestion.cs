namespace ExamTemplate.Documents.Model;

/// <summary>
/// A question of any type. Fields that don't apply to the current type are kept,
/// so switching the type in the editor loses nothing.
/// </summary>
public sealed class ExamQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public QuestionType Type { get; set; }
    public string Text { get; set; } = "";
    public decimal Marks { get; set; }
    public string Note { get; set; } = "";

    public List<ExamOption> Options { get; set; } = [];
    public bool KeepOptionOrder { get; set; }

    public bool? TrueFalseAnswer { get; set; }

    public int AnswerLines { get; set; } = 4;
    public string ModelAnswer { get; set; } = "";

    public static ExamQuestion Create(QuestionType type) => new()
    {
        Type = type,
        Options = type == QuestionType.MultipleChoice ? [new(), new(), new(), new()] : [],
    };

    /// <summary>Copies the question and its options with new ids.</summary>
    public ExamQuestion DeepClone() => new()
    {
        Type = Type,
        Text = Text,
        Marks = Marks,
        Note = Note,
        Options = Options.Select(option => new ExamOption { Text = option.Text, IsCorrect = option.IsCorrect }).ToList(),
        KeepOptionOrder = KeepOptionOrder,
        TrueFalseAnswer = TrueFalseAnswer,
        AnswerLines = AnswerLines,
        ModelAnswer = ModelAnswer,
    };
}
