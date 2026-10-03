using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Validation;

namespace SimpleErp.Core.ViewModels;

/// <summary>Editable working copy of a <see cref="Project"/>, including its team.</summary>
public sealed partial class ProjectEditorViewModel : EditorViewModelBase
{
    /// <summary>Property name under which team changes are tracked.</summary>
    public const string TeamProperty = "Team";

    private readonly DateOnly _today;
    private readonly IReadOnlyList<Employee> _employees;
    private Project _source;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _client = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private ProjectStatus _status;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private string _budget = string.Empty;

    [ObservableProperty]
    private string _spent = string.Empty;

    [ObservableProperty]
    private int _progress;

    /// <param name="employees">Everyone who can be assigned to the project.</param>
    public ProjectEditorViewModel(Project source, bool isNew, IReadOnlyList<Employee> employees, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(employees);

        _source = source.Clone();
        _employees = employees;
        _today = today;
        IsNew = isNew;
        LoadFromSource();
    }

    public override Guid Id => _source.Id;

    public IReadOnlyList<ProjectStatus> Statuses { get; } = Enum.GetValues<ProjectStatus>();

    /// <summary>All employees, with the current team members selected.</summary>
    public ObservableCollection<TeamMemberOption> TeamOptions { get; } = [];

    public override string DisplayName => string.IsNullOrWhiteSpace(Name) ? "New project" : Name.Trim();

    public int TeamSize => TeamOptions.Count(option => option.IsSelected);

    public decimal TeamMonthlyCost => TeamOptions.Where(option => option.IsSelected).Sum(option => option.MonthlySalary);

    /// <summary>Schedule and budget figures for the values in the form; <see langword="null"/> while the dates are missing.</summary>
    public ProjectInsight? Insight => StartDate is null || EndDate is null ? null : ProjectAnalyzer.Analyze(ToProject(), _today);

    public Project ToProject()
    {
        var project = _source.Clone();
        project.Name = Name.Trim();
        project.Client = Client.Trim();
        project.Description = Description.Trim();
        project.Status = Status;
        project.StartDate = StartDate is { } start ? DateOnly.FromDateTime(start) : default;
        project.EndDate = EndDate is { } end ? DateOnly.FromDateTime(end) : default;
        project.Budget = MoneyText.TryParse(Budget, out var budget) ? budget : 0;
        project.Spent = MoneyText.TryParse(Spent, out var spent) ? spent : 0;
        project.Progress = Progress;
        project.TeamIds = TeamOptions.Where(option => option.IsSelected).Select(option => option.Id).ToList();
        return project;
    }

    public void AcceptSaved(Project saved)
    {
        ArgumentNullException.ThrowIfNull(saved);

        _source = saved.Clone();
        IsNew = false;
        LoadFromSource();
    }

    public override void Revert() => LoadFromSource();

    protected override ValidationErrors ComputeErrors()
    {
        var errors = new ValidationErrors();

        if (StartDate is null)
        {
            errors.Add(nameof(StartDate), "Pick the start date.");
        }

        if (EndDate is null)
        {
            errors.Add(nameof(EndDate), "Pick the end date.");
        }

        if (!MoneyText.TryParse(Budget, out _))
        {
            errors.Add(nameof(Budget), string.IsNullOrWhiteSpace(Budget) ? "Budget is required (0 if none)." : "Enter a number, e.g. 150000.");
        }

        if (!string.IsNullOrWhiteSpace(Spent) && !MoneyText.TryParse(Spent, out _))
        {
            errors.Add(nameof(Spent), "Enter a number, e.g. 42000.");
        }

        var project = ToProject();
        foreach (var (property, message) in ProjectValidator.Validate(project).ByProperty)
        {
            // The date rule only makes sense once both dates are picked.
            if (property == nameof(Project.EndDate) && (StartDate is null || EndDate is null))
            {
                continue;
            }

            errors.Add(property, message);
        }

        return errors;
    }

    protected override void OnValuesChanged()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(TeamSize));
        OnPropertyChanged(nameof(TeamMonthlyCost));
        OnPropertyChanged(nameof(Insight));
    }

    partial void OnNameChanged(string value) => OnEdited(nameof(Name));

    partial void OnClientChanged(string value) => OnEdited(nameof(Client));

    partial void OnDescriptionChanged(string value) => OnEdited(nameof(Description));

    partial void OnStatusChanged(ProjectStatus value)
    {
        // Marking a project as completed implies it is done; the user can still lower it, which the validator reports.
        if (!IsLoading && value == ProjectStatus.Completed)
        {
            Progress = 100;
        }

        OnEdited(nameof(Status));
    }

    partial void OnStartDateChanged(DateTime? value) => OnEdited(nameof(StartDate));

    partial void OnEndDateChanged(DateTime? value) => OnEdited(nameof(EndDate));

    partial void OnBudgetChanged(string value) => OnEdited(nameof(Budget));

    partial void OnSpentChanged(string value) => OnEdited(nameof(Spent));

    partial void OnProgressChanged(int value) => OnEdited(nameof(Progress));

    private void OnTeamOptionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TeamMemberOption.IsSelected))
        {
            OnEdited(TeamProperty);
        }
    }

    private void LoadFromSource() => Load(() =>
    {
        Name = _source.Name;
        Client = _source.Client;
        Description = _source.Description;
        Status = _source.Status;
        StartDate = _source.StartDate == default ? null : _source.StartDate.ToDateTime(TimeOnly.MinValue);
        EndDate = _source.EndDate == default ? null : _source.EndDate.ToDateTime(TimeOnly.MinValue);
        Budget = MoneyText.Format(_source.Budget);
        Spent = MoneyText.Format(_source.Spent);
        Progress = _source.Progress;

        foreach (var option in TeamOptions)
        {
            option.PropertyChanged -= OnTeamOptionChanged;
        }

        TeamOptions.Clear();
        var team = _source.TeamIds.ToHashSet();
        var ordered = _employees
            .OrderBy(e => !team.Contains(e.Id))
            .ThenBy(e => e.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.FirstName, StringComparer.CurrentCultureIgnoreCase);

        // Current members first, so the team is visible without scrolling.
        foreach (var employee in ordered)
        {
            var option = new TeamMemberOption(employee, team.Contains(employee.Id));
            option.PropertyChanged += OnTeamOptionChanged;
            TeamOptions.Add(option);
        }
    });
}
