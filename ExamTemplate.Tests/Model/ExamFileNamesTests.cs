using ExamTemplate.Documents.Model;
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
}
