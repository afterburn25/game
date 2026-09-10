using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Restrained cinematic background for the early-release main menu.
/// The same project-owned image anchors startup and campaign loading as one visual experience.
/// </summary>
public partial class MainMenuBackdrop : Control
{
    private Texture2D _artwork = null!;
    private float _time;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _artwork = GD.Load<Texture2D>("res://assets/visual/loading/stellar-continuum-splash.png");
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += (float)Math.Min(delta, .1);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        if (size.X <= 1.0f || size.Y <= 1.0f)
            return;

        DrawRect(new Rect2(Vector2.Zero, size), Colors.Black);
        var breathe = 1.025f + MathF.Sin(_time * .11f) * .004f;
        var extent = size * breathe;
        var drift = new Vector2(MathF.Sin(_time * .07f) * 7f, MathF.Cos(_time * .05f) * 4f);
        DrawTextureRect(_artwork, new Rect2((size - extent) * .5f + drift, extent), false,
            new Color(.88f, .93f, 1f, 1f));

        // Live typography stays readable while Earth, departing ships and the Milky Way remain visible.
        for (var band = 0; band < 28; band++)
        {
            var x = size.X * band / 28f;
            var alpha = .76f * MathF.Pow(1f - band / 28f, 1.7f) + .08f;
            DrawRect(new Rect2(x, 0, size.X / 28f + 1, size.Y), new Color(.002f, .008f, .018f, alpha));
        }
        DrawRect(new Rect2(0, 0, size.X, 2), VisualPalette.WithAlpha(VisualPalette.Selected, .34f));
        var pulse = .22f + MathF.Sin(_time * .8f) * .05f;
        DrawArc(new Vector2(size.X * .79f, size.Y * .72f), 84, -2.7f, -.35f, 72,
            VisualPalette.WithAlpha(VisualPalette.Focus, pulse), 1.2f, true);
    }
}
