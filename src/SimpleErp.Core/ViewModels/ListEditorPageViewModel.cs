using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleErp.Core.Abstractions;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

/// <summary>A row of a list page.</summary>
public interface IListItem
{
    Guid Id { get; }
}

/// <summary>
/// Shared behaviour of the Employees and Projects pages: a filtered list on the left, an editor for the
/// selected item on the right, and a "save changes?" prompt whenever unsaved edits would be lost.
/// </summary>
public abstract partial class ListEditorPageViewModel<TItem, TEditor> : PageViewModel, IEditorPage
    where TItem : class, IListItem
    where TEditor : EditorViewModelBase
{
    private bool _suppressSelection;
    private bool _activatedOnce;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private TItem? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEditor))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(RevertCommand), nameof(DeleteCommand))]
    private TEditor? _editor;

    protected ListEditorPageViewModel(ErpStore store, IDialogService dialogs, AppStatus status)
    {
        Store = store;
        Dialogs = dialogs;
        Status = status;
    }

    public ObservableCollection<TItem> Items { get; } = [];

    public bool HasEditor => Editor is not null;

    public bool IsListEmpty => Items.Count == 0;

    /// <summary>"24 employees" or "3 of 24 employees".</summary>
    public string ResultsText
    {
        get
        {
            var total = TotalCount;
            var noun = total == 1 ? ItemNoun : ItemNounPlural;
            return Items.Count == total ? $"{total} {noun}" : $"{Items.Count} of {total} {noun}";
        }
    }

    protected ErpStore Store { get; }

    protected IDialogService Dialogs { get; }

    protected AppStatus Status { get; }

    protected abstract string ItemNoun { get; }

    protected abstract string ItemNounPlural { get; }

    protected abstract int TotalCount { get; }

    /// <summary>The rows that match the current filters, in display order.</summary>
    protected abstract IEnumerable<TItem> BuildItems();

    /// <summary>Resets search and filters, so that every item is listed.</summary>
    protected virtual void ResetFilters() => SearchText = string.Empty;

    /// <summary>Creates an editor for the stored item, or returns <see langword="null"/> if it no longer exists.</summary>
    protected abstract TEditor? CreateEditor(Guid id);

    protected abstract TEditor CreateNewEditor();

    /// <summary>Writes the editor's values to the store and makes them the editor's new baseline.</summary>
    protected abstract Task SaveToStoreAsync(TEditor editor);

    protected abstract Task DeleteFromStoreAsync(Guid id);

    /// <summary>Called whenever the editor is replaced or its item was saved, to refresh related information.</summary>
    protected virtual void OnEditorContextChanged()
    {
    }

    public override void OnActivated()
    {
        RebuildList();

        // Pick up changes made on other pages (e.g. new employees for the team picker), unless the user is mid-edit.
        if (Editor is { IsDirty: false, IsNew: false } editor)
        {
            Editor = CreateEditor(editor.Id);
            SelectWithoutPrompt(Items.FirstOrDefault(item => item.Id == Editor?.Id));
        }

        if (!_activatedOnce && Editor is null && Items.Count > 0)
        {
            Editor = CreateEditor(Items[0].Id);
            SelectWithoutPrompt(Items[0]);
        }

        _activatedOnce = true;
        OnEditorContextChanged();
    }

    public override Task<bool> CanLeaveAsync() => ConfirmLeaveEditorAsync();

    [RelayCommand]
    private async Task NewAsync()
    {
        if (!await ConfirmLeaveEditorAsync())
        {
            return;
        }

        SelectWithoutPrompt(null);
        Editor = CreateNewEditor();
        Status.Show($"New {ItemNoun}: fill in the form and press Save.");
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync() => await SaveEditorAsync();

    private bool CanSave() => Editor is { } editor && (editor.IsDirty || editor.IsNew);

    [RelayCommand(CanExecute = nameof(CanRevert))]
    private void Revert()
    {
        Editor!.Revert();
        Status.Show("Changes discarded.");
    }

    private bool CanRevert() => Editor?.IsDirty == true;

    [RelayCommand(CanExecute = nameof(HasEditor))]
    private async Task DeleteAsync()
    {
        var editor = Editor!;
        if (!Dialogs.Confirm($"Delete {ItemNoun}", $"Delete {editor.DisplayName}? This cannot be undone."))
        {
            return;
        }

        var index = Items.ToList().FindIndex(item => item.Id == editor.Id);

        if (!editor.IsNew)
        {
            try
            {
                await DeleteFromStoreAsync(editor.Id);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Dialogs.ShowError("Could not delete", ex.Message);
                return;
            }
        }

        Editor = null;
        RebuildList();

        // Keep the editor filled: move on to the item that took the deleted one's place.
        if (Items.Count > 0)
        {
            var next = Items[Math.Clamp(index, 0, Items.Count - 1)];
            Editor = CreateEditor(next.Id);
            SelectWithoutPrompt(next);
        }

        OnEditorContextChanged();
        Status.Show($"Deleted {editor.DisplayName}.");
    }

    /// <summary>Asks what to do with unsaved edits. Returns <see langword="true"/> if it is safe to move on.</summary>
    protected async Task<bool> ConfirmLeaveEditorAsync()
    {
        if (Editor is not { IsDirty: true } editor)
        {
            return true;
        }

        switch (Dialogs.AskToSaveChanges(editor.DisplayName))
        {
            case UnsavedChangesDecision.Discard:
                editor.Revert();
                if (editor.IsNew)
                {
                    Editor = null;
                }

                return true;

            case UnsavedChangesDecision.Save:
                return await SaveEditorAsync();

            default:
                return false;
        }
    }

    private async Task<bool> SaveEditorAsync()
    {
        if (Editor is not { } editor)
        {
            return false;
        }

        if (!editor.ValidateAll())
        {
            Status.Show($"Not saved: {editor.ErrorSummary}");
            return false;
        }

        try
        {
            await SaveToStoreAsync(editor);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Dialogs.ShowError("Could not save", ex.Message);
            Status.Show("Saving failed. Nothing was written to disk.");
            return false;
        }

        // Make sure the saved item is listed even if it no longer matches the filters.
        if (!BuildItems().Any(item => item.Id == editor.Id))
        {
            ResetFilters();
        }

        RebuildList();
        OnEditorContextChanged();
        Status.Show($"Saved {editor.DisplayName}.");
        return true;
    }

    protected void RebuildList()
    {
        _suppressSelection = true;
        try
        {
            Items.Clear();
            foreach (var item in BuildItems())
            {
                Items.Add(item);
            }

            SelectedItem = Items.FirstOrDefault(item => item.Id == Editor?.Id);
        }
        finally
        {
            _suppressSelection = false;
        }

        OnPropertyChanged(nameof(IsListEmpty));
        OnPropertyChanged(nameof(ResultsText));
    }

    partial void OnSearchTextChanged(string value) => RebuildList();

    partial void OnSelectedItemChanged(TItem? oldValue, TItem? newValue)
    {
        if (_suppressSelection || newValue is null || newValue.Id == Editor?.Id)
        {
            return;
        }

        _ = SwitchToAsync(newValue, oldValue);
    }

    partial void OnEditorChanged(TEditor? oldValue, TEditor? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnEditorPropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnEditorPropertyChanged;
        }

        OnEditorContextChanged();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModelBase.IsDirty) or nameof(EditorViewModelBase.IsNew))
        {
            SaveCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task SwitchToAsync(TItem target, TItem? previous)
    {
        if (await ConfirmLeaveEditorAsync())
        {
            Editor = CreateEditor(target.Id);

            // Saving may have rebuilt the list, replacing the row that was clicked.
            SelectWithoutPrompt(Items.FirstOrDefault(item => item.Id == target.Id));
        }
        else
        {
            // Defer, so the list control finishes its own selection change before it is undone.
            RunAfterCurrentUpdate(() => SelectWithoutPrompt(previous));
        }
    }

    protected void SelectWithoutPrompt(TItem? item)
    {
        _suppressSelection = true;
        try
        {
            SelectedItem = item;
        }
        finally
        {
            _suppressSelection = false;
        }
    }

    private static void RunAfterCurrentUpdate(Action action)
    {
        if (SynchronizationContext.Current is { } context)
        {
            context.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }
}
