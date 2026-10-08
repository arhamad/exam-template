using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Documents.Validation;

/// <summary>
/// Checks an exam before generation. Errors block generation; warnings don't. Messages are in both interface languages,
/// and option and version letters in them follow the exam's language.
/// </summary>
public static class ExamValidator
{
    private const int ClusterMinimumQuestions = 4;
    private const decimal ClusterShareThreshold = 0.6m;

    // Options whose meaning depends on their position, e.g. "جميع ما سبق".
    private static readonly string[] PositionDependentPhrases =
        ["ما سبق", "ما ذكر", "ما ذُكر", "جميع الإجابات", "كل الإجابات", "جميع الخيارات", "كل الخيارات",
         "of the above", "all the above", "none of these", "all of these", "both a and b"];

    public static ValidationReport Validate(ExamDefinition exam)
    {
        var issues = new List<ValidationIssue>();
        ValidateHeader(exam.Header, issues);
        ValidateSettings(exam, issues);
        ValidateContent(exam, issues);
        return new ValidationReport(issues);
    }

    private static void ValidateHeader(ExamHeader header, List<ValidationIssue> issues)
    {
        (string Field, LocalizedText Label, string Value, bool Required)[] fields =
        [
            (nameof(ExamHeader.UniversityName), new("اسم الجامعة", "University name"), header.UniversityName, true),
            (nameof(ExamHeader.ExamTitle), new("عنوان الامتحان", "Exam title"), header.ExamTitle, true),
            (nameof(ExamHeader.CourseName), new("اسم المقرر", "Course name"), header.CourseName, true),
            (nameof(ExamHeader.CourseCode), new("رقم المقرر", "Course code"), header.CourseCode, true),
            (nameof(ExamHeader.Semester), new("الفصل الدراسي", "Semester"), header.Semester, false),
            (nameof(ExamHeader.AcademicYear), new("العام الأكاديمي", "Academic year"), header.AcademicYear, true),
            (nameof(ExamHeader.Duration), new("مدة الامتحان", "Exam duration"), header.Duration, true),
            (nameof(ExamHeader.ExamMode), new("نوع الامتحان", "Exam type"), header.ExamMode, false),
        ];

        foreach (var (field, label, value, required) in fields)
        {
            if (required && string.IsNullOrWhiteSpace(value))
            {
                issues.Add(Error($"حقل «{label.Arabic}» مطلوب.", $"“{label.English}” is required.", field));
            }
            else if (value.Length > ExamLimits.MaxHeaderFieldLength)
            {
                issues.Add(Error(
                    $"حقل «{label.Arabic}» أطول من الحد المسموح به ({ExamLimits.MaxHeaderFieldLength} حرفًا).",
                    $"“{label.English}” is longer than the limit ({ExamLimits.MaxHeaderFieldLength} characters).",
                    field));
            }
        }
    }

    private static void ValidateSettings(ExamDefinition exam, List<ValidationIssue> issues)
    {
        if (exam.VersionCount is < ExamLimits.MinVersions or > ExamLimits.MaxVersions)
        {
            issues.Add(Error(
                $"عدد النماذج يجب أن يكون بين {ExamLimits.MinVersions} و{ExamLimits.MaxVersions}.",
                $"The number of versions must be between {ExamLimits.MinVersions} and {ExamLimits.MaxVersions}.",
                nameof(ExamDefinition.VersionCount)));
        }

        if (exam.ShuffleSeed <= 0)
        {
            issues.Add(Error(
                "رقم الخلط غير صالح؛ استخدم «إعادة خلط» لتوليد رقم جديد.",
                "The shuffle number isn't valid; use “Reshuffle” to get a new one.",
                nameof(ExamDefinition.ShuffleSeed)));
        }

        if (exam.Instructions.Count > ExamLimits.MaxInstructions)
        {
            issues.Add(Error(
                $"الحد الأقصى لعدد التعليمات هو {ExamLimits.MaxInstructions}.",
                $"An exam can have at most {ExamLimits.MaxInstructions} instructions.",
                nameof(ExamDefinition.Instructions)));
        }

        for (var index = 0; index < exam.Instructions.Count; index++)
        {
            if (exam.Instructions[index].Length > ExamLimits.MaxInstructionLength)
            {
                issues.Add(Error(
                    $"التعليمة {index + 1} أطول من الحد المسموح به ({ExamLimits.MaxInstructionLength} حرف).",
                    $"Instruction {index + 1} is longer than the limit ({ExamLimits.MaxInstructionLength} characters).",
                    nameof(ExamDefinition.Instructions)));
            }
        }
    }

