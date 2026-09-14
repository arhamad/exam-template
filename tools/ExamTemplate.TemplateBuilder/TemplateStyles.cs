using System.Text;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using static ExamTemplate.Documents.Design.DesignTokens;

namespace ExamTemplate.TemplateBuilder;

/// <summary>
/// styles.xml. Every style sets all four font slots and both the Latin and complex-script size/bold, because Word formats
/// right-to-left runs, and Arabic words anywhere, with the complex-script properties only.
/// Arabic paragraph styles are right to left (w:bidi); English ones are plain left-to-right paragraphs.
/// </summary>
internal sealed class TemplateStyles(ExamLanguage language)
{
    private const string LanguageTags = "<w:lang w:val=\"en-US\" w:eastAsia=\"en-US\" w:bidi=\"ar-SA\"/>";
    private const int VersionBoxIndentTwips = 1020;

    private static readonly string VersionBox = string.Concat(new[] { "top", "left", "bottom", "right" }
        .Select(side => $"<w:{side} w:val=\"single\" w:sz=\"{MediumBorder}\" w:space=\"2\" w:color=\"{TextColor}\"/>"));

    private static readonly ParagraphStyle[] ParagraphStyles =
    [
        new("TemplateControl", 8, Color: RuleColor),

        new("ExamLogo", SubtitleSize, Align: Align.Center, After: 2),
        new("ExamUniversity", UniversitySize, Bold: true, Align: Align.Center),
        new("ExamTitle", ExamTitleSize, Bold: true, Align: Align.Center, Before: 1),
        new("ExamSubtitle", SubtitleSize, Align: Align.Center, Color: MutedColor, Before: 1),
        new("ExamHeaderInfo", HeaderInfoSize, After: 2),
        new("ExamVersionLabel", VersionLabelSize, Align: Align.Center, Color: MutedColor, Border: VersionBox, IndentTwips: VersionBoxIndentTwips),
        new("ExamVersionLetter", VersionLetterSize, Bold: true, Align: Align.Center, Border: VersionBox, IndentTwips: VersionBoxIndentTwips),
        new("ExamHeaderNote", HeaderInfoSize, Align: Align.Center, Before: 3),

        new("ExamKeyBanner", KeyBannerSize, Bold: true, Align: Align.Center),
        new("ExamStudentLabel", StudentFieldSize, Bold: true),
        new("ExamStudentBlank", StudentFieldSize),

        new("ExamSummaryTitle", SummaryTitleSize, Bold: true, After: 2, KeepNext: true),
        new("ExamSummaryNumber", SummarySize, Bold: true, Align: Align.Center),
        new("ExamSummaryAnswer", SummarySize, Align: Align.Center),

        new("ExamInstructionsTitle", InstructionsTitleSize, Bold: true, Before: 1, After: 1),
        new("ExamInstructionNumber", InstructionSize, Bold: true, Align: Align.Center, After: SpaceAfterInstruction),
        new("ExamInstruction", InstructionSize, After: SpaceAfterInstruction),

        new("ExamSectionTitle", SectionTitleSize, Bold: true, KeepNext: true),
        new("ExamSectionInstruction", SectionInstructionSize, Color: MutedColor, Before: 1, KeepNext: true),
        new("ExamSectionMarks", SectionMarksSize, Bold: true, Align: Align.Center, KeepNext: true),

        new("ExamQuestionNumber", QuestionNumberSize, Bold: true, KeepNext: true),
        new("ExamQuestionText", QuestionTextSize, After: SpaceAfterQuestionText, KeepNext: true),
        new("ExamNote", NoteSize, Color: MutedColor, After: 2, KeepNext: true),
        new("ExamMarks", MarksSize, Bold: true, Align: Align.Center, KeepNext: true),
        new("ExamTrueFalse", TrueFalseSize, Align: Align.Center, KeepNext: true),

        new("ExamOptionBubble", BubbleSize, Align: Align.Center, KeepNext: true, Font: WordBubbleFontFamily),
        new("ExamOptionLabel", OptionSize, Bold: true, KeepNext: true),
        new("ExamOption", OptionSize, After: 1, KeepNext: true),

        new("ExamAnswerLine", 8, KeepNext: true),
        new("ExamModelAnswerLabel", ModelAnswerSize, Bold: true),
        new("ExamModelAnswer", ModelAnswerSize, After: 1),

        new("ExamEnd", EndMarkerSize, Bold: true, Align: Align.Center, Before: SpaceBeforeEndMarker),

        new("ExamFooter", FooterSize, Color: MutedColor),
        new("ExamFooterCenter", FooterSize, Align: Align.Center, Color: MutedColor),
        new("ExamFooterEnd", FooterSize, Align: Align.End, Color: MutedColor),

        // Spacers: tables must never touch once Templify removes the marker paragraphs between them.
        new("ExamSpacer", 2, ExactLineTwips: Twips(BlockGap)),
        new("ExamSpacerChain", 2, ExactLineTwips: Twips(ChainSpacerHeight), KeepNext: true),
        new("ExamSpacerEnd", 2, ExactLineTwips: Twips(QuestionGap)),
        new("ExamFooterTail", 1, ExactLineTwips: 20),
    ];

