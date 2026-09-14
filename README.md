# ExamTemplate — Arabic exam generator

A Blazor Server app (Arabic, right-to-left) for writing university exams and printing them. Enter the exam details and
questions (multiple choice, true/false, essay), then generate up to four shuffled versions (أ، ب، ج، د). Each version
has a student copy and an answer key, as Word (`.docx`) and PDF.

There is no database and no external service: drafts autosave in the browser, and exams are kept and shared as JSON
files.

## Run and test

Requires the .NET 10 SDK.

```bash
dotnet run --project ExamTemplate --launch-profile http   # http://localhost:5296
dotnet test ExamTemplate.sln
```

The app has three steps: **معلومات الامتحان** (header and instructions) → **الأسئلة** (sections and questions) →
**التوليد والتصدير** (issues, number of versions, downloads and preview).

- The draft is saved automatically in this browser's `localStorage` and nowhere else. To keep a copy or continue on
  another computer, use «تصدير ملف الامتحان (JSON)», then «استيراد ملف امتحان (JSON)» on the home page (up to 5 MB).
- «تحميل نموذج تجريبي» loads the demo exam (`DemoExams.ComputerLogicDesign`).
- Errors block generation and link to the field or question to fix. Warnings, such as an essay without a model
  answer, don't.

## Projects

| Path | Contents |
|---|---|
| `ExamTemplate/` | Blazor Web App: Interactive Server, prerender off, Bootstrap RTL. Pages in `Components/Pages`, editors in `Components/Exam`, draft autosave and browser interop in `Services/` |
| `ExamTemplate.Documents/` | Everything that doesn't need the UI: `Model` (exam and JSON), `Validation`, `Versioning`, `Papers` (the printable model), `Design` (shared layout tokens), `Word`, `Pdf`, `Services` (`IExamDocumentService`, `AddExamDocuments()`), `Demo` |
| `ExamTemplate.Tests/` | xUnit tests for the library, the template structure and both renderers |
| `tools/ExamTemplate.TemplateBuilder/` | Generates `ExamTemplate.Documents/Word/ExamTemplate.docx` |
| `tools/ExamTemplate.Samples/` | Renders the demo exams to files for visual checks |
| `tools/visual/` | macOS helpers: Word → PDF (AppleScript) and PDF → PNG (Swift, PDFKit) |

## From exam to documents

1. **Validate.** `ExamValidator` reports errors and warnings together with the section, question or option they
   belong to.
2. **Build papers.** `ExamPaperBuilder` creates one `ExamPaper` per version: numbering, marks text (علامتان، 5 علامات),
   option labels and the answer summary.
   - Version أ keeps the authored order. The other versions shuffle questions *within* their section, and shuffle
     multiple-choice options unless «الحفاظ على ترتيب الخيارات عند الخلط» is on.
   - Shuffling uses SplitMix64, seeded from `shuffleSeed`, the version and the section or question id. The same exam
     file always produces the same versions, and editing one question doesn't reshuffle the other sections.
     «إعادة خلط» picks a new seed.