    private static void ValidateContent(ExamDefinition exam, List<ValidationIssue> issues)
    {
        if (exam.Sections.Count == 0)
        {
            issues.Add(Error("أضف قسمًا واحدًا على الأقل.", "Add at least one section.", nameof(ExamDefinition.Sections)));
            return;
        }

        if (exam.QuestionCount() == 0)
        {
            issues.Add(Error("أضف سؤالًا واحدًا على الأقل.", "Add at least one question.", nameof(ExamDefinition.Sections)));
            return;
        }

        if (exam.Sections.Count > ExamLimits.MaxSections)
        {
            issues.Add(Error(
                $"الحد الأقصى لعدد الأقسام هو {ExamLimits.MaxSections}.",
                $"An exam can have at most {ExamLimits.MaxSections} sections.",
                nameof(ExamDefinition.Sections)));
        }

        if (exam.QuestionCount() > ExamLimits.MaxQuestions)
        {
            issues.Add(Error(
                $"الحد الأقصى لعدد الأسئلة هو {ExamLimits.MaxQuestions}.",
                $"An exam can have at most {ExamLimits.MaxQuestions} questions.",
                nameof(ExamDefinition.Sections)));
        }

        for (var sectionIndex = 0; sectionIndex < exam.Sections.Count; sectionIndex++)
        {
            var section = exam.Sections[sectionIndex];
            var sectionNumber = sectionIndex + 1;
            var labels = ItemLabels.For(section, exam.Language);

            if (section.Title.Length > ExamLimits.MaxSectionTitleLength)
            {
                issues.Add(Error(
                    $"عنوان القسم {sectionNumber} أطول من الحد المسموح به ({ExamLimits.MaxSectionTitleLength} حرف).",
                    $"The title of section {sectionNumber} is longer than the limit ({ExamLimits.MaxSectionTitleLength} characters).",
                    nameof(ExamSection.Title), section.Id));
            }

            if (section.Instruction.Length > ExamLimits.MaxInstructionLength)
            {
                issues.Add(Error(
                    $"تعليمات القسم {sectionNumber} أطول من الحد المسموح به ({ExamLimits.MaxInstructionLength} حرف).",
                    $"The instructions of section {sectionNumber} are longer than the limit ({ExamLimits.MaxInstructionLength} characters).",
                    nameof(ExamSection.Instruction), section.Id));
            }

            if (section.Questions.Count == 0)
            {
                issues.Add(Error(
                    $"القسم {sectionNumber} لا يحتوي على أسئلة؛ أضف إليه أسئلة أو احذفه.",
                    $"Section {sectionNumber} has no questions; add questions to it or delete it.",
                    nameof(ExamSection.Questions), section.Id));
            }

            if (section.AlternativeToNext)
                ValidateAlternative(exam, sectionIndex, issues);

            for (var index = 0; index < section.Questions.Count; index++)
                ValidateQuestion(exam, section, section.Questions[index], Place(labels, index, sectionNumber), issues);
        }

        WarnAboutClusteredAnswers(exam, issues);
    }

    // The student answers this section or the next one: there must be a next one, a section can't be in two pairs,
    // and both must be worth the same marks since only one of them counts.
    private static void ValidateAlternative(ExamDefinition exam, int sectionIndex, List<ValidationIssue> issues)
    {
        var section = exam.Sections[sectionIndex];
        var number = sectionIndex + 1;
        var field = nameof(ExamSection.AlternativeToNext);

        if (sectionIndex == exam.Sections.Count - 1)
        {
            issues.Add(Error(
                $"القسم {number} هو الأخير، فلا يمكن أن يكون بديلًا لقسم يليه؛ ألغِ الخيار أو أضف القسم البديل بعده.",
                $"Section {number} is the last one, so it can't be an alternative to the next section; turn the option off or add the alternative section after it.",
                field, section.Id));
        }
        else if (sectionIndex > 0 && exam.Sections[sectionIndex - 1].AlternativeToNext)
        {
            issues.Add(Error(
                $"القسم {number} بديل للقسم {number - 1} أصلًا، فلا يمكن أن يكون بديلًا للقسم التالي أيضًا؛ يضم كل اختيار قسمين فقط.",
                $"Section {number} is already the alternative of section {number - 1}, so it can't be an alternative to the next section too; a choice holds two sections only.",
                field, section.Id));
        }
        else if (section.TotalMarks() != exam.Sections[sectionIndex + 1].TotalMarks())
        {
            var marks = section.TotalMarks();
            var nextMarks = exam.Sections[sectionIndex + 1].TotalMarks();
            issues.Add(Error(
                $"علامات القسم {number} ({ArabicText.Marks(marks)}) تختلف عن علامات القسم {number + 1} ({ArabicText.Marks(nextMarks)})؛ يجب أن تتساوى لأن الطالب يجيب عن أحدهما.",
                $"Section {number} ({EnglishText.Marks(marks)}) and section {number + 1} ({EnglishText.Marks(nextMarks)}) must be worth the same marks because the student answers only one of them.",
                field, section.Id));
        }
    }

