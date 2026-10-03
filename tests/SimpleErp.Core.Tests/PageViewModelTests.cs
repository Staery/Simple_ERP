using SimpleErp.Core.Abstractions;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;
using SimpleErp.Core.ViewModels;

namespace SimpleErp.Core.Tests;

public class EmployeesViewModelTests
{
    private static async Task<(EmployeesViewModel Page, ErpStore Store, InMemoryRepository Repository, FakeDialogs Dialogs)> CreateAsync(ErpData? data = null)
    {
        var (store, repository) = await TestData.LoadedStoreAsync(data);
        var dialogs = new FakeDialogs();
        var page = new EmployeesViewModel(store, dialogs, new AppStatus());
        page.OnActivated();
        return (page, store, repository, dialogs);
    }

    [Fact]
    public async Task First_activation_lists_everyone_and_opens_the_first_employee()
    {
        var (page, store, _, _) = await CreateAsync();

        Assert.Equal(store.Employees.Count, page.Items.Count);
        Assert.NotNull(page.Editor);
        Assert.Equal(page.Items[0].Id, page.Editor.Id);
        Assert.Same(page.Items[0], page.SelectedItem);
        Assert.Equal($"{store.Employees.Count} employees", page.ResultsText);
        Assert.StartsWith("#", page.RankText);
    }

    [Fact]
    public async Task Search_and_position_filter_narrow_the_list()
    {
        var (page, store, _, _) = await CreateAsync();

        page.SelectedPositionFilter = page.PositionFilters.Single(f => f.Value == Position.Designer);
        Assert.All(page.Items, item => Assert.Equal(Position.Designer, item.Position));
        Assert.Equal($"{page.Items.Count} of {store.Employees.Count} employees", page.ResultsText);

        page.SearchText = "zzz-nobody";
        Assert.True(page.IsListEmpty);
    }

    [Fact]
    public async Task Sorting_by_kpi_puts_the_best_first()
    {
        var (page, _, _, _) = await CreateAsync();

        page.SelectedSort = page.SortOptions.Single(o => o.Value == EmployeeSort.KpiScore);

        Assert.Equal(1, page.Items[0].Rank);
        Assert.True(page.Items.Zip(page.Items.Skip(1)).All(pair => pair.First.Score >= pair.Second.Score));
    }