3. **Word.** `WordTemplateData` flattens the paper for [Templify](https://github.com/TriasDev/templify), which fills
   the template. `WordPostProcessor` then restores the characters shielded from Templify's markdown (`* _ ~ { }`) and
   splits left-to-right text into runs of its own.
4. **PDF.** `ExamPdfLayout` draws the same design with QuestPDF, from the same `DesignTokens`.

`ExamDocumentService` wraps these steps for the UI. Its ZIP bundle holds every version × student copy and answer key ×
docx and PDF, plus the exam JSON, so the same versions can be generated again later.

## Exam file format

An abridged exam file (schema version 1):

```json
{
  "schemaVersion": 1,
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

- `type` is `multipleChoice`, `trueFalse` (answer in `trueFalseAnswer`) or `essay` (`answerLines` 0–20 and
  `modelAnswer`).
- `ExamLimits` holds the limits, such as text lengths, 2–8 options, 200 questions and 1–4 versions. The validator and
  the UI share them.
- Files written by a newer version of the app are rejected with an Arabic message instead of being half-read.

## Word template

`ExamTemplate.docx` is generated, so don't edit it in Word. Change the builder, regenerate the template, and commit both:

```bash
dotnet run --project tools/ExamTemplate.TemplateBuilder
```

The builder refuses to write a package that fails Open XML validation. `TemplateStructureTests` fail when the committed
template differs from the builder's output or breaks one of the rules below.

### RTL rules

These rules make Arabic line up correctly in Word:

- **Alignment.** In a `w:bidi` paragraph, `w:jc` `left` and `right` mean start and end. Right-aligned Arabic therefore
  has *no* `w:jc`, and centred text uses `center`. `jc="right"` (visually left) appears only in the footer's last cell.
- **Complex-script formatting.** Runs marked `w:rtl` take size, bold and font from the complex-script properties.
  Every style sets `sz` and `szCs`, `b` and `bCs`, and all four `rFonts` slots, and never uses `w:hint`.
- **Tables.** Every table is full text width (10488 twips), fixed layout and `bidiVisual`, with grid columns that add
  up to that width. A spacer paragraph follows each table so neighbouring tables never merge.
- **Keeping questions together.** A question's rows are chained with `keepNext` and don't split across pages
  (`cantSplit`), and a section bar stays with its first question. Each question ends with a spacer that breaks the
  chain, so long exams still flow.
- **Mixed-direction text.** Inside a `w:rtl` run, neutral characters are laid out right to left, so `2026/2027` would
  print as `2027/2026`. `BidiText` finds left-to-right islands (numbers, dates, Latin words, formulas), and
  `WordPostProcessor` writes each one as a run without `w:rtl`. The PDF wraps the same islands in Unicode isolates
  (U+2066 … U+2069).
- **Settings.** The template uses `compatibilityMode` 15 and never triggers the "update fields" prompt. Noto Naskh
  Arabic Regular is embedded, obfuscated as ECMA-376 §17.8.1 requires.

Page size, margins, type sizes, colours and column widths live in `ExamTemplate.Documents/Design/DesignTokens.cs`.
Both the template builder and the PDF layout read them, so after changing a token, regenerate the template and run
the tests.

## Visual check

Tests prove the output is valid, not that it looks right. After a design change, render the samples and look at them:

```bash
dotnet run --project tools/ExamTemplate.Samples -- artifacts/samples   # docx and PDF: every version, student copy and key
swift tools/visual/pdf-to-png.swift artifacts/samples/logic-1290-A-exam.pdf artifacts/png 144
osascript tools/visual/word-to-pdf.applescript "$PWD/in.docx" "$PWD/out.pdf"   # Word's own rendering (macOS)
```

Look for:

- Right alignment, with sizes and bold applied.
- Mixed Arabic and Latin lines in reading order.
- No question split across pages, and no section bar left alone at the bottom of a page.
- Answer bubbles and ruled answer lines.
- The answer-key banner and answer summary.
- «صفحة X من Y» in the footer.

Word for Mac is sandboxed: it asks before opening files outside `~/Library/Containers/com.microsoft.Word/Data`, and
macOS may ask the terminal for permission to write inside that folder.

Word and QuestPDF paginate independently, so page breaks can differ slightly between the two formats.

## Fonts and licences

- **Noto Naskh Arabic** (Regular and Bold) is under the SIL Open Font License 1.1
  (`ExamTemplate.Documents/Assets/Fonts/NotoNaskhArabic-OFL.txt`). Both weights are embedded in PDFs; Word files
  embed the Regular weight.
- **DejaVu Sans** is the PDF fallback for symbols the Arabic font lacks (≤ ≥ ± ² → ● ○), under the Bitstream Vera and
  DejaVu licence (`DejaVu-LICENSE.txt`).
- **Install Noto Naskh Arabic on computers that open or print the Word files.** Word uses only one embedded font per
  family name, so without the installed font it synthesizes bold from the Regular weight.
- **QuestPDF** runs under its Community licence (see `QuestPdfSetup`). Check that your institution qualifies at
  <https://www.questpdf.com/license/>, or configure a commercial licence there.
- **The logo** (`Assets/Images/university-logo.png`) is only 120 × 117 px. For sharper printing, replace it with a
  higher-resolution file of the same name. The web UI uses its own copy, `wwwroot/images/qou-logo.png`.

## Privacy

Drafts are stored unencrypted in the browser. On a shared computer, use «حذف المسودة» on the home page when you
finish. If the same draft is open in two tabs, the last save wins.
