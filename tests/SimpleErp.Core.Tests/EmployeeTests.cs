using SimpleErp.Core.Models;
using SimpleErp.Core.Tests.Fakes;

namespace SimpleErp.Core.Tests;

public class EmployeeTests
{
    [Theory]
    [InlineData("1990-12-10", "2026-12-09", 35)]
    [InlineData("1990-12-10", "2026-12-10", 36)]
    [InlineData("2000-02-29", "2018-02-28", 17)]
    [InlineData("2000-02-29", "2018-03-01", 18)]
    [InlineData("2000-02-29", "2020-02-29", 20)]
    public void Age_is_counted_in_whole_years(string birth, string on, int expected)
    {
        var employee = new Employee { BirthDate = DateOnly.Parse(birth) };

        Assert.Equal(expected, employee.AgeOn(DateOnly.Parse(on)));
    }

    [Theory]
    [InlineData("ada", "lovelace", "AL")]
    [InlineData("  Grace ", "", "G")]
    [InlineData("", "", "?")]
    public void Initials_come_from_the_names(string first, string last, string expected) =>
        Assert.Equal(expected, new Employee { FirstName = first, LastName = last }.Initials);

    [Fact]
    public void Clone_copies_kpi_scores_deeply()
    {
        var original = TestData.Employee(kpi: 50);

        var copy = original.Clone();
        copy.Kpi.Teamwork = 99;
        copy.FirstName = "Changed";

        Assert.Equal(50, original.Kpi.Teamwork);
        Assert.Equal("Ada", original.FirstName);
        Assert.Equal(original.Id, copy.Id);
    }

    [Fact]
    public void Project_clone_copies_the_team_list()
    {
        var original = TestData.Project("P", Guid.NewGuid());

        var copy = original.Clone();
        copy.TeamIds.Add(Guid.NewGuid());

        Assert.Single(original.TeamIds);
    }
}
