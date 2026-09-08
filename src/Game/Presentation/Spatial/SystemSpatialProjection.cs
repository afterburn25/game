using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Exploration;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;

namespace Game.Presentation.Spatial;

/// <summary>
/// Presentation-only body classes. These are derived exclusively from observer-safe exploration
/// data and never become authoritative simulation facts.
/// </summary>
public enum SystemSpatialBodyVisualClass
{
    UnknownPlanet,
    UnknownMoon,
    Rocky,
    Oceanic,
    Frozen,
    HotRocky,
    GasGiant,
    IceGiant,
    Moon,
}

public sealed record SystemSpatialBodyMarker(
    int BodyId,
    int? ParentBodyId,
    int OrbitIndex,
    string Label,
    PlanetaryBodyKind Kind,
    SystemSpatialBodyVisualClass VisualClass,
    float OffsetX,
    float OffsetY,
    float OrbitRadius,
    float DisplayRadius,
    bool HasDetailedEnvironment,
    bool PositiveResourceSignature,
    bool PositiveAnomalySignature,
    bool PositiveActivitySignature,
    string? SurfaceKey = null);

public sealed record SystemSpatialSnapshot(
    int SystemId,
    string CatalogName,
    SystemSurveyLevel SurveyLevel,
    double SurveyProgress,
    StarArchetype? StarArchetype,
    float DesignRadius,
    IReadOnlyList<SystemSpatialBodyMarker> Bodies,
    string? CatalogPresetId = null);

/// <summary>
/// Converts the simulation-owned fog-safe exploration read model into deterministic schematic
/// display geometry. Simulation distance, travel timing and authoritative orbital facts are never
/// changed by this projection.
/// </summary>
public sealed class SystemSpatialProjection
{
    private const float FirstPlanetOrbit = 94.0f;
    private const float PlanetOrbitStep = 68.0f;
    private const float FirstMoonOrbit = 17.0f;
    private const float MoonOrbitStep = 8.0f;

    public SystemSpatialSnapshot Build(KnownSystemExplorationView system)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (system.SurveyLevel < SystemSurveyLevel.PartiallySurveyed || !system.HasReconnaissanceCatalog)
        {
            throw new InvalidOperationException(
                "A system spatial projection requires at least reconnaissance-grade orbital knowledge.");
        }

        var markers = new List<SystemSpatialBodyMarker>(system.PlanetaryBodies.Count);
        var positions = new Dictionary<int, (float X, float Y, float Radius)>();

        foreach (var body in system.PlanetaryBodies
                     .Where(body => body.Kind == PlanetaryBodyKind.Planet)
                     .OrderBy(body => body.OrbitIndex)
                     .ThenBy(body => body.BodyId))
        {
            var orbitRadius = FirstPlanetOrbit + body.OrbitIndex * PlanetOrbitStep;
            var angle = StableAngle(body.BodyId);
            var x = MathF.Cos(angle) * orbitRadius;
            var y = MathF.Sin(angle) * orbitRadius;
            var displayRadius = ResolveDisplayRadius(body);
            var marker = BuildMarker(body, x, y, orbitRadius, displayRadius, system.CatalogPresetId);
            markers.Add(marker);
            positions[body.BodyId] = (x, y, displayRadius);
        }

        foreach (var body in system.PlanetaryBodies
                     .Where(body => body.Kind == PlanetaryBodyKind.Moon)
                     .OrderBy(body => body.ParentBodyId)
                     .ThenBy(body => body.OrbitIndex)
                     .ThenBy(body => body.BodyId))
        {
            if (body.ParentBodyId is not int parentId || !positions.TryGetValue(parentId, out var parent))
            {
                throw new InvalidOperationException(
                    $"Observer-safe moon {body.BodyId} has no visible parent planet in system {system.SystemId}.");
            }

            var orbitRadius = FirstMoonOrbit + body.OrbitIndex * MoonOrbitStep + Math.Clamp(parent.Radius * 0.35f, 0.0f, 6.0f);
            var angle = StableAngle(body.BodyId ^ parentId);
            var x = parent.X + MathF.Cos(angle) * orbitRadius;
            var y = parent.Y + MathF.Sin(angle) * orbitRadius;
            var displayRadius = ResolveDisplayRadius(body);
            var marker = BuildMarker(body, x, y, orbitRadius, displayRadius, system.CatalogPresetId);
            markers.Add(marker);
            positions[body.BodyId] = (x, y, displayRadius);
        }

