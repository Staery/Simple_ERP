using SimpleErp.Core.Models;

namespace SimpleErp.Core.Services;

/// <summary>Human-readable names for the enums shown in the UI and in exported files.</summary>
public static class DisplayNames
{
    public static string For(Position position) => position switch
    {
        Position.Developer => "Developer",
        Position.Designer => "Designer",
        Position.QaEngineer => "QA engineer",
        Position.BusinessAnalyst => "Business analyst",
        Position.TeamLead => "Team lead",
        Position.ProjectManager => "Project manager",
        _ => position.ToString(),
    };

    public static string For(ProjectStatus status) => status switch
    {
        ProjectStatus.Planned => "Planned",
        ProjectStatus.Active => "Active",
        ProjectStatus.OnHold => "On hold",
        ProjectStatus.Completed => "Completed",
        _ => status.ToString(),
    };

    public static string For(KpiIndicator indicator) => indicator switch
    {
        KpiIndicator.Teamwork => "Teamwork",
        KpiIndicator.CodeEfficiency => "Code efficiency",
        KpiIndicator.DesignSkills => "Design skills",
        KpiIndicator.Leadership => "Leadership",
        KpiIndicator.ProjectSuccess => "Project success",
        _ => indicator.ToString(),
    };

    /// <summary>One-word indicator names for chart axes, where space is tight.</summary>
    public static string ShortFor(KpiIndicator indicator) => indicator switch
    {
        KpiIndicator.Teamwork => "Teamwork",
        KpiIndicator.CodeEfficiency => "Code",
        KpiIndicator.DesignSkills => "Design",
        KpiIndicator.Leadership => "Leadership",
        KpiIndicator.ProjectSuccess => "Success",
        _ => indicator.ToString(),
    };

    public static string For(ProjectHealth health) => health switch
    {
        ProjectHealth.NotStarted => "Not started",
        ProjectHealth.OnTrack => "On track",
        ProjectHealth.AtRisk => "At risk",
        ProjectHealth.OverBudget => "Over budget",
        ProjectHealth.Overdue => "Overdue",
        ProjectHealth.OnHold => "On hold",
        ProjectHealth.Completed => "Completed",
        _ => health.ToString(),
    };

    /// <summary>Display name for any enum value used in the app; other values fall back to <see cref="object.ToString"/>.</summary>
    public static string For(object? value) => value switch
    {
        Position position => For(position),
        ProjectStatus status => For(status),
        KpiIndicator indicator => For(indicator),
        ProjectHealth health => For(health),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty,
    };
}
