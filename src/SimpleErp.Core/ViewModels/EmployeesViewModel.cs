using CommunityToolkit.Mvvm.ComponentModel;
using SimpleErp.Core.Abstractions;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

public enum EmployeeSort
{
    Name,
    KpiScore,
    Salary,
    NewestHires,
}

/// <summary>A project shown on an employee's profile.</summary>
public sealed record EmployeeProjectItem(string Name, string Client, ProjectStatus Status)
{
    public string StatusName => DisplayNames.For(Status);
}

/// <summary>The Employees page: search and filter the staff list, edit an employee and their KPI scores.</summary>
public sealed partial class EmployeesViewModel : ListEditorPageViewModel<EmployeeListItem, EmployeeEditorViewModel>
{
    [ObservableProperty]
    private FilterOption<Position?> _selectedPositionFilter;

    [ObservableProperty]
    private FilterOption<EmployeeSort> _selectedSort;

    [ObservableProperty]
    private string _rankText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<EmployeeProjectItem> _editorProjects = [];

    /// <summary>Average indicator scores of all employees, drawn behind the edited employee's chart.</summary>
    [ObservableProperty]
    private IReadOnlyList<ChartPoint> _teamKpi = [];

    public EmployeesViewModel(ErpStore store, IDialogService dialogs, AppStatus status)
        : base(store, dialogs, status)
    {
        PositionFilters =
        [
            new FilterOption<Position?>("All positions", null),
            .. Enum.GetValues<Position>().Select(position => new FilterOption<Position?>(DisplayNames.For(position), position)),
        ];

        SortOptions =
        [
            new FilterOption<EmployeeSort>("Name (A–Z)", EmployeeSort.Name),
            new FilterOption<EmployeeSort>("KPI score", EmployeeSort.KpiScore),
            new FilterOption<EmployeeSort>("Salary", EmployeeSort.Salary),
            new FilterOption<EmployeeSort>("Newest hires", EmployeeSort.NewestHires),
        ];

        _selectedPositionFilter = PositionFilters[0];
        _selectedSort = SortOptions[0];
    }

    public override string Title => "Employees";

    public override string Subtitle => "Staff records, salaries and performance indicators";

    public IReadOnlyList<FilterOption<Position?>> PositionFilters { get; }

    public IReadOnlyList<FilterOption<EmployeeSort>> SortOptions { get; }

    public bool HasEditorProjects => EditorProjects.Count > 0;

    protected override string ItemNoun => "employee";

    protected override string ItemNounPlural => "employees";

    protected override int TotalCount => Store.Employees.Count;

    protected override IEnumerable<EmployeeListItem> BuildItems()
    {
        var search = SearchText.Trim();
        var position = SelectedPositionFilter?.Value;

        var items = KpiCalculator.Rank(Store.Employees)
            .Where(ranking => position is null || ranking.Employee.Position == position)
            .Where(ranking => search.Length == 0 || Matches(ranking.Employee, search))
            .Select(ranking => new EmployeeListItem(ranking.Employee, ranking));

        return (SelectedSort?.Value ?? EmployeeSort.Name) switch
        {
            EmployeeSort.KpiScore => items.OrderBy(item => item.Rank).ThenBy(item => item.FullName, StringComparer.CurrentCultureIgnoreCase),
            EmployeeSort.Salary => items.OrderByDescending(item => item.MonthlySalary),
            EmployeeSort.NewestHires => items.OrderByDescending(item => item.HireDate),
            _ => items.OrderBy(item => item.FullName, StringComparer.CurrentCultureIgnoreCase),
        };
    }

    protected override void ResetFilters()
    {
        SelectedPositionFilter = PositionFilters[0];
        base.ResetFilters();
    }

    protected override EmployeeEditorViewModel? CreateEditor(Guid id) =>
        Store.FindEmployee(id) is { } employee ? new EmployeeEditorViewModel(employee, isNew: false, Store.Today) : null;

    protected override EmployeeEditorViewModel CreateNewEditor()
    {
        var employee = new Employee
        {
            Position = SelectedPositionFilter?.Value ?? Position.Developer,
            HireDate = Store.Today,
            Kpi = new KpiScores { Teamwork = 50, CodeEfficiency = 50, DesignSkills = 50, Leadership = 50, ProjectSuccess = 50 },
        };

        return new EmployeeEditorViewModel(employee, isNew: true, Store.Today);
    }

    protected override async Task SaveToStoreAsync(EmployeeEditorViewModel editor)
    {
        var employee = editor.ToEmployee();
        await Store.SaveEmployeeAsync(employee);
        editor.AcceptSaved(employee);
    }

    protected override Task DeleteFromStoreAsync(Guid id) => Store.DeleteEmployeeAsync(id);

    protected override void OnEditorContextChanged()
    {
        TeamKpi = KpiCalculator.TeamAverages(Store.Employees.ToList());

        var editor = Editor;
        if (editor is null || editor.IsNew)
        {
            RankText = editor is null ? string.Empty : "Not ranked yet";
            EditorProjects = [];
            return;
        }

        var ranking = KpiCalculator.Rank(Store.Employees).FirstOrDefault(r => r.Employee.Id == editor.Id);
        RankText = ranking is null ? string.Empty : $"#{ranking.Rank} of {Store.Employees.Count}";
        EditorProjects = Store.ProjectsOf(editor.Id)
            .OrderBy(project => project.Status)
            .ThenBy(project => project.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(project => new EmployeeProjectItem(project.Name, project.Client, project.Status))
            .ToList();
    }

    partial void OnEditorProjectsChanged(IReadOnlyList<EmployeeProjectItem> value) => OnPropertyChanged(nameof(HasEditorProjects));

    partial void OnSelectedPositionFilterChanged(FilterOption<Position?> value) => RebuildList();

    partial void OnSelectedSortChanged(FilterOption<EmployeeSort> value) => RebuildList();

    private static bool Matches(Employee employee, string search) =>
        employee.FullName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || employee.Email.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || employee.City.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || DisplayNames.For(employee.Position).Contains(search, StringComparison.CurrentCultureIgnoreCase);
}
