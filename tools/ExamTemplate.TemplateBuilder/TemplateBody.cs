using static ExamTemplate.Documents.Design.DesignTokens;
using static ExamTemplate.TemplateBuilder.WordXml;

namespace ExamTemplate.TemplateBuilder;

/// <summary>
/// document.xml: the exam layout with Templify placeholders, loops and conditions.
/// Keep-together: question, option and answer-line rows keep with the next paragraph, and every
/// question ends with an <c>ExamSpacerEnd</c> paragraph that breaks the chain.
/// </summary>
internal static class TemplateBody
{
    private const int LogoPixelWidth = 120;
    private const int LogoPixelHeight = 117;

    public static string Xml(string logoRelationshipId, string footerRelationshipId) => Document(string.Concat(
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
    private static string Header(string logoRelationshipId)
    {
        var grid = HeaderGrid;
        return Table(grid, new TableOptions(TableBorders.BottomOnly(Line.Heavy), MarginBottomTwips: 60),
            Row(RowOptions.KeepTogether,
                Cell(grid[0], CellOptions.Plain,
                    P("ExamHeaderInfo", R("المقرر: ", "ExamBoldChar"), R("{{CourseName}}")),
                    P("ExamHeaderInfo", R("رقم المقرر: ", "ExamBoldChar"), R("{{CourseCode}}")),
                    P("ExamHeaderInfo", R("مدة الامتحان: ", "ExamBoldChar"), R("{{Duration}}")),
                    P("ExamHeaderInfo", R("عدد الأسئلة: ", "ExamBoldChar"), R("{{QuestionCountText}}")),
                    P("ExamHeaderInfo", R("العلامة الكلية: ", "ExamBoldChar"), R("{{TotalMarksText}}"))),
                Cell(grid[1], CellOptions.Plain,
                    P("ExamLogo", InlineImage(logoRelationshipId, LogoWidthCm, LogoPixelWidth, LogoPixelHeight, "شعار الجامعة")),
                    P("ExamUniversity", R("{{UniversityName}}")),
                    P("ExamTitle", R("{{ExamTitle}}")),
                    P("ExamSubtitle", R("{{SemesterLine}}"))),
                Cell(grid[2], CellOptions.Plain,
                    P("ExamVersionLabel", R("النموذج")),
                    P("ExamVersionLetter", R("{{VersionLetter}}")),
                    Control("{{#if HasExamMode}}"),
                    P("ExamHeaderNote", R("نوع الامتحان: ", "ExamBoldChar"), R("{{ExamMode}}")),
                    Control("{{/if}}"),
                    Control("{{#if HasExamDate}}"),
                    P("ExamHeaderNote", R("التاريخ: ", "ExamBoldChar"), R("{{ExamDateText}}")),
                    Control("{{/if}}"))));
    }

    private static string KeyBanner() =>
        Table([TextWidthTwips], new TableOptions(TableBorders.Box(Line.Heavy)),
            Row(RowOptions.KeepTogether,
                Cell(TextWidthTwips, new CellOptions(PaddingTwips: 60, VerticalAlign: "center"),
                    P("ExamKeyBanner", R("مفتاح الإجابة – النموذج {{VersionLetter}} – للمصحّح فقط")))));

    // Name, ID and group with ruled blanks instead of underscore characters.
    private static string StudentFields()
    {
        var grid = StudentRowGrid;
        var label = new CellOptions(VerticalAlign: "bottom");
        var blank = new CellOptions(Borders: CellBorders.BottomOnly(Line.Hairline), VerticalAlign: "bottom");

        return Table(grid, new TableOptions(TableBorders.None),
            Row(new RowOptions(CantSplit: true, ExactHeightTwips: StudentRowHeightTwips),
                Cell(grid[0], label, P("ExamStudentLabel", R("اسم الطالب:"))),
                Cell(grid[1], blank, P("ExamStudentBlank")),
                Cell(grid[2], label, P("ExamStudentLabel", R("الرقم الجامعي:"))),
                Cell(grid[3], blank, P("ExamStudentBlank")),
                Cell(grid[4], label, P("ExamStudentLabel", R("الشعبة:"))),
                Cell(grid[5], blank, P("ExamStudentBlank"))));
    }

    // Answer key only: question numbers and answers, five pairs per row.
    private static string AnswerSummary()
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
            P("ExamSummaryTitle", R("ملخص الإجابات")),
            Table(grid, new TableOptions(TableBorders.Grid(Line.Hairline), MarginTopTwips: 20, MarginBottomTwips: 20),
                ControlRow(grid, "{{#foreach r in SummaryRows}}"),
                Row(RowOptions.KeepTogether, cells.ToArray()),
                ControlRow(grid, "{{/foreach}}")),
            P("ExamSpacer"),
            Control("{{/if}}"));
    }

