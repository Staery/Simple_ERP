using CommunityToolkit.Mvvm.ComponentModel;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

/// <summary>An employee in the project editor's team picker.</summary>
public sealed partial class TeamMemberOption : ObservableObject
{
    public TeamMemberOption(Employee employee, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(employee);

        Id = employee.Id;
        FullName = employee.FullName;
        Initials = employee.Initials;
        PositionName = DisplayNames.For(employee.Position);
        MonthlySalary = employee.MonthlySalary;
        AvatarIndex = Avatar.ColorIndex(employee.Id);
        _isSelected = isSelected;
    }

    public Guid Id { get; }

    public string FullName { get; }

    public string Initials { get; }

    public string PositionName { get; }

    public decimal MonthlySalary { get; }

    public int AvatarIndex { get; }

    [ObservableProperty]
    private bool _isSelected;
}
