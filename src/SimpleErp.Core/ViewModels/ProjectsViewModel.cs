using CommunityToolkit.Mvvm.ComponentModel;
using SimpleErp.Core.Abstractions;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

public enum ProjectSort
{
    EndDate,
    Name,
    Progress,
    Budget,
}

/// <summary>The Projects page: the project portfolio, schedule and budget tracking, and team assignment.</summary>
public sealed partial class ProjectsViewModel : ListEditorPageViewModel<ProjectListItem, ProjectEditorViewModel>
{
    [ObservableProperty]
    private FilterOption<ProjectStatus?> _selectedStatusFilter;

    [ObservableProperty]
    private FilterOption<ProjectSort> _selectedSort;

    public ProjectsViewModel(ErpStore store, IDialogService dialogs, AppStatus status)
        : base(store, dialogs, status)
    {
        StatusFilters =
        [
            new FilterOption<ProjectStatus?>("All statuses", null),
            .. Enum.GetValues<ProjectStatus>().Select(s => new FilterOption<ProjectStatus?>(DisplayNames.For(s), s)),
        ];

        SortOptions =
        [
            new FilterOption<ProjectSort>("End date", ProjectSort.EndDate),
            new FilterOption<ProjectSort>("Name (A–Z)", ProjectSort.Name),
            new FilterOption<ProjectSort>("Progress", ProjectSort.Progress),
            new FilterOption<ProjectSort>("Budget", ProjectSort.Budget),
        ];

        _selectedStatusFilter = StatusFilters[0];
        _selectedSort = SortOptions[0];
    }

    public override string Title => "Projects";

    public override string Subtitle => "Portfolio, schedules, budgets and team assignment";

    public IReadOnlyList<FilterOption<ProjectStatus?>> StatusFilters { get; }

    public IReadOnlyList<FilterOption<ProjectSort>> SortOptions { get; }

    protected override string ItemNoun => "project";

    protected override string ItemNounPlural => "projects";

    protected override int TotalCount => Store.Projects.Count;

    protected override IEnumerable<ProjectListItem> BuildItems()
    {
        var search = SearchText.Trim();
        var status = SelectedStatusFilter?.Value;
        var employees = Store.Employees.ToDictionary(employee => employee.Id);
        var today = Store.Today;

        var items = Store.Projects
            .Where(project => status is null || project.Status == status)
            .Where(project => search.Length == 0
                || project.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || project.Client.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            .Select(project => new ProjectListItem(project, employees, today));

        return (SelectedSort?.Value ?? ProjectSort.EndDate) switch
        {
            ProjectSort.Name => items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase),
            ProjectSort.Progress => items.OrderByDescending(item => item.Progress),
            ProjectSort.Budget => items.OrderByDescending(item => item.Budget),
            // Open projects by deadline first, finished ones at the end.
            _ => items.OrderBy(item => item.Status == ProjectStatus.Completed).ThenBy(item => item.EndDate),
        };
    }

    protected override void ResetFilters()
    {
        SelectedStatusFilter = StatusFilters[0];
        base.ResetFilters();
    }

    protected override ProjectEditorViewModel? CreateEditor(Guid id) =>
        Store.FindProject(id) is { } project
            ? new ProjectEditorViewModel(project, isNew: false, Store.Employees, Store.Today)
            : null;

    protected override ProjectEditorViewModel CreateNewEditor()
    {
        var today = Store.Today;
        var project = new Project
        {
            Status = SelectedStatusFilter?.Value ?? ProjectStatus.Planned,
            StartDate = today,
            EndDate = today.AddMonths(3),
        };

        if (project.Status == ProjectStatus.Completed)
        {
            project.Progress = 100;
        }

        return new ProjectEditorViewModel(project, isNew: true, Store.Employees, today);
    }

    protected override async Task SaveToStoreAsync(ProjectEditorViewModel editor)
    {
        var project = editor.ToProject();
        await Store.SaveProjectAsync(project);
        editor.AcceptSaved(Store.FindProject(project.Id) ?? project);
    }

    protected override Task DeleteFromStoreAsync(Guid id) => Store.DeleteProjectAsync(id);

    partial void OnSelectedStatusFilterChanged(FilterOption<ProjectStatus?> value) => RebuildList();

    partial void OnSelectedSortChanged(FilterOption<ProjectSort> value) => RebuildList();
}
