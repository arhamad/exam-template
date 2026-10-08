using ExamTemplate.Documents.Model;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Model;

public class ExamEditingTests
{
    [Fact]
    public void MoveQuestion_swaps_with_its_neighbour()
    {
        var exam = TestExams.Valid();
        var (first, second, third) = (exam.Sections[0].Questions[0], exam.Sections[0].Questions[1], exam.Sections[0].Questions[2]);

        Assert.True(ExamEditing.MoveQuestion(exam, second.Id, -1));

        Assert.Equal(new[] { second, first, third }, exam.Sections[0].Questions);
    }

    [Fact]
    public void MoveQuestion_past_the_edge_does_nothing()
    {
        var exam = TestExams.Valid();
        var before = exam.Sections[0].Questions.ToList();

        Assert.False(ExamEditing.MoveQuestion(exam, before[0].Id, -1));
        Assert.False(ExamEditing.MoveQuestion(exam, before[^1].Id, +1));
        Assert.Equal(before, exam.Sections[0].Questions);
    }

    [Fact]
    public void MoveSection_reorders_sections()
    {
        var exam = TestExams.TwoSections();
        var (first, second) = (exam.Sections[0], exam.Sections[1]);

        Assert.True(ExamEditing.MoveSection(exam, first.Id, +1));

        Assert.Equal(new[] { second, first }, exam.Sections);
    }

    [Fact]
    public void MoveQuestionToSection_appends_to_the_target_section()
    {
        var exam = TestExams.TwoSections();
        var moved = exam.Sections[0].Questions[0];

        Assert.True(ExamEditing.MoveQuestionToSection(exam, moved.Id, exam.Sections[1].Id));

        Assert.DoesNotContain(moved, exam.Sections[0].Questions);
        Assert.Same(moved, exam.Sections[1].Questions[^1]);
    }

    [Fact]
    public void MoveQuestionToSection_within_the_same_section_does_nothing()
    {
        var exam = TestExams.Valid();
        var before = exam.Sections[0].Questions.ToList();

        Assert.False(ExamEditing.MoveQuestionToSection(exam, before[0].Id, exam.Sections[0].Id));
        Assert.Equal(before, exam.Sections[0].Questions);
    }

    [Fact]
    public void DuplicateQuestion_inserts_a_copy_after_the_original()
    {
        var exam = TestExams.Valid();
        var original = exam.Sections[0].Questions[0];

        var copy = ExamEditing.DuplicateQuestion(exam, original.Id);

        Assert.NotNull(copy);
        Assert.Same(copy, exam.Sections[0].Questions[1]);
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(original.Text, copy.Text);
        Assert.Equal(4, exam.Sections[0].Questions.Count);
    }

    [Fact]
    public void RemoveQuestion_deletes_the_question()
    {
        var exam = TestExams.Valid();
        var removed = exam.Sections[0].Questions[1];

        Assert.True(ExamEditing.RemoveQuestion(exam, removed.Id));

        Assert.DoesNotContain(removed, exam.Sections[0].Questions);
        Assert.False(ExamEditing.RemoveQuestion(exam, removed.Id));
    }

    [Fact]
    public void RemoveSection_deletes_the_section_and_its_questions()
    {
        var exam = TestExams.TwoSections();
        var removed = exam.Sections[1];

        Assert.True(ExamEditing.RemoveSection(exam, removed.Id));

        Assert.DoesNotContain(removed, exam.Sections);
        Assert.Equal(3, exam.QuestionCount());
    }

    [Fact]
    public void SetCorrectOption_marks_exactly_one_option()
    {
        var question = TestExams.Mcq("سؤال", 5, 0, "أول", "ثانٍ", "ثالث");

        ExamEditing.SetCorrectOption(question, question.Options[2].Id);

        Assert.Equal(new[] { false, false, true }, question.Options.Select(option => option.IsCorrect));
    }

    [Fact]
    public void ChangeLanguage_replaces_untouched_defaults_and_keeps_authored_text()
    {
        var exam = ExamDefinition.CreateNew(new DateOnly(2026, 9, 14));
        exam.Header.ExamTitle = "الامتحان النصفي";
        exam.Header.CourseName = "تصميم منطق الحاسوب";
        exam.Header.ExamMode = "عملي";

        ExamEditing.ChangeLanguage(exam, ExamLanguage.English);

        Assert.Equal(ExamLanguage.English, exam.Language);
        Assert.Equal("Al-Quds Open University", exam.Header.UniversityName);
        Assert.Equal("الامتحان النصفي", exam.Header.ExamTitle);
        Assert.Equal("تصميم منطق الحاسوب", exam.Header.CourseName);
        Assert.Equal("First Semester", exam.Header.Semester);
        Assert.Equal("1.5 hours", exam.Header.Duration);
        Assert.Equal("Practical", exam.Header.ExamMode);
        Assert.Equal("2026/2027", exam.Header.AcademicYear);
        Assert.Equal(ExamDefaults.English.Instructions, exam.Instructions);
    }