    /// <summary>How messages refer to a question: «السؤال ب من القسم 2», or «السؤال الوحيد في القسم 4» for a lone essay.</summary>
    private static LocalizedText Place(ItemLabels labels, int index, int sectionNumber)
    {
        var label = labels.Label(index);
        return label.Length > 0
            ? new LocalizedText($"السؤال {label} من القسم {sectionNumber}", $"question {label} of section {sectionNumber}")
            : new LocalizedText($"السؤال الوحيد في القسم {sectionNumber}", $"the only question in section {sectionNumber}");
    }

    private static void ValidateQuestion(ExamDefinition exam, ExamSection section, ExamQuestion question, LocalizedText place,
        List<ValidationIssue> issues)
    {
        void Add(IssueSeverity severity, string arabic, string english, string field, Guid? optionId = null) =>
            issues.Add(new ValidationIssue(severity, new LocalizedText(arabic, english), field, section.Id, question.Id, optionId));

        if (string.IsNullOrWhiteSpace(question.Text))
        {
            Add(IssueSeverity.Error, $"نص {place.Arabic} مطلوب.", $"The text of {place.English} is required.",
                nameof(ExamQuestion.Text));
        }
        else if (question.Text.Length > ExamLimits.MaxQuestionTextLength)
        {
            Add(IssueSeverity.Error,
                $"نص {place.Arabic} أطول من الحد المسموح به ({ExamLimits.MaxQuestionTextLength} حرف).",
                $"The text of {place.English} is longer than the limit ({ExamLimits.MaxQuestionTextLength} characters).",
                nameof(ExamQuestion.Text));
        }

        if (question.Marks <= 0 || question.Marks > ExamLimits.MaxMarks
            || decimal.Round(question.Marks, ExamLimits.MarksDecimals) != question.Marks)
        {
            Add(IssueSeverity.Error,
                $"علامة {place.Arabic} يجب أن تكون أكبر من صفر ولا تتجاوز {ArabicText.Number(ExamLimits.MaxMarks)}، بمنزلتين عشريتين على الأكثر.",
                $"The marks for {place.English} must be more than zero and at most {ArabicText.Number(ExamLimits.MaxMarks)}, with at most two decimal places.",
                nameof(ExamQuestion.Marks));
        }

        if (question.Note.Length > ExamLimits.MaxNoteLength)
        {
            Add(IssueSeverity.Error,
                $"ملاحظة {place.Arabic} أطول من الحد المسموح به ({ExamLimits.MaxNoteLength} حرف).",
                $"The note for {place.English} is longer than the limit ({ExamLimits.MaxNoteLength} characters).",
                nameof(ExamQuestion.Note));
        }

        switch (question.Type)
        {
            case QuestionType.MultipleChoice:
                ValidateOptions(exam, question, place, Add);
                break;

            case QuestionType.TrueFalse when question.TrueFalseAnswer is null:
                Add(IssueSeverity.Error,
                    $"حدّد إن كانت عبارة {place.Arabic} صحيحة أم خاطئة.",
                    $"Choose whether the statement in {place.English} is true or false.",
                    nameof(ExamQuestion.TrueFalseAnswer));
                break;

            case QuestionType.Essay:
                if (question.AnswerLines is < ExamLimits.MinAnswerLines or > ExamLimits.MaxAnswerLines)
                {
                    Add(IssueSeverity.Error,
                        $"عدد أسطر إجابة {place.Arabic} يجب أن يكون بين {ExamLimits.MinAnswerLines} و{ExamLimits.MaxAnswerLines}.",
                        $"The number of answer lines in {place.English} must be between {ExamLimits.MinAnswerLines} and {ExamLimits.MaxAnswerLines}.",
                        nameof(ExamQuestion.AnswerLines));
                }

                if (question.ModelAnswer.Length > ExamLimits.MaxModelAnswerLength)
                {
                    Add(IssueSeverity.Error,
                        $"الإجابة النموذجية في {place.Arabic} أطول من الحد المسموح به ({ExamLimits.MaxModelAnswerLength} حرف).",
                        $"The model answer in {place.English} is longer than the limit ({ExamLimits.MaxModelAnswerLength} characters).",
                        nameof(ExamQuestion.ModelAnswer));
                }
                else if (string.IsNullOrWhiteSpace(question.ModelAnswer))
                {
                    Add(IssueSeverity.Warning,
                        $"{place.Arabic} بلا إجابة نموذجية؛ لن يظهر له شيء في مفتاح الإجابة.",
                        $"There is no model answer for {place.English}; nothing will appear for it in the answer key.",
                        nameof(ExamQuestion.ModelAnswer));
                }

                break;
        }
    }

