using SimpleErp.Core.Models;
using SimpleErp.Core.Tests.Fakes;
using SimpleErp.Core.Validation;

namespace SimpleErp.Core.Tests;

public class EmployeeValidatorTests
{
    private static ValidationErrors Validate(Action<Employee> change)
    {
        var employee = TestData.Employee();
        change(employee);
        return EmployeeValidator.Validate(employee, TestData.Today);
    }

    [Fact]
    public void Valid_employee_has_no_errors() => Assert.True(Validate(_ => { }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Names_are_required(string name)
    {
        var errors = Validate(e => { e.FirstName = name; e.LastName = name; });

        Assert.Equal("First name is required.", errors[nameof(Employee.FirstName)]);
        Assert.Equal("Last name is required.", errors[nameof(Employee.LastName)]);
    }

    [Fact]
    public void Names_cannot_contain_digits_or_be_too_long()
    {
        var errors = Validate(e => { e.FirstName = "R2D2"; e.LastName = new string('x', 51); });

        Assert.Contains("digits", errors[nameof(Employee.FirstName)]);
        Assert.Contains("at most 50", errors[nameof(Employee.LastName)]);
    }

    [Theory]
    [InlineData("no-at-sign.com")]
    [InlineData("two words@example.com")]
    [InlineData("name@domain")]
    public void Malformed_email_is_rejected(string email) => Assert.True(Validate(e => e.Email = email).Contains(nameof(Employee.Email)));

    [Fact]
    public void Email_is_optional() => Assert.True(Validate(e => e.Email = "").IsValid);

    [Fact]
    public void Employee_must_be_at_least_16()
    {
        var errors = Validate(e =>
        {
            e.BirthDate = TestData.Today.AddYears(-15);
            e.HireDate = TestData.Today;
        });

        Assert.Contains("at least 16", errors[nameof(Employee.BirthDate)]);
        Assert.False(errors.Contains(nameof(Employee.HireDate)), "The hire date rule depends on a valid birth date.");
    }

    [Fact]
    public void Implausible_age_is_rejected() =>
        Assert.Contains("age would be", Validate(e => e.BirthDate = new DateOnly(1900, 1, 1))[nameof(Employee.BirthDate)]);

    [Fact]
    public void Hire_date_cannot_be_in_the_future() =>
        Assert.Contains("future", Validate(e => e.HireDate = TestData.Today.AddDays(1))[nameof(Employee.HireDate)]);

    [Fact]
    public void Hire_date_must_be_after_the_16th_birthday() =>
        Assert.Contains("16th birthday", Validate(e => e.HireDate = e.BirthDate.AddYears(10))[nameof(Employee.HireDate)]);

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(1_000_001)]
    public void Salary_must_be_positive_and_plausible(decimal salary) =>
        Assert.True(Validate(e => e.MonthlySalary = salary).Contains(nameof(Employee.MonthlySalary)));

    [Fact]
    public void Kpi_scores_must_be_between_0_and_100()
    {
        var errors = Validate(e => { e.Kpi.Leadership = 101; e.Kpi.Teamwork = -1; });

        Assert.True(errors.Contains(nameof(KpiScores.Leadership)));
        Assert.True(errors.Contains(nameof(KpiScores.Teamwork)));
        Assert.Equal(2, errors.Count);
    }
}

public class ProjectValidatorTests
{
    private static ValidationErrors Validate(Action<Project> change)
    {
        var project = TestData.Project();
        change(project);
        return ProjectValidator.Validate(project);
    }

    [Fact]
    public void Valid_project_has_no_errors() => Assert.True(Validate(_ => { }).IsValid);

    [Fact]
    public void Name_and_client_are_required()
    {
        var errors = Validate(p => { p.Name = " "; p.Client = ""; });

        Assert.Equal("Project name is required.", errors[nameof(Project.Name)]);
        Assert.Equal("Client is required.", errors[nameof(Project.Client)]);
    }

    [Fact]
    public void End_date_cannot_precede_start_date() =>
        Assert.True(Validate(p => p.EndDate = p.StartDate.AddDays(-1)).Contains(nameof(Project.EndDate)));

    [Fact]
    public void One_day_project_is_valid() => Assert.True(Validate(p => p.EndDate = p.StartDate).IsValid);

    [Fact]
    public void Money_cannot_be_negative()
    {
        var errors = Validate(p => { p.Budget = -1; p.Spent = -1; });

        Assert.True(errors.Contains(nameof(Project.Budget)));
        Assert.True(errors.Contains(nameof(Project.Spent)));
    }

    [Fact]
    public void Spending_over_budget_is_allowed_and_reported_elsewhere() => Assert.True(Validate(p => p.Spent = p.Budget * 2).IsValid);

    [Theory]
    [InlineData(-5)]
    [InlineData(101)]
    public void Progress_must_be_a_percentage(int progress) =>
        Assert.True(Validate(p => p.Progress = progress).Contains(nameof(Project.Progress)));

    [Fact]
    public void Completed_project_must_be_100_percent_done() =>
        Assert.Contains("100%", Validate(p => { p.Status = ProjectStatus.Completed; p.Progress = 90; })[nameof(Project.Progress)]);
}
