using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Validation;

namespace ExamTemplate.Tests.Demo;

public class DemoExamsTests
{
    [Fact]
    public void Computer_logic_design_is_valid_and_worth_100_marks()
    {
        var exam = DemoExams.ComputerLogicDesign();

        Assert.Empty(ExamValidator.Validate(exam).Issues);
        Assert.Equal(100m, exam.TotalMarks());
        Assert.Equal(8, exam.QuestionCount());
    }

    [Fact]
    public void Computer_logic_design_is_deterministic() =>
        Assert.Equal(ExamJson.Serialize(DemoExams.ComputerLogicDesign()), ExamJson.Serialize(DemoExams.ComputerLogicDesign()));
}
