namespace SimpleErp.Core.Models;

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Client { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal Budget { get; set; }

    /// <summary>Money spent so far. It may exceed <see cref="Budget"/>; that is reported as over budget.</summary>
    public decimal Spent { get; set; }

    /// <summary>Completion in percent, 0–100.</summary>
    public int Progress { get; set; }

    /// <summary>Ids of the employees assigned to the project.</summary>
    public List<Guid> TeamIds { get; set; } = [];

    public Project Clone()
    {
        var copy = (Project)MemberwiseClone();
        copy.TeamIds = [.. TeamIds];
        return copy;
    }
}
