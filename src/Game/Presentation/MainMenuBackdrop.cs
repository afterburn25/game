using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Lightweight deterministic background for the early-release main menu.
/// It is intentionally procedural: no giant texture, no animation loop, and no fake UI data.
/// </summary>
public partial class MainMenuBackdrop : Control
{
    private const int StarCount = 92;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        if (size.X <= 1.0f || size.Y <= 1.0f)
            return;

        DrawRect(new Rect2(Vector2.Zero, size), VisualPalette.Canvas);

        var random = new Random(2050);
        for (var index = 0; index < StarCount; index++)
        {
            var x = 0.02f + (float)random.NextDouble() * 0.96f;
            var y = 0.03f + (float)random.NextDouble() * 0.90f;
            var radius = 0.55f + (float)random.NextDouble() * 1.15f;
            var alpha = 0.20f + (float)random.NextDouble() * 0.46f;
            DrawCircle(
                new Vector2(size.X * x, size.Y * y),
                radius,
                VisualPalette.WithAlpha(VisualPalette.TextPrimary, alpha));
        }

        // A restrained distant stellar focus gives the menu depth without competing with controls.
        var stellarFocus = new Vector2(size.X * 0.82f, size.Y * 0.23f);
        DrawCircle(stellarFocus, 42.0f, VisualPalette.WithAlpha(VisualPalette.Selected, 0.025f));
        DrawCircle(stellarFocus, 20.0f, VisualPalette.WithAlpha(VisualPalette.Focus, 0.045f));
        DrawCircle(stellarFocus, 5.0f, VisualPalette.WithAlpha(VisualPalette.TextPrimary, 0.78f));
        DrawArc(stellarFocus, 82.0f, -2.5f, 2.4f, 72, VisualPalette.WithAlpha(VisualPalette.Selected, 0.15f), 1.0f, true);
        DrawArc(stellarFocus, 132.0f, -2.0f, 1.35f, 84, VisualPalette.WithAlpha(VisualPalette.Keyline, 0.24f), 1.0f, true);

        // A dark planetary limb anchors the lower-left edge while keeping the menu center clean.
        var limbRadius = Mathf.Min(size.X, size.Y) * 0.34f;
        var limbCenter = new Vector2(size.X * 0.12f, size.Y + limbRadius * 0.45f);
        DrawCircle(limbCenter, limbRadius, VisualPalette.SurfacePrimary);
        DrawArc(limbCenter, limbRadius, -2.92f, -0.22f, 96, VisualPalette.WithAlpha(VisualPalette.Selected, 0.13f), 2.0f, true);
        DrawArc(limbCenter, limbRadius - 7.0f, -2.85f, -0.30f, 96, VisualPalette.WithAlpha(VisualPalette.Focus, 0.04f), 5.0f, true);
    }
}