    private readonly bool _rightToLeft = PaperWording.For(language).IsRightToLeft;
    private readonly FontSlots _textFonts = new(TextFontFamily(language), ArabicFontFamily);

    public string Xml()
    {
        var xml = new StringBuilder($"<w:styles xmlns:w=\"{WordXml.WordNamespace}\">");

        xml.Append("<w:docDefaults><w:rPrDefault><w:rPr>")
            .Append(_textFonts.Xml()).Append(Size(11)).Append(LanguageTags)
            .Append("</w:rPr></w:rPrDefault>")
            .Append("<w:pPrDefault><w:pPr><w:spacing w:after=\"0\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr></w:pPrDefault>")
            .Append("</w:docDefaults>");

        xml.Append("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:qFormat/>")
            .Append("<w:pPr>").Append(_rightToLeft ? "<w:bidi/>" : "").Append("<w:spacing w:before=\"0\" w:after=\"0\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr>")
            .Append("<w:rPr>").Append(RunProperties(_textFonts, 11, bold: false, TextColor)).Append(LanguageTags).Append("</w:rPr>")
            .Append("</w:style>");

        xml.Append("<w:style w:type=\"character\" w:default=\"1\" w:styleId=\"DefaultParagraphFont\"><w:name w:val=\"Default Paragraph Font\"/>")
            .Append("<w:uiPriority w:val=\"1\"/><w:semiHidden/><w:unhideWhenUsed/></w:style>");

        xml.Append("<w:style w:type=\"table\" w:default=\"1\" w:styleId=\"TableNormal\"><w:name w:val=\"Normal Table\"/>")
            .Append("<w:uiPriority w:val=\"99\"/><w:semiHidden/><w:unhideWhenUsed/>")
            .Append($"<w:tblPr><w:tblInd w:w=\"0\" w:type=\"dxa\"/><w:tblCellMar><w:top w:w=\"0\" w:type=\"dxa\"/><w:left w:w=\"{CellMarginTwips}\" w:type=\"dxa\"/>")
            .Append($"<w:bottom w:w=\"0\" w:type=\"dxa\"/><w:right w:w=\"{CellMarginTwips}\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr></w:style>");

        xml.Append("<w:style w:type=\"numbering\" w:default=\"1\" w:styleId=\"NoList\"><w:name w:val=\"No List\"/>")
            .Append("<w:uiPriority w:val=\"99\"/><w:semiHidden/><w:unhideWhenUsed/></w:style>");

        foreach (var style in ParagraphStyles)
            xml.Append(style.Xml(_rightToLeft, _textFonts));

        xml.Append(CharacterStyle("ExamBoldChar", "<w:b/><w:bCs/>"));
        xml.Append(CharacterStyle("ExamBubbleChar", new FontSlots(WordBubbleFontFamily, WordBubbleFontFamily).Xml() + Size(BubbleSize)));
        xml.Append(CharacterStyle("ExamEndNoteChar", $"<w:b w:val=\"0\"/><w:bCs w:val=\"0\"/><w:color w:val=\"{MutedColor}\"/>" + Size(EndNoteSize)));

        return xml.Append("</w:styles>").ToString();
    }