    // Numbers sit in their own column so wrapped instruction lines stay aligned.
    private static string Instructions()
    {
        var grid = InstructionsGrid;
        return string.Concat(
            Control("{{#if HasInstructions}}"),
            Table(grid, new TableOptions(TableBorders.Box(Line.Hairline), MarginTopTwips: 20, MarginBottomTwips: 20),
                Row(RowOptions.KeepTogether,
                    Cell(TextWidthTwips, new CellOptions(GridSpan: 2, Fill: FillColor), P("ExamInstructionsTitle", R("تعليمات الامتحان")))),
                ControlRow(grid, "{{#foreach i in Instructions}}"),
                Row(RowOptions.KeepTogether,
                    Cell(grid[0], CellOptions.Plain, P("ExamInstructionNumber", R("{{i.Number}}."))),
                    Cell(grid[1], CellOptions.Plain, P("ExamInstruction", R("{{i.Text}}")))),
                ControlRow(grid, "{{/foreach}}")),
            P("ExamSpacer"),
            Control("{{/if}}"));
    }

    private static string SectionBar()
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
    private static string QuestionRow()
    {
        var grid = QuestionGrid;
        var trueFalse = TrueFalseQuestionGrid;
        var options = new TableOptions(TableBorders.None, MarginTopTwips: 20);

        return string.Concat(
            Control("{{#if q.IsTrueFalse}}"),
            Table(trueFalse, options,
                Row(RowOptions.KeepTogether,
                    Cell(trueFalse[0], CellOptions.Plain, P("ExamQuestionNumber", R("{{q.Number}}."))),
                    Cell(trueFalse[1], CellOptions.Plain, QuestionText()),
                    Cell(trueFalse[2], CellOptions.Plain, P("ExamTrueFalse", R("{{q.TrueBubble}}", "ExamBubbleChar"), R(" صح"))),
                    Cell(trueFalse[3], CellOptions.Plain, P("ExamTrueFalse", R("{{q.FalseBubble}}", "ExamBubbleChar"), R(" خطأ"))),
                    Cell(trueFalse[4], CellOptions.Plain, P("ExamMarks", R("({{q.MarksText}})"))))),
            Control("{{#else}}"),
            Table(grid, options,
                Row(RowOptions.KeepTogether,
                    Cell(grid[0], CellOptions.Plain, P("ExamQuestionNumber", R("{{q.Number}}."))),
                    Cell(grid[1], CellOptions.Plain, QuestionText()),
                    Cell(grid[2], CellOptions.Plain, P("ExamMarks", R("({{q.MarksText}})"))))),
            P("ExamSpacerChain"),
            Control("{{/if}}"));
    }

    private static string QuestionText() => string.Concat(
        P("ExamQuestionText", R("{{q.Text}}")),
        Control("{{#if q.HasNote}}"),
        P("ExamNote", R("ملاحظة: ", "ExamBoldChar"), R("{{q.Note}}")),
        Control("{{/if}}"));

    private static string OptionsOneColumn()
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

    private static string OptionsTwoColumns()
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
    private static string AnswerLines()
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
    private static string ModelAnswer()
    {
        var grid = AnswerAreaGrid;
        return string.Concat(
            Control("{{#if q.ShowModelAnswer}}"),
            Table(grid, new TableOptions(TableBorders.None),
                Row(RowOptions.Plain,
                    Cell(grid[0], CellOptions.Plain, P("ExamModelAnswer")),
                    Cell(grid[1], new CellOptions(Fill: FillColor, Borders: CellBorders.Box(Line.Hairline), PaddingTwips: 80),
                        P("ExamModelAnswerLabel", R("الإجابة النموذجية:")),
                        P("ExamModelAnswer", R("{{q.ModelAnswer}}"))))),
            Control("{{/if}}"));
    }

    private static string SectionProperties(string footerRelationshipId) =>
        $"<w:sectPr><w:footerReference w:type=\"default\" r:id=\"{footerRelationshipId}\"/>"
        + $"<w:pgSz w:w=\"{PageWidthTwips}\" w:h=\"{PageHeightTwips}\"/>"
        + $"<w:pgMar w:top=\"{MarginTopTwips}\" w:right=\"{MarginSideTwips}\" w:bottom=\"{MarginBottomTwips}\" w:left=\"{MarginSideTwips}\" "
        + $"w:header=\"{HeaderDistanceTwips}\" w:footer=\"{FooterDistanceTwips}\" w:gutter=\"0\"/>"
        + "<w:cols w:space=\"708\"/><w:bidi/><w:docGrid w:linePitch=\"360\"/></w:sectPr>";
}
