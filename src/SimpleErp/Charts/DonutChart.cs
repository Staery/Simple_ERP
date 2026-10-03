using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SimpleErp.Charts;

/// <summary>Donut chart with the total in the middle and a legend on the right.</summary>
public sealed class DonutChart : ChartBase
{
    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette), typeof(IList), typeof(DonutChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(DonutChart),
        new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(DonutChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Brush[] DefaultPalette =
    [
        Frozen("#6366F1"), Frozen("#10B981"), Frozen("#F59E0B"), Frozen("#0EA5E9"), Frozen("#EC4899"), Frozen("#94A3B8"),
    ];

    /// <summary>Brushes for the segments, in point order; repeats when there are more points.</summary>
    public IList? Palette
    {
        get => (IList?)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <summary>Small text under the total in the middle.</summary>
    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var data = Data;
        if (data.Count == 0 || ActualHeight < 40)
        {
            return;
        }

        var palette = Palette is { Count: > 0 } custom ? custom.OfType<Brush>().ToList() : DefaultPalette.ToList();
        Brush BrushAt(int index) => palette[index % palette.Count];

        var size = Math.Min(ActualHeight, ActualWidth * 0.48);
        var radius = (size - Thickness) / 2;
        var center = new Point(size / 2, ActualHeight / 2);
        var total = data.Sum(point => Math.Max(0, point.Value));

        dc.DrawEllipse(null, MakePen(GridBrush, Thickness), center, radius, radius);

        if (total > 0)
        {
            var nonZero = data.Count(point => point.Value > 0);
            var gap = nonZero > 1 ? 2.5 : 0;
            var start = -90.0;
            for (var i = 0; i < data.Count; i++)
            {
                var sweep = Math.Max(0, data[i].Value) / total * 360;
                if (sweep <= 0)
                {
                    continue;
                }

                var pen = new Pen(BrushAt(i), Thickness) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
                if (pen.CanFreeze)
                {
                    pen.Freeze();
                }

                dc.DrawGeometry(null, pen, Arc(center, radius, start + (gap / 2), Math.Max(0.5, sweep - gap)));
                start += sweep;
            }
        }

        var totalText = Text(total.ToString("0", CultureInfo.CurrentCulture), 22, ValueBrush, FontWeights.Bold);
        var caption = Text(Caption, 11, LabelBrush);
        var textTop = center.Y - ((totalText.Height + caption.Height) / 2);
        dc.DrawText(totalText, new Point(center.X - (totalText.Width / 2), textTop));
        dc.DrawText(caption, new Point(center.X - (caption.Width / 2), textTop + totalText.Height));

        // Legend
        var left = size + 24;
        var rowHeight = 24.0;
        var top = (ActualHeight - (rowHeight * data.Count)) / 2;
        for (var i = 0; i < data.Count; i++)
        {
            var middle = top + (i * rowHeight) + (rowHeight / 2);
            dc.DrawEllipse(BrushAt(i), null, new Point(left + 4, middle), 4.5, 4.5);

            var label = Text(data[i].Label, 12.5, LabelBrush);
            dc.DrawText(label, new Point(left + 16, middle - (label.Height / 2)));

            var value = Text(data[i].Value.ToString("0", CultureInfo.CurrentCulture), 12.5, ValueBrush, FontWeights.SemiBold);
            dc.DrawText(value, new Point(Math.Max(left + 20 + label.Width, ActualWidth - value.Width), middle - (value.Height / 2)));
        }
    }

    private static Geometry Arc(Point center, double radius, double startDegrees, double sweepDegrees)
    {
        Point At(double degrees)
        {
            var radians = degrees * Math.PI / 180;
            return new Point(center.X + (radius * Math.Cos(radians)), center.Y + (radius * Math.Sin(radians)));
        }

        // A full circle cannot be drawn with one arc segment.
        sweepDegrees = Math.Min(sweepDegrees, 359.99);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(At(startDegrees), isFilled: false, isClosed: false);
            context.ArcTo(At(startDegrees + sweepDegrees), new Size(radius, radius), 0, sweepDegrees > 180, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
