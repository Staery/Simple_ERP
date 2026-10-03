using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

public enum ProjectHealth
{
    NotStarted,
    OnTrack,
    AtRisk,
    OverBudget,
    Overdue,
    OnHold,
    Completed,
}

/// <summary>Schedule and budget figures of a project on a given day.</summary>
/// <param name="ScheduleElapsed">Share of the planned duration that has passed, 0–1.</param>
/// <param name="BudgetUsed">Spent divided by budget; above 1 when over budget, 0 when there is no budget.</param>
/// <param name="DaysLeft">Days until the end date; negative once the end date has passed.</param>
public sealed record ProjectInsight(
    ProjectHealth Health,
    double ScheduleElapsed,
    double BudgetUsed,
    decimal BudgetRemaining,
    int DaysLeft)
{
    public string HealthText => DisplayNames.For(Health);

    public bool NeedsAttention => Health is ProjectHealth.AtRisk or ProjectHealth.OverBudget or ProjectHealth.Overdue;

    public string DaysLeftText => DaysLeft switch
    {
        0 => "Due today",
        1 => "1 day left",
        > 1 => $"{DaysLeft} days left",
        -1 => "1 day overdue",
        _ => $"{-DaysLeft} days overdue",
    };
}

public static class ProjectAnalyzer
{
    /// <summary>How far progress may fall behind the elapsed schedule before a project is "at risk".</summary>
    public const double ScheduleTolerance = 0.15;

    /// <summary>How far spending may run ahead of progress before a project is "at risk".</summary>
    public const double SpendingTolerance = 0.20;

    public static ProjectInsight Analyze(Project project, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(project);

        var totalDays = project.EndDate.DayNumber - project.StartDate.DayNumber;
        var elapsedDays = today.DayNumber - project.StartDate.DayNumber;
        var scheduleElapsed = totalDays <= 0
            ? (today >= project.EndDate ? 1.0 : 0.0)
            : Math.Clamp(elapsedDays / (double)totalDays, 0.0, 1.0);

        var budgetUsed = project.Budget > 0 ? (double)(project.Spent / project.Budget) : 0.0;
        var daysLeft = project.EndDate.DayNumber - today.DayNumber;
        var progress = Math.Clamp(project.Progress, 0, 100) / 100.0;

        var health = project.Status switch
        {
            ProjectStatus.Completed => ProjectHealth.Completed,
            ProjectStatus.OnHold => ProjectHealth.OnHold,
            _ when daysLeft < 0 => ProjectHealth.Overdue,
            ProjectStatus.Planned => ProjectHealth.NotStarted,
            _ when project.Budget > 0 && project.Spent > project.Budget => ProjectHealth.OverBudget,
            _ when progress + ScheduleTolerance < scheduleElapsed => ProjectHealth.AtRisk,
            _ when project.Budget > 0 && budgetUsed > progress + SpendingTolerance => ProjectHealth.AtRisk,
            _ => ProjectHealth.OnTrack,
        };

        return new ProjectInsight(health, scheduleElapsed, budgetUsed, project.Budget - project.Spent, daysLeft);
    }

    /// <summary>Monthly salary cost of the employees assigned to <paramref name="project"/>.</summary>
    public static decimal MonthlyTeamCost(Project project, IEnumerable<Employee> employees)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(employees);

        var team = project.TeamIds.ToHashSet();
        return employees.Where(employee => team.Contains(employee.Id)).Sum(employee => employee.MonthlySalary);
    }
}