    private static void ValidateOptions(ExamDefinition exam, ExamQuestion question, LocalizedText place,
        Action<IssueSeverity, string, string, string, Guid?> add)
    {
        var options = question.Options;
        var wording = PaperWording.For(exam.Language);
        if (options.Count is < ExamLimits.MinOptions or > ExamLimits.MaxOptions)
        {
            add(IssueSeverity.Error,
                $"يجب أن يحتوي {place.Arabic} على {ExamLimits.MinOptions} إلى {ExamLimits.MaxOptions} خيارات.",
                $"There must be {ExamLimits.MinOptions} to {ExamLimits.MaxOptions} options in {place.English}.",
                nameof(ExamQuestion.Options), null);
            return;
        }

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            var label = wording.OptionLabel(index);
            if (string.IsNullOrWhiteSpace(option.Text))
            {
                add(IssueSeverity.Error, $"الخيار ({label}) في {place.Arabic} فارغ.", $"Option ({label}) in {place.English} is empty.",
                    "OptionText", option.Id);
            }
            else if (option.Text.Length > ExamLimits.MaxOptionTextLength)
            {
                add(IssueSeverity.Error,
                    $"الخيار ({label}) في {place.Arabic} أطول من الحد المسموح به ({ExamLimits.MaxOptionTextLength} حرف).",
                    $"Option ({label}) in {place.English} is longer than the limit ({ExamLimits.MaxOptionTextLength} characters).",
                    "OptionText", option.Id);
            }
        }

        var correctCount = options.Count(option => option.IsCorrect);
        if (correctCount == 0)
        {
            add(IssueSeverity.Error, $"حدّد الإجابة الصحيحة في {place.Arabic}.", $"Choose the correct answer in {place.English}.",
                "CorrectOption", null);
        }
        else if (correctCount > 1)
        {
            add(IssueSeverity.Error,
                $"{place.Arabic} له أكثر من إجابة صحيحة؛ حدّد إجابة واحدة فقط.",
                $"There is more than one correct answer in {place.English}; choose only one.",
                "CorrectOption", null);
        }

        var texts = options.Select(option => option.Text.Trim()).Where(text => text.Length > 0).ToList();
        if (texts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != texts.Count)
        {
            add(IssueSeverity.Warning,
                $"يوجد خياران متطابقان أو أكثر في {place.Arabic}.",
                $"Two or more options in {place.English} are identical.",
                nameof(ExamQuestion.Options), null);
        }

        if (exam.VersionCount > 1 && !question.KeepOptionOrder && options.Any(option =>
                PositionDependentPhrases.Any(phrase => option.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase))))
        {
            add(IssueSeverity.Warning,
                $"يبدو أن خيارات {place.Arabic} تعتمد على ترتيبها (مثل «جميع ما سبق»)؛ فعّل «الحفاظ على ترتيب الخيارات عند الخلط».",
                $"The options of {place.English} seem to depend on their order (such as “All of the above”); turn on “Keep option order when shuffling”.",
                nameof(ExamQuestion.KeepOptionOrder), null);
        }
    }

    // Authors often type the correct option in the same position; the first version keeps the authored order.
    private static void WarnAboutClusteredAnswers(ExamDefinition exam, List<ValidationIssue> issues)
    {
        var correctPositions = exam.Sections
            .SelectMany(section => section.Questions)
            .Where(question => question.Type == QuestionType.MultipleChoice
                && question.Options.Count <= ExamLimits.MaxOptions
                && question.Options.Count(option => option.IsCorrect) == 1)
            .Select(question => question.Options.FindIndex(option => option.IsCorrect))
            .ToList();

        if (correctPositions.Count < ClusterMinimumQuestions)
            return;

        var (position, count) = correctPositions
            .GroupBy(index => index)
            .Select(group => (Position: group.Key, Count: group.Count()))
            .MaxBy(group => group.Count);

        var share = (decimal)count / correctPositions.Count;
        if (share >= ClusterShareThreshold)
        {
            var wording = PaperWording.For(exam.Language);
            var percent = ArabicText.Number(Math.Round(share * 100));
            var version = wording.VersionLetter(0);
            var option = wording.OptionLabel(position);
            issues.Add(new ValidationIssue(IssueSeverity.Warning,
                new LocalizedText(
                    $"{percent}% من الإجابات الصحيحة في النموذج ({version}) هي الخيار ({option})؛ يُفضّل توزيعها على الخيارات.",
                    $"{percent}% of the correct answers in version ({version}) are option ({option}); consider spreading them across the options."),
                nameof(ExamDefinition.Sections)));
        }
    }

    private static ValidationIssue Error(string arabic, string english, string field, Guid? sectionId = null) =>
        new(IssueSeverity.Error, new LocalizedText(arabic, english), field, sectionId);
}
