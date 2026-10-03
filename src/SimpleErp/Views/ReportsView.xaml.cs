using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleErp.Views;

public partial class ReportsView : UserControl
{
    public ReportsView() => InitializeComponent();

    /// <summary>
    /// The portfolio table is as tall as its rows, so it never scrolls itself. Pass the mouse wheel
    /// on to the page instead of letting the table swallow it.
    /// </summary>
    private void OnPortfolioMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not DependencyObject element || VisualTreeHelper.GetParent(element) is not UIElement parent)
        {
            return;
        }

        e.Handled = true;
        parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = MouseWheelEvent, Source = sender });
    }
}