    [Fact]
    public void ChangeLanguage_keeps_edited_instructions_and_unlisted_exam_modes()
    {
        var exam = ExamDefinition.CreateNew(new DateOnly(2026, 9, 14));
        exam.Instructions.Add("تعليمة إضافية.");
        exam.Header.ExamMode = "";
        var instructions = exam.Instructions.ToList();

        ExamEditing.ChangeLanguage(exam, ExamLanguage.English);

        Assert.Equal(instructions, exam.Instructions);
        Assert.Equal("", exam.Header.ExamMode);
    }

    [Fact]
    public void ChangeLanguage_there_and_back_restores_the_original_exam()
    {
        var exam = ExamDefinition.CreateNew(new DateOnly(2026, 9, 14));
        var original = ExamJson.Serialize(exam);

        ExamEditing.ChangeLanguage(exam, ExamLanguage.English);
        ExamEditing.ChangeLanguage(exam, ExamLanguage.Arabic);

        Assert.Equal(original, ExamJson.Serialize(exam));
    }

    [Fact]
    public void Swapping_the_two_sections_of_a_pair_keeps_them_paired()
    {
        var exam = TestExams.TwoSections();
        var (first, second) = (exam.Sections[0], exam.Sections[1]);
        first.AlternativeToNext = true;

        Assert.True(ExamEditing.MoveSection(exam, second.Id, -1));

        Assert.Equal(new[] { second, first }, exam.Sections);
        Assert.True(second.AlternativeToNext);
        Assert.False(first.AlternativeToNext);
    }

    [Fact]
    public void Moving_a_section_away_from_its_pair_clears_the_flag()
    {
        var exam = Sections("A", "B", "C");
        exam.Sections[0].AlternativeToNext = true;

        Assert.True(ExamEditing.MoveSection(exam, exam.Sections[1].Id, +1));

        Assert.Equal(new[] { "A", "C", "B" }, exam.Sections.Select(section => section.Title));
        Assert.All(exam.Sections, section => Assert.False(section.AlternativeToNext));
    }

    [Fact]
    public void Removing_a_section_of_a_pair_clears_the_flag()
    {
        var exam = TestExams.TwoSections();
        exam.Sections[0].AlternativeToNext = true;

        Assert.True(ExamEditing.RemoveSection(exam, exam.Sections[1].Id));

        Assert.False(exam.Sections[0].AlternativeToNext);
    }

    [Fact]
    public void Two_separate_pairs_survive_moving_an_unrelated_section()
    {
        var exam = Sections("A", "B", "C", "X", "D", "E");
        exam.Sections[0].AlternativeToNext = true;
        exam.Sections[4].AlternativeToNext = true;

        Assert.True(ExamEditing.MoveSection(exam, exam.Sections[2].Id, +1));

        Assert.Equal(new[] { "A", "B", "X", "C", "D", "E" }, exam.Sections.Select(section => section.Title));
        Assert.Equal(new[] { "A", "D" }, exam.Sections.Where(section => section.AlternativeToNext).Select(section => section.Title));
    }

    [Fact]
    public void A_flag_without_a_partner_stays_on_its_section_when_sections_move()
    {
        var exam = Sections("A", "B");
        exam.Sections[1].AlternativeToNext = true;

        Assert.True(ExamEditing.MoveSection(exam, exam.Sections[1].Id, -1));

        Assert.Equal(new[] { "B", "A" }, exam.Sections.Select(section => section.Title));
        Assert.True(exam.Sections[0].AlternativeToNext);
    }

    [Fact]
    public void Adding_an_instruction_once_skips_duplicates_and_respects_the_limit()
    {
        var exam = TestExams.Valid();

        Assert.True(ExamEditing.AddInstructionOnce(exam, "أجب عن أحد السؤالين."));
        Assert.False(ExamEditing.AddInstructionOnce(exam, " أجب عن أحد السؤالين. "));
        Assert.False(ExamEditing.AddInstructionOnce(exam, "  "));
        Assert.Equal(["اقرأ/ي السؤال كاملًا قبل الإجابة.", "أجب عن أحد السؤالين."], exam.Instructions);

        exam.Instructions.AddRange(Enumerable.Range(1, ExamLimits.MaxInstructions).Select(i => $"تعليمة {i}"));
        Assert.False(ExamEditing.AddInstructionOnce(exam, "تعليمة جديدة"));
    }

    private static ExamDefinition Sections(params string[] titles)
    {
        var exam = TestExams.Valid();
        exam.Sections = titles
            .Select(title => new ExamSection { Title = title, Questions = [TestExams.TrueFalse($"عبارة {title}", 5, true)] })
            .ToList();
        return exam;
    }
}
