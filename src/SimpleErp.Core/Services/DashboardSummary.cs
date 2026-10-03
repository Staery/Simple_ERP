using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>An active project as shown on the dashboard.</summary>
public sealed record ProjectProgressItem(Project Project, ProjectInsight Insight)
{
    public string Name => Project.Name;

    public string Client => Project.Client;

    public int Progress => Project.Progress;
}

/// <summary>Key figures and chart data for the dashboard, calculated from the current data.</summary>
public sealed class DashboardSummary
{
    public static DashboardSummary Empty { get; } = Create([], [], DateOnly.MinValue);

    public int Headcount { get; private init; }

    public decimal MonthlyPayroll { get; private init; }

    public decimal AverageSalary { get; private init; }

    public double AverageKpi { get; private init; }

    public int ActiveProjects { get; private init; }

    public int ProjectsNeedingAttention { get; private init; }

    public decimal ActiveBudget { get; private init; }

    public decimal ActiveSpent { get; private init; }

    /// <summary>Spent divided by budget across active projects, 0–1+ (0 when there is no budget).</summary>
    public double ActiveBudgetUsed => ActiveBudget > 0 ? (double)(ActiveSpent / ActiveBudget) : 0;

    public IReadOnlyList<ChartPoint> HeadcountByPosition { get; private init; } = [];

    public IReadOnlyList<ChartPoint> ProjectsByStatus { get; private init; } = [];

    public IReadOnlyList<ChartPoint> TeamKpi { get; private init; } = [];

    public IReadOnlyList<ChartPoint> AverageScoreByPosition { get; private init; } = [];

    public IReadOnlyList<EmployeeRanking> TopPerformers { get; private init; } = [];

    /// <summary>Active projects, the ones needing attention first, then by end date.</summary>
    public IReadOnlyList<ProjectProgressItem> ActiveProjectProgress { get; private init; } = [];

    public static DashboardSummary Create(IReadOnlyCollection<Employee> employees, IReadOnlyCollection<Project> projects, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(projects);

        var rankings = KpiCalculator.Rank(employees);
        var active = projects
            .Where(project => project.Status == ProjectStatus.Active)
            .Select(project => new ProjectProgressItem(project, ProjectAnalyzer.Analyze(project, today)))
            .OrderByDescending(item => item.Insight.NeedsAttention)
            .ThenBy(item => item.Project.EndDate)
            .ToList();

        var positions = Enum.GetValues<Position>();

        return new DashboardSummary
        {
            Headcount = employees.Count,
            MonthlyPayroll = employees.Sum(employee => employee.MonthlySalary),
            AverageSalary = employees.Count == 0 ? 0 : Math.Round(employees.Average(employee => employee.MonthlySalary), 2),
            AverageKpi = rankings.Count == 0 ? 0 : Math.Round(rankings.Average(ranking => ranking.Score), 1),
            ActiveProjects = active.Count,
            ProjectsNeedingAttention = projects.Count(project => ProjectAnalyzer.Analyze(project, today).NeedsAttention),
            ActiveBudget = active.Sum(item => item.Project.Budget),
            ActiveSpent = active.Sum(item => item.Project.Spent),
            HeadcountByPosition = positions
                .Select(position => new ChartPoint(DisplayNames.For(position), employees.Count(employee => employee.Position == position)))
                .Where(point => point.Value > 0)
                .OrderByDescending(point => point.Value)
                .ToList(),
            ProjectsByStatus = Enum.GetValues<ProjectStatus>()
                .Select(status => new ChartPoint(DisplayNames.For(status), projects.Count(project => project.Status == status)))
                .ToList(),
            TeamKpi = KpiCalculator.TeamAverages(employees),
            AverageScoreByPosition = positions
                .Select(position => (Label: DisplayNames.For(position), Scores: rankings.Where(r => r.Employee.Position == position).ToList()))
                .Where(group => group.Scores.Count > 0)
                .Select(group => new ChartPoint(group.Label, Math.Round(group.Scores.Average(r => r.Score), 1)))
                .OrderByDescending(point => point.Value)
                .ToList(),
            TopPerformers = rankings.Take(5).ToList(),
            ActiveProjectProgress = active,
        };
    }
}
