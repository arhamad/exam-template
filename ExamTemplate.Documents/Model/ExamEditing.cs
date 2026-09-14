namespace ExamTemplate.Documents.Model;

/// <summary>Structural edits used by the question editor.</summary>
public static class ExamEditing
{
    public static bool MoveSection(ExamDefinition exam, Guid sectionId, int delta) =>
        Move(exam.Sections, exam.Sections.FindIndex(section => section.Id == sectionId), delta);

    public static bool MoveQuestion(ExamDefinition exam, Guid questionId, int delta) =>
        Find(exam, questionId) is { } found && Move(found.Section.Questions, found.Index, delta);

    /// <summary>Moves a question to the end of another section.</summary>
    public static bool MoveQuestionToSection(ExamDefinition exam, Guid questionId, Guid targetSectionId)
    {
        var target = exam.Sections.Find(section => section.Id == targetSectionId);
        if (target is null || Find(exam, questionId) is not { } found || found.Section == target)
            return false;

        var question = found.Section.Questions[found.Index];
        found.Section.Questions.RemoveAt(found.Index);
        target.Questions.Add(question);
        return true;
    }

    /// <summary>Inserts a copy (with new ids) right after the original question.</summary>
    public static ExamQuestion? DuplicateQuestion(ExamDefinition exam, Guid questionId)
    {
        if (Find(exam, questionId) is not { } found)
            return null;

        var copy = found.Section.Questions[found.Index].DeepClone();
        found.Section.Questions.Insert(found.Index + 1, copy);
        return copy;
    }

    public static bool RemoveQuestion(ExamDefinition exam, Guid questionId)
    {
        if (Find(exam, questionId) is not { } found)
            return false;

        found.Section.Questions.RemoveAt(found.Index);
        return true;
    }

    public static bool RemoveSection(ExamDefinition exam, Guid sectionId) =>
        exam.Sections.RemoveAll(section => section.Id == sectionId) > 0;

    public static void SetCorrectOption(ExamQuestion question, Guid optionId)
    {
        foreach (var option in question.Options)
            option.IsCorrect = option.Id == optionId;
    }

    /// <summary>The question's 1-based number across all sections, or 0 when it isn't in the exam.</summary>
    public static int NumberOf(ExamDefinition exam, Guid questionId)
    {
        var number = 0;
        foreach (var question in exam.Sections.SelectMany(section => section.Questions))
        {
            number++;
            if (question.Id == questionId)
                return number;
        }

        return 0;
    }

    private static (ExamSection Section, int Index)? Find(ExamDefinition exam, Guid questionId)
    {
        foreach (var section in exam.Sections)
        {
            var index = section.Questions.FindIndex(question => question.Id == questionId);
            if (index >= 0)
                return (section, index);
        }

        return null;
    }

    private static bool Move<T>(List<T> items, int index, int delta)
    {
        var target = index + delta;
        if (delta == 0 || index < 0 || target < 0 || target >= items.Count)
            return false;

        var item = items[index];
        items.RemoveAt(index);
        items.Insert(target, item);
        return true;
    }
}
