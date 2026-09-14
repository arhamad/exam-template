using ExamTemplate.Documents.Model;
using ExamTemplate.Documents.Validation;
using Microsoft.JSInterop;

namespace ExamTemplate.Services;

public enum DraftSaveStatus
{
    /// <summary>No exam is open.</summary>
    None,

    /// <summary>Edited; the autosave is waiting for typing to settle.</summary>
    Pending,

    Saving,
    Saved,
    Failed,
}

/// <summary>
/// The exam being edited in this circuit, autosaved to the browser's localStorage one second after the last change.
/// Every edit must be followed by <see cref="NotifyChanged"/>. Used from the renderer's dispatcher only.
/// </summary>
public sealed class ExamDraftState(BrowserInterop browser, ILogger<ExamDraftState> logger) : IDisposable
{
    public static readonly TimeSpan AutosaveDelay = TimeSpan.FromSeconds(1);

    private Task? loading;
    private CancellationTokenSource? pendingSave;
    private DraftValidation? validation;
    private ExamDefinition? validatedExam;
    private int validatedRevision;

    public ExamDefinition? Exam { get; private set; }

    /// <summary>Incremented by every change, so late autosave results can be recognised and ignored.</summary>
    public int Revision { get; private set; }

    public DraftSaveStatus SaveStatus { get; private set; }

    /// <summary>Show every validation issue in the editors, not only those of fields the user has touched.</summary>
    public bool ShowAllIssues { get; set; }

    /// <summary>Arabic explanation when the stored draft could not be restored.</summary>
    public string? RestoreError { get; private set; }

    public bool HasUnsavedChanges => SaveStatus is DraftSaveStatus.Pending or DraftSaveStatus.Saving or DraftSaveStatus.Failed;

    /// <summary>Raised after any change to the exam, the save status or the restore state.</summary>
    public event Action? Changed;

    /// <summary>The current exam's validation, computed at most once per revision.</summary>
    public DraftValidation Validation
    {
        get
        {
            if (Exam is null)
                return DraftValidation.Empty;

            if (validation is null || validatedExam != Exam || validatedRevision != Revision)
            {
                validation = new DraftValidation(ExamValidator.Validate(Exam));
                validatedExam = Exam;
                validatedRevision = Revision;
            }

            return validation;
        }
    }

    /// <summary>Restores the draft stored in the browser, once per circuit.</summary>
    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    /// <summary>Opens another exam (new, imported or sample) in place of the current one.</summary>
    public void Replace(ExamDefinition exam)
    {
        ArgumentNullException.ThrowIfNull(exam);
        Exam = exam;
        ShowAllIssues = false;
        RestoreError = null;
        NotifyChanged();
    }

    /// <summary>Records an edit of <see cref="Exam"/> and schedules the autosave.</summary>
    public void NotifyChanged()
    {
        if (Exam is null)
            return;

        Revision++;
        SaveStatus = DraftSaveStatus.Pending;

        CancelPendingSave();
        pendingSave = new CancellationTokenSource();
        _ = SaveAfterDelayAsync(Revision, pendingSave.Token);

        Changed?.Invoke();
    }

    /// <summary>Saves right away, e.g. to retry after a failure.</summary>
    public Task SaveNowAsync()
    {
        CancelPendingSave();
        return SaveAsync(Revision);
    }

    /// <summary>Closes the exam and deletes the draft stored in this browser.</summary>
    /// <returns><c>false</c> when the browser could not delete the stored draft.</returns>
    public async Task<bool> DiscardAsync()
    {
        CancelPendingSave();
        Exam = null;
        Revision++;
        SaveStatus = DraftSaveStatus.None;
        ShowAllIssues = false;
        RestoreError = null;
        Changed?.Invoke();

        try
        {
            return await browser.ClearDraftAsync();
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (JSException exception)
        {
            logger.LogWarning(exception, "Deleting the draft stored in the browser failed.");
            return false;
        }
    }

    public void Dispose() => CancelPendingSave();

    private async Task LoadAsync()
    {
        try
        {
            var exam = await browser.LoadDraftAsync();

            // An exam opened while the draft was loading wins over the stored one.
            if (exam is not null && Exam is null && Revision == 0)
            {
                Exam = exam;
                SaveStatus = DraftSaveStatus.Saved;
            }
        }
        catch (ExamJsonException exception)
        {
            RestoreError = $"تعذّرت استعادة المسودة المحفوظة في هذا المتصفح: {exception.Message}";
            logger.LogWarning(exception, "The draft stored in the browser could not be read.");
        }
        catch (JSDisconnectedException)
        {
            // The browser is gone; nothing to show.
        }
        catch (Exception exception)
        {
            RestoreError = "تعذّرت قراءة المسودة المحفوظة في هذا المتصفح. أعد تحميل الصفحة للمحاولة مرة أخرى.";
            logger.LogError(exception, "Loading the draft from the browser failed.");
        }

        Changed?.Invoke();
    }

    private async Task SaveAfterDelayAsync(int revision, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutosaveDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await SaveAsync(revision);
    }

    private async Task SaveAsync(int revision)
    {
        if (Exam is not { } exam || revision != Revision)
            return;

        SaveStatus = DraftSaveStatus.Saving;
        Changed?.Invoke();

        bool saved;
        try
        {
            saved = await browser.SaveDraftAsync(ExamJson.Serialize(exam), exam);
        }
        catch (JSDisconnectedException)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Saving the draft to the browser failed.");
            saved = false;
        }

        // A newer edit arrived meanwhile; its own autosave reports the status.
        if (revision != Revision)
            return;

        SaveStatus = saved ? DraftSaveStatus.Saved : DraftSaveStatus.Failed;
        Changed?.Invoke();
    }

    private void CancelPendingSave()
    {
        pendingSave?.Cancel();
        pendingSave?.Dispose();
        pendingSave = null;
    }
}
