namespace ExamTemplate.Documents.Model;

/// <summary>Limits shared by the validator, the JSON reader and the editor inputs.</summary>
public static class ExamLimits
{
    public const int MinVersions = 1;
    public const int MaxVersions = 4;
    public const int MinOptions = 2;
    public const int MaxOptions = 8;
    public const int MinAnswerLines = 0;
    public const int MaxAnswerLines = 20;
    public const int MaxQuestions = 200;
    public const int MaxSections = 20;
    public const int MaxInstructions = 20;
    public const decimal MaxMarks = 100m;
    public const int MarksDecimals = 2;

    public const int MaxHeaderFieldLength = 150;
    public const int MaxSectionTitleLength = 200;
    public const int MaxInstructionLength = 500;
    public const int MaxQuestionTextLength = 2000;
    public const int MaxOptionTextLength = 500;
    public const int MaxNoteLength = 500;
    public const int MaxModelAnswerLength = 4000;
}