    [Fact]
    public async Task Editing_and_saving_updates_the_employee_without_duplicates()
    {
        var (page, store, repository, _) = await CreateAsync();
        var count = store.Employees.Count;
        var id = page.Editor!.Id;

        page.Editor.City = "Lisbon";
        Assert.True(page.SaveCommand.CanExecute(null));
        await page.SaveCommand.ExecuteAsync(null);

        Assert.Equal(count, store.Employees.Count);
        Assert.Equal("Lisbon", store.FindEmployee(id)!.City);
        Assert.Equal("Lisbon", repository.Stored!.Employees.Single(e => e.Id == id).City);
        Assert.False(page.Editor.IsDirty);
        Assert.False(page.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task New_employee_is_validated_saved_once_and_selected()
    {
        var (page, store, _, _) = await CreateAsync(new ErpData());

        await page.NewCommand.ExecuteAsync(null);
        var editor = page.Editor!;
        Assert.True(editor.IsNew);

        await page.SaveCommand.ExecuteAsync(null);
        Assert.Empty(store.Employees);
        Assert.True(editor.HasErrors);

        editor.FirstName = "Grace";
        editor.LastName = "Hopper";
        editor.BirthDate = new DateTime(1990, 1, 1);
        editor.HireDate = new DateTime(2024, 1, 1);
        editor.MonthlySalary = "9000";
        await page.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(store.Employees);
        Assert.Equal("Grace Hopper", saved.FullName);
        Assert.False(editor.IsNew);
        Assert.Equal(saved.Id, page.SelectedItem!.Id);
    }

    [Fact]
    public async Task Saved_employee_stays_visible_when_it_no_longer_matches_the_filter()
    {
        var (page, _, _, _) = await CreateAsync();
        page.SelectedPositionFilter = page.PositionFilters.Single(f => f.Value == Position.Developer);
        page.SelectedItem = page.Items[0];
        var id = page.Editor!.Id;

        page.Editor.Position = Position.Designer;
        await page.SaveCommand.ExecuteAsync(null);

        Assert.Null(page.SelectedPositionFilter.Value);
        Assert.Equal(id, page.SelectedItem!.Id);
    }

    [Fact]
    public async Task Switching_with_unsaved_changes_asks_and_cancel_keeps_the_edit()
    {
        var (page, _, _, dialogs) = await CreateAsync();
        var first = page.Editor!;
        first.City = "Edited";
        dialogs.UnsavedChangesDecision = UnsavedChangesDecision.Cancel;

        page.SelectedItem = page.Items[1];

        Assert.Equal(1, dialogs.SavePrompts);
        Assert.Same(first, page.Editor);
        Assert.Equal("Edited", page.Editor!.City);
        Assert.Equal(first.Id, page.SelectedItem!.Id);
    }

    [Fact]
    public async Task Switching_with_discard_drops_the_edit()
    {
        var (page, store, _, dialogs) = await CreateAsync();
        var firstId = page.Editor!.Id;
        page.Editor.City = "Edited";
        dialogs.UnsavedChangesDecision = UnsavedChangesDecision.Discard;

        page.SelectedItem = page.Items[1];

        Assert.Equal(page.Items[1].Id, page.Editor!.Id);
        Assert.NotEqual("Edited", store.FindEmployee(firstId)!.City);
    }

    [Fact]
    public async Task Leaving_the_page_with_save_writes_the_changes()
    {
        var (page, store, _, dialogs) = await CreateAsync();
        var id = page.Editor!.Id;
        page.Editor.City = "Saved on leave";
        dialogs.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        Assert.True(await page.CanLeaveAsync());
        Assert.Equal("Saved on leave", store.FindEmployee(id)!.City);
    }

    [Fact]
    public async Task Deleting_removes_the_employee_from_teams_and_opens_the_next_one()
    {
        var (page, store, _, _) = await CreateAsync();
        var id = page.Editor!.Id;
        Assert.NotEmpty(store.ProjectsOf(id));

        await page.DeleteCommand.ExecuteAsync(null);

        Assert.Null(store.FindEmployee(id));
        Assert.Empty(store.ProjectsOf(id));
        Assert.NotNull(page.Editor);
        Assert.NotEqual(id, page.Editor.Id);
    }

    [Fact]
    public async Task Declining_the_delete_confirmation_keeps_the_employee()
    {
        var (page, store, _, dialogs) = await CreateAsync();
        dialogs.ConfirmResult = false;
        var count = store.Employees.Count;

        await page.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(count, store.Employees.Count);
    }

    [Fact]
    public async Task Failed_save_shows_an_error_and_keeps_the_edit()
    {
        var (page, store, repository, dialogs) = await CreateAsync();
        var id = page.Editor!.Id;
        page.Editor.City = "Unsaved";
        repository.FailSaves = true;

        await page.SaveCommand.ExecuteAsync(null);

        Assert.Single(dialogs.Errors);
        Assert.True(page.Editor.IsDirty);
        Assert.NotEqual("Unsaved", store.FindEmployee(id)!.City);
    }

    [Fact]
    public async Task Profile_lists_the_projects_of_the_employee()
    {
        var a = TestData.Employee();
        var (page, _, _, _) = await CreateAsync(new ErpData { Employees = [a], Projects = [TestData.Project("Alpha", a.Id)] });

        Assert.True(page.HasEditorProjects);
        Assert.Equal("Alpha", Assert.Single(page.EditorProjects).Name);
        Assert.Equal("#1 of 1", page.RankText);
    }
}

public class ProjectsViewModelTests
{
    private static async Task<(ProjectsViewModel Page, ErpStore Store)> CreateAsync()
    {
        var (store, _) = await TestData.LoadedStoreAsync();
        var page = new ProjectsViewModel(store, new FakeDialogs(), new AppStatus());
        page.OnActivated();
        return (page, store);
    }

    [Fact]
    public async Task Open_projects_are_listed_by_deadline_with_completed_last()
    {
        var (page, _) = await CreateAsync();

        Assert.Equal(ProjectStatus.Completed, page.Items[^1].Status);
        var open = page.Items.Where(i => i.Status != ProjectStatus.Completed).ToList();
        Assert.Equal(open.OrderBy(i => i.EndDate).Select(i => i.Id), open.Select(i => i.Id));
    }

    [Fact]
    public async Task Status_filter_shows_only_matching_projects()
    {
        var (page, _) = await CreateAsync();

        page.SelectedStatusFilter = page.StatusFilters.Single(f => f.Value == ProjectStatus.Active);

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item => Assert.Equal(ProjectStatus.Active, item.Status));
    }

    [Fact]
    public async Task Assigning_a_team_member_is_saved()
    {
        var (page, store) = await CreateAsync();
        var editor = page.Editor!;
        var newcomer = editor.TeamOptions.First(o => !o.IsSelected);

        newcomer.IsSelected = true;
        await page.SaveCommand.ExecuteAsync(null);

        Assert.Contains(newcomer.Id, store.FindProject(editor.Id)!.TeamIds);
        Assert.Contains(store.FindProject(editor.Id)!, store.ProjectsOf(newcomer.Id));
    }

    [Fact]
    public async Task List_rows_show_team_avatars_and_health()
    {
        var (page, store) = await CreateAsync();

        var row = page.Items.Single(i => i.TeamSize == 5 && i.Status == ProjectStatus.Active && i.Insight.Health == ProjectHealth.OnTrack);

        Assert.Equal(ProjectListItem.MaxAvatars, row.Team.Count);
        Assert.Equal(1, row.HiddenTeamCount);
        Assert.True(row.HasHiddenTeam);
    }

    [Fact]
    public async Task Activation_refreshes_the_team_picker_with_new_employees()
    {
        var (page, store) = await CreateAsync();
        var before = page.Editor!.TeamOptions.Count;

        await store.SaveEmployeeAsync(TestData.Employee("New", "Hire"));
        page.OnActivated();

        Assert.Equal(before + 1, page.Editor!.TeamOptions.Count);
    }
}

public class ShellViewModelTests
{
    private static (ShellViewModel Shell, InMemoryRepository Repository, FakeDialogs Dialogs, EmployeesViewModel Employees) Create(ErpData? data = null)
    {
        var repository = new InMemoryRepository(data);
        var store = new ErpStore(repository, TestData.Time);
        var dialogs = new FakeDialogs();
        var status = new AppStatus();
        var employees = new EmployeesViewModel(store, dialogs, status);
        var shell = new ShellViewModel(
            store,
            dialogs,
            status,
            new DashboardViewModel(store),
            employees,
            new ProjectsViewModel(store, dialogs, status),
            new ReportsViewModel(store, dialogs, new FakeShell(), status));
        return (shell, repository, dialogs, employees);
    }

