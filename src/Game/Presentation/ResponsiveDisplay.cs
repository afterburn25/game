using System;
using Godot;

namespace Game.Presentation;

/// <summary>1080p reference UI, readable 720p layout, and native-pixel high-DPI rendering.</summary>
public partial class ResponsiveDisplay : Node
{
    private Vector2I _lastWindowSize;
    public override void _Ready()
    {
        GetWindow().MinSize = new(1280, 720);
        GetWindow().SizeChanged += RefreshScale;
        RefreshScale();
    }
    public override void _ExitTree() => GetWindow().SizeChanged -= RefreshScale;

    private void RefreshScale()
    {
        var window = GetWindow();
        var physical = window.Size;
        if (_lastWindowSize == physical || physical.X <= 0 || physical.Y <= 0) return;
        _lastWindowSize = physical;
        // At 720p use the full 1280x720 layout rather than shrinking 1080p text.
        // Above 1080p the canvas remains the reference size and is rasterized at
        // the native window resolution (4K = 2x), including text and GPU planets.
        var scale = Math.Max(1f, Math.Min(physical.X / 1920f, physical.Y / 1080f));
        window.ContentScaleSize = new((int)Math.Round(physical.X / scale), (int)Math.Round(physical.Y / scale));
    }
}
