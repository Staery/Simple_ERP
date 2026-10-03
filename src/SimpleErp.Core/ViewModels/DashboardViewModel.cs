using CommunityToolkit.Mvvm.ComponentModel;
using SimpleErp.Core.Services;

namespace SimpleErp.Core.ViewModels;

/// <summary>The Dashboard page: key figures and charts, recalculated every time the page is shown.</summary>
public sealed partial class DashboardViewModel(ErpStore store) : PageViewModel
{
    [ObservableProperty]
    private DashboardSummary _summary = DashboardSummary.Empty;

    /// <summary>The five best employees, as list rows (with avatar colors).</summary>
    public IReadOnlyList<EmployeeListItem> TopPerformers =>
        Summary.TopPerformers.Select(ranking => new EmployeeListItem(ranking.Employee, ranking)).ToList();

    public override string Title => "Dashboard";

    public override string Subtitle => $"Company overview · {store.Today:dddd, d MMMM yyyy}";

    public override void OnActivated() => Refresh();

    public void Refresh()
    {
        Summary = DashboardSummary.Create(store.Employees.ToList(), store.Projects.ToList(), store.Today);
        OnPropertyChanged(nameof(TopPerformers));
        OnPropertyChanged(nameof(Subtitle));
    }
}
