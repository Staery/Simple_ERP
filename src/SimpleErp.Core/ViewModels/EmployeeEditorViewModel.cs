using CommunityToolkit.Mvvm.ComponentModel;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Validation;

namespace SimpleErp.Core.ViewModels;

/// <summary>Editable working copy of an <see cref="Employee"/>. The stored employee is not touched until the page saves.</summary>
public sealed partial class EmployeeEditorViewModel : EditorViewModelBase
{
    private readonly DateOnly _today;
    private Employee _source;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private Position _position;

    [ObservableProperty]
    private DateTime? _birthDate;

    [ObservableProperty]
    private DateTime? _hireDate;

    [ObservableProperty]
    private string _monthlySalary = string.Empty;

    [ObservableProperty]
    private int _teamwork;

    [ObservableProperty]
    private int _codeEfficiency;

    [ObservableProperty]
    private int _designSkills;

    [ObservableProperty]
    private int _leadership;

    [ObservableProperty]
    private int _projectSuccess;

    public EmployeeEditorViewModel(Employee source, bool isNew, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source.Clone();
        _today = today;
        IsNew = isNew;
        LoadFromSource();
    }

    public override Guid Id => _source.Id;

    public IReadOnlyList<Position> Positions { get; } = Enum.GetValues<Position>();

    public override string DisplayName => string.IsNullOrWhiteSpace(FullName) ? "New employee" : FullName;

    public string FullName => $"{FirstName?.Trim()} {LastName?.Trim()}".Trim();

    public string Initials => PersonName.Initials(FirstName, LastName);

    public int AvatarIndex => Avatar.ColorIndex(Id);

    public string PositionName => DisplayNames.For(Position);

    /// <summary>Overall KPI score for the values currently in the form.</summary>
    public double Score => KpiCalculator.Score(CurrentKpi(), Position);

    public string Grade => KpiCalculator.Grade(Score);

    public IReadOnlyList<ChartPoint> KpiChart => KpiCalculator.ToChart(CurrentKpi());

    /// <summary>How the indicators are weighted for the selected position, e.g. "Code efficiency 35%".</summary>
    public IReadOnlyList<ChartPoint> Weights =>
        KpiScores.Indicators.Select(i => new ChartPoint(DisplayNames.For(i), KpiCalculator.WeightOf(Position, i))).ToList();

    public string AgeText => BirthDate is { } birth && DateOnly.FromDateTime(birth) <= _today
        ? $"age {Dates.WholeYearsBetween(DateOnly.FromDateTime(birth), _today)}"
        : string.Empty;

    public string TenureText
    {
        get
        {
            if (HireDate is not { } hire || DateOnly.FromDateTime(hire) > _today)
            {
                return string.Empty;
            }

            var years = Dates.WholeYearsBetween(DateOnly.FromDateTime(hire), _today);
            return years switch
            {
                0 => "joined recently",
                1 => "1 year here",
                _ => $"{years} years here",
            };
        }
    }

    /// <summary>The edited values as a new <see cref="Employee"/> with the same id. Unparsable numbers become 0.</summary>
    public Employee ToEmployee()
    {
        var employee = _source.Clone();
        employee.FirstName = FirstName.Trim();
        employee.LastName = LastName.Trim();
        employee.Email = Email.Trim();
        employee.City = City.Trim();
        employee.Position = Position;
        employee.BirthDate = BirthDate is { } birth ? DateOnly.FromDateTime(birth) : default;
        employee.HireDate = HireDate is { } hire ? DateOnly.FromDateTime(hire) : default;
        employee.MonthlySalary = MoneyText.TryParse(MonthlySalary, out var salary) ? salary : 0;
        employee.Kpi = CurrentKpi();
        return employee;
    }

    /// <summary>Makes <paramref name="saved"/> the new baseline after a successful save.</summary>
    public void AcceptSaved(Employee saved)
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

        if (BirthDate is null)
        {
            errors.Add(nameof(BirthDate), "Pick the birth date.");
        }

        if (HireDate is null)
        {
            errors.Add(nameof(HireDate), "Pick the hire date.");
        }

        if (!MoneyText.TryParse(MonthlySalary, out _))
        {
            errors.Add(nameof(MonthlySalary), string.IsNullOrWhiteSpace(MonthlySalary) ? "Salary is required." : "Enter a number, e.g. 5200.");
        }

        // Rules on the model; errors above (missing or unparsable input) take precedence.
        foreach (var (property, message) in EmployeeValidator.Validate(ToEmployee(), _today).ByProperty)
        {
            errors.Add(property, message);
        }

        return errors;
    }

    protected override void OnValuesChanged()
    {
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Initials));
        OnPropertyChanged(nameof(PositionName));
        OnPropertyChanged(nameof(Score));
        OnPropertyChanged(nameof(Grade));
        OnPropertyChanged(nameof(KpiChart));
        OnPropertyChanged(nameof(Weights));
        OnPropertyChanged(nameof(AgeText));
        OnPropertyChanged(nameof(TenureText));
    }

    partial void OnFirstNameChanged(string value) => OnEdited(nameof(FirstName));

    partial void OnLastNameChanged(string value) => OnEdited(nameof(LastName));

    partial void OnEmailChanged(string value) => OnEdited(nameof(Email));

    partial void OnCityChanged(string value) => OnEdited(nameof(City));

    partial void OnPositionChanged(Position value) => OnEdited(nameof(Position));

    partial void OnBirthDateChanged(DateTime? value) => OnEdited(nameof(BirthDate));

    partial void OnHireDateChanged(DateTime? value) => OnEdited(nameof(HireDate));

    partial void OnMonthlySalaryChanged(string value) => OnEdited(nameof(MonthlySalary));

    partial void OnTeamworkChanged(int value) => OnEdited(nameof(Teamwork));

    partial void OnCodeEfficiencyChanged(int value) => OnEdited(nameof(CodeEfficiency));

    partial void OnDesignSkillsChanged(int value) => OnEdited(nameof(DesignSkills));

    partial void OnLeadershipChanged(int value) => OnEdited(nameof(Leadership));

    partial void OnProjectSuccessChanged(int value) => OnEdited(nameof(ProjectSuccess));

    private KpiScores CurrentKpi() => new()
    {
        Teamwork = Teamwork,
        CodeEfficiency = CodeEfficiency,
        DesignSkills = DesignSkills,
        Leadership = Leadership,
        ProjectSuccess = ProjectSuccess,
    };

    private void LoadFromSource() => Load(() =>
    {
        FirstName = _source.FirstName;
        LastName = _source.LastName;
        Email = _source.Email;
        City = _source.City;
        Position = _source.Position;
        BirthDate = _source.BirthDate == default ? null : _source.BirthDate.ToDateTime(TimeOnly.MinValue);
        HireDate = _source.HireDate == default ? null : _source.HireDate.ToDateTime(TimeOnly.MinValue);
        MonthlySalary = _source.MonthlySalary == 0 ? string.Empty : MoneyText.Format(_source.MonthlySalary);
        Teamwork = _source.Kpi.Teamwork;
        CodeEfficiency = _source.Kpi.CodeEfficiency;
        DesignSkills = _source.Kpi.DesignSkills;
        Leadership = _source.Kpi.Leadership;
        ProjectSuccess = _source.Kpi.ProjectSuccess;
    });
}
