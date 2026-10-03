using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleErp.Core.Abstractions;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

/// <summary>The main window: sidebar navigation, the current page and the status bar.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly ErpStore _store;
    private readonly IDialogService _dialogs;
    private readonly NavigationItem _employeesItem;
    private readonly NavigationItem _projectsItem;
    private bool _suppressNavigation;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty]
    private bool _isLoading;

    public ShellViewModel(
        ErpStore store,
        IDialogService dialogs,
        AppStatus status,
        DashboardViewModel dashboard,
        EmployeesViewModel employees,
        ProjectsViewModel projects,
        ReportsViewModel reports)
    {
        _store = store;
        _dialogs = dialogs;
        Status = status;

        _employeesItem = new NavigationItem("Employees", "Icon.People", employees);
        _projectsItem = new NavigationItem("Projects", "Icon.Briefcase", projects);
        NavigationItems =
        [
            new NavigationItem("Dashboard", "Icon.Dashboard", dashboard),
            _employeesItem,
            _projectsItem,
            new NavigationItem("Reports", "Icon.Report", reports),
        ];

        _store.Changed += (_, _) => UpdateBadges();
    }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public AppStatus Status { get; }

    public string DataLocation => _store.Location;

    /// <summary>Loads the data and shows the dashboard. Called once when the window opens.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var seeded = await _store.LoadAsync();
            Status.Show(seeded
                ? "Welcome! Demo data was created so you can look around. It is saved in " + _store.Location
                : $"Loaded {_store.Employees.Count} employees and {_store.Projects.Count} projects from {_store.Location}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _dialogs.ShowError("Could not load data", ex.Message);
            _store.Reset();
            Status.Show("Started with empty data.");
        }
        finally
        {
            IsLoading = false;
        }

        SelectedNavigationItem = NavigationItems[0];
    }

    /// <summary>Ctrl+N on a page with an editor.</summary>
    [RelayCommand]
    private async Task NewItemAsync()
    {
        if (CurrentPage is IEditorPage page && page.NewCommand.CanExecute(null))
        {
            await page.NewCommand.ExecuteAsync(null);
        }
    }

    /// <summary>Ctrl+S on a page with an editor.</summary>
    [RelayCommand]
    private async Task SaveItemAsync()
    {
        if (CurrentPage is IEditorPage page && page.SaveCommand.CanExecute(null))
        {
            await page.SaveCommand.ExecuteAsync(null);
        }
    }

    /// <summary>Called when the window is about to close. Returns <see langword="false"/> to keep it open.</summary>
    public Task<bool> PrepareToCloseAsync() => CurrentPage?.CanLeaveAsync() ?? Task.FromResult(true);

    partial void OnSelectedNavigationItemChanged(NavigationItem? oldValue, NavigationItem? newValue)
    {
        if (_suppressNavigation || newValue is null || newValue.Page == CurrentPage)
        {
            return;
        }

        _ = NavigateAsync(newValue, oldValue);
    }

    private async Task NavigateAsync(NavigationItem target, NavigationItem? previous)
    {
        if (CurrentPage is { } current && !await current.CanLeaveAsync())
        {
            // Defer, so the list control finishes its own selection change before it is undone.
            if (SynchronizationContext.Current is { } context)
            {
                context.Post(_ => SelectWithoutNavigation(previous), null);
            }
            else
            {
                SelectWithoutNavigation(previous);
            }

            return;
        }

        target.Page.OnActivated();
        CurrentPage = target.Page;

        // The selection may have changed while a save prompt was open.
        SelectWithoutNavigation(target);
    }

    private void SelectWithoutNavigation(NavigationItem? item)
    {
        _suppressNavigation = true;
        try
        {
            SelectedNavigationItem = item;
        }
        finally
        {
            _suppressNavigation = false;
        }
    }

    private void UpdateBadges()
    {
        _employeesItem.Badge = _store.Employees.Count;
        _projectsItem.Badge = _store.Projects.Count;
    }
}
