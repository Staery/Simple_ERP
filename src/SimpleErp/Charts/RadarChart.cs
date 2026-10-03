using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SimpleErp.Charts;

/// <summary>
/// Spider chart for the KPI indicators: one axis per point, values from 0 to <see cref="Maximum"/>.
/// An optional second series (<see cref="ComparisonPoints"/>, e.g. the team average) is drawn as a dashed outline.
/// </summary>
public sealed class RadarChart : ChartBase
{
    public static readonly DependencyProperty ComparisonPointsProperty = DependencyProperty.Register(
        nameof(ComparisonPoints), typeof(IEnumerable), typeof(RadarChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RadarChart),
        new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(RadarChart),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x40, 0x4F, 0x46, 0xE5)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(RadarChart),
        new FrameworkPropertyMetadata(Brushes.SlateBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ComparisonStrokeProperty = DependencyProperty.Register(
        nameof(ComparisonStroke), typeof(Brush), typeof(RadarChart),
        new FrameworkPropertyMetadata(Brushes.Orange, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? ComparisonPoints
    {
        get => (IEnumerable?)GetValue(ComparisonPointsProperty);
        set => SetValue(ComparisonPointsProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush ComparisonStroke
    {
        get => (Brush)GetValue(ComparisonStrokeProperty);
        set => SetValue(ComparisonStrokeProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var data = Data;
        var count = data.Count;
        if (count < 3 || ActualWidth < 40 || ActualHeight < 40)
        {
            return;
        }

        var labels = data.Select(point => Text(point.Label, 11.5, LabelBrush)).ToList();
        var values = data.Select(point => Text(point.Value.ToString("0", CultureInfo.CurrentCulture), 12, ValueBrush, FontWeights.SemiBold)).ToList();
        var labelWidth = labels.Max(label => label.Width);
        var labelHeight = labels[0].Height + values[0].Height;

        // Fit the polygon and its labels: the vertical extent depends on where the vertices fall (a pentagon is
        // taller above the centre than below), the horizontal one on the widest label.
        var angles = Enumerable.Range(0, count).Select(i => (-90 + (i * 360.0 / count)) * Math.PI / 180).ToList();
        var top = -angles.Min(Math.Sin);
        var bottom = angles.Max(Math.Sin);
        var side = Math.Max(0.1, angles.Max(a => Math.Abs(Math.Cos(a))));
        var verticalRoom = ActualHeight - (2 * labelHeight) - 16;
        var horizontalRoom = (ActualWidth / 2) - labelWidth - 12;
        var radius = Math.Max(10, Math.Min(verticalRoom / (top + bottom), horizontalRoom / side));
        var center = new Point(ActualWidth / 2, labelHeight + 10 + (top * radius) + ((verticalRoom - ((top + bottom) * radius)) / 2));
        var maximum = Maximum > 0 ? Maximum : 100;

        Point At(int index, double fraction)
        {
            var angle = angles[index];
            return new Point(center.X + (Math.Cos(angle) * radius * fraction), center.Y + (Math.Sin(angle) * radius * fraction));
        }

        // Grid: rings at 25 % steps and one spoke per axis.
        var gridPen = MakePen(GridBrush, 1);
        foreach (var ring in new[] { 0.25, 0.5, 0.75, 1.0 })
        {
            dc.DrawGeometry(null, gridPen, Polygon(Enumerable.Range(0, count).Select(i => At(i, ring))));
        }

        for (var i = 0; i < count; i++)
        {
            dc.DrawLine(gridPen, center, At(i, 1));
        }

        // Comparison series (dashed outline).
        var comparison = ToList(ComparisonPoints);
        if (comparison.Count == count)
        {
            dc.DrawGeometry(null, MakePen(ComparisonStroke, 1.6, dashed: true),
                Polygon(comparison.Select((point, i) => At(i, Fraction(point.Value, maximum)))));
        }

        // Main series.
        var vertices = data.Select((point, i) => At(i, Fraction(point.Value, maximum))).ToList();
        dc.DrawGeometry(Fill, MakePen(Stroke, 2), Polygon(vertices));
        var dotPen = MakePen(Stroke, 2);
        foreach (var vertex in vertices)
        {
            dc.DrawEllipse(Brushes.White, dotPen, vertex, 3.5, 3.5);
        }

        // Labels outside the outer ring: name, then value.
        for (var i = 0; i < count; i++)
        {
            var anchor = At(i, 1);
            var (cos, sin) = (Math.Cos(angles[i]), Math.Sin(angles[i]));
            var label = labels[i];
            var value = values[i];
            var width = Math.Max(label.Width, value.Width);
            var height = label.Height + value.Height;

            var x = cos > 0.3 ? anchor.X + 10 : cos < -0.3 ? anchor.X - 10 - width : anchor.X - (width / 2);
            var y = sin < -0.3 ? anchor.Y - 8 - height : sin > 0.3 ? anchor.Y + 6 : anchor.Y - (height / 2);
            var align = cos > 0.3 ? 0 : cos < -0.3 ? 1 : 0.5;

            dc.DrawText(label, new Point(x + ((width - label.Width) * align), y));
            dc.DrawText(value, new Point(x + ((width - value.Width) * align), y + label.Height));
        }
    }

    private static double Fraction(double value, double maximum) => Math.Clamp(value / maximum, 0, 1);

    private static StreamGeometry Polygon(IEnumerable<Point> points)
    {
        var list = points.ToList();
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(list[0], isFilled: true, isClosed: true);
            context.PolyLineTo(list.Skip(1).ToList(), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        return geometry;
    }
}
