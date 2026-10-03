using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

/// <summary>A small avatar in a row of team members.</summary>
public sealed record TeamAvatar(string Initials, string FullName, int AvatarIndex);

/// <summary>A row of the project list.</summary>
public sealed class ProjectListItem : IListItem
{
    /// <summary>How many avatars a row shows before "+N".</summary>
    public const int MaxAvatars = 4;

    public ProjectListItem(Project project, IReadOnlyDictionary<Guid, Employee> employees, DateOnly today)
    {
        Id = project.Id;
        Name = project.Name;
        Client = project.Client;
        Status = project.Status;
        Progress = project.Progress;
        EndDate = project.EndDate;
        Budget = project.Budget;
        Insight = ProjectAnalyzer.Analyze(project, today);

        var team = project.TeamIds.Where(employees.ContainsKey).Select(id => employees[id]).ToList();
        TeamSize = team.Count;
        Team = team.Take(MaxAvatars).Select(e => new TeamAvatar(e.Initials, e.FullName, Avatar.ColorIndex(e.Id))).ToList();
        HiddenTeamCount = Math.Max(0, team.Count - MaxAvatars);
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Client { get; }

    public ProjectStatus Status { get; }

    public string StatusName => DisplayNames.For(Status);

    public int Progress { get; }

    public DateOnly EndDate { get; }

    public decimal Budget { get; }

    public ProjectInsight Insight { get; }

    public int TeamSize { get; }

    public IReadOnlyList<TeamAvatar> Team { get; }

    public int HiddenTeamCount { get; }

    public bool HasHiddenTeam => HiddenTeamCount > 0;
}
