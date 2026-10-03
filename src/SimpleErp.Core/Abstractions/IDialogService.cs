namespace SimpleErp.Core.Abstractions;

/// <summary>Answer to a "save changes?" prompt.</summary>
public enum UnsavedChangesDecision
{
    Save,
    Discard,
    Cancel,
}

/// <summary>User interaction that view models need but must not implement themselves.</summary>
public interface IDialogService
{
    bool Confirm(string title, string message);

    UnsavedChangesDecision AskToSaveChanges(string itemName);

    void ShowError(string title, string message);

    /// <summary>Asks where to save a file. Returns the chosen path, or <see langword="null"/> if the user cancelled.</summary>
    /// <param name="filter">A file dialog filter such as <c>CSV files (*.csv)|*.csv</c>.</param>
    string? PickSaveFile(string title, string defaultFileName, string filter);
}
