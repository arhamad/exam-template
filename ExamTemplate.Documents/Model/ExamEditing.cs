namespace ExamTemplate.Documents.Model;

/// <summary>Structural edits used by the question editor.</summary>
public static class ExamEditing
{
    public static bool MoveSection(ExamDefinition exam, Guid sectionId, int delta)
    {
        var pairs = AlternativePairs(exam);
        if (!Move(exam.Sections, exam.Sections.FindIndex(section => section.Id == sectionId), delta))
            return false;

        KeepPairsAdjacent(exam, pairs);
        return true;
    }

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

    public static bool RemoveSection(ExamDefinition exam, Guid sectionId)
    {
        var pairs = AlternativePairs(exam);
        if (exam.Sections.RemoveAll(section => section.Id == sectionId) == 0)
            return false;

        KeepPairsAdjacent(exam, pairs);
        return true;
    }

    /// <summary>Appends an instruction unless the same text is already listed or the list is full.</summary>
    public static bool AddInstructionOnce(ExamDefinition exam, string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0 || exam.Instructions.Count >= ExamLimits.MaxInstructions
            || exam.Instructions.Any(instruction => instruction.Trim() == trimmed))
        {
            return false;
        }

        exam.Instructions.Add(trimmed);
        return true;
    }

    public static void SetCorrectOption(ExamQuestion question, Guid optionId)
    {
        foreach (var option in question.Options)
            option.IsCorrect = option.Id == optionId;
    }

    /// <summary>
    /// Switches the exam language. Header fields and the instruction list that still hold the old language's defaults
    /// take the new language's defaults, and a listed exam mode keeps its meaning. Anything the author wrote stays.
    /// </summary>
    public static void ChangeLanguage(ExamDefinition exam, ExamLanguage language)
    {
        if (exam.Language == language)
            return;

        var (from, to) = (ExamDefaults.For(exam.Language), ExamDefaults.For(language));
        var header = exam.Header;

        header.UniversityName = Swap(header.UniversityName, from.UniversityName, to.UniversityName);
        header.ExamTitle = Swap(header.ExamTitle, from.ExamTitle, to.ExamTitle);
        header.Semester = Swap(header.Semester, from.Semester, to.Semester);
        header.Duration = Swap(header.Duration, from.Duration, to.Duration);

        var mode = from.ExamModes.ToList().IndexOf(header.ExamMode);
        if (mode >= 0)
            header.ExamMode = to.ExamModes[mode];

        if (exam.Instructions.SequenceEqual(from.Instructions))
            exam.Instructions = [.. to.Instructions];

        exam.Language = language;

        static string Swap(string value, string oldDefault, string newDefault) => value == oldDefault ? newDefault : value;
    }

    // An alternative pair is two neighbouring sections, flagged on the first. Moving or removing sections must not
    // pair a section with a different neighbour by accident, so the pairs are remembered as ids before the edit and
    // the flags re-applied after it: a pair that is still adjacent keeps its flag on whichever member now comes first,
    // a separated pair loses it, and a flag that had no partner (an error the validator reports) stays where it was.
    private static List<(Guid First, Guid? Second)> AlternativePairs(ExamDefinition exam) =>
        exam.Sections
            .Select((section, index) => (section, index))
            .Where(pair => pair.section.AlternativeToNext)
            .Select(pair => (pair.section.Id, index: pair.index + 1 < exam.Sections.Count ? exam.Sections[pair.index + 1].Id : (Guid?)null))
            .ToList();

    private static void KeepPairsAdjacent(ExamDefinition exam, List<(Guid First, Guid? Second)> pairs)
    {
        foreach (var section in exam.Sections)
            section.AlternativeToNext = false;

        foreach (var (first, second) in pairs)
        {
            var firstIndex = exam.Sections.FindIndex(section => section.Id == first);
            var secondIndex = second is null ? -1 : exam.Sections.FindIndex(section => section.Id == second);

            if (second is null && firstIndex >= 0)
                exam.Sections[firstIndex].AlternativeToNext = true;
            else if (firstIndex >= 0 && secondIndex >= 0 && Math.Abs(firstIndex - secondIndex) == 1)
                exam.Sections[Math.Min(firstIndex, secondIndex)].AlternativeToNext = true;
        }
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
