using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamTemplate.Documents.Model;

/// <summary>Reads and writes exam files (JSON).</summary>
public static class ExamJson
{
    private static readonly LocalizedText NotAnExamFile = new("الملف ليس ملف امتحان صالحًا.", "The file isn't a valid exam file.");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        // Keeps Arabic readable in the file. The output is a download, never embedded in HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // Deserializing from a JsonDocument re-reads its raw text, so these must match DocumentOptions.
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public static string Serialize(ExamDefinition exam) => JsonSerializer.Serialize(exam, SerializerOptions);

    /// <exception cref="ExamJsonException">The text is not a readable exam file.</exception>
    public static ExamDefinition Deserialize(string json) => Read(() => JsonDocument.Parse(json, DocumentOptions));

    /// <exception cref="ExamJsonException">The stream is not a readable exam file.</exception>
    public static ExamDefinition Deserialize(Stream utf8Json) => Read(() => JsonDocument.Parse(utf8Json, DocumentOptions));

    private static ExamDefinition Read(Func<JsonDocument> parse)
    {
        try
        {
            using var document = parse();
            CheckSchemaVersion(document.RootElement);

            var exam = document.Deserialize<ExamDefinition>(SerializerOptions) ?? throw new ExamJsonException(NotAnExamFile);
            CheckStructure(exam);
            EnsureUniqueIds(exam);
            return exam;
        }
        catch (JsonException exception)
        {
            var line = exception.LineNumber + 1;
            throw new ExamJsonException(new LocalizedText(
                $"تعذّرت قراءة الملف: صيغة JSON غير صالحة{(line is null ? "" : $" (السطر {line})")}.",
                $"The file couldn't be read: the JSON isn't valid{(line is null ? "" : $" (line {line})")}."), exception);
        }
    }

    private static void CheckSchemaVersion(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("schemaVersion", out var version)
            || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out var schemaVersion)
            || schemaVersion < 1)
        {
            throw new ExamJsonException(NotAnExamFile);
        }

        if (schemaVersion > ExamDefinition.CurrentSchemaVersion)
            throw new ExamJsonException(new LocalizedText(
                "أُنشئ هذا الملف بإصدار أحدث من التطبيق ولا يمكن فتحه.",
                "This file was created by a newer version of the app and can't be opened."));
    }

    private static void CheckStructure(ExamDefinition exam)
    {
        // Nullable annotations aren't enforced for list elements, so hand-edited files can still contain nulls.
        // The enum converter also accepts numbers, so a language like 9 gets this far.
        if (!Enum.IsDefined(exam.Language)
            || exam.Instructions.Any(instruction => instruction is null)
            || exam.Sections.Any(section => section is null
                || section.Questions.Any(question => question is null || question.Options.Any(option => option is null))))
        {
            throw new ExamJsonException(NotAnExamFile);
        }

        var exceeded =
            exam.Sections.Count > ExamLimits.MaxSections
                ? new LocalizedText($"عدد الأقسام أكثر من {ExamLimits.MaxSections}", $"more than {ExamLimits.MaxSections} sections") :
            exam.QuestionCount() > ExamLimits.MaxQuestions
                ? new LocalizedText($"عدد الأسئلة أكثر من {ExamLimits.MaxQuestions}", $"more than {ExamLimits.MaxQuestions} questions") :
            exam.Instructions.Count > ExamLimits.MaxInstructions
                ? new LocalizedText($"عدد التعليمات أكثر من {ExamLimits.MaxInstructions}", $"more than {ExamLimits.MaxInstructions} instructions") :
            exam.Sections.Any(section => section.Questions.Any(question => question.Options.Count > ExamLimits.MaxOptions))
                ? new LocalizedText($"يوجد سؤال بأكثر من {ExamLimits.MaxOptions} خيارات", $"a question with more than {ExamLimits.MaxOptions} options")
                : null;

        if (exceeded is not null)
        {
            throw new ExamJsonException(new LocalizedText(
                $"يتجاوز الملف الحدود المسموح بها: {exceeded.Arabic}.",
                $"The file exceeds the allowed limits: {exceeded.English}."));
        }
    }

    private static void EnsureUniqueIds(ExamDefinition exam)
    {
        var seen = new HashSet<Guid>();

        foreach (var section in exam.Sections)
        {
            section.Id = Unique(section.Id);
            foreach (var question in section.Questions)
            {
                question.Id = Unique(question.Id);
                foreach (var option in question.Options)
                    option.Id = Unique(option.Id);
            }
        }

        Guid Unique(Guid id)
        {
            while (id == Guid.Empty || !seen.Add(id))
                id = Guid.NewGuid();
            return id;
        }
    }
}
