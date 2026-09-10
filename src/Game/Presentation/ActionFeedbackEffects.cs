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
        var alpha = MathF.Sin(progress * MathF.PI) * .65f;
        var color = CategoryColor(_category);
        // Notification feedback belongs near the HUD, not as giant unexplained
        // orbital rings covering stars, planets and the player's current target.
        var end = new Vector2(Size.X - 24, 72);
        var start = end - new Vector2(160, 0);
        DrawLine(start, end, new Color(color, alpha * .2f), 4, true);
        DrawLine(start, start.Lerp(end, progress), new Color(color, alpha), 1.5f, true);
        CinematicArt.DrawStarlight(this, start.Lerp(end, progress), 1.4f, color, alpha);
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
