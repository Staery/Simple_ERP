using System.Collections;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SimpleErp.Core.Validation;

namespace SimpleErp.Core.ViewModels;

/// <summary>
/// Base for editors that work on a copy of a stored item. Tracks unsaved changes and reports validation
/// errors through <see cref="INotifyDataErrorInfo"/>, so WPF highlights the fields. An error is only shown once
/// its field has been edited, or after a save attempt, so a new, empty form does not start out red.
/// </summary>
public abstract partial class EditorViewModelBase : ObservableObject, INotifyDataErrorInfo
{
    private static readonly IReadOnlyDictionary<string, string> NoErrors = new Dictionary<string, string>();

    private readonly HashSet<string> _touched = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, string> _visibleErrors = NoErrors;
    private bool _revealAll;
    private int _loadDepth;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isNew;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>Whether any error is currently shown.</summary>
    public bool HasErrors => _visibleErrors.Count > 0;

    /// <summary>Text for the banner above the form, or <see langword="null"/> when there is nothing to show.</summary>
    public string? ErrorSummary => _visibleErrors.Count switch
    {
        0 => null,
        1 => _visibleErrors.Values.First(),
        var count => $"Please fix the {count} highlighted fields.",
    };

    /// <summary>Id of the edited item.</summary>
    public abstract Guid Id { get; }

    /// <summary>Name shown in the "save changes?" prompt.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Discards unsaved edits.</summary>
    public abstract void Revert();

    protected bool IsLoading => _loadDepth > 0;

    public IEnumerable GetErrors(string? propertyName) =>
        propertyName is not null && _visibleErrors.TryGetValue(propertyName, out var error) ? new[] { error } : Array.Empty<string>();

    /// <summary>Shows every error, as on a save attempt. Returns <see langword="true"/> if the values are valid.</summary>
    public bool ValidateAll()
    {
        _revealAll = true;
        return Revalidate().IsValid;
    }

    /// <summary>Runs <paramref name="assign"/> without marking the editor dirty, then resets the change tracking.</summary>
    protected void Load(Action assign)
    {
        _loadDepth++;
        try
        {
            assign();
        }
        finally
        {
            _loadDepth--;
        }

        _touched.Clear();
        _revealAll = false;
        IsDirty = false;
        OnValuesChanged();
        Revalidate();
    }

    /// <summary>Call from every editable property's change handler.</summary>
    protected void OnEdited(string propertyName)
    {
        if (IsLoading)
        {
            return;
        }

        _touched.Add(propertyName);
        IsDirty = true;
        OnValuesChanged();
        Revalidate();
    }

    /// <summary>Hook for refreshing values derived from the inputs (previews, totals).</summary>
    protected virtual void OnValuesChanged()
    {
    }

    /// <summary>All current errors, keyed by property name.</summary>
    protected abstract ValidationErrors ComputeErrors();

    private ValidationErrors Revalidate()
    {
        var all = ComputeErrors();
        var visible = all.ByProperty
            .Where(error => _revealAll || _touched.Contains(error.Key))
            .ToDictionary(error => error.Key, error => error.Value, StringComparer.Ordinal);

        var changed = visible.Keys.Union(_visibleErrors.Keys)
            .Where(key => visible.GetValueOrDefault(key) != _visibleErrors.GetValueOrDefault(key))
            .ToList();

        _visibleErrors = visible;

        foreach (var key in changed)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
        }

        if (changed.Count > 0)
        {
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(ErrorSummary));
        }

        return all;
    }
}
