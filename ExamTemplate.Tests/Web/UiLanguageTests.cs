using System.Net;
using ExamTemplate.Services;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExamTemplate.Tests.Web;

public class UiLanguageTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData(null)]
    [InlineData("ar")]
    [InlineData("fr")]
    public async Task Pages_are_arabic_and_right_to_left_unless_the_cookie_asks_for_english(string? cookie)
    {
        var html = await GetPageAsync(cookie);

        Assert.Contains("""<html lang="ar" dir="rtl">""", html);
        Assert.Contains("bootstrap.rtl.min", html);
        Assert.Contains("جارٍ تحميل التطبيق", html);
    }

    [Fact]
    public async Task The_english_cookie_serves_a_left_to_right_page_with_the_ltr_bootstrap_build()
    {
        var html = await GetPageAsync("en");

        Assert.Contains("""<html lang="en" dir="ltr">""", html);
        Assert.Contains("css/bootstrap.min", html);
        Assert.DoesNotContain("bootstrap.rtl", html);
        Assert.Contains("Loading the app", html);
    }

    [Theory]
    [InlineData("ar", true, UiLanguage.Arabic)]
    [InlineData("en", true, UiLanguage.English)]
    [InlineData("EN", false, UiLanguage.Arabic)]
    [InlineData("", false, UiLanguage.Arabic)]
    [InlineData(null, false, UiLanguage.Arabic)]
    public void Only_the_two_known_codes_parse(string? code, bool known, UiLanguage expected)
    {
        Assert.Equal(known, UiLanguageCookie.TryParse(code, out var language));
        Assert.Equal(expected, language);
    }

    [Theory]
    [InlineData(UiLanguage.Arabic)]
    [InlineData(UiLanguage.English)]
    public void Codes_round_trip(UiLanguage language)
    {
        Assert.True(UiLanguageCookie.TryParse(UiLanguageCookie.Code(language), out var parsed));
        Assert.Equal(language, parsed);
    }

    private async Task<string> GetPageAsync(string? cookie)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (cookie is not null)
            request.Headers.Add("Cookie", $"{UiLanguageCookie.Name}={cookie}");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        // Text written through Razor expressions is HTML-encoded, Arabic letters included.
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }
}