    [Fact]
    public async Task Loading_seeds_demo_data_and_opens_the_dashboard()
    {
        var (shell, repository, _, _) = Create();

        await shell.LoadCommand.ExecuteAsync(null);

        var dashboard = Assert.IsType<DashboardViewModel>(shell.CurrentPage);
        Assert.Equal(repository.Stored!.Employees.Count, dashboard.Summary.Headcount);
        Assert.StartsWith("Welcome!", shell.Status.Message);
        Assert.Equal(repository.Stored.Employees.Count, shell.NavigationItems[1].Badge);
        Assert.Equal(repository.Stored.Projects.Count, shell.NavigationItems[2].Badge);
    }

    [Fact]
    public async Task Unreadable_data_is_reported_and_the_app_starts_empty()
    {
        var (shell, repository, dialogs, _) = Create();
        repository.LoadException = new InvalidDataException("broken");

        await shell.LoadCommand.ExecuteAsync(null);

        Assert.Equal("broken", Assert.Single(dialogs.Errors));
        Assert.Equal(0, shell.NavigationItems[1].Badge);
        Assert.NotNull(shell.CurrentPage);
    }

    [Fact]
    public async Task Navigation_is_blocked_when_the_user_cancels_the_save_prompt()
    {
        var (shell, _, dialogs, employees) = Create();
        await shell.LoadCommand.ExecuteAsync(null);
        shell.SelectedNavigationItem = shell.NavigationItems[1];
        Assert.Same(employees, shell.CurrentPage);

        employees.Editor!.City = "Unsaved";
        dialogs.UnsavedChangesDecision = UnsavedChangesDecision.Cancel;
        shell.SelectedNavigationItem = shell.NavigationItems[3];

        Assert.Same(employees, shell.CurrentPage);
        Assert.Same(shell.NavigationItems[1], shell.SelectedNavigationItem);
        Assert.False(await shell.PrepareToCloseAsync());
    }

    [Fact]
    public async Task Keyboard_shortcuts_reach_the_current_editor_page()
    {
        var (shell, _, _, employees) = Create();
        await shell.LoadCommand.ExecuteAsync(null);
        shell.SelectedNavigationItem = shell.NavigationItems[1];

        await shell.NewItemCommand.ExecuteAsync(null);

        Assert.True(employees.Editor!.IsNew);
    }
}

public sealed class ReportsViewModelTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"simple-erp-report-{Guid.NewGuid():N}.csv");

    public void Dispose() => File.Delete(_path);

    private static async Task<(ReportsViewModel Page, FakeDialogs Dialogs, FakeShell Shell)> CreateAsync()
    {
        var (store, _) = await TestData.LoadedStoreAsync();
        var dialogs = new FakeDialogs();
        var shell = new FakeShell();
        var page = new ReportsViewModel(store, dialogs, shell, new AppStatus());
        page.OnActivated();
        return (page, dialogs, shell);
    }

    [Fact]
    public async Task Tables_are_filled_on_activation()
    {
        var (page, _, _) = await CreateAsync();

        Assert.Equal(1, page.Leaderboard[0].Rank);
        Assert.NotEmpty(page.Portfolio);
        Assert.True(page.AnnualPayroll > 0);
        Assert.True(page.PortfolioSpent < page.PortfolioBudget);
    }

    [Fact]
    public async Task Export_writes_the_chosen_file_and_offers_to_open_it()
    {
        var (page, dialogs, shell) = await CreateAsync();
        dialogs.FileToSave = _path;

        await page.ExportEmployeesCommand.ExecuteAsync(null);

        Assert.StartsWith("Rank,First name", (await File.ReadAllTextAsync(_path)).TrimStart('﻿'));
        Assert.Equal([_path], shell.Opened);
    }

    [Fact]
    public async Task Cancelled_export_writes_nothing()
    {
        var (page, dialogs, shell) = await CreateAsync();
        dialogs.FileToSave = null;

        await page.ExportProjectsCommand.ExecuteAsync(null);

        Assert.False(File.Exists(_path));
        Assert.Empty(shell.Opened);
        Assert.Equal(0, dialogs.Confirmations);
    }
}
