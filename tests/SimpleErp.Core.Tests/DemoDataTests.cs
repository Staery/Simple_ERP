using SimpleErp.Core.Services;
using SimpleErp.Core.Tests.Fakes;
using SimpleErp.Core.Validation;

namespace SimpleErp.Core.Tests;

public class DemoDataTests
{
    private readonly Models.ErpData _data = DemoData.Create(TestData.Today);

    [Fact]
    public void Demo_data_is_reproducible()
    {
        var again = DemoData.Create(TestData.Today);

        Assert.Equal(_data.Employees.Select(e => (e.Id, e.FullName, e.MonthlySalary, e.Kpi.Teamwork)),
            again.Employees.Select(e => (e.Id, e.FullName, e.MonthlySalary, e.Kpi.Teamwork)));
    }

    [Fact]
    public void Every_demo_record_passes_validation()
    {
        Assert.All(_data.Employees, employee => Assert.True(EmployeeValidator.Validate(employee, TestData.Today).IsValid, employee.FullName));
        Assert.All(_data.Projects, project => Assert.True(ProjectValidator.Validate(project).IsValid, project.Name));
    }

    [Fact]
    public void Ids_are_unique_and_teams_reference_existing_employees()
    {
        var ids = _data.Employees.Select(e => e.Id).ToHashSet();

        Assert.Equal(_data.Employees.Count, ids.Count);
        Assert.Equal(_data.Projects.Count, _data.Projects.Select(p => p.Id).Distinct().Count());
        Assert.All(_data.Projects, project => Assert.All(project.TeamIds, id => Assert.Contains(id, ids)));
    }

    [Fact]
    public void Demo_projects_show_every_kind_of_health()
    {
        var health = _data.Projects.Select(p => ProjectAnalyzer.Analyze(p, TestData.Today).Health).ToHashSet();

        Assert.Equal(Enum.GetValues<ProjectHealth>().ToHashSet(), health);
    }

    [Fact]
    public void Demo_employees_cover_every_position() =>
        Assert.Equal(Enum.GetValues<Models.Position>().Length, _data.Employees.Select(e => e.Position).Distinct().Count());
}
