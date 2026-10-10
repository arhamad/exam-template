using System.Globalization;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Validation;
using ExamTemplate.Documents.Versioning;

namespace ExamTemplate.Documents.Papers;

/// <summary>Turns an exam definition into printable papers, one per version.</summary>
public static class ExamPaperBuilder
{
    /// <exception cref="ExamValidationException">The exam has validation errors.</exception>
    public static IReadOnlyList<ExamPaper> BuildAll(ExamDefinition exam)
    {
        ThrowIfInvalid(exam);
        return Enumerable.Range(0, exam.VersionCount).Select(index => BuildVersion(exam, index)).ToList();
    }

    /// <exception cref="ExamValidationException">The exam has validation errors.</exception>
    public static ExamPaper Build(ExamDefinition exam, int versionIndex)
    {
        ThrowIfInvalid(exam);
        ArgumentOutOfRangeException.ThrowIfNegative(versionIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(versionIndex, exam.VersionCount);
        return BuildVersion(exam, versionIndex);
    }

    private static void ThrowIfInvalid(ExamDefinition exam)
    {
        var report = ExamValidator.Validate(exam);
        if (report.HasErrors)
            throw new ExamValidationException(report);
    }

    private static ExamPaper BuildVersion(ExamDefinition exam, int versionIndex)
    {
        var wording = PaperWording.For(exam.Language);
        var sections = new List<PaperSection>(exam.Sections.Count);

        for (var index = 0; index < exam.Sections.Count; index++)
        {
            var section = exam.Sections[index];

            // Labels restart in every section and, like option labels, are assigned after shuffling.
            var labels = ItemLabels.For(section, exam.Language);
            var questions = Shuffle(section.Questions, exam.ShuffleSeed, versionIndex, SeedPurpose.QuestionOrder, section.Id)
                .Select((question, position) => BuildQuestion(question, labels.Label(position), exam.ShuffleSeed, versionIndex, wording))
                .ToList();

            var marks = section.TotalMarks();
            sections.Add(new PaperSection(
                wording.SectionName(index),
                Clean(section.Title),
                Clean(section.Instruction),
                section.AlternativeToNext ? wording.ChoiceNotice(index) : "",
                marks,
                wording.Marks(marks),
                questions));
        }

        var mainQuestionCount = exam.MainQuestionCount();
        var totalMarks = exam.TotalMarks();
        return new ExamPaper(
            exam.Language,
            versionIndex,
            wording.VersionLetter(versionIndex),
            PaperWording.VersionLatin(versionIndex),
            BuildHeader(exam.Header),
            exam.Instructions.Select(Clean).Where(instruction => instruction.Length > 0).ToList(),
            sections,
            mainQuestionCount,
            wording.Questions(mainQuestionCount),
            totalMarks,
            wording.Marks(totalMarks),
            BuildAnswerSummary(sections));
    }

    private static PaperQuestion BuildQuestion(ExamQuestion question, string label, int shuffleSeed, int versionIndex, PaperWording wording)
    {
        IEnumerable<ExamOption> orderedOptions = question.Type != QuestionType.MultipleChoice ? []
            : question.KeepOptionOrder ? question.Options
            : Shuffle(question.Options, shuffleSeed, versionIndex, SeedPurpose.OptionOrder, question.Id);

        // Labels are assigned after shuffling, so every version reads أ، ب، ج… (or A, B, C…) from the top.
        var options = orderedOptions
            .Select((option, index) => new PaperOption(wording.OptionLabel(index), Clean(option.Text), option.IsCorrect))
            .ToList();

        var correctAnswer = question.Type switch
        {
            QuestionType.MultipleChoice => CorrectOptionText(options),
            QuestionType.TrueFalse => wording.TrueFalse(question.TrueFalseAnswer!.Value),
            _ => "",
        };

        return new PaperQuestion(
            question.Id,
            label,
            question.Type,
            Clean(question.Text),
            Clean(question.Note),
            question.Marks,
            wording.Marks(question.Marks),
            options,
            question.Type == QuestionType.MultipleChoice ? OptionLayoutRules.Choose(options) : OptionLayout.None,
            question.Type == QuestionType.TrueFalse ? question.TrueFalseAnswer : null,
            question.Type == QuestionType.Essay ? question.AnswerLines : 0,
            question.Type == QuestionType.Essay ? Clean(question.ModelAnswer) : "",
            correctAnswer);
    }

    private static string CorrectOptionText(IEnumerable<PaperOption> options)
    {
        var correct = options.Single(option => option.IsCorrect);
        return $"{correct.Label}) {correct.Text}";
    }

    // One group per section with objective items, since labels restart in every section.
    // A true/false answer is its word; a multiple-choice answer is the correct option's label.
    private static List<AnswerSummaryGroup> BuildAnswerSummary(IEnumerable<PaperSection> sections) =>
        sections
            .Select(section => new AnswerSummaryGroup(
                section.SummaryHeading,
                section.Questions
                    .Where(question => question.Type != QuestionType.Essay)
                    .Select(question => new AnswerSummaryEntry(
                        question.Label,
                        question.Type == QuestionType.TrueFalse
                            ? question.CorrectAnswerText
                            : question.Options.Single(option => option.IsCorrect).Label))
                    .ToList()))
            .Where(group => group.Entries.Count > 0)
            .ToList();

    private static PaperHeader BuildHeader(ExamHeader header) => new(
        Clean(header.UniversityName),
        Clean(header.ExamTitle),
        Clean(header.Semester),
        Clean(header.AcademicYear),
        Clean(header.CourseName),
        Clean(header.CourseCode),
        Clean(header.Duration),
        Clean(header.ExamMode),
        header.ExamDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "");

    private static IEnumerable<T> Shuffle<T>(List<T> items, int shuffleSeed, int versionIndex, SeedPurpose purpose, Guid id)
    {
        // The first version is the authored order. Other versions are seeded per section/question id,
        // so editing one part of the exam doesn't reshuffle the rest.
        if (versionIndex == 0 || items.Count < 2)
            return items;

        var random = new ShuffleRandom(SeedDerivation.Derive(shuffleSeed, versionIndex, purpose, id));
        return Shuffler.Permutation(items.Count, random, avoidIdentity: true).Select(index => items[index]);
    }

    // Removes the private-use characters reserved for Word escaping, normalizes line endings and trims.
    private static string Clean(string text)
    {
        var withoutReserved = text.Any(IsReserved) ? new string(text.Where(character => !IsReserved(character)).ToArray()) : text;
        return withoutReserved.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }

    private static bool IsReserved(char character) => character is >= (char)0xE000 and <= (char)0xE004;
}
