using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleErp.Core.Abstractions;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

/// <summary>A row of the KPI leaderboard.</summary>
public sealed record LeaderboardRow(int Rank, string FullName, string PositionName, KpiScores Kpi, double Score, string Grade);

/// <summary>A row of the project portfolio table.</summary>
public sealed record PortfolioRow(
    string Name,
    string Client,
    string StatusName,
    ProjectInsight Insight,
    int Progress,
    decimal Budget,
    decimal Spent,
    int TeamSize,
    decimal MonthlyTeamCost,
    DateOnly EndDate);

/// <summary>The Reports page: the KPI leaderboard, the project portfolio and CSV export.</summary>
public sealed partial class ReportsViewModel(ErpStore store, IDialogService dialogs, IShellService shell, AppStatus status) : PageViewModel
{
    private const string CsvFilter = "CSV files (*.csv)|*.csv";

    [ObservableProperty]
    private IReadOnlyList<LeaderboardRow> _leaderboard = [];

    [ObservableProperty]
    private IReadOnlyList<PortfolioRow> _portfolio = [];

    [ObservableProperty]
    private decimal _annualPayroll;

    [ObservableProperty]
    private decimal _portfolioBudget;

    [ObservableProperty]
    private decimal _portfolioSpent;

    public override string Title => "Reports";

    public override string Subtitle => "KPI leaderboard, project portfolio and CSV export";

    public string DataLocation => store.Location;

    public override void OnActivated() => Refresh();

    public void Refresh()
    {
        var today = store.Today;

        Leaderboard = KpiCalculator.Rank(store.Employees)
            .Select(r => new LeaderboardRow(r.Rank, r.Employee.FullName, DisplayNames.For(r.Employee.Position), r.Employee.Kpi, r.Score, r.Grade))
            .ToList();

        Portfolio = store.Projects
            .OrderBy(project => project.Status == ProjectStatus.Completed)
            .ThenBy(project => project.EndDate)
            .Select(project => new PortfolioRow(
                project.Name,
                project.Client,
                DisplayNames.For(project.Status),
                ProjectAnalyzer.Analyze(project, today),
                project.Progress,
                project.Budget,
                project.Spent,
                project.TeamIds.Count,
                ProjectAnalyzer.MonthlyTeamCost(project, store.Employees),
                project.EndDate))
            .ToList();

        AnnualPayroll = store.Employees.Sum(employee => employee.MonthlySalary) * 12;
        PortfolioBudget = store.Projects.Sum(project => project.Budget);
        PortfolioSpent = store.Projects.Sum(project => project.Spent);
    }

    [RelayCommand]
    private Task ExportEmployeesAsync() =>
        ExportAsync("Export employees", $"employees-{store.Today:yyyy-MM-dd}.csv", () => CsvExporter.Employees(store.Employees.ToList(), store.Today));

    [RelayCommand]
    private Task ExportProjectsAsync() =>
        ExportAsync("Export projects", $"projects-{store.Today:yyyy-MM-dd}.csv",
            () => CsvExporter.Projects(store.Projects.ToList(), store.Employees.ToList(), store.Today));

    [RelayCommand]
    private void OpenDataFolder()
    {
        if (Path.GetDirectoryName(store.Location) is { } folder && Directory.Exists(folder))
        {
            OpenWithShell(folder);
        }
    }

    private async Task ExportAsync(string title, string fileName, Func<string> createCsv)
    {
        var path = dialogs.PickSaveFile(title, fileName, CsvFilter);
        if (path is null)
        {
            return;
        }

        try
        {
            await CsvExporter.SaveAsync(path, createCsv());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            dialogs.ShowError("Export failed", ex.Message);
            return;
        }

        status.Show($"Exported to {path}");
        if (dialogs.Confirm("Export complete", $"Saved {Path.GetFileName(path)}.\n\nOpen the file now?"))
        {
            OpenWithShell(path);
        }
    }

    private void OpenWithShell(string path)
    {
        try
        {
            shell.Open(path);
        }
        catch (InvalidOperationException ex)
        {
            dialogs.ShowError("Could not open", ex.Message);
        }
    }
}
