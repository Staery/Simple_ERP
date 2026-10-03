using System.Text.RegularExpressions;
using SimpleErp.Core.Models;

namespace SimpleErp.Core.Validation;

public static partial class EmployeeValidator
{
    public const int MaxNameLength = 50;
    public const int MaxCityLength = 60;
    public const int MinAge = 16;
    public const int MaxAge = 80;
    public const decimal MaxMonthlySalary = 1_000_000m;

    public static ValidationErrors Validate(Employee employee, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var errors = new ValidationErrors();

        CheckName(errors, nameof(Employee.FirstName), employee.FirstName, "First name");
        CheckName(errors, nameof(Employee.LastName), employee.LastName, "Last name");

        if (!string.IsNullOrWhiteSpace(employee.Email) && !EmailPattern().IsMatch(employee.Email.Trim()))
        {
            errors.Add(nameof(Employee.Email), "Enter an email address like name@company.com.");
        }

        if (employee.City?.Trim().Length > MaxCityLength)
        {
            errors.Add(nameof(Employee.City), $"City must be at most {MaxCityLength} characters.");
        }

        var age = employee.AgeOn(today);
        if (employee.BirthDate > today || age < MinAge)
        {
            errors.Add(nameof(Employee.BirthDate), $"Employees must be at least {MinAge} years old.");
        }
        else if (age > MaxAge)
        {
            errors.Add(nameof(Employee.BirthDate), $"Check the birth date: the age would be {age}.");
        }

        if (employee.HireDate > today)
        {
            errors.Add(nameof(Employee.HireDate), "The hire date cannot be in the future.");
        }
        else if (!errors.Contains(nameof(Employee.BirthDate)) && employee.HireDate < employee.BirthDate.AddYears(MinAge))
        {
            errors.Add(nameof(Employee.HireDate), $"The hire date must be after the employee's {MinAge}th birthday.");
        }

        if (employee.MonthlySalary <= 0)
        {
            errors.Add(nameof(Employee.MonthlySalary), "Salary must be greater than zero.");
        }
        else if (employee.MonthlySalary > MaxMonthlySalary)
        {
            errors.Add(nameof(Employee.MonthlySalary), $"Salary cannot exceed {MaxMonthlySalary:N0} a month.");
        }

        foreach (var indicator in KpiScores.Indicators)
        {
            var value = employee.Kpi.Get(indicator);
            if (value is < KpiScores.MinScore or > KpiScores.MaxScore)
            {
                errors.Add(indicator.ToString(), $"Scores must be between {KpiScores.MinScore} and {KpiScores.MaxScore}.");
            }
        }

        return errors;
    }

    private static void CheckName(ValidationErrors errors, string property, string? value, string label)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            errors.Add(property, $"{label} is required.");
        }
        else if (trimmed.Length > MaxNameLength)
        {
            errors.Add(property, $"{label} must be at most {MaxNameLength} characters.");
        }
        else if (trimmed.Any(char.IsDigit))
        {
            errors.Add(property, $"{label} cannot contain digits.");
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
