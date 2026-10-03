using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SimpleErp.Charts;

/// <summary>Horizontal bar chart: one labelled row per point, with the value at the end of the row.</summary>
public sealed class BarChart : ChartBase
{
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(BarChart),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
        nameof(BarBrush), typeof(Brush), typeof(BarChart),
        new FrameworkPropertyMetadata(Brushes.SlateBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(BarChart),
        new FrameworkPropertyMetadata(Brushes.WhiteSmoke, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(BarChart),
        new FrameworkPropertyMetadata("0", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth), typeof(double), typeof(BarChart),
        new FrameworkPropertyMetadata(120.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarThicknessProperty = DependencyProperty.Register(
        nameof(BarThickness), typeof(double), typeof(BarChart),
        new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The value of a full bar. <see cref="double.NaN"/> (the default) uses the largest value.</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public Brush BarBrush
    {
        get => (Brush)GetValue(BarBrushProperty);
        set => SetValue(BarBrushProperty, value);
    }

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    /// <summary>.NET format string for the values, e.g. <c>0</c> or <c>0.0</c>.</summary>
    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public double LabelWidth
    {
        get => (double)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public double BarThickness
    {
        get => (double)GetValue(BarThicknessProperty);
        set => SetValue(BarThicknessProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Ask for comfortable rows when the height is not constrained (e.g. inside a StackPanel).
        var rows = Math.Max(1, Data.Count);
        return new Size(double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width, Math.Min(availableSize.Height, rows * 30));
    }

    protected override void OnRender(DrawingContext dc)
    {
        var data = Data;
        if (data.Count == 0 || ActualWidth < 60)
        {
            return;
        }

        var maximum = double.IsNaN(Maximum) || Maximum <= 0 ? Math.Max(1, data.Max(point => point.Value)) : Maximum;
        var values = data.Select(point => Text(point.Value.ToString(ValueFormat, CultureInfo.CurrentCulture), 12, ValueBrush, FontWeights.SemiBold)).ToList();
        var valueWidth = values.Max(value => value.Width) + 10;
        var trackLeft = LabelWidth;
        var trackWidth = Math.Max(10, ActualWidth - trackLeft - valueWidth);
        var rowHeight = ActualHeight / data.Count;
        var thickness = Math.Min(BarThickness, rowHeight * 0.6);
        var corner = thickness / 2;

        for (var i = 0; i < data.Count; i++)
        {
            var middle = (i * rowHeight) + (rowHeight / 2);

            var label = Text(data[i].Label, 12, LabelBrush);
            label.MaxTextWidth = Math.Max(1, LabelWidth - 10);
            label.MaxLineCount = 1;
            label.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(label, new Point(0, middle - (label.Height / 2)));

            var track = new Rect(trackLeft, middle - (thickness / 2), trackWidth, thickness);
            dc.DrawRoundedRectangle(TrackBrush, null, track, corner, corner);

            var length = trackWidth * Math.Clamp(data[i].Value / maximum, 0, 1);
            if (length > 0.5)
            {
                var bar = new Rect(trackLeft, track.Top, Math.Max(length, thickness), thickness);
                dc.DrawRoundedRectangle(BarBrush, null, bar, corner, corner);
            }

            dc.DrawText(values[i], new Point(ActualWidth - values[i].Width, middle - (values[i].Height / 2)));
        }
    }
}