        var designRadius = Math.Max(
            180.0f,
            markers.Count == 0
                ? 180.0f
                : markers.Max(marker => MathF.Sqrt(marker.OffsetX * marker.OffsetX + marker.OffsetY * marker.OffsetY) + 38.0f));

        return new SystemSpatialSnapshot(
            system.SystemId,
            system.CatalogName,
            system.SurveyLevel,
            system.SurveyProgress,
            system.Archetype,
            designRadius,
            markers.OrderBy(marker => marker.BodyId).ToArray(), system.CatalogPresetId);
    }

    private static SystemSpatialBodyMarker BuildMarker(
        PlanetaryBodyExplorationView body,
        float x,
        float y,
        float orbitRadius,
        float displayRadius,
        string? catalogPresetId) =>
        new(
            body.BodyId,
            body.ParentBodyId,
            body.OrbitIndex,
            body.Name,
            body.Kind,
            ResolveVisualClass(body),
            x,
            y,
            orbitRadius,
            displayRadius,
            body.HasDetailedEnvironment,
            body.HasRareResource == true || body.HasRareResourceSignature == true,
            body.HasAnomaly == true || body.HasAnomalySignature == true,
            body.HasPreWarpCivilization == true || body.HasActivitySignature == true,
            body.HasDetailedEnvironment && catalogPresetId == "sol-v1" ? body.Name.ToLowerInvariant() : null);

    private static float ResolveDisplayRadius(PlanetaryBodyExplorationView body)
    {
        var radius = (float)Math.Max(0.01, body.RadiusEarth);
        return body.Kind == PlanetaryBodyKind.Moon
            ? Math.Clamp(2.8f + MathF.Sqrt(radius) * 1.8f, 3.0f, 6.0f)
            : Math.Clamp(4.8f + MathF.Sqrt(radius) * 2.8f, 5.2f, 16.0f);
    }

    private static SystemSpatialBodyVisualClass ResolveVisualClass(PlanetaryBodyExplorationView body)
    {
        if (!body.HasDetailedEnvironment)
        {
            return body.Kind == PlanetaryBodyKind.Moon
                ? SystemSpatialBodyVisualClass.UnknownMoon
                : SystemSpatialBodyVisualClass.UnknownPlanet;
        }

        if (body.Kind == PlanetaryBodyKind.Moon)
            return SystemSpatialBodyVisualClass.Moon;

        var temperature = body.TemperatureKelvin ?? 280.0;
        if (body.HasSolidSurface == false)
        {
            return temperature < 170.0
                ? SystemSpatialBodyVisualClass.IceGiant
                : SystemSpatialBodyVisualClass.GasGiant;
        }

        if (body.IsImmersedEnvironment == true)
            return SystemSpatialBodyVisualClass.Oceanic;
        if (temperature < 200.0)
            return SystemSpatialBodyVisualClass.Frozen;
        if (temperature > 410.0)
            return SystemSpatialBodyVisualClass.HotRocky;
        return SystemSpatialBodyVisualClass.Rocky;
    }

    private static float StableAngle(int bodyId)
    {
        var value = unchecked((uint)(bodyId + 1) * 2654435761u);
        value ^= value >> 16;
        var fraction = (value & 0x00FF_FFFFu) / 16777216.0f;
        return fraction * MathF.PI * 2.0f;
    }
}
