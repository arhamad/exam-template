using ExamTemplate.Documents.Model;
using static ExamTemplate.Documents.Design.DesignTokens;
using static ExamTemplate.TemplateBuilder.WordXml;

namespace ExamTemplate.TemplateBuilder;

/// <summary>
/// document.xml: the exam layout with Templify placeholders, loops and conditions.
/// Keep-together: question, option and answer-line rows keep with the next paragraph, and every
/// question ends with an <c>ExamSpacerEnd</c> paragraph that breaks the chain.
/// </summary>
internal sealed class TemplateBody(ExamLanguage language) : TemplatePart(language)
{
    private const int LogoPixelWidth = 120;
    private const int LogoPixelHeight = 117;

    public string Xml(string logoRelationshipId, string footerRelationshipId) => Document(string.Concat(
        Header(logoRelationshipId),
        P("ExamSpacer"),
        Control("{{#if IsAnswerKey}}"),
        KeyBanner(),
        P("ExamSpacer"),
        Control("{{#else}}"),
        StudentFields(),
        P("ExamSpacer"),
        Control("{{/if}}"),
        AnswerSummary(),
        Instructions(),
        Control("{{#foreach s in Sections}}"),
        ChoiceNotice(),
        SectionBar(),
        Control("{{#foreach q in s.Questions}}"),
        QuestionRow(),
        OptionsOneColumn(),
        OptionsTwoColumns(),
        AnswerLines(),
        ModelAnswer(),
        P("ExamSpacerEnd"),
        Control("{{/foreach}}"),
        Control("{{/foreach}}"),
        // One line, so the closing wish can never end up alone on an extra page.
        P("ExamEnd", R("{{EndText}}"), R("{{EndNote}}", "ExamEndNoteChar")),
        SectionProperties(footerRelationshipId)));

    // Course details | logo, university and exam title | version box.
    private string Header(string logoRelationshipId)
    {
        var grid = HeaderGrid;
        return Table(grid, new TableOptions(TableBorders.BottomOnly(Line.Heavy), MarginBottomTwips: 60),
            Row(RowOptions.KeepTogether,
                Cell(grid[0], CellOptions.Plain,
                    P("ExamHeaderInfo", R(Wording.Course, "ExamBoldChar"), R("{{CourseName}}")),
                    P("ExamHeaderInfo", R(Wording.CourseCode, "ExamBoldChar"), R("{{CourseCode}}")),
                    P("ExamHeaderInfo", R(Wording.Duration, "ExamBoldChar"), R("{{Duration}}")),
                    P("ExamHeaderInfo", R(Wording.QuestionCount, "ExamBoldChar"), R("{{QuestionCountText}}")),
                    P("ExamHeaderInfo", R(Wording.TotalMarks, "ExamBoldChar"), R("{{TotalMarksText}}"))),
                Cell(grid[1], CellOptions.Plain,
                    P("ExamLogo", InlineImage(logoRelationshipId, LogoWidthCm, LogoPixelWidth, LogoPixelHeight, Wording.LogoDescription)),
                    P("ExamUniversity", R("{{UniversityName}}")),
                    P("ExamTitle", R("{{ExamTitle}}")),
                    P("ExamSubtitle", R("{{SemesterLine}}"))),
                Cell(grid[2], CellOptions.Plain,
                    P("ExamVersionLabel", R(Wording.Version)),
                    P("ExamVersionLetter", R("{{VersionLetter}}")),
                    Control("{{#if HasExamMode}}"),
                    P("ExamHeaderNote", R(Wording.ExamMode, "ExamBoldChar"), R("{{ExamMode}}")),
                    Control("{{/if}}"),
                    Control("{{#if HasExamDate}}"),
                    P("ExamHeaderNote", R(Wording.Date, "ExamBoldChar"), R("{{ExamDateText}}")),
                    Control("{{/if}}"))));
    }

