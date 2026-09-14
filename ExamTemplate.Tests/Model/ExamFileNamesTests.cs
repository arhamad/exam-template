using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Model;

public class ExamFileNamesTests
{
    [Theory]
    [InlineData("1290", "1290")]
    [InlineData("CS 101/ب", "CS-101")]
    [InlineData("--a__b--", "a-b")]
    [InlineData("  ", "exam")]
    [InlineData("مقرر", "exam")]
    public void Stem_keeps_only_ascii_letters_digits_and_single_dashes(string courseCode, string expected) =>
        Assert.Equal(expected, ExamFileNames.Stem(courseCode));

    [Fact]
    public void Json_and_bundle_names_use_the_course_code()
    {
        var exam = TestExams.Valid();

        Assert.Equal("1290-exam.json", ExamFileNames.Json(exam));
        Assert.Equal("1290-exam-bundle.zip", ExamFileNames.Bundle(exam));
    }

    [Theory]
    [InlineData(0, PaperKind.Student, "docx", "1290-A-exam.docx")]
    [InlineData(0, PaperKind.AnswerKey, "pdf", "1290-A-key.pdf")]
    [InlineData(1, PaperKind.Student, "pdf", "1290-B-exam.pdf")]
    public void Paper_names_include_the_version_letter_and_the_kind(int version, PaperKind kind, string extension, string expected)
    {
        var exam = TestExams.Valid();
        exam.VersionCount = 2;

        var paper = ExamPaperBuilder.Build(exam, version);

        Assert.Equal(expected, ExamFileNames.Paper(paper, kind, extension));
    }
}
