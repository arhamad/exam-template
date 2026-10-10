using System.Security.Cryptography;

namespace ExamTemplate.Documents.Model;

/// <summary>An exam as authored in the editor and saved as JSON.</summary>
public sealed class ExamDefinition
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public ExamLanguage Language { get; set; } = ExamLanguage.Arabic;
    public ExamHeader Header { get; set; } = new();
    public List<string> Instructions { get; set; } = [];
    public List<ExamSection> Sections { get; set; } = [];
    public int VersionCount { get; set; } = 1;
    public int ShuffleSeed { get; set; }

    /// <summary>The number of items across all sections; the question limit counts these.</summary>
    public int QuestionCount() => Sections.Sum(section => section.Questions.Count);

    /// <summary>The number of main questions on the paper: every section is one.</summary>
    public int MainQuestionCount() => Sections.Count;

    /// <summary>Whether the section at <paramref name="sectionIndex"/> is the alternative of the one before it.</summary>
    public bool IsSecondOfPair(int sectionIndex) => sectionIndex > 0 && Sections[sectionIndex - 1].AlternativeToNext;

    /// <summary>The marks a student can earn: the second section of each alternative pair doesn't count.</summary>
    public decimal TotalMarks() => Sections.Where((_, index) => !IsSecondOfPair(index)).Sum(section => section.TotalMarks());

    /// <summary>A new exam in <paramref name="language"/>, with that language's header defaults and instructions.</summary>
    public static ExamDefinition CreateNew(ExamLanguage language = ExamLanguage.Arabic) =>
        CreateNew(DateOnly.FromDateTime(DateTime.Now), language);

    public static ExamDefinition CreateNew(DateOnly today, ExamLanguage language = ExamLanguage.Arabic)
    {
        // The academic year starts in the autumn semester.
        var startYear = today.Month >= 8 ? today.Year : today.Year - 1;
        var defaults = ExamDefaults.For(language);

        return new ExamDefinition
        {
            Language = language,
            Header = defaults.Header($"{startYear}/{startYear + 1}"),
            Instructions = [.. defaults.Instructions],
            Sections = [new ExamSection()],
            ShuffleSeed = RandomNumberGenerator.GetInt32(1, int.MaxValue),
        };
    }
}