    // No <w:rtl/> here: the schema doesn't allow it in style run properties, so every run sets it itself.
    public static string RunProperties(FontSlots fonts, float size, bool bold, string color) =>
        fonts.Xml() + (bold ? "<w:b/><w:bCs/>" : "") + $"<w:color w:val=\"{color}\"/>" + Size(size);

    public static int Twips(float points) => (int)Math.Round(points * 20);

    private static string Size(float points)
    {
        var halfPoints = (int)Math.Round(points * 2);
        return $"<w:sz w:val=\"{halfPoints}\"/><w:szCs w:val=\"{halfPoints}\"/>";
    }

    private static string CharacterStyle(string id, string runProperties) =>
        $"<w:style w:type=\"character\" w:customStyle=\"1\" w:styleId=\"{id}\"><w:name w:val=\"{id}\"/>"
        + $"<w:basedOn w:val=\"DefaultParagraphFont\"/><w:rPr>{runProperties}</w:rPr></w:style>";
}

/// <summary>A run's font slots: one font for Latin, high-ANSI and East Asian text, one for complex scripts such as Arabic.</summary>
internal sealed record FontSlots(string Text, string ComplexScript)
{
    public string Xml() => $"<w:rFonts w:ascii=\"{Text}\" w:hAnsi=\"{Text}\" w:eastAsia=\"{Text}\" w:cs=\"{ComplexScript}\"/>";
}

/// <param name="Font">A font for all four slots instead of the paper's text fonts.</param>
internal sealed record ParagraphStyle(
    string Id,
    float Size,
    bool Bold = false,
    Align Align = Align.Start,
    string Color = TextColor,
    float Before = 0,
    float After = 0,
    bool KeepNext = false,
    int ExactLineTwips = 0,
    string? Font = null,
    string? Border = null,
    int IndentTwips = 0)
{
    public string Xml(bool rightToLeft, FontSlots textFonts)
    {
        var paragraph = new StringBuilder();
        if (KeepNext)
            paragraph.Append("<w:keepNext/>");
        if (Border is not null)
            paragraph.Append($"<w:pBdr>{Border}</w:pBdr>");
        if (rightToLeft)
            paragraph.Append("<w:bidi/>");

        var line = ExactLineTwips > 0 ? $"w:line=\"{ExactLineTwips}\" w:lineRule=\"exact\"" : "w:line=\"240\" w:lineRule=\"auto\"";
        paragraph.Append($"<w:spacing w:before=\"{TemplateStyles.Twips(Before)}\" w:after=\"{TemplateStyles.Twips(After)}\" {line}/>");

        // Symmetric indents, so the logical left/right reading of w:ind in right-to-left paragraphs doesn't matter.
        if (IndentTwips > 0)
            paragraph.Append($"<w:ind w:left=\"{IndentTwips}\" w:right=\"{IndentTwips}\"/>");

        // Start needs no jc in either direction, and "right" is the end in both: Word reads left and right in
        // right-to-left paragraphs as start and end.
        if (Align != Align.Start)
            paragraph.Append($"<w:jc w:val=\"{(Align == Align.Center ? "center" : "right")}\"/>");

        var fonts = Font is null ? textFonts : new FontSlots(Font, Font);
        return $"<w:style w:type=\"paragraph\" w:customStyle=\"1\" w:styleId=\"{Id}\"><w:name w:val=\"{Id}\"/><w:basedOn w:val=\"Normal\"/><w:qFormat/>"
            + $"<w:pPr>{paragraph}</w:pPr><w:rPr>{TemplateStyles.RunProperties(fonts, Size, Bold, Color)}</w:rPr></w:style>";
    }
}
