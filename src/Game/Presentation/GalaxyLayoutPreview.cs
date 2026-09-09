using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Game.Simulation.Generation;

namespace Game.Presentation;

public partial class GalaxyLayoutPreview : Control
{
    private IReadOnlyList<System.Numerics.Vector2> _positions = Array.Empty<System.Numerics.Vector2>();
    public void SetRecipe(long seed, GalaxySetupOptions options) { _positions = SeededGalaxyLayout.Generate(seed, options); QueueRedraw(); }
    public void Clear() { _positions = Array.Empty<System.Numerics.Vector2>(); QueueRedraw(); }
    public override void _Ready() { MouseFilter = MouseFilterEnum.Ignore; Resized += QueueRedraw; }
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("050c16"));
        DrawRect(new Rect2(Vector2.One, Size - Vector2.One * 2), new Color("214556"), false);
        if (_positions.Count == 0) return;
        var min = new Vector2(_positions.Min(p => p.X), _positions.Min(p => p.Y));
        var max = new Vector2(_positions.Max(p => p.X), _positions.Max(p => p.Y));
        var scale = Math.Min((Size.X - 48) / Math.Max(1, max.X - min.X), (Size.Y - 48) / Math.Max(1, max.Y - min.Y));
        for (var i = 0; i < _positions.Count; i++)
        {
            var at = Size * .5f + (new Vector2(_positions[i].X, _positions[i].Y) - (min + max) * .5f) * scale;
            var color = i == 0 ? VisualUi.Gold : VisualUi.Accent;
            DrawCircle(at, i == 0 ? 6 : 4, new Color(color, .13f));
            DrawCircle(at, i == 0 ? 3 : 1.6f, color, true, -1, true);
            if (i == 0) DrawArc(at, 8, 0, Mathf.Tau, 32, color, 1, true);
        }
    }
}
