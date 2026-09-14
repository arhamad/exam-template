using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;

namespace ExamTemplate.Documents.Validation;

/// <summary>Checks an exam before generation. Errors block generation; warnings don't.</summary>
public static class ExamValidator
{
    private const int ClusterMinimumQuestions = 4;
    private const decimal ClusterShareThreshold = 0.6m;

    // Options whose meaning depends on their position, e.g. "جميع ما سبق".
    private static readonly string[] PositionDependentPhrases =
        ["ما سبق", "ما ذكر", "ما ذُكر", "جميع الإجابات", "كل الإجابات", "جميع الخيارات", "كل الخيارات", "of the above"];

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
        (string Field, string Label, string Value, bool Required)[] fields =
        [
            (nameof(ExamHeader.UniversityName), "اسم الجامعة", header.UniversityName, true),
            (nameof(ExamHeader.ExamTitle), "عنوان الامتحان", header.ExamTitle, true),
            (nameof(ExamHeader.CourseName), "اسم المقرر", header.CourseName, true),
            (nameof(ExamHeader.CourseCode), "رقم المقرر", header.CourseCode, true),
            (nameof(ExamHeader.Semester), "الفصل الدراسي", header.Semester, false),
            (nameof(ExamHeader.AcademicYear), "العام الأكاديمي", header.AcademicYear, true),
            (nameof(ExamHeader.Duration), "مدة الامتحان", header.Duration, true),
            (nameof(ExamHeader.ExamMode), "نوع الامتحان", header.ExamMode, false),
        ];

