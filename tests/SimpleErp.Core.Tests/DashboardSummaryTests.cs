using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;

namespace SimpleErp.Core.Tests;

public class DashboardSummaryTests
{
    [Fact]
    public void Key_figures_are_calculated()
    {
        var a = TestData.Employee("Ann", "A", Position.Developer, kpi: 80, salary: 8_000m);
        var b = TestData.Employee("Bob", "B", Position.Designer, kpi: 60, salary: 6_000m);
        var onTrack = TestData.Project("On track", a.Id);
        var overBudget = TestData.Project("Over budget", b.Id);
        overBudget.Spent = 150_000m;
        var done = TestData.Project("Done");
        done.Status = ProjectStatus.Completed;
        done.Progress = 100;

        var summary = DashboardSummary.Create([a, b], [onTrack, overBudget, done], TestData.Today);

        Assert.Equal(2, summary.Headcount);
        Assert.Equal(14_000m, summary.MonthlyPayroll);
        Assert.Equal(7_000m, summary.AverageSalary);
        Assert.Equal(70, summary.AverageKpi);
        Assert.Equal(2, summary.ActiveProjects);
        Assert.Equal(1, summary.ProjectsNeedingAttention);
        Assert.Equal(200_000m, summary.ActiveBudget);
        Assert.Equal(190_000m, summary.ActiveSpent);
        Assert.Equal("Over budget", summary.ActiveProjectProgress[0].Name);
        Assert.Equal("Ann A", summary.TopPerformers[0].Employee.FullName);
        Assert.Equal(["Designer", "Developer"], summary.HeadcountByPosition.Select(p => p.Label).Order());
        Assert.Equal([0.0, 2, 0, 1], summary.ProjectsByStatus.Select(p => p.Value));
        Assert.Equal(KpiScores.Indicators.Count, summary.TeamKpi.Count);
    }

    [Fact]
    public void Empty_data_gives_zeros_not_errors()
    {
        var summary = DashboardSummary.Create([], [], TestData.Today);

        Assert.Equal(0, summary.Headcount);
        Assert.Equal(0, summary.AverageKpi);
        Assert.Equal(0, summary.AverageSalary);
        Assert.Equal(0, summary.ActiveBudgetUsed);
        Assert.Empty(summary.TopPerformers);
        Assert.Empty(summary.HeadcountByPosition);
    }

    [Fact]
    public void Top_performers_are_limited_to_five()
    {
        var employees = Enumerable.Range(0, 8).Select(i => TestData.Employee($"E{i}", "X", kpi: 50 + i)).ToList();

        var summary = DashboardSummary.Create(employees, [], TestData.Today);

        Assert.Equal(5, summary.TopPerformers.Count);
        Assert.Equal(57, summary.TopPerformers[0].Score);
    }
}