    private string KeyBanner() =>
        Table([TextWidthTwips], new TableOptions(TableBorders.Box(Line.Heavy)),
            Row(RowOptions.KeepTogether,
                Cell(TextWidthTwips, new CellOptions(PaddingTwips: 60, VerticalAlign: "center"),
                    P("ExamKeyBanner", R(Wording.AnswerKeyBanner("{{VersionLetter}}"))))));

    // Name, ID and group with ruled blanks instead of underscore characters.
    private string StudentFields()
    {
        var grid = StudentRowGrid(Language);
        var label = new CellOptions(VerticalAlign: "bottom");
        var blank = new CellOptions(Borders: CellBorders.BottomOnly(Line.Hairline), VerticalAlign: "bottom");

        return Table(grid, new TableOptions(TableBorders.None),
            Row(new RowOptions(CantSplit: true, ExactHeightTwips: StudentRowHeightTwips),
                Cell(grid[0], label, P("ExamStudentLabel", R(Wording.StudentName))),
                Cell(grid[1], blank, P("ExamStudentBlank")),
                Cell(grid[2], label, P("ExamStudentLabel", R(Wording.StudentId))),
                Cell(grid[3], blank, P("ExamStudentBlank")),
                Cell(grid[4], label, P("ExamStudentLabel", R(Wording.StudentGroup))),
                Cell(grid[5], blank, P("ExamStudentBlank"))));
    }

    // Answer key only: item labels and answers, five pairs per row, in one table per section since labels restart
    // in every section. The heading paragraph keeps consecutive tables apart.
    private string AnswerSummary()
    {
        var grid = SummaryGrid;
        var cells = new List<string>();
        for (var pair = 1; pair <= SummaryPairsPerRow; pair++)
        {
            cells.Add(Cell(grid[2 * pair - 2], new CellOptions(Fill: FillColor, VerticalAlign: "center"), P("ExamSummaryNumber", R("{{r.N" + pair + "}}"))));
            cells.Add(Cell(grid[2 * pair - 1], CellOptions.Middle, P("ExamSummaryAnswer", R("{{r.A" + pair + "}}"))));
        }

        return string.Concat(
            Control("{{#if HasAnswerSummary}}"),
            P("ExamSummaryTitle", R(Wording.AnswerSummary)),
            Control("{{#foreach g in SummaryGroups}}"),
            P("ExamSummaryHeading", R("{{g.Heading}}")),
            Table(grid, new TableOptions(TableBorders.Grid(Line.Hairline), MarginTopTwips: 20, MarginBottomTwips: 20),
                ControlRow(grid, "{{#foreach r in g.Rows}}"),
                Row(RowOptions.KeepTogether, cells.ToArray()),
                ControlRow(grid, "{{/foreach}}")),
            Control("{{/foreach}}"),
            P("ExamSpacer"),
            Control("{{/if}}"));
    }

    // «أجب عن أحد السؤالين…» before the first section of an alternative pair; keepNext ties it to the section bar.
    private string ChoiceNotice() => string.Concat(
        Control("{{#if s.HasChoiceNotice}}"),
        P("ExamChoiceNotice", R("{{s.ChoiceNotice}}")),
        Control("{{/if}}"));

    // Numbers sit in their own column so wrapped instruction lines stay aligned.
    private string Instructions()
    {
        var grid = InstructionsGrid;
        return string.Concat(
            Control("{{#if HasInstructions}}"),
            Table(grid, new TableOptions(TableBorders.Box(Line.Hairline), MarginTopTwips: 20, MarginBottomTwips: 20),
                Row(RowOptions.KeepTogether,
                    Cell(TextWidthTwips, new CellOptions(GridSpan: 2, Fill: FillColor), P("ExamInstructionsTitle", R(Wording.Instructions)))),
                ControlRow(grid, "{{#foreach i in Instructions}}"),
                Row(RowOptions.KeepTogether,
                    Cell(grid[0], CellOptions.Plain, P("ExamInstructionNumber", R("{{i.Number}}."))),
                    Cell(grid[1], CellOptions.Plain, P("ExamInstruction", R("{{i.Text}}")))),
                ControlRow(grid, "{{/foreach}}")),
            P("ExamSpacer"),
            Control("{{/if}}"));
    }

