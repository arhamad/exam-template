using System.Globalization;
using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Papers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ExamTemplate.Documents.Design.DesignTokens;

namespace ExamTemplate.Documents.Pdf;

/// <summary>
/// The PDF counterpart of the Word templates, with the same design tokens (sizes, colors, grids) and wording so both formats
/// print alike. Arabic papers are laid out right to left; English papers mirror them left to right.
/// Each question is one block that moves to the next page rather than splitting; a section bar travels with its first question.
/// Grids that span the full text width use their twip widths as relative weights, so they always fit the page exactly.
/// </summary>
internal sealed class ExamPdfLayout(ExamPaper paper, PaperKind kind, byte[] logo)
{
    private const float PointsPerCentimeter = 28.3465f;
    private const float BubbleDiameter = 9f;

    private readonly PaperWording _wording = PaperWording.For(paper.Language);

    private bool IsAnswerKey => kind == PaperKind.AnswerKey;

    public void Compose(IDocumentContainer container) => container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginTop(Points(MarginTopTwips));
        page.MarginBottom(Points(FooterDistanceTwips));
        page.MarginHorizontal(Points(MarginSideTwips));
        if (_wording.IsRightToLeft)
            page.ContentFromRightToLeft();
        page.DefaultTextStyle(style => style.FontFamily(PdfFontFamilies(paper.Language)).FontSize(11).FontColor(Hex(TextColor)));

        page.Content().Column(column =>
        {
            column.Item().Element(Header);
            column.Item().Height(BlockGap);
            column.Item().Element(IsAnswerKey ? KeyBanner : StudentFields);
            column.Item().Height(BlockGap);

            if (IsAnswerKey && paper.AnswerSummary.Count > 0)
            {
                column.Item().Element(AnswerSummary);
                column.Item().Height(BlockGap);
            }

            if (paper.Instructions.Count > 0)
            {
                column.Item().Element(Instructions);
                column.Item().Height(BlockGap);
            }

            foreach (var section in paper.Sections)
                Section(column, section);

            column.Item().PaddingTop(SpaceBeforeEndMarker).Element(EndLine);
        });

