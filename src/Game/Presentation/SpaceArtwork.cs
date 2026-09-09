using Godot;

namespace Game.Presentation;

/// <summary>Original decorative art; it never creates systems or changes navigation coordinates.</summary>
public static class SpaceArtwork
{
    private static Texture2D? _galaxy;
    private static Texture2D? _nebula;
    public static Texture2D Galaxy => _galaxy ??= GD.Load<Texture2D>("res://assets/visual/space/milky-way-b.png");
    public static Texture2D Nebula => _nebula ??= GD.Load<Texture2D>("res://assets/visual/space/regional-nebula-b.png");

    public static void DrawNebula(CanvasItem canvas, Vector2 size, Vector2 pan, float opacity = .70f)
    {
        var texture = Nebula;
        var scale = Mathf.Max(size.X / texture.GetWidth(), size.Y / texture.GetHeight()) * 1.12f;
        var extent = texture.GetSize() * scale;
        var drift = new Vector2(Mathf.Sin(pan.X * .0007f), Mathf.Sin(pan.Y * .0007f)) * 18;
        canvas.DrawTextureRect(texture, new Rect2((size - extent) * .5f + drift, extent), false, new Color(1, 1, 1, opacity));
    }
}
