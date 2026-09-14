using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Design;

/// <summary>
/// Layout values shared by the Word template builder and the PDF renderer so both outputs print alike.
/// Type sizes are in points, page geometry in twips (1/20 pt), borders in eighths of a point.
/// </summary>
public static class DesignTokens
{
    public const string ArabicFontFamily = "Noto Naskh Arabic";
    public const string LatinFontFamily = "Noto Serif";

    /// <summary>PDF fallback for symbols the text fonts lack (○ ● ≤ ≥ ² π →).</summary>
    public const string SymbolFontFamily = "DejaVu Sans";

    /// <summary>Word font for the answer bubbles; ○ and ● are in Arial on Windows and macOS.</summary>
    public const string WordBubbleFontFamily = "Arial";

    public const string EmptyBubble = "○";
    public const string FilledBubble = "●";

    // A4 page
    public const int PageWidthTwips = 11906;
    public const int PageHeightTwips = 16838;
    public const int MarginTopTwips = 720;
    public const int MarginBottomTwips = 850;
    public const int MarginSideTwips = 709;
    public const int HeaderDistanceTwips = 360;
    public const int FooterDistanceTwips = 425;
    public const int TextWidthTwips = PageWidthTwips - 2 * MarginSideTwips;

    // Type scale
    public const float UniversitySize = 14f;
    public const float ExamTitleSize = 12f;
    public const float SubtitleSize = 10f;
    public const float HeaderInfoSize = 10f;
    public const float VersionLabelSize = 9f;
    public const float VersionLetterSize = 20f;
    public const float KeyBannerSize = 14f;
    public const float StudentFieldSize = 10f;
    public const float InstructionsTitleSize = 11f;
    public const float InstructionSize = 10.5f;
    public const float SummaryTitleSize = 11f;
    public const float SummarySize = 10f;
    public const float SectionTitleSize = 12f;
    public const float SectionInstructionSize = 10f;
    public const float SectionMarksSize = 10f;
    public const float QuestionNumberSize = 12f;
    public const float QuestionTextSize = 12f;
    public const float NoteSize = 10f;
    public const float MarksSize = 9.5f;
    public const float TrueFalseSize = 11f;
    public const float OptionSize = 11f;
    public const float BubbleSize = 12f;
    public const float ModelAnswerSize = 10.5f;
    public const float EndMarkerSize = 11f;
    public const float EndNoteSize = 10f;
    public const float FooterSize = 9f;

    // Grayscale palette (RGB hex, no '#')
    public const string TextColor = "000000";
    public const string MutedColor = "404040";
    public const string RuleColor = "808080";
    public const string LightRuleColor = "A6A6A6";
    public const string FillColor = "F2F2F2";

    public const int HairlineBorder = 4;
    public const int MediumBorder = 8;
    public const int HeavyBorder = 12;

    // Vertical rhythm
    public const float SpaceAfterInstruction = 1f;
    public const float SpaceAfterQuestionText = 2f;
    public const float SpaceBeforeEndMarker = 8f;
    public const float ChainSpacerHeight = 2f;
    public const float QuestionGap = 8f;
    public const float BlockGap = 6f;
    public const int AnswerLinePitchTwips = 454;
    public const int StudentRowHeightTwips = 567;
    public const int CellMarginTwips = 57;

    public const float LogoWidthCm = 2f;

    // Table grids in twips, listed from the start side (right in Arabic papers, left in English ones); each sums to TextWidthTwips.
    public static IReadOnlyList<int> HeaderGrid { get; } = [3402, 3684, 3402];
    public static IReadOnlyList<int> SummaryGrid { get; } = [680, 1417, 680, 1417, 680, 1417, 680, 1417, 680, 1420];
    public static IReadOnlyList<int> InstructionsGrid { get; } = [454, 10034];
    public static IReadOnlyList<int> SectionBarGrid { get; } = [8787, 1701];
    public static IReadOnlyList<int> OptionsOneColumnGrid { get; } = [567, 340, 454, 9127];
    public static IReadOnlyList<int> OptionsTwoColumnGrid { get; } = [567, 340, 454, 4166, 340, 454, 4167];
    public static IReadOnlyList<int> AnswerAreaGrid { get; } = [567, 9921];
    public static IReadOnlyList<int> FooterGrid { get; } = [3496, 3496, 3496];

    public const int SummaryPairsPerRow = 5;

    // English labels and marks ("Section:", "(12.25 marks)") are wider than the Arabic ones, so their columns are wider.
    private static readonly IReadOnlyList<int> ArabicStudentRowGrid = [1418, 3402, 1701, 2268, 850, 849];
    private static readonly IReadOnlyList<int> EnglishStudentRowGrid = [1021, 3799, 1417, 2268, 1134, 849];
    private static readonly IReadOnlyList<int> ArabicQuestionGrid = [567, 8504, 1417];
    private static readonly IReadOnlyList<int> EnglishQuestionGrid = [567, 8220, 1701];
    private static readonly IReadOnlyList<int> ArabicTrueFalseQuestionGrid = [567, 6236, 1134, 1134, 1417];
    private static readonly IReadOnlyList<int> EnglishTrueFalseQuestionGrid = [567, 5952, 1134, 1134, 1701];

    /// <summary>The font of a paper's text. Arabic words in English papers use <see cref="ArabicFontFamily"/>.</summary>
    public static string TextFontFamily(ExamLanguage language) =>
        language == ExamLanguage.English ? LatinFontFamily : ArabicFontFamily;

    /// <summary>The PDF font fallback chain: the text font, then the Arabic font for Arabic words in English papers, then symbols.</summary>
    public static string[] PdfFontFamilies(ExamLanguage language) =>
        language == ExamLanguage.English ? [LatinFontFamily, ArabicFontFamily, SymbolFontFamily] : [ArabicFontFamily, SymbolFontFamily];

    /// <summary>Name, student ID and section labels, each followed by its blank.</summary>
    public static IReadOnlyList<int> StudentRowGrid(ExamLanguage language) =>
        language == ExamLanguage.English ? EnglishStudentRowGrid : ArabicStudentRowGrid;

    /// <summary>Number | text and note | marks.</summary>
    public static IReadOnlyList<int> QuestionGrid(ExamLanguage language) =>
        language == ExamLanguage.English ? EnglishQuestionGrid : ArabicQuestionGrid;

    /// <summary>Number | text and note | true | false | marks.</summary>
    public static IReadOnlyList<int> TrueFalseQuestionGrid(ExamLanguage language) =>
        language == ExamLanguage.English ? EnglishTrueFalseQuestionGrid : ArabicTrueFalseQuestionGrid;
}
