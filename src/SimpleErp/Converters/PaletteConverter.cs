using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SimpleErp.Core.Models;
using SimpleErp.Core.Services;

namespace SimpleErp.Converters;

/// <summary>
/// Picks the color for a status pill, a health badge, a KPI score or an avatar.
/// Pass <c>Soft</c> as the converter parameter for the light background tint instead of the strong color.
/// </summary>
public sealed class PaletteConverter : IValueConverter
{
    private static readonly (Color Strong, Color Soft) Green = (C("#047857"), C("#D1FAE5"));
    private static readonly (Color Strong, Color Soft) Amber = (C("#B45309"), C("#FEF3C7"));
    private static readonly (Color Strong, Color Soft) Red = (C("#B91C1C"), C("#FEE2E2"));
    private static readonly (Color Strong, Color Soft) Gray = (C("#4B5563"), C("#E5E7EB"));
    private static readonly (Color Strong, Color Soft) Blue = (C("#1D4ED8"), C("#DBEAFE"));
    private static readonly (Color Strong, Color Soft) Indigo = (C("#4338CA"), C("#E0E7FF"));

    private static readonly Brush[] AvatarBrushes =
    [
        Gradient("#6366F1", "#8B5CF6"),
        Gradient("#0EA5E9", "#6366F1"),
        Gradient("#10B981", "#0EA5E9"),
        Gradient("#F59E0B", "#EF4444"),
        Gradient("#EC4899", "#8B5CF6"),
        Gradient("#14B8A6", "#22C55E"),
        Gradient("#F97316", "#EC4899"),
        Gradient("#64748B", "#334155"),
    ];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int avatarIndex)
        {
            return AvatarBrushes[Math.Abs(avatarIndex) % AvatarBrushes.Length];
        }

        var pair = value switch
        {
            ProjectStatus.Planned => Blue,
            ProjectStatus.Active => Green,
            ProjectStatus.OnHold => Amber,
            ProjectStatus.Completed => Indigo,
            ProjectHealth.OnTrack => Green,
            ProjectHealth.AtRisk => Amber,
            ProjectHealth.OverBudget or ProjectHealth.Overdue => Red,
            ProjectHealth.NotStarted => Blue,
            ProjectHealth.Completed => Indigo,
            ProjectHealth.OnHold => Gray,
            double score => score switch
            {
                >= 85 => Green,
                >= 70 => Indigo,
                >= 55 => Blue,
                >= 40 => Amber,
                _ => Red,
            },
            _ => Gray,
        };

        var soft = string.Equals(parameter as string, "Soft", StringComparison.OrdinalIgnoreCase);
        var brush = new SolidColorBrush(soft ? pair.Soft : pair.Strong);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static Brush Gradient(string from, string to)
    {
        var brush = new LinearGradientBrush(C(from), C(to), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
        brush.Freeze();
        return brush;
    }
}