        foreach (var (field, label, value, required) in fields)
        {
            if (required && string.IsNullOrWhiteSpace(value))
                issues.Add(Error($"حقل «{label}» مطلوب.", field));
            else if (value.Length > ExamLimits.MaxHeaderFieldLength)
                issues.Add(Error($"حقل «{label}» أطول من الحد المسموح به ({ExamLimits.MaxHeaderFieldLength} حرفًا).", field));
        }
    }

    private static void ValidateSettings(ExamDefinition exam, List<ValidationIssue> issues)
    {
        if (exam.VersionCount is < ExamLimits.MinVersions or > ExamLimits.MaxVersions)
        {
            issues.Add(Error($"عدد النماذج يجب أن يكون بين {ExamLimits.MinVersions} و{ExamLimits.MaxVersions}.",
                nameof(ExamDefinition.VersionCount)));
        }

        if (exam.ShuffleSeed <= 0)
            issues.Add(Error("رقم الخلط غير صالح؛ استخدم «إعادة خلط» لتوليد رقم جديد.", nameof(ExamDefinition.ShuffleSeed)));

        if (exam.Instructions.Count > ExamLimits.MaxInstructions)
            issues.Add(Error($"الحد الأقصى لعدد التعليمات هو {ExamLimits.MaxInstructions}.", nameof(ExamDefinition.Instructions)));

        for (var index = 0; index < exam.Instructions.Count; index++)
        {
            if (exam.Instructions[index].Length > ExamLimits.MaxInstructionLength)
            {
                issues.Add(Error($"التعليمة {index + 1} أطول من الحد المسموح به ({ExamLimits.MaxInstructionLength} حرف).",
                    nameof(ExamDefinition.Instructions)));
            }
        }
    }

    private static void ValidateContent(ExamDefinition exam, List<ValidationIssue> issues)
    {
        if (exam.Sections.Count == 0)
        {
            issues.Add(Error("أضف قسمًا واحدًا على الأقل.", nameof(ExamDefinition.Sections)));
            return;
        }

        if (exam.QuestionCount() == 0)
        {
            issues.Add(Error("أضف سؤالًا واحدًا على الأقل.", nameof(ExamDefinition.Sections)));
            return;
        }

        if (exam.Sections.Count > ExamLimits.MaxSections)
            issues.Add(Error($"الحد الأقصى لعدد الأقسام هو {ExamLimits.MaxSections}.", nameof(ExamDefinition.Sections)));

        if (exam.QuestionCount() > ExamLimits.MaxQuestions)
            issues.Add(Error($"الحد الأقصى لعدد الأسئلة هو {ExamLimits.MaxQuestions}.", nameof(ExamDefinition.Sections)));

        var number = 0;
        for (var sectionIndex = 0; sectionIndex < exam.Sections.Count; sectionIndex++)
        {
            var section = exam.Sections[sectionIndex];

            if (section.Title.Length > ExamLimits.MaxSectionTitleLength)
            {
                issues.Add(Error($"عنوان القسم {sectionIndex + 1} أطول من الحد المسموح به ({ExamLimits.MaxSectionTitleLength} حرف).",
                    nameof(ExamSection.Title), section.Id));
            }

            if (section.Instruction.Length > ExamLimits.MaxInstructionLength)
            {
                issues.Add(Error($"تعليمات القسم {sectionIndex + 1} أطول من الحد المسموح به ({ExamLimits.MaxInstructionLength} حرف).",
                    nameof(ExamSection.Instruction), section.Id));
            }

            if (section.Questions.Count == 0)
            {
                issues.Add(Error($"القسم {sectionIndex + 1} لا يحتوي على أسئلة؛ أضف إليه أسئلة أو احذفه.",
                    nameof(ExamSection.Questions), section.Id));
            }

            foreach (var question in section.Questions)
                ValidateQuestion(exam, section, question, ++number, issues);
        }

        WarnAboutClusteredAnswers(exam, issues);
    }

    private static void ValidateQuestion(ExamDefinition exam, ExamSection section, ExamQuestion question, int number,
        List<ValidationIssue> issues)
    {
        void Add(IssueSeverity severity, string message, string field, Guid? optionId = null) =>
            issues.Add(new ValidationIssue(severity, message, field, section.Id, question.Id, optionId));

        if (string.IsNullOrWhiteSpace(question.Text))
            Add(IssueSeverity.Error, $"نص السؤال {number} مطلوب.", nameof(ExamQuestion.Text));
        else if (question.Text.Length > ExamLimits.MaxQuestionTextLength)
            Add(IssueSeverity.Error, $"نص السؤال {number} أطول من الحد المسموح به ({ExamLimits.MaxQuestionTextLength} حرف).", nameof(ExamQuestion.Text));

        if (question.Marks <= 0 || question.Marks > ExamLimits.MaxMarks
            || decimal.Round(question.Marks, ExamLimits.MarksDecimals) != question.Marks)
        {
            Add(IssueSeverity.Error,
                $"علامة السؤال {number} يجب أن تكون أكبر من صفر ولا تتجاوز {ArabicText.Number(ExamLimits.MaxMarks)}، بمنزلتين عشريتين على الأكثر.",
                nameof(ExamQuestion.Marks));
        }

        if (question.Note.Length > ExamLimits.MaxNoteLength)
            Add(IssueSeverity.Error, $"ملاحظة السؤال {number} أطول من الحد المسموح به ({ExamLimits.MaxNoteLength} حرف).", nameof(ExamQuestion.Note));

        switch (question.Type)
        {
            case QuestionType.MultipleChoice:
                ValidateOptions(exam, question, number, Add);
                break;

            case QuestionType.TrueFalse when question.TrueFalseAnswer is null:
                Add(IssueSeverity.Error, $"حدّد إن كانت عبارة السؤال {number} صحيحة أم خاطئة.", nameof(ExamQuestion.TrueFalseAnswer));
                break;

            case QuestionType.Essay:
                if (question.AnswerLines is < ExamLimits.MinAnswerLines or > ExamLimits.MaxAnswerLines)
                {
                    Add(IssueSeverity.Error,
                        $"عدد أسطر الإجابة للسؤال {number} يجب أن يكون بين {ExamLimits.MinAnswerLines} و{ExamLimits.MaxAnswerLines}.",
                        nameof(ExamQuestion.AnswerLines));
                }

                if (question.ModelAnswer.Length > ExamLimits.MaxModelAnswerLength)
                    Add(IssueSeverity.Error, $"الإجابة النموذجية للسؤال {number} أطول من الحد المسموح به ({ExamLimits.MaxModelAnswerLength} حرف).", nameof(ExamQuestion.ModelAnswer));
                else if (string.IsNullOrWhiteSpace(question.ModelAnswer))
                    Add(IssueSeverity.Warning, $"لا توجد إجابة نموذجية للسؤال {number}؛ لن يظهر له شيء في مفتاح الإجابة.", nameof(ExamQuestion.ModelAnswer));
                break;
        }
    }

    private static void ValidateOptions(ExamDefinition exam, ExamQuestion question, int number,
        Action<IssueSeverity, string, string, Guid?> add)
    {
        var options = question.Options;
        if (options.Count is < ExamLimits.MinOptions or > ExamLimits.MaxOptions)
        {
            add(IssueSeverity.Error, $"يجب أن يحتوي السؤال {number} على {ExamLimits.MinOptions} إلى {ExamLimits.MaxOptions} خيارات.",
                nameof(ExamQuestion.Options), null);
            return;
        }

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            var label = ArabicText.OptionLabel(index);
            if (string.IsNullOrWhiteSpace(option.Text))
                add(IssueSeverity.Error, $"الخيار ({label}) في السؤال {number} فارغ.", "OptionText", option.Id);
            else if (option.Text.Length > ExamLimits.MaxOptionTextLength)
                add(IssueSeverity.Error, $"الخيار ({label}) في السؤال {number} أطول من الحد المسموح به ({ExamLimits.MaxOptionTextLength} حرف).", "OptionText", option.Id);
        }

        var correctCount = options.Count(option => option.IsCorrect);
        if (correctCount == 0)
            add(IssueSeverity.Error, $"حدّد الإجابة الصحيحة للسؤال {number}.", "CorrectOption", null);
        else if (correctCount > 1)
            add(IssueSeverity.Error, $"للسؤال {number} أكثر من إجابة صحيحة؛ حدّد إجابة واحدة فقط.", "CorrectOption", null);

        var texts = options.Select(option => option.Text.Trim()).Where(text => text.Length > 0).ToList();
        if (texts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != texts.Count)
            add(IssueSeverity.Warning, $"يوجد خياران متطابقان أو أكثر في السؤال {number}.", nameof(ExamQuestion.Options), null);

        if (exam.VersionCount > 1 && !question.KeepOptionOrder && options.Any(option =>
                PositionDependentPhrases.Any(phrase => option.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase))))
        {
            add(IssueSeverity.Warning,
                $"يبدو أن خيارات السؤال {number} تعتمد على ترتيبها (مثل «جميع ما سبق»)؛ فعّل «الحفاظ على ترتيب الخيارات عند الخلط».",
                nameof(ExamQuestion.KeepOptionOrder), null);
        }
    }

    // Authors often type the correct option in the same position; version أ keeps the authored order.
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
            issues.Add(new ValidationIssue(IssueSeverity.Warning,
                $"{ArabicText.Number(Math.Round(share * 100))}% من الإجابات الصحيحة في النموذج (أ) هي الخيار ({ArabicText.OptionLabel(position)})؛ يُفضّل توزيعها على الخيارات.",
                nameof(ExamDefinition.Sections)));
        }
    }

    private static ValidationIssue Error(string message, string field, Guid? sectionId = null) =>
        new(IssueSeverity.Error, message, field, sectionId);
}
