using ExamTemplate.Documents.Model;
using ExamTemplate.Services;

namespace ExamTemplate.Tests.Web;

public class UiTextTests
{
    [Fact]
    public void Arabic_is_the_default_and_right_to_left()
    {
        var ui = new UiText();

        Assert.Equal(UiLanguage.Arabic, ui.Language);
        Assert.Equal(("rtl", "ar"), (ui.Dir, ui.Lang));
        Assert.Equal("الأسئلة", ui.Text("الأسئلة", "Questions"));
        Assert.Equal("سؤالان، علامتان", ui.Join([ui.Questions(2), ui.Marks(2)]));
        Assert.Equal("خطأ واحد", ui.Errors(1));
        Assert.Equal("صح", ui.TrueFalse(true));
    }

    [Fact]
    public void English_is_left_to_right_with_english_counts_and_commas()
    {
        var ui = new UiText();
        ui.Use(UiLanguage.English);

        Assert.Equal(("ltr", "en"), (ui.Dir, ui.Lang));
        Assert.Equal("Questions", ui.Text("الأسئلة", "Questions"));
        Assert.Equal("The file", ui.Text(new LocalizedText("الملف", "The file")));
        Assert.Equal("2 questions, 2 marks", ui.Join([ui.Questions(2), ui.Marks(2)]));
        Assert.Equal("1 error", ui.Errors(1));
        Assert.Equal("Essay", ui.TypeLabel(QuestionType.Essay));
        Assert.Equal("False", ui.TrueFalse(false));
        Assert.Equal("en-GB", ui.DateLocale);
    }

    [Fact]
    public void Undefined_languages_are_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new UiText().Use((UiLanguage)9));
}
