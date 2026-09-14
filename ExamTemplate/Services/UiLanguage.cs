namespace ExamTemplate.Services;

/// <summary>The language of the interface, chosen with the top-bar switch. Separate from the exam's own language.</summary>
public enum UiLanguage
{
    Arabic,
    English,
}

/// <summary>The cookie that remembers the interface language in this browser. Written by <c>exam-interop.js</c>.</summary>
public static class UiLanguageCookie
{
    public const string Name = "examTemplate.uiLanguage";

    public static string Code(UiLanguage language) => language switch
    {
        UiLanguage.Arabic => "ar",
        UiLanguage.English => "en",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };

    public static bool TryParse(string? code, out UiLanguage language)
    {
        (var known, language) = code switch
        {
            "ar" => (true, UiLanguage.Arabic),
            "en" => (true, UiLanguage.English),
            _ => (false, UiLanguage.Arabic),
        };
        return known;
    }

    /// <summary>The language of this request: Arabic unless the cookie holds a known code.</summary>
    public static UiLanguage Read(HttpContext context) =>
        TryParse(context.Request.Cookies[Name], out var language) ? language : UiLanguage.Arabic;
}