    private string SectionBar()
    {
        var grid = SectionBarGrid;
        var shaded = new CellOptions(Fill: FillColor, VerticalAlign: "center");

        return string.Concat(
            Control("{{#if s.HasBar}}"),
            Table(grid, new TableOptions(TableBorders.TopAndBottom(Line.Medium), MarginTopTwips: 40, MarginBottomTwips: 40),
                Row(RowOptions.KeepTogether,
                    Cell(grid[0], shaded,
                        Control("{{#if s.HasTitle}}"),
                        P("ExamSectionTitle", R("{{s.Title}}")),
                        Control("{{/if}}"),
                        Control("{{#if s.HasInstruction}}"),
                        P("ExamSectionInstruction", R("{{s.Instruction}}")),
                        Control("{{/if}}")),
                    Cell(grid[1], shaded, P("ExamSectionMarks", R("{{s.MarksText}}"))))),
            P("ExamSpacerChain"),
            Control("{{/if}}"));
    }

    // Number | text and note | marks. True/false questions carry their answer bubbles in the same row.
    private string QuestionRow()
    {
        var grid = QuestionGrid(Language);
        var trueFalse = TrueFalseQuestionGrid(Language);
        var options = new TableOptions(TableBorders.None, MarginTopTwips: 20);

        return string.Concat(
            Control("{{#if q.IsTrueFalse}}"),
            Table(trueFalse, options,
                Row(RowOptions.KeepTogether,
                    Cell(trueFalse[0], CellOptions.Plain, P("ExamQuestionNumber", R("{{q.Label}}"))),
                    Cell(trueFalse[1], CellOptions.Plain, QuestionText()),
                    Cell(trueFalse[2], CellOptions.Plain, P("ExamTrueFalse", R("{{q.TrueBubble}}", "ExamBubbleChar"), R(" " + Wording.TrueFalse(true)))),
                    Cell(trueFalse[3], CellOptions.Plain, P("ExamTrueFalse", R("{{q.FalseBubble}}", "ExamBubbleChar"), R(" " + Wording.TrueFalse(false)))),
                    Cell(trueFalse[4], CellOptions.Plain, P("ExamMarks", R("({{q.MarksText}})"))))),
            Control("{{#else}}"),
            Table(grid, options,
                Row(RowOptions.KeepTogether,
                    Cell(grid[0], CellOptions.Plain, P("ExamQuestionNumber", R("{{q.Label}}"))),
                    Cell(grid[1], CellOptions.Plain, QuestionText()),
                    Cell(grid[2], CellOptions.Plain, P("ExamMarks", R("({{q.MarksText}})"))))),
            P("ExamSpacerChain"),
            Control("{{/if}}"));
    }

    private string QuestionText() => string.Concat(
        P("ExamQuestionText", R("{{q.Text}}")),
        Control("{{#if q.HasNote}}"),
        P("ExamNote", R(Wording.Note, "ExamBoldChar"), R("{{q.Note}}")),
        Control("{{/if}}"));

    private string OptionsOneColumn()
    {
        var grid = OptionsOneColumnGrid;
        return string.Concat(
            Control("{{#if q.ShowOptionsOneColumn}}"),
            Table(grid, new TableOptions(TableBorders.None),
                ControlRow(grid, "{{#foreach o in q.Options}}"),
                Row(RowOptions.KeepTogether,
                    Cell(grid[0], CellOptions.Plain, P("ExamOption")),
                    Cell(grid[1], CellOptions.Plain, P("ExamOptionBubble", R("{{o.Bubble}}"))),
                    Cell(grid[2], CellOptions.Plain, P("ExamOptionLabel", R("{{o.Label}}"))),
                    Cell(grid[3], CellOptions.Plain, P("ExamOption", R("{{o.Text}}")))),
                ControlRow(grid, "{{/foreach}}")),
            Control("{{/if}}"));
    }

