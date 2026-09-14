using ExamTemplate.Services;
using Microsoft.AspNetCore.Components;

namespace ExamTemplate.Components.Shared;

/// <summary>
/// Base for small components that show live draft information (totals, save status, navigation).
/// Editors don't derive from it: they re-render from their own events, so typing never re-renders the page.
/// </summary>
public abstract class DraftSubscriber : ComponentBase, IDisposable
{
    private bool subscribed;

    [Inject] protected ExamDraftState Draft { get; set; } = default!;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Subscribed here rather than in OnInitialized, so derived components can't skip it.
        if (!subscribed)
        {
            Draft.Changed += OnDraftChanged;
            subscribed = true;
        }

        return base.SetParametersAsync(parameters);
    }

    public void Dispose()
    {
        if (subscribed)
        {
            Draft.Changed -= OnDraftChanged;
            subscribed = false;
        }
    }

    private void OnDraftChanged() => _ = InvokeAsync(StateHasChanged);
}
