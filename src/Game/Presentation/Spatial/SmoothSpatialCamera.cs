using System;

namespace Game.Presentation.Spatial;

/// <summary>Presentation-only affine camera. Drawing and picking read the same interpolated
/// scale/origin. Interpolating both with one weight preserves a zoom gesture's pointer anchor.</summary>
public sealed class SmoothSpatialCamera
{
    public float Scale { get; private set; } = 1f;
    public float OriginX { get; private set; }
    public float OriginY { get; private set; }
    public float TargetScale { get; private set; } = 1f;
    public float TargetOriginX { get; private set; }
    public float TargetOriginY { get; private set; }
    public bool IsMoving => Math.Abs(Scale - TargetScale) > 0.00001f ||
        Math.Abs(OriginX - TargetOriginX) > 0.05f || Math.Abs(OriginY - TargetOriginY) > 0.05f;

    public void Snap(float scale, float originX, float originY)
    {
        SetTarget(scale, originX, originY);
        Scale = TargetScale;
        OriginX = TargetOriginX;
        OriginY = TargetOriginY;
    }

    public void SetTarget(float scale, float originX, float originY)
    {
        if (!float.IsFinite(scale) || scale <= 0 || !float.IsFinite(originX) || !float.IsFinite(originY))
            throw new ArgumentOutOfRangeException(nameof(scale), "Camera transform must be finite with positive scale.");
        TargetScale = scale;
        TargetOriginX = originX;
        TargetOriginY = originY;
    }

    public void ZoomAt(float factor, float anchorX, float anchorY, float minimum, float maximum)
    {
        if (!float.IsFinite(factor) || factor <= 0 || minimum <= 0 || maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(factor));
        var worldX = (anchorX - OriginX) / Scale;
        var worldY = (anchorY - OriginY) / Scale;
        var next = Math.Clamp(TargetScale * factor, minimum, maximum);
        SetTarget(next, anchorX - worldX * next, anchorY - worldY * next);
    }

    public void Pan(float x, float y) => Snap(Scale, OriginX + x, OriginY + y);

    /// <summary>Resize translation preserves an in-progress gesture and its target.</summary>
    public void Translate(float x, float y)
    {
        SetTarget(TargetScale, TargetOriginX + x, TargetOriginY + y);
        OriginX += x;
        OriginY += y;
    }

    public bool Advance(double delta)
    {
        if (!IsMoving) return false;
        var weight = (float)(1.0 - Math.Exp(-12.0 * Math.Clamp(delta, 0, 0.1)));
        Scale += (TargetScale - Scale) * weight;
        OriginX += (TargetOriginX - OriginX) * weight;
        OriginY += (TargetOriginY - OriginY) * weight;
        if (!IsMoving) Snap(TargetScale, TargetOriginX, TargetOriginY);
        return true;
    }
}
