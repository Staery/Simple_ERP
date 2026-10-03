using System.Text.Json.Serialization;

namespace SimpleErp.Core.Models;

public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public Position Position { get; set; }

    public DateOnly BirthDate { get; set; }

    public DateOnly HireDate { get; set; }

    public decimal MonthlySalary { get; set; }

    public KpiScores Kpi { get; set; } = new();

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [JsonIgnore]
    public string Initials => PersonName.Initials(FirstName, LastName);

    /// <summary>Age in whole years on <paramref name="date"/>. The age is derived from the birth date, never stored.</summary>
    public int AgeOn(DateOnly date) => Dates.WholeYearsBetween(BirthDate, date);

    public Employee Clone()
    {
        var copy = (Employee)MemberwiseClone();
        copy.Kpi = Kpi.Clone();
        return copy;
    }
}
