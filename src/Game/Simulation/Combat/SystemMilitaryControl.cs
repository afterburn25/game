using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

public enum SystemMilitaryControlState
{
    NoEffectiveArmedPresence,
    UnopposedEffectiveControl,
    SharedNonHostilePresence,
    Contested,
}

/// <summary>
/// Exact authoritative armed presence inside one physical star system. This is simulation-side
/// state for subsystem composition and must not be exposed as exact foreign intelligence to a
/// player or Civilization AI without an observer-knowledge filter.
/// </summary>
public sealed record SystemMilitaryPresence(
    int CivilizationId,
    int ActiveArmedVessels,
    int CombatEffectiveArmedVessels,
    int RetreatingArmedVessels,
    int DisengagedArmedVessels,
    double CurrentArmedStrength,
    double CombatEffectiveArmedStrength);

public sealed record SystemMilitaryControlSnapshot(
    int SystemId,
    SystemMilitaryControlState State,
    int? SoleControllerCivilizationId,
    int HostileEffectivePairCount,
    IReadOnlyList<SystemMilitaryPresence> Presences)
{
    public bool IsContested => State == SystemMilitaryControlState.Contested;
    public int TotalCombatEffectiveArmedVessels => Presences.Sum(presence => presence.CombatEffectiveArmedVessels);
    public double TotalCombatEffectiveArmedStrength => Presences.Sum(presence => presence.CombatEffectiveArmedStrength);
}

/// <summary>
/// Authoritative exposure assessment for simulation systems. It describes hostile effective
/// presence only; it does not itself stop movement, reduce logistics, alter trade, or apply any
/// other consequence. Allied strength is intentionally not pooled because alliance/coordination
/// semantics remain Diplomacy/AI-owned.
/// </summary>
public sealed record MilitaryInterdictionAssessment(
    int SystemId,
    int CivilizationId,
    bool IsThreatened,
    int HostileCivilizationCount,
    int HostileCombatEffectiveArmedVessels,
    double HostileCombatEffectiveArmedStrength,
    int OwnCombatEffectiveArmedVessels,
    double OwnCombatEffectiveArmedStrength)
{
    public double StrengthBalance => OwnCombatEffectiveArmedStrength - HostileCombatEffectiveArmedStrength;
}

/// <summary>
/// Reconstructible, non-persistent and non-mutating military-control read model. Political
/// opposition is delegated to the same narrow Combat hostility contract used by engagement
/// resolution. Either-direction hostility counts as interdiction exposure because a force is at
/// risk if either side is politically permitted to initiate hostile action.
/// </summary>
public sealed class AuthoritativeSystemMilitaryControlView
{
    private readonly ICombatHostilityView _hostility;

    public AuthoritativeSystemMilitaryControlView(ICombatHostilityView? hostility = null)
    {
        _hostility = hostility ?? PeacefulCombatHostilityView.Instance;
    }

    public SystemMilitaryControlSnapshot Build(GalaxyState galaxy, int systemId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!galaxy.Systems.Any(system => system.Id == systemId))
            throw new InvalidOperationException($"Unknown star system {systemId}.");

        var mutable = new Dictionary<int, MutablePresence>();
        foreach (var fleet in galaxy.Fleets
                     .Where(candidate => candidate.IsActive && candidate.CurrentSystemId == systemId)
                     .OrderBy(candidate => candidate.Id))
        {
            var readiness = CombatReadinessCalculator.ReadFleet(fleet);
            if (!readiness.IsArmed)
                continue;

            if (!mutable.TryGetValue(fleet.CivilizationId, out var presence))
            {
                presence = new MutablePresence(fleet.CivilizationId);
                mutable.Add(fleet.CivilizationId, presence);
            }

            presence.Add(readiness);
        }

        var presences = mutable.Values
            .OrderBy(presence => presence.CivilizationId)
            .Select(presence => presence.Snapshot())
            .ToArray();
        var effective = presences
            .Where(presence => presence.CombatEffectiveArmedVessels > 0)
            .ToArray();

        var hostilePairs = 0;
        for (var first = 0; first < effective.Length; first++)
        {
            for (var second = first + 1; second < effective.Length; second++)
            {
                if (AreOpposed(effective[first].CivilizationId, effective[second].CivilizationId))
                    hostilePairs++;
            }
        }

        var state = effective.Length switch
        {
            0 => SystemMilitaryControlState.NoEffectiveArmedPresence,
            1 => SystemMilitaryControlState.UnopposedEffectiveControl,
            _ when hostilePairs > 0 => SystemMilitaryControlState.Contested,
            _ => SystemMilitaryControlState.SharedNonHostilePresence,
        };
        var soleController = state == SystemMilitaryControlState.UnopposedEffectiveControl
            ? effective[0].CivilizationId
            : (int?)null;

        return new SystemMilitaryControlSnapshot(
            systemId,
            state,
            soleController,
            hostilePairs,
            presences);
    }

    public MilitaryInterdictionAssessment AssessInterdiction(
        GalaxyState galaxy,
        int systemId,
        int civilizationId)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!galaxy.Civilizations.Any(civilization => civilization.Id == civilizationId))
            throw new InvalidOperationException($"Unknown civilization {civilizationId}.");

        var control = Build(galaxy, systemId);
        var own = control.Presences.FirstOrDefault(presence => presence.CivilizationId == civilizationId);
        var hostile = control.Presences
            .Where(presence =>
                presence.CivilizationId != civilizationId &&
                presence.CombatEffectiveArmedVessels > 0 &&
                AreOpposed(civilizationId, presence.CivilizationId))
            .ToArray();

        var hostileVessels = hostile.Sum(presence => presence.CombatEffectiveArmedVessels);
        var hostileStrength = hostile.Sum(presence => presence.CombatEffectiveArmedStrength);
        return new MilitaryInterdictionAssessment(
            systemId,
            civilizationId,
            hostileVessels > 0,
            hostile.Length,
            hostileVessels,
            hostileStrength,
            own?.CombatEffectiveArmedVessels ?? 0,
            own?.CombatEffectiveArmedStrength ?? 0.0);
    }

    private bool AreOpposed(int firstCivilizationId, int secondCivilizationId) =>
        _hostility.AreHostile(firstCivilizationId, secondCivilizationId) ||
        _hostility.AreHostile(secondCivilizationId, firstCivilizationId);

    private sealed class MutablePresence(int civilizationId)
    {
        public int CivilizationId { get; } = civilizationId;
        private int ActiveArmedVessels { get; set; }
        private int CombatEffectiveArmedVessels { get; set; }
        private int RetreatingArmedVessels { get; set; }
        private int DisengagedArmedVessels { get; set; }
        private double CurrentArmedStrength { get; set; }
        private double CombatEffectiveArmedStrength { get; set; }

        public void Add(FleetCombatReadinessSnapshot readiness)
        {
            ActiveArmedVessels++;
            CurrentArmedStrength += readiness.CurrentStrength;
            if (readiness.IsRetreating)
                RetreatingArmedVessels++;
            if (readiness.IsDisengaged)
                DisengagedArmedVessels++;
            if (!readiness.IsCombatEffective)
                return;

            CombatEffectiveArmedVessels++;
            CombatEffectiveArmedStrength += readiness.CurrentStrength;
        }

        public SystemMilitaryPresence Snapshot() => new(
            CivilizationId,
            ActiveArmedVessels,
            CombatEffectiveArmedVessels,
            RetreatingArmedVessels,
            DisengagedArmedVessels,
            CurrentArmedStrength,
            CombatEffectiveArmedStrength);
    }
}
