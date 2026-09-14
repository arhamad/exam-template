using ExamTemplate.Documents.Model;
using ExamTemplate.Services;
using Microsoft.AspNetCore.Components;

namespace ExamTemplate.Components.Shared;

/// <summary>
/// Base for the exam editing pages: restores the draft, then focuses the element named by the URL fragment
/// (deep links such as <c>exam/questions#q-…</c>) or by <see cref="FocusAfterRender"/>.
/// </summary>
public abstract class ExamPageBase : ComponentBase
{
    private const int MaxElementIdLength = 100;

    private string? pendingFocusId;
    private bool pendingFocusAlignsToTop;

    [Inject] protected ExamDraftState Draft { get; set; } = default!;
    [Inject] protected BrowserInterop Browser { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    protected bool IsLoading { get; private set; } = true;

    /// <summary>The open exam, or <c>null</c> while loading or when none is open.</summary>
    protected ExamDefinition? Exam => Draft.Exam;

    protected override async Task OnInitializedAsync()
    {
        if (FragmentId(Navigation.Uri) is { } target)
        {
            pendingFocusId = target;
            pendingFocusAlignsToTop = true;
        }

        await Draft.EnsureLoadedAsync();
        IsLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsLoading || pendingFocusId is not { } id)
            return;

        pendingFocusId = null;
        if (Exam is not null)
            await Browser.FocusElementAsync(id, pendingFocusAlignsToTop);
    }

    /// <summary>Moves the focus to an element once the current changes are on screen.</summary>
    protected void FocusAfterRender(string? elementId)
    {
        if (string.IsNullOrEmpty(elementId))
            return;

        pendingFocusId = elementId;
        pendingFocusAlignsToTop = false;
    }

    private static string? FragmentId(string uri)
    {
        var hash = uri.IndexOf('#');
        if (hash < 0)
            return null;

        var id = Uri.UnescapeDataString(uri[(hash + 1)..]);
        return id.Length is > 0 and <= MaxElementIdLength ? id : null;
    }
}
