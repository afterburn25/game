using System;
using System.Collections.Generic;
using Godot;
using Game.Simulation.Combat.Massive;
using NumericsVector2 = System.Numerics.Vector2;

namespace Game.Presentation;

/// <summary>
/// One canvas item renders all ordinary ships in a tactical encounter. Tokens represent
/// bounded visual samples of observer-visible formations; they are never simulation entities.
/// </summary>
internal sealed partial class MassiveCombatFormationPool : MultiMeshInstance2D
{
    internal const int MaximumTokens = 4096;
    private readonly MultiMesh _instances = new();

    public override void _Ready()
    {
        Texture = CreateTokenTexture();
        _instances.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
        _instances.UseColors = true;
        _instances.Mesh = new QuadMesh { Size = new Vector2(16, 9) };
        Multimesh = _instances;
        ZIndex = 2;
    }

    internal int Populate(
        IReadOnlyList<MassiveObservedFormation> formations,
        Func<NumericsVector2, Vector2> project,
        Func<MassiveObservedFormation, Color> colorFor,
        float zoom,
        ISet<long> selected)
    {
        var counts = new int[formations.Count];
        var total = 0;
        for (var index = 0; index < formations.Count; index++)
        {
            var formation = formations[index];
            var midpoint = Math.Max(1, (formation.ShipCountLow + formation.ShipCountHigh) / 2);
            var desired = zoom switch
            {
                < .55f => 1,
                < 1.15f => Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(midpoint) / 2.4f), 1, 12),
                _ => Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(midpoint) / 1.35f), 2, 28),
            };
            counts[index] = desired;
            total += desired;
        }

        var divisor = total <= MaximumTokens ? 1f : total / (float)MaximumTokens;
        var allocated = 0;
        for (var index = 0; index < counts.Length; index++)
        {
            counts[index] = Math.Max(1, (int)MathF.Floor(counts[index] / divisor));
            allocated += counts[index];
        }
        _instances.VisibleInstanceCount = -1;
        _instances.InstanceCount = Math.Min(allocated, MaximumTokens);

        var instance = 0;
        for (var formationIndex = 0; formationIndex < formations.Count && instance < _instances.InstanceCount; formationIndex++)
        {
            var formation = formations[formationIndex];
            var center = project(formation.Position);
            var velocity = new Vector2(formation.Velocity.X, formation.Velocity.Y);
            var heading = velocity.LengthSquared() > .0001f ? velocity.Normalized() : Vector2.Right;
            var angle = heading.Angle();
            var color = colorFor(formation);
            var selectedScale = selected.Contains(formation.FormationId) ? 1.28f : 1f;
            for (var token = 0; token < counts[formationIndex] && instance < _instances.InstanceCount; token++, instance++)
            {
                var offset = FormationOffset(formation.FormationId, token, counts[formationIndex], formation.Shape);
                var tokenScale = selectedScale * (token == 0 ? 1.12f : .82f);
                _instances.SetInstanceTransform2D(instance,
                    new Transform2D(angle, new Vector2(tokenScale, tokenScale), 0, center + offset));
                _instances.SetInstanceColor(instance, color);
            }
        }
        return _instances.InstanceCount;
    }

    private static Vector2 FormationOffset(long id, int token, int count, MassiveFormationShape shape)
    {
        if (token == 0) return Vector2.Zero;
        var row = 1 + (token - 1) / 7;
        var column = (token - 1) % 7 - 3;
        var jitter = Hash01(id, token) - .5f;
        return shape switch
        {
            MassiveFormationShape.Wedge => new Vector2(-row * 10, column * (6 + row * 1.2f) + jitter * 3),
            MassiveFormationShape.Screen => new Vector2(column * 10 + jitter * 3, row * 5),
            MassiveFormationShape.Standoff => new Vector2(column * 11, row * 9 + jitter * 3),
            MassiveFormationShape.Dispersed => new Vector2(column * 13 + jitter * 8, row * 11 + Hash01(id, token + 91) * 8),
            MassiveFormationShape.Escort => new Vector2(MathF.Cos(token * 2.399f) * row * 10, MathF.Sin(token * 2.399f) * row * 7),
            MassiveFormationShape.RetreatColumn or MassiveFormationShape.Breakout => new Vector2(-row * 13, column * 5 + jitter * 3),
            _ => new Vector2(column * 9, row * 7 + jitter * 2),
        };
    }

    private static float Hash01(long id, int token)
    {
        unchecked
        {
            var value = (ulong)id ^ ((ulong)(token + 1) * 0x9E3779B97F4A7C15UL);
            value ^= value >> 30; value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27; value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return (value & 0xffff) / 65535f;
        }
    }

    private static Texture2D CreateTokenTexture()
    {
        using var image = Image.CreateEmpty(16, 9, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        for (var y = 0; y < 9; y++)
        for (var x = 0; x < 16; x++)
        {
            var half = 1.2f + (15 - x) * .21f;
            if (MathF.Abs(y - 4) <= half)
                image.SetPixel(x, y, x > 12 ? new Color(1, 1, 1, .72f) : Colors.White);
        }
        return ImageTexture.CreateFromImage(image);
    }
}
