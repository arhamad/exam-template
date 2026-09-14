using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Services;
using Microsoft.JSInterop;

namespace ExamTemplate.Services;

/// <summary>Summary of the draft stored in this browser, for the home page.</summary>
public sealed class DraftInfo
{
    public string CourseName { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public int QuestionCount { get; set; }

    /// <summary>Formatted by the browser, in its own time zone.</summary>
    public string SavedAtText { get; set; } = "";
}

/// <summary>Typed calls into <c>wwwroot/js/exam-interop.js</c>. Scoped: one module import per circuit.</summary>
public sealed class BrowserInterop(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>Largest stored draft that is read back (the JSON as UTF-8).</summary>
    public const long MaxDraftBytes = 5 * 1024 * 1024;

    // The draft summary is sent back over SignalR, so its free text stays short.
    private const int MaxInfoTextLength = 200;

    private Task<IJSObjectReference>? module;

    public async Task<bool> SaveDraftAsync(string json, ExamDefinition exam)
    {
        var info = new
        {
            courseName = Shorten(exam.Header.CourseName),
            courseCode = Shorten(exam.Header.CourseCode),
            questionCount = exam.QuestionCount(),
        };

        return await (await GetModuleAsync()).InvokeAsync<bool>("saveDraft", json, info);
    }

    public async Task<DraftInfo?> GetDraftInfoAsync() =>
        await (await GetModuleAsync()).InvokeAsync<DraftInfo?>("getDraftInfo");

    /// <summary>Reads the stored draft as a stream, so its size isn't limited by the SignalR message size.</summary>
    /// <returns>The draft, or <c>null</c> when this browser has none.</returns>
    /// <exception cref="ExamJsonException">The stored draft is too large or not a readable exam.</exception>
    public async Task<ExamDefinition?> LoadDraftAsync()
    {
        await using var data = await (await GetModuleAsync()).InvokeAsync<IJSStreamReference>("loadDraft");
        if (data.Length == 0)
            return null;

        if (data.Length > MaxDraftBytes)
            throw new ExamJsonException("المسودة المحفوظة أكبر من الحجم المسموح به.");

        await using var stream = await data.OpenReadStreamAsync(MaxDraftBytes);
        using var buffer = new MemoryStream((int)data.Length);
        await stream.CopyToAsync(buffer);
        buffer.Position = 0;
        return ExamJson.Deserialize(buffer);
    }

    public async Task<bool> ClearDraftAsync() =>
        await (await GetModuleAsync()).InvokeAsync<bool>("clearDraft");

    /// <summary>Streams the file to the browser and saves it with its own name and content type.</summary>
    public async Task DownloadAsync(RenderedFile file)
    {
        var jsModule = await GetModuleAsync();
        using var content = new MemoryStream(file.Content, writable: false);
        using var streamReference = new DotNetStreamReference(content);
        await jsModule.InvokeVoidAsync("downloadFromStream", file.FileName, file.ContentType, streamReference);
    }

    /// <summary>A <c>blob:</c> URL for the file; release it with <see cref="RevokeObjectUrlAsync"/>.</summary>
    public async Task<string> CreateObjectUrlAsync(RenderedFile file)
    {
        var jsModule = await GetModuleAsync();
        using var content = new MemoryStream(file.Content, writable: false);
        using var streamReference = new DotNetStreamReference(content);
        return await jsModule.InvokeAsync<string>("createObjectUrl", streamReference, file.ContentType);
    }

    /// <summary>Best effort: a disconnected browser has already released its URLs.</summary>
    public async Task RevokeObjectUrlAsync(string url)
    {
        try
        {
            await (await GetModuleAsync()).InvokeVoidAsync("revokeObjectUrl", url);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Focuses the element with this id. Best effort.</summary>
    /// <param name="alignToTop">Scroll the element to the top of the page (deep links) instead of only into view.</param>
    public async Task<bool> FocusElementAsync(string id, bool alignToTop = false)
    {
        try
        {
            return await (await GetModuleAsync()).InvokeAsync<bool>("focusElement", id, alignToTop);
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
    }

    /// <summary>The browser's own confirmation dialog.</summary>
    public async Task<bool> ConfirmAsync(string message) => await js.InvokeAsync<bool>("confirm", message);

    public async ValueTask DisposeAsync()
    {
        if (module is { IsCompletedSuccessfully: true })
        {
            try
            {
                await module.Result.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        var import = module ??= js.InvokeAsync<IJSObjectReference>("import", "./js/exam-interop.js").AsTask();
        try
        {
            return await import;
        }
        catch
        {
            // Let the next call try again instead of caching the failure for the whole circuit.
            if (module == import)
                module = null;
            throw;
        }
    }

    private static string Shorten(string text) => text.Length <= MaxInfoTextLength ? text : text[..MaxInfoTextLength];
}
