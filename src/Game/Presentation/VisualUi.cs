using System;
using Godot;

namespace Game.Presentation;

/// <summary>Shared presentation styling. No simulation state or command rules live here.</summary>
public static class VisualUi
{
    public static readonly Color Accent = new("93cbbd");
    public static readonly Color Muted = new("a4b4b9");
    public static readonly Color Gold = new("edc47d");

    /// <summary>Contain pointer input at an outer UI surface. Godot otherwise forwards wheel
    /// events even through MouseFilter.Stop. Apply at the boundary, not each descendant:
    /// buttons and content must still bubble wheel events to their own ScrollContainer.</summary>
    public static void ContainPointerInput(Control boundary)
    {
        boundary.MouseFilter = Control.MouseFilterEnum.Stop;
        boundary.MouseForcePassScrollEvents = false;
    }

    public static StyleBoxFlat Surface(bool highlighted = false, int margin = 14) => new()
    {
        BgColor = new Color(0.026f, 0.045f, 0.055f, 0.98f),
        BorderColor = highlighted ? new Color("587d73") : new Color("344950"),
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
        CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
        ShadowColor = new Color(0,0,0,.30f), ShadowSize = 5, ShadowOffset = new Vector2(0,3),
        ContentMarginLeft = margin, ContentMarginRight = margin,
        ContentMarginTop = margin, ContentMarginBottom = margin,
    };

    public static Label Text(string text, int size = 14, Color? color = null, bool wrap = false)
    {
        var label = new Label
        {
            Text = text, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        if (color.HasValue) label.AddThemeColorOverride("font_color", color.Value);
        return label;
    }

    public static TextureRect Icon(Texture2D texture, float size = 24) => new()
    {
        Texture = texture, CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    public static Button Button(string text, string tooltip, Action action, Texture2D? icon = null)
    {
        var button = new Button
        {
            Text = text, TooltipText = tooltip, Icon = icon,
            ExpandIcon = false, CustomMinimumSize = new Vector2(string.IsNullOrEmpty(text) ? 38 : 0, 38),
            FocusMode = Control.FocusModeEnum.All,
        };
        button.AddThemeStyleboxOverride("normal", CinematicArt.Frame("button", 9));
        button.AddThemeStyleboxOverride("hover", CinematicArt.Frame("button-hover", 9));
        button.AddThemeStyleboxOverride("pressed", CinematicArt.Frame("button-pressed", 9));
        button.AddThemeConstantOverride("icon_max_width", 22);
        button.MouseEntered += AudioDirector.PlayHover;
        button.Pressed += () =>
        {
            AudioDirector.PlayConfirm();
            action();
        };
        return button;
    }

    public static HFlowContainer Actions(Container parent)
    {
        var row = new HFlowContainer();
        row.AddThemeConstantOverride("h_separation", 6);
        row.AddThemeConstantOverride("v_separation", 6);
        parent.AddChild(row);
        return row;
    }
}
