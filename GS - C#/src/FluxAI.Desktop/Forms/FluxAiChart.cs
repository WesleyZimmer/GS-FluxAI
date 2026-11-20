using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace FluxAI.Desktop.Forms;

internal enum FluxAiChartKind
{
    Line,
    Bar
}

internal sealed record FluxAiChartPoint(string Label, double Value);

/// <summary>A small dependency-free chart for metrics returned by the FluxAI API.</summary>
internal sealed class FluxAiChart : Control
{
    private IReadOnlyList<FluxAiChartPoint> _points = [];

    public FluxAiChart(string title, FluxAiChartKind kind = FluxAiChartKind.Line)
    {
        Title = title;
        Kind = kind;
        DoubleBuffered = true;
        MinimumSize = new Size(360, 220);
        BackColor = Color.White;
        ForeColor = FluxAiTheme.Ink;
        Font = new Font("Segoe UI", 8.5F);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyMessage { get; set; } = "Ainda não há dados suficientes para exibir este gráfico.";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Subtitle { get; set; }
    public FluxAiChartKind Kind { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<FluxAiChartPoint> Points
    {
        get => _points;
        set
        {
            _points = value.Where(point => double.IsFinite(point.Value)).ToArray();
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        using var titleFont = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        using var titleBrush = new SolidBrush(FluxAiTheme.Ink);
        using var mutedBrush = new SolidBrush(FluxAiTheme.Muted);
        graphics.DrawString(Title, titleFont, titleBrush, new PointF(18, 14));
        var plotTop = 54f;
        if (!string.IsNullOrWhiteSpace(Subtitle))
        {
            graphics.DrawString(Subtitle, Font, mutedBrush, new PointF(18, 37));
            plotTop = 72f;
        }

        if (_points.Count == 0)
        {
            var empty = TextRenderer.MeasureText(EmptyMessage, Font, new Size(Math.Max(120, Width - 48), 80), TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(graphics, EmptyMessage, Font, new Rectangle(20, Height / 2 - empty.Height / 2, Width - 40, empty.Height), FluxAiTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            return;
        }

        var left = 68f;
        var right = Math.Max(left + 50, Width - 22f);
        var bottom = Math.Max(plotTop + 80, Height - 44f);
        var top = plotTop;
        var values = _points.Select(point => point.Value).ToArray();
        var integerBarValues = Kind == FluxAiChartKind.Bar &&
            values.All(value => value >= 0 && Math.Abs(value - Math.Round(value)) < 1e-9);
        var min = integerBarValues ? 0 : Math.Min(0, values.Min());
        var max = values.Max();
        if (integerBarValues)
        {
            var tickStep = Math.Max(1, Math.Ceiling(max / 4));
            max = tickStep * 4;
        }
        else if (Math.Abs(max - min) < 1e-9)
        {
            var padding = Math.Max(Math.Abs(max) * .15, 1);
            min -= padding;
            max += padding;
        }
        else
        {
            var padding = (max - min) * .12;
            min -= padding;
            max += padding;
        }

        using var gridPen = new Pen(FluxAiTheme.Border, 1);
        using var axisPen = new Pen(Color.FromArgb(160, 180, 187), 1);
        using var linePen = new Pen(FluxAiTheme.Brand, 2.6F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var pointBrush = new SolidBrush(FluxAiTheme.Brand);
        using var accentBrush = new SolidBrush(FluxAiTheme.Accent);
        using var zeroPen = new Pen(Color.FromArgb(125, 149, 158), 1.4F);

        for (var index = 0; index <= 4; index++)
        {
            var ratio = index / 4f;
            var y = bottom - (bottom - top) * ratio;
            graphics.DrawLine(gridPen, left, y, right, y);
            var value = min + (max - min) * ratio;
            var text = integerBarValues
                ? Math.Round(value).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
                : value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
            var size = graphics.MeasureString(text, Font);
            graphics.DrawString(text, Font, mutedBrush, left - size.Width - 10, y - size.Height / 2);
        }
        graphics.DrawLine(axisPen, left, top, left, bottom);
        graphics.DrawLine(axisPen, left, bottom, right, bottom);

        float MapY(double value) => bottom - (float)((value - min) / (max - min)) * (bottom - top);
        float MapX(int index) => _points.Count == 1
            ? (left + right) / 2
            : left + (right - left) * index / (_points.Count - 1f);

        if (Kind == FluxAiChartKind.Bar)
        {
            var zeroY = MapY(0);
            graphics.DrawLine(zeroPen, left, zeroY, right, zeroY);
            var step = (right - left) / _points.Count;
            var barWidth = Math.Clamp(step * .54f, 8, 42);
            for (var index = 0; index < _points.Count; index++)
            {
                var x = left + step * (index + .5f);
                var valueY = MapY(_points[index].Value);
                var rect = RectangleF.FromLTRB(x - barWidth / 2, Math.Min(valueY, zeroY), x + barWidth / 2, Math.Max(valueY, zeroY));
                graphics.FillRectangle(accentBrush, rect);
                graphics.DrawRectangle(linePen, rect.X, rect.Y, rect.Width, Math.Max(rect.Height, 1));
                DrawPointLabel(graphics, _points[index].Label, x, bottom + 7);
            }
        }
        else
        {
            var visibleLabels = Math.Min(_points.Count, Math.Max(2, (int)((right - left) / 86)));
            if (_points.Count > 1)
            {
                var pathPoints = Enumerable.Range(0, _points.Count)
                    .Select(index => new PointF(MapX(index), MapY(_points[index].Value)))
                    .ToArray();
                if (pathPoints.Length >= 2)
                    graphics.DrawLines(linePen, pathPoints);
            }
            for (var index = 0; index < _points.Count; index++)
            {
                var x = MapX(index);
                var y = MapY(_points[index].Value);
                graphics.FillEllipse(pointBrush, x - 4, y - 4, 8, 8);
                if (_points.Count == 1 || index == 0 || index == _points.Count - 1 || index % Math.Max(1, _points.Count / visibleLabels) == 0)
                    DrawPointLabel(graphics, _points[index].Label, x, bottom + 7);
            }
        }
    }

    private void DrawPointLabel(Graphics graphics, string label, float centerX, float y)
    {
        using var brush = new SolidBrush(FluxAiTheme.Muted);
        var compact = label.Length > 13 ? label[..12] + "…" : label;
        var size = graphics.MeasureString(compact, Font);
        graphics.DrawString(compact, Font, brush, centerX - size.Width / 2, y);
    }
}
