using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

/// <summary>A row of the employee list.</summary>
public sealed class EmployeeListItem(Employee employee, EmployeeRanking ranking) : IListItem
{
    public Guid Id { get; } = employee.Id;

    public string FullName { get; } = employee.FullName;

    public string Initials { get; } = employee.Initials;

    public int AvatarIndex { get; } = Avatar.ColorIndex(employee.Id);

    public Position Position { get; } = employee.Position;

    public string PositionName { get; } = DisplayNames.For(employee.Position);

    public string City { get; } = employee.City;

    public decimal MonthlySalary { get; } = employee.MonthlySalary;

    public DateOnly HireDate { get; } = employee.HireDate;

    public double Score { get; } = ranking.Score;

    public int Rank { get; } = ranking.Rank;

    public string Grade { get; } = ranking.Grade;
}
