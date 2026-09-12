using System;

namespace Game.Presentation.Spatial;

/// <summary>Presentation-only affine camera. Drawing and picking read the same interpolated
/// scale/origin. Interpolating both with one weight preserves a zoom gesture's pointer anchor.</summary>
public sealed class SmoothSpatialCamera
{
    private double _scale = 1, _originX, _originY;
    public float Scale => (float)_scale;
    public float OriginX => (float)_originX;
    public float OriginY => (float)_originY;
    public float TargetScale { get; private set; } = 1f;
    public float TargetOriginX { get; private set; }
    public float TargetOriginY { get; private set; }
    public bool IsMoving => Math.Abs(Scale - TargetScale) > Math.Max(0.00001f, TargetScale * .000002f) ||
        Math.Abs(OriginX - TargetOriginX) > Math.Max(.05f, Math.Abs(TargetOriginX) * .000002f) ||
        Math.Abs(OriginY - TargetOriginY) > Math.Max(.05f, Math.Abs(TargetOriginY) * .000002f);

    public void Snap(float scale, float originX, float originY)
    {
        SetTarget(scale, originX, originY);
        _scale = TargetScale;
        _originX = TargetOriginX;
        _originY = TargetOriginY;
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
        _originX += x;
        _originY += y;
    }

    public bool Advance(double delta)
    {
        if (!IsMoving)
        {
            var changed = Scale != TargetScale || OriginX != TargetOriginX || OriginY != TargetOriginY;
            if (changed) Snap(TargetScale, TargetOriginX, TargetOriginY);
            return changed;
        }
        var weight = 1.0 - Math.Exp(-12.0 * Math.Clamp(delta, 0, 0.1));
        _scale += (TargetScale - _scale) * weight;
        _originX += (TargetOriginX - _originX) * weight;
        _originY += (TargetOriginY - _originY) * weight;
        if (!IsMoving && (Scale != TargetScale || OriginX != TargetOriginX || OriginY != TargetOriginY))
            Snap(TargetScale, TargetOriginX, TargetOriginY);
        return true;
    }
}