        page.Footer().PaddingTop(Points(MarginBottomTwips - FooterDistanceTwips) / 2).Element(Footer);
    });

    // Course details | logo, university and exam title | version box.
    private void Header(IContainer container) => container
        .BorderBottom(HeavyBorder / 8f).BorderColor(Hex(TextColor)).PaddingBottom(3)
        .Row(row =>
        {
            row.RelativeItem(HeaderGrid[0]).Column(details =>
            {
                details.Spacing(2);
                LabeledLine(details.Item(), _wording.Course, paper.Header.CourseName, HeaderInfoSize);
                LabeledLine(details.Item(), _wording.CourseCode, paper.Header.CourseCode, HeaderInfoSize);
                LabeledLine(details.Item(), _wording.Duration, paper.Header.Duration, HeaderInfoSize);
                LabeledLine(details.Item(), _wording.QuestionCount, paper.QuestionCountText, HeaderInfoSize);
                LabeledLine(details.Item(), _wording.TotalMarks, paper.TotalMarksText, HeaderInfoSize);
            });

            row.RelativeItem(HeaderGrid[1]).Column(center =>
            {
                center.Item().AlignCenter().Width(LogoWidthCm * PointsPerCentimeter).Image(logo);
                center.Item().PaddingTop(2).Text(Isolate(paper.Header.UniversityName)).FontSize(UniversitySize).Bold().AlignCenter();
                center.Item().PaddingTop(1).Text(Isolate(paper.Header.ExamTitle)).FontSize(ExamTitleSize).Bold().AlignCenter();
                center.Item().PaddingTop(1).Text(Isolate(SemesterLine())).FontSize(SubtitleSize).FontColor(Hex(MutedColor)).AlignCenter();
            });

            row.RelativeItem(HeaderGrid[2]).Column(side =>
            {
                side.Item().AlignCenter().Width(80).Border(MediumBorder / 8f).BorderColor(Hex(TextColor)).PaddingVertical(3).Column(box =>
                {
                    box.Item().Text(_wording.Version).FontSize(VersionLabelSize).FontColor(Hex(MutedColor)).AlignCenter();
                    box.Item().Text(paper.VersionLetter).FontSize(VersionLetterSize).Bold().AlignCenter();
                });

                if (paper.Header.ExamMode.Length > 0)
                    LabeledLine(side.Item().PaddingTop(4).AlignCenter(), _wording.ExamMode, paper.Header.ExamMode, HeaderInfoSize);
                if (paper.Header.ExamDateText.Length > 0)
                    LabeledLine(side.Item().PaddingTop(3).AlignCenter(), _wording.Date, paper.Header.ExamDateText, HeaderInfoSize);
            });
        });

    private void KeyBanner(IContainer container) => container
        .Border(HeavyBorder / 8f).BorderColor(Hex(TextColor)).PaddingVertical(3)
        .Text(_wording.AnswerKeyBanner(paper.VersionLetter)).FontSize(KeyBannerSize).Bold().AlignCenter();

    // Name, ID and group with drawn rules instead of underscore characters.
    private void StudentFields(IContainer container) => container.Height(Points(StudentRowHeightTwips)).Row(row =>
    {
        var grid = StudentRowGrid(paper.Language);
        StudentField(row, grid[0], grid[1], _wording.StudentName);
        StudentField(row, grid[2], grid[3], _wording.StudentId);
        StudentField(row, grid[4], grid[5], _wording.StudentGroup);
    });

    private static void StudentField(RowDescriptor row, int labelTwips, int blankTwips, string label)
    {
        row.RelativeItem(labelTwips).AlignBottom().Text(label).FontSize(StudentFieldSize).Bold();
        row.RelativeItem(blankTwips).AlignBottom().PaddingHorizontal(3).PaddingBottom(3)
            .LineHorizontal(HairlineBorder / 8f).LineColor(Hex(RuleColor));
    }

    // Answer key only: item labels and answers, five pairs per row, one table per section since labels restart.
    private void AnswerSummary(IContainer container) => container.Column(column =>
    {
        column.Item().PaddingBottom(2).Text(_wording.AnswerSummary).FontSize(SummaryTitleSize).Bold();
        foreach (var group in paper.AnswerSummary)
        {
            column.Item().PaddingTop(3).PaddingBottom(1).Text(Isolate(group.Heading)).FontSize(SummaryHeadingSize).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var width in SummaryGrid)
                        columns.RelativeColumn(width);
                });

                var padding = (SummaryPairsPerRow - group.Entries.Count % SummaryPairsPerRow) % SummaryPairsPerRow;
                var entries = group.Entries
                    .Select(entry => (entry.Label, entry.Answer))
                    .Concat(Enumerable.Repeat((Label: "", Answer: ""), padding));

                foreach (var (label, answer) in entries)
                {
                    SummaryCell(table.Cell(), label, shaded: true);
                    SummaryCell(table.Cell(), answer, shaded: false);
                }
            });
        }
    });

    private static void SummaryCell(IContainer cell, string text, bool shaded)
    {
        var block = cell.Border(HairlineBorder / 8f).BorderColor(Hex(RuleColor))
            .Background(Hex(shaded ? FillColor : "FFFFFF"))
            .PaddingVertical(2)
            .Text(text).FontSize(SummarySize).AlignCenter();
        if (shaded)
            block.Bold();
    }

    // Numbers sit in their own column so wrapped instruction lines stay aligned.
    private void Instructions(IContainer container) => container
        .Border(HairlineBorder / 8f).BorderColor(Hex(RuleColor))
        .Column(column =>
        {
            column.Item().Background(Hex(FillColor)).PaddingHorizontal(4).PaddingVertical(2)
                .Text(_wording.Instructions).FontSize(InstructionsTitleSize).Bold();

            for (var index = 0; index < paper.Instructions.Count; index++)
            {
                var number = (index + 1).ToString(CultureInfo.InvariantCulture);
                var instruction = paper.Instructions[index];
                column.Item().PaddingHorizontal(4).PaddingVertical(1).Row(row =>
                {
                    row.ConstantItem(Points(InstructionsGrid[0])).Text($"{number}.").FontSize(InstructionSize).Bold().AlignCenter();
                    row.RelativeItem().Text(Isolate(instruction)).FontSize(InstructionSize);
                });
            }
        });

    private void Section(ColumnDescriptor column, PaperSection section)
    {
        for (var index = 0; index < section.Questions.Count; index++)
        {
            var question = section.Questions[index];
            var withBar = index == 0 && section.HasBar;

            column.Item().PreventPageBreak().Column(block =>
            {
                // The choice notice stays on the page with the section bar (or the first question) it introduces.
                if (index == 0 && section.HasChoiceNotice)
                {
                    block.Item().PaddingTop(4).PaddingBottom(4).Text(Isolate(section.ChoiceNotice))
                        .FontSize(ChoiceNoticeSize).Bold().AlignCenter();
                }

                if (withBar)
                {
                    block.Item().Element(container => SectionBar(container, section));
                    block.Item().Height(ChainSpacerHeight);
                }

                block.Item().Element(container => Question(container, question));
            });

            column.Item().Height(QuestionGap);
        }
    }

    private void SectionBar(IContainer container, PaperSection section) => container
        .BorderTop(MediumBorder / 8f).BorderBottom(MediumBorder / 8f).BorderColor(Hex(TextColor))
        .Background(Hex(FillColor)).PaddingVertical(2)
        .Row(row =>
        {
            row.RelativeItem().PaddingHorizontal(3).Column(text =>
            {
                if (section.Title.Length > 0)
                    text.Item().Text(Isolate(section.Title)).FontSize(SectionTitleSize).Bold();
                if (section.Instruction.Length > 0)
                    text.Item().PaddingTop(1).Text(Isolate(section.Instruction)).FontSize(SectionInstructionSize).FontColor(Hex(MutedColor));
            });
            row.ConstantItem(Points(SectionBarGrid[1])).AlignMiddle().Text(Isolate(section.MarksText)).FontSize(SectionMarksSize).Bold().AlignCenter();
        });

    // Number | text and note | (true/false bubbles) | marks, followed by options, answer lines or the model answer.
    private void Question(IContainer container, PaperQuestion question) => container.Column(column =>
    {
        var questionGrid = QuestionGrid(paper.Language);
        var trueFalseGrid = TrueFalseQuestionGrid(paper.Language);

        column.Item().PaddingTop(1).Row(row =>
        {
            row.ConstantItem(Points(questionGrid[0])).Text(question.PrintedLabel).FontSize(QuestionNumberSize).Bold();

            row.RelativeItem().Column(text =>
            {
                text.Item().Text(Isolate(question.Text)).FontSize(QuestionTextSize);
                if (question.Note.Length > 0)
                {
                    text.Item().PaddingTop(SpaceAfterQuestionText).Text(note =>
                    {
                        note.Span(_wording.Note).FontSize(NoteSize).Bold().FontColor(Hex(MutedColor));
                        note.Span(Isolate(question.Note)).FontSize(NoteSize).FontColor(Hex(MutedColor));
                    });
                }
            });

            if (question.Type == QuestionType.TrueFalse)
            {
                row.ConstantItem(Points(trueFalseGrid[2])).Element(cell => TrueFalseChoice(cell, _wording.TrueFalse(true), IsAnswerKey && question.TrueFalseAnswer == true));
                row.ConstantItem(Points(trueFalseGrid[3])).Element(cell => TrueFalseChoice(cell, _wording.TrueFalse(false), IsAnswerKey && question.TrueFalseAnswer == false));
            }

            row.ConstantItem(Points(questionGrid[2])).Text(Isolate($"({question.MarksText})")).FontSize(MarksSize).Bold().AlignCenter();
        });

        switch (question.Type)
        {
            case QuestionType.MultipleChoice:
                column.Item().PaddingTop(SpaceAfterQuestionText).Element(options => Options(options, question));
                break;

            case QuestionType.Essay when !IsAnswerKey:
                for (var line = 0; line < question.AnswerLines; line++)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(Points(AnswerAreaGrid[0]));
                        row.RelativeItem().Height(Points(AnswerLinePitchTwips)).AlignBottom()
                            .LineHorizontal(HairlineBorder / 8f).LineColor(Hex(LightRuleColor));
                    });
                }
                break;

            case QuestionType.Essay when question.ModelAnswer.Length > 0:
                column.Item().PaddingTop(3).Row(row =>
                {
                    row.ConstantItem(Points(AnswerAreaGrid[0]));
                    row.RelativeItem().Element(box => ModelAnswer(box, question.ModelAnswer));
                });
                break;
        }
    });

    private static void TrueFalseChoice(IContainer container, string label, bool filled) => container.AlignCenter().Row(row =>
    {
        row.AutoItem().PaddingTop(4).Element(bubble => Bubble(bubble, filled));
        row.AutoItem().PaddingHorizontal(3).Text(label).FontSize(TrueFalseSize);
    });

    // Short options in two columns (A B / C D), otherwise one row per option.
    private void Options(IContainer container, PaperQuestion question) => container.Column(column =>
    {
        if (question.OptionLayout == OptionLayout.TwoColumns)
        {
            var grid = OptionsTwoColumnGrid;
            foreach (var pair in question.Options.Chunk(2))
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(Points(grid[0]));
                    Option(row, pair[0], textWeight: grid[3]);
                    if (pair.Length > 1)
                        Option(row, pair[1], textWeight: grid[6]);
                    else
                        row.RelativeItem(grid[4] + grid[5] + grid[6]);
                });
            }
        }
        else
        {
            foreach (var option in question.Options)
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(Points(OptionsOneColumnGrid[0]));
                    Option(row, option, textWeight: 1);
                });
            }
        }
    });

    private void Option(RowDescriptor row, PaperOption option, int textWeight)
    {
        row.ConstantItem(Points(OptionsOneColumnGrid[1])).PaddingTop(4).AlignCenter().Element(bubble => Bubble(bubble, IsAnswerKey && option.IsCorrect));
        row.ConstantItem(Points(OptionsOneColumnGrid[2])).Text($"{option.Label})").FontSize(OptionSize).Bold();
        row.RelativeItem(textWeight).PaddingBottom(1).Text(Isolate(option.Text)).FontSize(OptionSize);
    }

    private void ModelAnswer(IContainer container, string answer) => container
        .Border(HairlineBorder / 8f).BorderColor(Hex(RuleColor)).Background(Hex(FillColor)).Padding(4)
        .Column(column =>
        {
            column.Item().Text(_wording.ModelAnswer).FontSize(ModelAnswerSize).Bold();
            column.Item().PaddingTop(1).Text(Isolate(answer)).FontSize(ModelAnswerSize);
        });

    private void EndLine(IContainer container) => container.AlignCenter().Text(text =>
    {
        text.Span(IsAnswerKey ? _wording.EndOfAnswerKey : _wording.EndOfQuestions).FontSize(EndMarkerSize).Bold();
        if (!IsAnswerKey)
            text.Span(_wording.GoodLuck).FontSize(EndNoteSize).FontColor(Hex(MutedColor));
    });

    private void Footer(IContainer container) => container
        .BorderTop(HairlineBorder / 8f).BorderColor(Hex(RuleColor)).PaddingTop(2)
        .Row(row =>
        {
            row.RelativeItem().Text(Isolate($"{paper.Header.CourseCode} – {paper.Header.CourseName}")).FontSize(FooterSize).FontColor(Hex(MutedColor));
            row.RelativeItem().Text(text =>
            {
                text.AlignCenter();
                text.Span(_wording.Page).FontSize(FooterSize).FontColor(Hex(MutedColor));
                text.CurrentPageNumber().FontSize(FooterSize).FontColor(Hex(MutedColor));
                text.Span(_wording.PageOf).FontSize(FooterSize).FontColor(Hex(MutedColor));
                text.TotalPages().FontSize(FooterSize).FontColor(Hex(MutedColor));
            });
            row.RelativeItem().Text(Isolate($"{_wording.VersionName(paper.VersionLetter)} – {paper.Header.AcademicYear}")).FontSize(FooterSize).FontColor(Hex(MutedColor)).AlignEnd();
        });

    private void LabeledLine(IContainer container, string label, string value, float size) => container.Text(text =>
    {
        text.Span(label).FontSize(size).Bold();
        text.Span(Isolate(value)).FontSize(size);
    });

    // Answer bubbles are drawn, so they don't depend on a font having ○ and ●.
    private static void Bubble(IContainer container, bool filled)
    {
        var circle = container.Width(BubbleDiameter).Height(BubbleDiameter)
            .CornerRadius(BubbleDiameter / 2).Border(0.75f).BorderColor(Hex(TextColor));
        if (filled)
            circle.Background(Hex(TextColor));
    }

    private string Isolate(string text) => PdfText.Isolate(text, _wording.IsRightToLeft);

    private string SemesterLine() => paper.Header.Semester.Length > 0
        ? $"{paper.Header.Semester} – {paper.Header.AcademicYear}"
        : paper.Header.AcademicYear;

    private static float Points(int twips) => twips / 20f;

    private static Color Hex(string rgb) => Color.FromHex("#" + rgb);
}
