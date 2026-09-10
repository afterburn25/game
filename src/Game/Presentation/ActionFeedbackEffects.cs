using System;
using Godot;

namespace Game.Presentation;

/// <summary>Short visual acknowledgement driven by observer-visible player notifications.</summary>
public partial class ActionFeedbackEffects : Control
{
    private string _category = string.Empty;
    private float _remaining;
    public int TriggerCount { get; private set; }
    public string LastCategory => _category;
    public string ActiveCategory => _remaining > 0 ? _category : string.Empty;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SetProcess(false);
    }

    public void Trigger(string category)
    {
        _category = category;
        _remaining = 1.35f;
        TriggerCount++;
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _remaining = Math.Max(0, _remaining - (float)delta);
        QueueRedraw();
        if (_remaining <= 0) SetProcess(false);
    }

    public override void _Draw()
    {
        if (_remaining <= 0) return;
        var progress = 1 - _remaining / 1.35f;
        var alpha = MathF.Sin(progress * MathF.PI) * .68f;
        var color = CategoryColor(_category);
        var center = new Vector2(Size.X * .62f, Size.Y * .43f);
        var radius = 34 + progress * 115;
        for (var ring = 0; ring < 3; ring++)
            DrawArc(center, radius + ring * 13, -MathF.PI * .62f + ring * .5f,
                MathF.PI * (1.15f + progress) + ring * .5f, 54,
                new Color(color, alpha * (1 - ring * .22f)), 2.2f - ring * .45f, true);
        for (var ray = 0; ray < 10; ray++)
        {
            var angle = ray * MathF.Tau / 10 + progress * .7f;
            var direction = Vector2.FromAngle(angle);
            DrawLine(center + direction * (radius - 16), center + direction * (radius + 8 + ray % 3 * 5),
                new Color(color, alpha * .55f), 1.2f, true);
        }
        if (_category.Equals("Ships", StringComparison.OrdinalIgnoreCase))
            for (var trail = 0; trail < 4; trail++)
                DrawLine(center + new Vector2(-120 - trail * 18, 38 + trail * 7),
                    center + new Vector2(-18 - trail * 5, 10 + trail * 2), new Color(color, alpha * .42f), 2, true);
        if (_category.Equals("Exploration", StringComparison.OrdinalIgnoreCase))
            DrawLine(center, center + Vector2.FromAngle(-1.6f + progress * MathF.Tau) * radius,
                new Color(color, alpha), 1.4f, true);
    }

    private static Color CategoryColor(string category) => category.ToLowerInvariant() switch
    {
        "research" => new Color("a78bfa"),
        "industry" => new Color("f1b85b"),
        "ships" => new Color("62d5ff"),
        "exploration" => new Color("6ee7d1"),
        "colony" => new Color("83e39e"),
        "combat" => new Color("ff766d"),
        _ => new Color("8bdcf5"),
    };
}
