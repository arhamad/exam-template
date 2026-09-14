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
    public void NumberOf_counts_questions_continuously_across_sections()
    {
        var exam = TestExams.TwoSections();

        Assert.Equal(1, ExamEditing.NumberOf(exam, exam.Sections[0].Questions[0].Id));
        Assert.Equal(4, ExamEditing.NumberOf(exam, exam.Sections[1].Questions[0].Id));
        Assert.Equal(0, ExamEditing.NumberOf(exam, Guid.NewGuid()));
    }
}
