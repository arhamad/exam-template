using System.Text;
using ExamTemplate.Documents.Demo;
using ExamTemplate.Documents.Model;
using ExamTemplate.Tests.TestSupport;

namespace ExamTemplate.Tests.Model;

public class ExamJsonTests
{
    [Fact]
    public void Round_trip_preserves_the_exam()
    {
        var json = ExamJson.Serialize(DemoExams.ComputerLogicDesign());

        Assert.Equal(json, ExamJson.Serialize(ExamJson.Deserialize(json)));
    }

    [Fact]
    public void Json_keeps_arabic_readable_and_writes_enums_as_names()
    {
        var json = ExamJson.Serialize(TestExams.Valid());

        Assert.Contains("جامعة القدس المفتوحة", json);
        Assert.Contains("\"multipleChoice\"", json);
        Assert.Contains("\"schemaVersion\": 1", json);
    }

    [Fact]
    public void Stream_overload_reads_utf8_json()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ExamJson.Serialize(TestExams.Valid())));

        Assert.Equal("1290", ExamJson.Deserialize(stream).Header.CourseCode);
    }

    [Fact]
    public void Comments_and_trailing_commas_are_accepted()
    {
        const string json = """
            {
              // edited by hand
              "schemaVersion": 1,
              "header": { "courseCode": "1290", },
            }
            """;

        Assert.Equal("1290", ExamJson.Deserialize(json).Header.CourseCode);
    }

    [Theory]
    [InlineData("""{ "header": {} }""")]
    [InlineData("""{ "schemaVersion": 0 }""")]
    [InlineData("""[1, 2, 3]""")]
    public void Files_without_a_valid_schema_version_are_rejected(string json)
    {
        var error = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize(json));

        Assert.Equal("الملف ليس ملف امتحان صالحًا.", error.UserMessage.Arabic);
    }

    [Fact]
    public void Newer_schema_versions_are_rejected()
    {
        var error = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("""{ "schemaVersion": 999 }"""));

        Assert.Contains("إصدار أحدث", error.UserMessage.Arabic);
    }

    [Fact]
    public void Malformed_json_is_rejected_with_an_arabic_message()
    {
        var error = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("{ not json"));

        Assert.Contains("صيغة JSON غير صالحة", error.UserMessage.Arabic);
    }

    [Fact]
    public void Errors_have_an_english_message_that_is_also_the_log_message()
    {
        var notAnExam = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("[1, 2, 3]"));
        var newer = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("""{ "schemaVersion": 999 }"""));
        var malformed = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("{\n  not json"));

        Assert.Equal("The file isn't a valid exam file.", notAnExam.UserMessage.English);
        Assert.Equal("This file was created by a newer version of the app and can't be opened.", newer.UserMessage.English);
        Assert.Equal("The file couldn't be read: the JSON isn't valid (line 2).", malformed.UserMessage.English);
        Assert.Contains("(السطر 2)", malformed.UserMessage.Arabic);
        Assert.Equal(malformed.UserMessage.English, malformed.Message);
    }

    [Fact]
    public void Null_for_a_required_value_is_rejected() =>
        Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("""{ "schemaVersion": 1, "header": { "courseName": null } }"""));

    [Fact]
    public void Files_exceeding_the_question_limit_are_rejected()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions = Enumerable.Range(1, ExamLimits.MaxQuestions + 1)
            .Select(i => TestExams.TrueFalse($"عبارة {i}", 1, true))
            .ToList();

        var error = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize(ExamJson.Serialize(exam)));

        Assert.Contains($"عدد الأسئلة أكثر من {ExamLimits.MaxQuestions}", error.UserMessage.Arabic);
        Assert.Equal($"The file exceeds the allowed limits: more than {ExamLimits.MaxQuestions} questions.", error.UserMessage.English);
    }

    [Fact]
    public void Files_exceeding_the_option_limit_are_rejected()
    {
        var exam = TestExams.Valid();
        exam.Sections[0].Questions[0] = TestExams.Mcq("سؤال", 5, 0, Enumerable.Range(1, ExamLimits.MaxOptions + 1).Select(i => $"خيار {i}").ToArray());

        Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize(ExamJson.Serialize(exam)));
    }

    [Fact]
    public void Language_round_trips_and_reads_as_arabic_when_missing()
    {
        var exam = TestExams.Valid();
        exam.Language = ExamLanguage.English;

        var json = ExamJson.Serialize(exam);

        Assert.Contains("\"language\": \"english\"", json);
        Assert.Equal(ExamLanguage.English, ExamJson.Deserialize(json).Language);
        Assert.Equal(ExamLanguage.Arabic, ExamJson.Deserialize("""{ "schemaVersion": 1 }""").Language);
    }

    [Fact]
    public void Alternative_flag_round_trips_and_reads_as_false_when_missing()
    {
        var exam = TestExams.TwoSections();
        exam.Sections[0].AlternativeToNext = true;

        var json = ExamJson.Serialize(exam);

        Assert.Contains("\"alternativeToNext\": true", json);
        Assert.True(ExamJson.Deserialize(json).Sections[0].AlternativeToNext);
        Assert.False(ExamJson.Deserialize(json).Sections[1].AlternativeToNext);
        Assert.All(ExamJson.Deserialize(ExamJson.Serialize(TestExams.TwoSections())).Sections, section => Assert.False(section.AlternativeToNext));
    }

    [Fact]
    public void Undefined_language_numbers_are_rejected()
    {
        var error = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("""{ "schemaVersion": 1, "language": 9 }"""));

        Assert.Equal("الملف ليس ملف امتحان صالحًا.", error.UserMessage.Arabic);
    }

    [Fact]
    public void Unknown_language_names_are_rejected_as_invalid_json()
    {
        var error = Assert.Throws<ExamJsonException>(() => ExamJson.Deserialize("""{ "schemaVersion": 1, "language": "french" }"""));

        Assert.Contains("صيغة JSON غير صالحة", error.UserMessage.Arabic);
    }

    [Fact]
    public void Duplicate_and_empty_ids_are_replaced()
    {
        var exam = TestExams.Valid();
        var questions = exam.Sections[0].Questions;
        questions[1].Id = questions[0].Id;
        questions[2].Id = Guid.Empty;

        var ids = ExamJson.Deserialize(ExamJson.Serialize(exam)).Sections[0].Questions.Select(q => q.Id).ToList();

        Assert.Equal(questions[0].Id, ids[0]);
        Assert.Equal(3, ids.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, ids);
    }
}
