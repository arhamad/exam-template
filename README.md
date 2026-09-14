# ExamTemplate — exam generator

A Blazor Server app, with an Arabic or English interface, for writing university exams in Arabic or English and printing them. Enter the
exam details and questions (multiple choice, true/false, essay), then generate up to four shuffled versions. Each version has a
student copy and an answer key, as Word (`.docx`) and PDF. Arabic papers read right to left; English papers use the same
design mirrored left to right.

There is no database and no external service: drafts autosave in the browser, and exams are kept and shared as JSON
files.

## Run and test

Requires the .NET 10 SDK.

```bash
dotnet run --project ExamTemplate --launch-profile http   # http://localhost:5296
dotnet test ExamTemplate.sln
```

The app has three steps: **معلومات الامتحان** (language, header and instructions) → **الأسئلة** (sections and questions) →
**التوليد والتصدير** (issues, number of versions, downloads and preview).

- The draft is saved automatically in this browser's `localStorage` and nowhere else. To keep a copy or continue on
  another computer, use «تصدير ملف الامتحان (JSON)», then «استيراد ملف امتحان (JSON)» on the home page (up to 5 MB).
- «تحميل نموذج تجريبي» and «نموذج تجريبي بالإنجليزية» load the demo exam in Arabic or English
  (`DemoExams.ComputerLogicDesign`, `DemoExams.ComputerLogicDesignEnglish`).
- Errors block generation and link to the field or question to fix. Warnings, such as an essay without a model
  answer, don't.

## Projects

| Path | Contents |
|---|---|
| `ExamTemplate/` | Blazor Web App: Interactive Server, prerender off, Bootstrap's right-to-left or left-to-right build. Pages in `Components/Pages`, editors in `Components/Exam`, draft autosave, browser interop and the interface language in `Services/` |
| `ExamTemplate.Documents/` | Everything that doesn't need the UI: `Model` (exam, defaults, JSON and user messages), `Validation`, `Versioning`, `Papers` (the printable model and its wording), `Design` (shared layout tokens), `Word`, `Pdf`, `Services` (`IExamDocumentService`, `AddExamDocuments()`), `Demo` |
| `ExamTemplate.Tests/` | xUnit tests for the library, the template structure, both renderers and the web app's interface language |
| `tools/ExamTemplate.TemplateBuilder/` | Generates the Word templates in `ExamTemplate.Documents/Word/` |
| `tools/ExamTemplate.Samples/` | Renders the demo exams to files for visual checks |
| `tools/visual/` | macOS helpers: Word → PDF (AppleScript) and PDF → PNG (Swift, PDFKit) |

## From exam to documents

1. **Validate.** `ExamValidator` reports errors and warnings together with the section, question or option they
   belong to.
2. **Build papers.** `ExamPaperBuilder` creates one `ExamPaper` per version: numbering, marks text, option labels and the
   answer summary, all worded by `PaperWording` in the exam's language («علامتان» or "2 marks", أ ب ج د or A B C D).
   - The first version keeps the authored order. The other versions shuffle questions *within* their section, and shuffle
     multiple-choice options unless «الحفاظ على ترتيب الخيارات عند الخلط» is on.
   - Shuffling uses SplitMix64, seeded from `shuffleSeed`, the version and the section or question id. The same exam
     file always produces the same versions, and editing one question doesn't reshuffle the other sections.
     «إعادة خلط» picks a new seed.
