using ExamTemplate.Documents.Services;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Services;

public class ExamDocumentServiceTests
{
    private readonly ExamDocumentService _service = new();

    [Fact]
    public void Validate_reports_errors()
    {
        var exam = TestExams.Valid();
        exam.Header.CourseCode = "";

        Assert.True(_service.Validate(exam).HasErrors);
    }

    [Fact]
    public void BuildPapers_returns_one_paper_per_version()
    {
        var exam = TestExams.Valid();
        exam.VersionCount = 3;

        Assert.Equal(new[] { "أ", "ب", "ج" }, _service.BuildPapers(exam).Select(paper => paper.VersionLetter));
    }
}
