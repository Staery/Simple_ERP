namespace SimpleErp.Core.Models;

/// <summary>Everything the application stores: the root object of the data file.</summary>
public sealed class ErpData
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<Employee> Employees { get; set; } = [];

    public List<Project> Projects { get; set; } = [];

    public ErpData Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Employees = Employees.Select(employee => employee.Clone()).ToList(),
        Projects = Projects.Select(project => project.Clone()).ToList(),
    };
}
