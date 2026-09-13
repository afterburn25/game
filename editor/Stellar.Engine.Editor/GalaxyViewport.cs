using System.Globalization;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Stellar.Editor;
public sealed class GalaxyViewport : FrameworkElement
{
    private List<WorldItem> stars = [];
    private Dictionary<int, Annotation> annotations = [];
    private double scale = 1;
    private Point center;
    private Point down, last;
    private bool dragging, moved;
    public int SelectedSystem { get; set; } = -1;
    public bool ShowNames { get; set; } = true;
    public event Action<int>? SystemSelected;
    public static readonly string[] Classes = ["Red dwarf", "Orange dwarf", "Yellow dwarf", "Yellow-white dwarf", "White star", "Hot blue star", "Giant", "White dwarf", "Neutron star", "Black hole", "Protostar", "Pulsar"];
    private static readonly string[] Colors = ["#DA7567", "#EFB078", "#F5DA9D", "#EAE5C7", "#EBF1FF", "#83B9FF", "#F09965", "#C5D6F3", "#77DAE6", "#A689D4", "#DAA17A", "#AA98EF"];
    public GalaxyViewport() { Focusable = true; ClipToBounds = true; Cursor = Cursors.Cross; SizeChanged += (_, _) => InvalidateVisual(); }
    public void SetWorld(List<WorldItem> items, Dictionary<int, Annotation> notes) { stars = items.Where(i => i.Kind == "System").ToList(); annotations = notes; InvalidateVisual(); }
    private static Point Position(WorldItem star) => new(star.Data["xLightYears"]!.GetValue<double>(), star.Data["yLightYears"]!.GetValue<double>());
    public Point ScreenFor(int id) => ToScreen(Position(stars.First(s => s.Id == id)));
    private Point ToScreen(Point world) => new(ActualWidth / 2 + (world.X - center.X) * scale, ActualHeight / 2 - (world.Y - center.Y) * scale);
    public void Fit()
    {
        if (stars.Count == 0) return;
        var points = stars.Select(Position).ToList();
        var minX = points.Min(p => p.X); var maxX = points.Max(p => p.X); var minY = points.Min(p => p.Y); var maxY = points.Max(p => p.Y);
        center = new((minX + maxX) / 2, (minY + maxY) / 2);
        scale = Math.Max(.0001, Math.Min(Math.Max(10, ActualWidth - 80) / Math.Max(1, maxX - minX), Math.Max(10, ActualHeight - 70) / Math.Max(1, maxY - minY)));
        InvalidateVisual();
    }
    public void FocusSystem(int id)
    {
        var star = stars.FirstOrDefault(s => s.Id == id); if (star is null) return;
        center = Position(star); SelectedSystem = id; InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(8, 14, 24)), null, new Rect(RenderSize));
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(20, 33, 49)), 1);
        for (double x = 0; x < ActualWidth; x += 64) dc.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
        for (double y = 0; y < ActualHeight; y += 64) dc.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
        if (stars.Count == 0) return;
        var origin = ToScreen(new Point(0, 0));
        dc.DrawLine(new Pen(Brush("#20344A"), 1), new Point(origin.X - 9, origin.Y), new Point(origin.X + 9, origin.Y));
        dc.DrawLine(new Pen(Brush("#20344A"), 1), new Point(origin.X, origin.Y - 9), new Point(origin.X, origin.Y + 9));
        var labels = new List<Rect>();
        var selectedStar = stars.FirstOrDefault(s => s.Id == SelectedSystem);
        if (ShowNames && selectedStar is not null)
        {
            var selectedPoint = ToScreen(Position(selectedStar)); var name = Text(selectedStar.Name, "#B9FFF4", 12);
            labels.Add(new Rect(selectedPoint.X + 6, selectedPoint.Y - 18, name.Width + 12, name.Height + 8));
        }
        foreach (var star in stars.OrderBy(s => s.Id == SelectedSystem ? 1 : 0))
        {
            var p = ToScreen(Position(star));
            if (p.X < -20 || p.Y < -20 || p.X > ActualWidth + 20 || p.Y > ActualHeight + 20) continue;
            var type = Math.Clamp(star.Data["primaryClass"]?.GetValue<int>() ?? 2, 0, 11);
            var radius = type == 6 ? 3.4 : type == 5 ? 2.9 : 2.1;
            var c = (Color)ColorConverter.ConvertFromString(Colors[type]);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(25, c.R, c.G, c.B)), null, p, radius * 3, radius * 3);
            dc.DrawEllipse(new SolidColorBrush(c), null, p, radius, radius);
            bool bookmarked = annotations.TryGetValue(star.Id, out var note) && note.Bookmarked;
            if (bookmarked) dc.DrawEllipse(null, new Pen(Brush("#CFB876"), 1), p, radius + 4, radius + 4);
            bool selected = star.Id == SelectedSystem;
            if (selected) dc.DrawEllipse(null, new Pen(Brush("#72E4D8"), 1.5), p, 12, 12);
            if (ShowNames && (selected || bookmarked || star.Id < 18 || scale > 1.2))
            {
                var text = Text(star.Name, selected ? "#B9FFF4" : "#829BB6", selected ? 12 : 10);
                var box = new Rect(p.X + 9, p.Y - 15, text.Width + 6, text.Height + 3);
                if (selected || !labels.Any(r => r.IntersectsWith(box)))
                {
                    if (selected) dc.DrawRoundedRectangle(Brush("#112635"), null, new Rect(box.X - 3, box.Y - 2, box.Width + 4, box.Height + 2), 3, 3);
                    dc.DrawText(text, box.TopLeft); labels.Add(box);
                }
            }
        }
        dc.DrawText(Text($"{stars.Count:N0} SYSTEMS  /  AUTHORING VIEW", "#59728C", 10), new Point(14, 12));
        dc.DrawText(Text("Star markers are symbolic; this is an editor preview.", "#59728C", 10), new Point(14, Math.Max(32, ActualHeight - 24)));
    }
    private FormattedText Text(string value, string color, double size) => new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, Brush(color), VisualTreeHelper.GetDpi(this).PixelsPerDip);
    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { Focus(); down = last = e.GetPosition(this); dragging = true; moved = false; CaptureMouse(); e.Handled = true; }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!dragging) return; var p = e.GetPosition(this);
        if ((p - down).Length > 4) moved = true;
        if (moved) { center.X -= (p.X - last.X) / scale; center.Y += (p.Y - last.Y) / scale; InvalidateVisual(); }
        last = p;
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!dragging) return; dragging = false; ReleaseMouseCapture();
        if (!moved) SelectAt(e.GetPosition(this)); e.Handled = true;
    }
    public bool SelectAt(Point p)
    {
        var star = stars.OrderBy(s => (ToScreen(Position(s)) - p).LengthSquared).FirstOrDefault();
        if (star is null || (ToScreen(Position(star)) - p).Length > 16) return false;
        SelectedSystem = star.Id; SystemSelected?.Invoke(star.Id); InvalidateVisual(); return true;
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var p = e.GetPosition(this); var old = scale;
        scale = Math.Clamp(scale * (e.Delta > 0 ? 1.2 : 1 / 1.2), .0001, 200);
        center.X += (p.X - ActualWidth / 2) * (1 / old - 1 / scale);
        center.Y -= (p.Y - ActualHeight / 2) * (1 / old - 1 / scale);
        InvalidateVisual(); e.Handled = true;
    }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.Key == Key.F) { FocusSystem(SelectedSystem); e.Handled = true; } base.OnKeyDown(e); }
}