    private string OptionsTwoColumns()
    {
        var grid = OptionsTwoColumnGrid;
        return string.Concat(
            Control("{{#if q.ShowOptionsTwoColumns}}"),
            Table(grid, new TableOptions(TableBorders.None),
                ControlRow(grid, "{{#foreach r in q.OptionRows}}"),
                Row(RowOptions.KeepTogether,
                    Cell(grid[0], CellOptions.Plain, P("ExamOption")),
                    Cell(grid[1], CellOptions.Plain, P("ExamOptionBubble", R("{{r.Bubble1}}"))),
                    Cell(grid[2], CellOptions.Plain, P("ExamOptionLabel", R("{{r.Label1}}"))),
                    Cell(grid[3], CellOptions.Plain, P("ExamOption", R("{{r.Text1}}"))),
                    Cell(grid[4], CellOptions.Plain, P("ExamOptionBubble", R("{{r.Bubble2}}"))),
                    Cell(grid[5], CellOptions.Plain, P("ExamOptionLabel", R("{{r.Label2}}"))),
                    Cell(grid[6], CellOptions.Plain, P("ExamOption", R("{{r.Text2}}")))),
                ControlRow(grid, "{{/foreach}}")),
            Control("{{/if}}"));
    }

    // Student copy of an essay: ruled lines drawn as cell borders at a fixed pitch.
    private string AnswerLines()
    {
        var grid = AnswerAreaGrid;
        return string.Concat(
            Control("{{#if q.ShowAnswerLines}}"),
            Table(grid, new TableOptions(TableBorders.None),
                ControlRow(grid, "{{#foreach line in q.AnswerLines}}"),
                Row(new RowOptions(CantSplit: true, ExactHeightTwips: AnswerLinePitchTwips),
                    Cell(grid[0], CellOptions.Plain, P("ExamAnswerLine")),
                    Cell(grid[1], new CellOptions(Borders: CellBorders.BottomOnly(Line.LightHairline), VerticalAlign: "bottom"), P("ExamAnswerLine"))),
                ControlRow(grid, "{{/foreach}}")),
            Control("{{/if}}"));
    }

    // Answer key of an essay. Not kept with the next paragraph, so long answers can flow across pages.
    private string ModelAnswer()
    {
        var grid = AnswerAreaGrid;
        return string.Concat(
            Control("{{#if q.ShowModelAnswer}}"),
            Table(grid, new TableOptions(TableBorders.None),
                Row(RowOptions.Plain,
                    Cell(grid[0], CellOptions.Plain, P("ExamModelAnswer")),
                    Cell(grid[1], new CellOptions(Fill: FillColor, Borders: CellBorders.Box(Line.Hairline), PaddingTwips: 80),
                        P("ExamModelAnswerLabel", R(Wording.ModelAnswer)),
                        P("ExamModelAnswer", R("{{q.ModelAnswer}}"))))),
            Control("{{/if}}"));
    }

    private string SectionProperties(string footerRelationshipId) =>
        $"<w:sectPr><w:footerReference w:type=\"default\" r:id=\"{footerRelationshipId}\"/>"
        + $"<w:pgSz w:w=\"{PageWidthTwips}\" w:h=\"{PageHeightTwips}\"/>"
        + $"<w:pgMar w:top=\"{MarginTopTwips}\" w:right=\"{MarginSideTwips}\" w:bottom=\"{MarginBottomTwips}\" w:left=\"{MarginSideTwips}\" "
        + $"w:header=\"{HeaderDistanceTwips}\" w:footer=\"{FooterDistanceTwips}\" w:gutter=\"0\"/>"
        + "<w:cols w:space=\"708\"/>" + (Wording.IsRightToLeft ? "<w:bidi/>" : "") + "<w:docGrid w:linePitch=\"360\"/></w:sectPr>";
}
