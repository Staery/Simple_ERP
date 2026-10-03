using SimpleErp.Core.Models;
using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;

namespace SimpleErp.Core.Tests;

public class ProjectAnalyzerTests
{
    private static readonly DateOnly Today = TestData.Today;

    [Fact]
    public void Project_on_schedule_and_budget_is_on_track()
    {
        var insight = ProjectAnalyzer.Analyze(TestData.Project(), Today);

        Assert.Equal(ProjectHealth.OnTrack, insight.Health);
        Assert.Equal(0.5, insight.ScheduleElapsed, 3);
        Assert.Equal(0.4, insight.BudgetUsed, 3);
        Assert.Equal(60_000m, insight.BudgetRemaining);
        Assert.Equal(50, insight.DaysLeft);
        Assert.False(insight.NeedsAttention);
    }

    [Fact]
    public void Progress_far_behind_the_schedule_is_at_risk()
    {
        var project = TestData.Project();
        project.Progress = 30; // 50% of the time has passed

        Assert.Equal(ProjectHealth.AtRisk, ProjectAnalyzer.Analyze(project, Today).Health);
    }

    [Fact]
    public void Spending_far_ahead_of_progress_is_at_risk()
    {
        var project = TestData.Project();
        project.Spent = 80_000m; // 80% spent for 50% progress

        Assert.Equal(ProjectHealth.AtRisk, ProjectAnalyzer.Analyze(project, Today).Health);
    }

    [Fact]
    public void Spending_more_than_the_budget_is_over_budget()
    {
        var project = TestData.Project();
        project.Spent = 100_001m;

        var insight = ProjectAnalyzer.Analyze(project, Today);

        Assert.Equal(ProjectHealth.OverBudget, insight.Health);
        Assert.True(insight.NeedsAttention);
        Assert.True(insight.BudgetUsed > 1);
    }

    [Fact]
    public void Unfinished_project_past_its_end_date_is_overdue()
    {
        var project = TestData.Project();
        project.EndDate = Today.AddDays(-3);

        var insight = ProjectAnalyzer.Analyze(project, Today);

        Assert.Equal(ProjectHealth.Overdue, insight.Health);
        Assert.Equal(1.0, insight.ScheduleElapsed);
        Assert.Equal("3 days overdue", insight.DaysLeftText);
    }

    [Theory]
    [InlineData(ProjectStatus.Completed, ProjectHealth.Completed)]
    [InlineData(ProjectStatus.OnHold, ProjectHealth.OnHold)]
    [InlineData(ProjectStatus.Planned, ProjectHealth.NotStarted)]
    public void Status_decides_health_of_inactive_projects(ProjectStatus status, ProjectHealth expected)
    {
        var project = TestData.Project();
        project.Status = status;
        project.Spent = 500_000m;

        Assert.Equal(expected, ProjectAnalyzer.Analyze(project, Today).Health);
    }

    [Fact]
    public void Completed_project_is_never_overdue()
    {
        var project = TestData.Project();
        project.Status = ProjectStatus.Completed;
        project.EndDate = Today.AddDays(-30);

        Assert.Equal(ProjectHealth.Completed, ProjectAnalyzer.Analyze(project, Today).Health);
    }

    [Fact]
    public void Schedule_is_zero_before_the_start_and_handles_one_day_projects()
    {
        var future = TestData.Project();
        future.StartDate = Today.AddDays(10);
        future.EndDate = Today.AddDays(20);

        var oneDay = TestData.Project();
        oneDay.StartDate = Today;
        oneDay.EndDate = Today;

        Assert.Equal(0, ProjectAnalyzer.Analyze(future, Today).ScheduleElapsed);
        Assert.Equal(1, ProjectAnalyzer.Analyze(oneDay, Today).ScheduleElapsed);
        Assert.Equal("Due today", ProjectAnalyzer.Analyze(oneDay, Today).DaysLeftText);
    }

    [Fact]
    public void Project_without_budget_reports_zero_usage()
    {
        var project = TestData.Project();
        project.Budget = 0;
        project.Spent = 1_000m;

        var insight = ProjectAnalyzer.Analyze(project, Today);

        Assert.Equal(0, insight.BudgetUsed);
        Assert.Equal(ProjectHealth.OnTrack, insight.Health);
    }

    [Fact]
    public void Monthly_team_cost_adds_up_assigned_salaries_only()
    {
        var a = TestData.Employee(salary: 5_000m);
        var b = TestData.Employee(salary: 7_500m);
        var outsider = TestData.Employee(salary: 9_000m);
        var project = TestData.Project("P", a.Id, b.Id, Guid.NewGuid());

        Assert.Equal(12_500m, ProjectAnalyzer.MonthlyTeamCost(project, [a, b, outsider]));
    }
}