3. **Word.** `WordTemplateData` flattens the paper for [Templify](https://github.com/TriasDev/templify), which fills
   the template of the paper's language. `WordPostProcessor` then restores the characters shielded from Templify's
   markdown (`* _ ~ { }`) and gives text of the other direction runs of its own.
4. **PDF.** `ExamPdfLayout` draws the same design with QuestPDF, from the same `DesignTokens` and wording.

`ExamDocumentService` wraps these steps for the UI. Its ZIP bundle holds every version × student copy and answer key ×
docx and PDF, plus the exam JSON, so the same versions can be generated again later.

## Languages

The interface and the exam each have their own language. An Arabic interface can edit an English exam, and the other way
round.

### Interface language

The button at the end of the top bar («English» or «العربية») switches the whole interface: pages, menus, dialogs and
validation messages. Arabic is laid out right to left and English left to right, with the same design.

- **Where it's kept.** The `examTemplate.uiLanguage` cookie (`ar` or `en`) remembers the choice in this browser. Without it
  the interface is Arabic. `App.razor` reads the cookie on every page load and sets `<html lang dir>`, Bootstrap's
  right-to-left or left-to-right build, and the language of the circuit (passed to `Routes`).
- **Switching.** The button saves the draft right away, writes the cookie and reloads the page. The editor opens where it
  was, but issues shown for touched fields are hidden again until the fields are edited or the generate page is visited.
  The static error page doesn't show the button.
- **Wording.** Components get `UiText` as `Ui` and write every text in both languages where it is used:
  `@Ui.Text("الأسئلة", "Questions")`. Counts and lists go through `Ui.Questions`, `Ui.Marks`, `Ui.Errors` and `Ui.Join`.
  The library doesn't know the interface language: validation issues and the messages of `ExamUserException`
  (`ExamJsonException`, `ExamRenderException`, `ExamValidationException`) carry a `LocalizedText` with both, and
  `Exception.Message` holds the English text for logs.
- **Mixed direction.** Exam text shown inside interface text (course names, section titles, version and option letters)
  is isolated with `dir="auto"`, `<bdi>` or, inside `<option>`, `ExamContent.Isolate`, so it keeps its own direction.

### Exam language

Each exam is Arabic or English (`language` in the exam file). The choice is at the top of «معلومات الامتحان».

- **Papers.** Arabic papers are laid out right to left. English papers use the same blocks, sizes and spacing mirrored left
  to right: question numbers and option letters at the left, marks at the right. Every fixed word on a paper (labels,
  «صفحة X من Y» / "Page X of Y", marks and question counts, true/false, version and option letters) comes from
  `PaperWording`.
- **Editor.** Whatever the interface language, fields that hold exam content are typed right to left for Arabic exams and
  left to right for English ones. Option and version letters, placeholders and exam modes follow the exam language too, so
  the editor matches the paper.
- **Switching.** `ExamEditing.ChangeLanguage` replaces header values and the instruction list that still hold the old
  language's defaults (`ExamDefaults`), and maps the exam mode (نظري ↔ Theoretical, عملي ↔ Practical). Anything the
  author wrote stays.
- **Mixed text.** `BidiText` splits text into islands of the other direction. In Arabic text these are numbers, dates, Latin
  words and formulas. In English text they are Arabic words and phrases: an island runs from an Arabic letter to the last
  Arabic letter or digit before the next Latin letter, so a phrase such as «دائرة تتابعية – مثل المسجل» never breaks apart.
- **Widths.** English labels and marks are wider, so the student row and the marks column have their own grids
  (`DesignTokens.StudentRowGrid`, `QuestionGrid`, `TrueFalseQuestionGrid`).

## Exam file format

An abridged exam file (schema version 1):

```json
{
  "schemaVersion": 1,
  "language": "arabic",
  "header": {
    "universityName": "جامعة القدس المفتوحة", "examTitle": "الامتحان النهائي", "semester": "الفصل الأول",
    "academicYear": "2026/2027", "courseName": "تصميم منطق الحاسوب", "courseCode": "1290",
    "duration": "ساعة ونصف", "examMode": "نظري", "examDate": null
  },
  "instructions": ["…"],
  "sections": [{
    "id": "…", "title": "القسم الأول: أسئلة موضوعية", "instruction": "…",
    "questions": [{
      "id": "…", "type": "multipleChoice", "text": "أي من العناصر الآتية يُعد دائرة منطقية توافقية؟", "marks": 10,
      "note": "اختر/ي إجابة واحدة فقط.", "keepOptionOrder": false,
      "options": [{ "id": "…", "text": "المسجل", "isCorrect": false }, "…"]
    }]
  }],
  "versionCount": 2,
  "shuffleSeed": 12345
}
```

- `language` is `arabic` or `english`. Files without it, including those written before languages existed, open as Arabic.
- `type` is `multipleChoice`, `trueFalse` (answer in `trueFalseAnswer`) or `essay` (`answerLines` 0–20 and
  `modelAnswer`).
- `ExamLimits` holds the limits, such as text lengths, 2–8 options, 200 questions and 1–4 versions. The validator and
  the UI share them.
- Files written by a newer version of the app are rejected with a message instead of being half-read.

## Word templates

`ExamTemplate-ar.docx` and `ExamTemplate-en.docx` are generated, so don't edit them in Word. Change the builder, regenerate
both templates, and commit them:

```bash
dotnet run --project tools/ExamTemplate.TemplateBuilder   # [outputDirectory], default ExamTemplate.Documents/Word
```

The file names use a hyphen: MSBuild would read `.ar` or `.en` before `.docx` as a culture and move the file into a satellite
assembly. The builder refuses to write a package that fails Open XML validation. `TemplateStructureTests` check both
templates, and fail when a committed template differs from the builder's output or breaks one of the rules below.

### Direction rules

These rules make both languages line up correctly in Word:

- **Direction.** The Arabic template marks paragraphs `w:bidi`, runs `w:rtl`, tables `bidiVisual` and the section `w:bidi`.
  The English template has none of them, and contains no Arabic characters.
- **Alignment.** In a `w:bidi` paragraph, `w:jc` `left` and `right` mean start and end, so in both templates start-aligned
  text has *no* `w:jc`, centred text uses `center`, and `right` is the end edge. It appears only in the footer's last cell.
- **Complex-script formatting.** Word takes size, bold and font from the complex-script properties for right-to-left runs
  and for Arabic characters anywhere. Every style sets `sz` and `szCs`, `b` and `bCs`, and all four `rFonts` slots: the
  text font (Noto Naskh Arabic or Noto Serif) for `ascii`, `hAnsi` and `eastAsia`, Noto Naskh Arabic for `cs`. No
  `w:hint` is used.
- **Tables.** Every table is full text width (10488 twips), fixed layout, with grid columns that add up to that width.
  Grids list columns from the start side, so the same grid mirrors between the two templates. A spacer paragraph follows
  each table so neighbouring tables never merge.
- **Keeping questions together.** A question's rows are chained with `keepNext` and don't split across pages
  (`cantSplit`), and a section bar stays with its first question. Each question ends with a spacer that breaks the
  chain, so long exams still flow.
- **Mixed-direction text.** Word lays out neutral characters in the direction of their run, so `2026/2027` in a `w:rtl` run
  would print as `2027/2026`, and an Arabic phrase in a left-to-right run would lose its word order. `WordPostProcessor`
  writes each `BidiText` island as a run of its own: without `w:rtl` in Arabic papers, with `w:rtl` in English ones. The
  PDF wraps the same islands in Unicode isolates (LRI or RLI … PDI).
- **Settings and fonts.** The templates use `compatibilityMode` 15 and never trigger the "update fields" prompt. The text
  font's Regular weight is embedded, obfuscated as ECMA-376 §17.8.1 requires. The English template lists Noto Naskh Arabic
  for Arabic words without embedding it.

Page size, margins, type sizes, colours and column widths live in `ExamTemplate.Documents/Design/DesignTokens.cs`.
Both the template builder and the PDF layout read them, so after changing a token, regenerate the templates and run
the tests.

## Visual check

Tests prove the output is valid, not that it looks right. After a design change, render the samples and look at them:

```bash
dotnet run --project tools/ExamTemplate.Samples -- artifacts/samples   # logic, logic-en and edge: every version, copy and key
swift tools/visual/pdf-to-png.swift artifacts/samples/logic-en-1290-A-exam.pdf artifacts/png 144
osascript tools/visual/word-to-pdf.applescript "$PWD/in.docx" "$PWD/out.pdf"   # Word's own rendering (macOS)
```

Look for:

- Start alignment (right in Arabic, left in English), with sizes and bold applied.
- Mixed Arabic and Latin text in reading order.
- Labels and marks on one line in their columns.
- No question split across pages, and no section bar left alone at the bottom of a page.
- Answer bubbles and ruled answer lines.
- The answer-key banner and answer summary.
- «صفحة X من Y» or "Page X of Y" in the footer.

Word for Mac is sandboxed: it asks before opening files outside `~/Library/Containers/com.microsoft.Word/Data`, and
macOS may ask the terminal for permission to write inside that folder.

Word and QuestPDF paginate independently, so page breaks can differ slightly between the two formats. Lines holding Arabic
words take the taller Arabic line height.

## Fonts and licences

- **Noto Naskh Arabic** (Regular and Bold) and **Noto Serif** v2.015 (Regular and Bold, unhinted) are under the SIL Open Font
  License 1.1 (`ExamTemplate.Documents/Assets/Fonts/NotoNaskhArabic-OFL.txt`, `NotoSerif-OFL.txt`). Both weights of both
  fonts are embedded in PDFs; Word files embed the Regular weight of their text font.
- **DejaVu Sans** is the PDF fallback for symbols the text fonts lack (≤ ≥ ± ² → ● ○), under the Bitstream Vera and
  DejaVu licence (`DejaVu-LICENSE.txt`).
- **Install Noto Naskh Arabic, and Noto Serif for English exams, on computers that open or print the Word files.** Word uses
  only one embedded font per family name, so without the installed font it synthesizes bold from the Regular weight, and
  Arabic words in English Word files fall back to another Arabic font.
- **QuestPDF** runs under its Community licence (see `QuestPdfSetup`). Check that your institution qualifies at
  <https://www.questpdf.com/license/>, or configure a commercial licence there.
- **The logo** (`Assets/Images/university-logo.png`) is only 120 × 117 px. For sharper printing, replace it with a
  higher-resolution file of the same name. The web UI uses its own copy, `wwwroot/images/qou-logo.png`.

## Privacy

Drafts are stored unencrypted in the browser. On a shared computer, use «حذف المسودة» on the home page when you
finish. If the same draft is open in two tabs, the last save wins.

The only cookie the app sets is `examTemplate.uiLanguage`, which holds `ar` or `en`.
