using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Game.Simulation.Combat.Massive;
using Game.Simulation.Models;

namespace Game.Simulation.Combat;

public sealed class CampaignMassiveEncounter
{
    public const int MaxEngagementEvidence = 65_536;
    public int SystemId { get; set; }
    public double StartedDay { get; set; }
    public MassiveCombatBattleState Battle { get; set; } = new();
    public List<CampaignCombatBinding> Vessels { get; set; } = new();
    public List<CampaignCombatEngagement> EngagedFormationPairs { get; set; } = new();
    public long LastObservedEventSequence { get; set; }
    public bool Reconciled { get; set; }

    public void Validate(GalaxyState galaxy)
    {
        Battle.Validate();
        if (!galaxy.Systems.Any(s => s.Id == SystemId) || !double.IsFinite(StartedDay) || StartedDay < 0 ||
            Vessels.Count == 0 || Vessels.Count > MassiveCombatLimits.MaxShips ||
            Vessels.Select(v => v.FleetId).Distinct().Count() != Vessels.Count || LastObservedEventSequence < 0 ||
            EngagedFormationPairs.Count > MaxEngagementEvidence ||
            EngagedFormationPairs.Distinct().Count() != EngagedFormationPairs.Count)
            throw new InvalidDataException("Campaign combat encounter identity, participants, or engagement evidence is invalid.");
        var fleets = galaxy.Fleets.ToDictionary(f => f.Id);
        var formations = Battle.Formations.ToDictionary(f => f.Id);
        foreach (var binding in Vessels)
            if (!fleets.TryGetValue(binding.FleetId, out var fleet) ||
                !formations.TryGetValue(binding.FormationId, out var formation) ||
                fleet.CivilizationId != formation.CivilizationId || formation.FleetId != fleet.Id ||
                !formation.ImportantVessels.Any(v => v.Id == binding.FleetId))
                throw new InvalidDataException("Campaign combat participant does not match its persistent vessel.");
        if (EngagedFormationPairs.Any(pair => pair.FirstFormationId >= pair.SecondFormationId ||
            !formations.ContainsKey(pair.FirstFormationId) || !formations.ContainsKey(pair.SecondFormationId)))
            throw new InvalidDataException("Campaign combat engagement evidence references invalid formations.");
    }
}

public sealed record CampaignCombatBinding(int FleetId, long FormationId);
public sealed record CampaignCombatEngagement(long FirstFormationId, long SecondFormationId)
{
    public static CampaignCombatEngagement Create(long first, long second) => first < second ? new(first, second) : new(second, first);
}

/// <summary>Owns one live tactical encounter while the strategic clock is suspended.</summary>
public sealed class CampaignMassiveCombat
{
    private readonly ICombatHostilityView _hostility;
    public MassiveCombatEngine Engine { get; }
    public CampaignMassiveCombat(ICombatHostilityView hostility)
    {
        _hostility = hostility ?? throw new ArgumentNullException(nameof(hostility));
        Engine = new MassiveCombatEngine(new HostilityAdapter(hostility));
    }

    public CombatOrderResult Begin(GalaxyState galaxy, int civilizationId, int actorFleetId, double day)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (!double.IsFinite(day) || day < 0) return new(false, "Combat time is invalid.");
        if (galaxy.ActiveCombatEncounter is { Reconciled: false })
            return new(false, "An encounter is already active. Use its tactical orders.");
        var actor = galaxy.Fleets.FirstOrDefault(f => f.Id == actorFleetId && f.IsActive &&
            f.CivilizationId == civilizationId && f.Role == FleetRole.Military);
        if (actor?.CurrentSystemId is not int systemId || actor.DestinationSystemId is not null)
            return new(false, "An armed fleet must be stationed in a system before engaging.");
        var participants = galaxy.Fleets.Where(f => f.IsActive && f.CurrentSystemId == systemId &&
            f.DestinationSystemId is null && (f.CivilizationId == civilizationId || _hostility.AreHostile(civilizationId, f.CivilizationId)))
            .OrderBy(f => f.Id).ToArray();
        if (!participants.Any(f => _hostility.AreHostile(civilizationId, f.CivilizationId)))
            return new(false, "No attackable hostile formation is detected in this system.");
        if (participants.Length > MassiveCombatLimits.MaxFormations)
            return new(false, "This encounter has more independently equipped vessels than the tactical formation limit.");

        var formations = new List<MassiveFormationState>(participants.Length);
        var bindings = new List<CampaignCombatBinding>(participants.Length);
        foreach (var fleet in participants)
        {
            var profile = Profile(fleet);
            var combat = CombatProfileRegistry.EnsureState(fleet);
            var loadout = Clone(fleet.TacticalLoadout ?? MassiveCombatLoadouts.FromLegacy(profile));
            var vessel = fleet.TacticalVessel is null ? new MassiveVesselState
            {
                Id = fleet.Id, Name = fleet.Name, DesignId = fleet.DesignId ?? combat.ProfileId,
                IsStoryShip = fleet.Role == FleetRole.Colony,
            } : Clone(fleet.TacticalVessel);
            vessel.Name = fleet.Name;
            vessel.HullFraction = (float)Math.Clamp(combat.Hull / Math.Max(1, profile.MaxHull), 0, 1);
            vessel.Destroyed = false; vessel.Escaped = false;
            var side = fleet.CivilizationId == civilizationId ? -1f : 1f;
            var formation = new MassiveFormationState
            {
                Id = formations.Count + 1L, FleetId = fleet.Id, TaskForceId = fleet.CivilizationId,
                CivilizationId = fleet.CivilizationId, Name = fleet.Name,
                Position = new(side * 420f, (formations.Count % 24 - 12) * 55f),
                Heading = new(-side, 0), Objective = new(0, 0),
                Order = fleet.Role == FleetRole.Military ? MassiveCombatOrderType.Engage : MassiveCombatOrderType.Retreat,
                Shape = fleet.Role == FleetRole.Military ? MassiveFormationShape.Line : MassiveFormationShape.RetreatColumn,
                Loadout = loadout, ImportantVessels = [vessel],
            };
            formations.Add(formation); bindings.Add(new(fleet.Id, formation.Id));
        }
        var battle = MassiveCombatBattleState.Create(
            unchecked((ulong)galaxy.Seed ^ (ulong)BitConverter.DoubleToInt64Bits(day) ^ (uint)actorFleetId), formations);
        foreach (var formation in formations)
        {
            var fleet = participants.Single(f => f.Id == formation.FleetId);
            formation.ShieldPool = (float)fleet.Combat!.Shields;
            formation.ArmorPool = (float)fleet.Combat.Armor;
            formation.HullPool = (float)fleet.Combat.Hull;
        }
        galaxy.ActiveCombatEncounter = new() { SystemId = systemId, StartedDay = day, Battle = battle, Vessels = bindings };
        galaxy.ActiveCombatEncounter.Validate(galaxy);
        return new(true, $"Encounter established: {participants.Length:N0} commissioned vessels. Tactical orders ready.");
    }

    public MassiveCombatSnapshot Observe(GalaxyState galaxy, int observerId, bool scanningCapability)
    {
        var encounter = galaxy.ActiveCombatEncounter ?? throw new InvalidOperationException("No tactical encounter is active.");
        return MassiveCombatObserver.BuildSnapshot(encounter.Battle, observerId, new EncounterSensors(encounter, scanningCapability));
    }

    public IReadOnlyList<CombatEvent> Advance(GalaxyState galaxy, double elapsedSeconds, Func<int, bool>? hasCombatScanner = null)
    {
        var encounter = galaxy.ActiveCombatEncounter ?? throw new InvalidOperationException("No tactical encounter is active.");
        if (encounter.Reconciled) return Array.Empty<CombatEvent>();
        Engine.Advance(encounter.Battle, elapsedSeconds);
        CaptureEngagementEvidence(galaxy, encounter);
        ApplyObserverSafeDoctrine(galaxy, encounter, hasCombatScanner);
        return Reconcile(galaxy);
    }

    public IReadOnlyList<CombatEvent> Reconcile(GalaxyState galaxy)
    {
        var encounter = galaxy.ActiveCombatEncounter;
        if (encounter is null || encounter.Reconciled ||
            (!encounter.Battle.IsComplete && Engine.HasActiveHostilities(encounter.Battle))) return Array.Empty<CombatEvent>();
        var events = new List<CombatEvent>();
        var fleetMap = galaxy.Fleets.ToDictionary(f => f.Id);
        foreach (var formation in encounter.Battle.Formations)
        {
            if (!fleetMap.TryGetValue(formation.FleetId, out var fleet)) continue;
            var profile = Profile(fleet); var combat = CombatProfileRegistry.EnsureState(fleet);
            var vessel = formation.ImportantVessels.Single(v => v.Id == fleet.Id);
            fleet.TacticalLoadout = Clone(formation.Loadout); fleet.TacticalVessel = Clone(vessel);
            fleet.TacticalVessel.BattlesFought++;
            combat.Shields = vessel.Destroyed ? 0 : Math.Min(profile.MaxShields, formation.ShieldPool);
            combat.Armor = vessel.Destroyed ? 0 : Math.Min(profile.MaxArmor, formation.ArmorPool);
            combat.Hull = vessel.Destroyed ? 0 : Math.Min(profile.MaxHull, formation.HullPool);
            fleet.TacticalVessel.HullFraction = (float)Math.Clamp(combat.Hull / Math.Max(1, profile.MaxHull), 0, 1);
            combat.Order = MilitaryOrderType.Hold; combat.TargetFleetId = null;
            combat.IsDisengaged = formation.Escaped; combat.DisengagedSystemId = formation.Escaped ? encounter.SystemId : null;
            if (!vessel.Destroyed) continue;
            fleet.IsActive = false;
            var casualties = Math.Max(0, fleet.EmbarkedPopulationMillions);
            fleet.EmbarkedPopulationMillions = 0; fleet.EmbarkedPopulationSpeciesId = null;
            fleet.DestinationSystemId = null; fleet.PlannedRouteSystemIds.Clear(); fleet.DestinationPlanetaryBodyId = null;
            if (events.Count < 128) events.Add(new(CombatEventType.FleetDestroyed, encounter.SystemId,
                fleet.CivilizationId, fleet.Id, null, null, 0, 0, profile.MaxHull,
                fleet.Name + " was lost in combat.", casualties));
        }
        encounter.Reconciled = true;
        events.Add(new(CombatEventType.EngagementEnded, encounter.SystemId, galaxy.PlayerCivilizationId, 0,
            null, null, 0, 0, 0, "Tactical encounter concluded. Damage and losses are persistent."));
        return events;
    }

    private void CaptureEngagementEvidence(GalaxyState galaxy, CampaignMassiveEncounter encounter)
    {
        var formations = encounter.Battle.Formations.ToDictionary(x => x.Id);
        foreach (var combatEvent in encounter.Battle.Events.Where(x => x.Sequence > encounter.LastObservedEventSequence).OrderBy(x => x.Sequence))
        {
            encounter.LastObservedEventSequence = Math.Max(encounter.LastObservedEventSequence, combatEvent.Sequence);
            if (combatEvent.TargetFormationId is not long targetId ||
                combatEvent.Type is not (MassiveCombatEventType.BeamVolley or MassiveCombatEventType.KineticVolley or MassiveCombatEventType.MissileSalvo or MassiveCombatEventType.Damage) ||
                !formations.TryGetValue(combatEvent.ActorFormationId, out var actor) || !formations.TryGetValue(targetId, out var target)) continue;
            var pair = CampaignCombatEngagement.Create(actor.Id, target.Id);
            if (!encounter.EngagedFormationPairs.Contains(pair) && encounter.EngagedFormationPairs.Count < CampaignMassiveEncounter.MaxEngagementEvidence)
                encounter.EngagedFormationPairs.Add(pair);
            if (actor.CivilizationId == target.CivilizationId) continue;
            FleetCombatPower.Observe(galaxy, actor.CivilizationId, galaxy.Fleets.Single(x => x.Id == target.FleetId), encounter.StartedDay, true, false);
            FleetCombatPower.Observe(galaxy, target.CivilizationId, galaxy.Fleets.Single(x => x.Id == actor.FleetId), encounter.StartedDay, true, false);
        }
        encounter.EngagedFormationPairs.Sort((a, b) => a.FirstFormationId != b.FirstFormationId
            ? a.FirstFormationId.CompareTo(b.FirstFormationId) : a.SecondFormationId.CompareTo(b.SecondFormationId));
    }

    private void ApplyObserverSafeDoctrine(GalaxyState galaxy, CampaignMassiveEncounter encounter, Func<int, bool>? hasCombatScanner)
    {
        if (encounter.Battle.Tick % 10 != 0) return;
        foreach (var civilizationId in encounter.Battle.Formations.Where(x => x.Active).Select(x => x.CivilizationId)
            .Distinct().Where(x => x != galaxy.PlayerCivilizationId).OrderBy(x => x))
        {
            var snapshot = Observe(galaxy, civilizationId, hasCombatScanner?.Invoke(civilizationId) == true);
            foreach (var order in MassiveCombatDoctrine.Decide(snapshot, civilizationId)) Engine.IssueOrder(encounter.Battle, civilizationId, order);
        }
    }

    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static CombatProfileDefinition Profile(FleetState fleet) =>
        CombatProfileRegistry.TryGet(fleet.Combat?.ProfileId ?? string.Empty, out var profile) ? profile :
            CombatProfileRegistry.Get(CombatProfileRegistry.DefaultProfileId(fleet.Role));
    private sealed class HostilityAdapter(ICombatHostilityView source) : IMassiveCombatHostilityView
    { public bool AreHostile(int first, int second) => source.AreHostile(first, second); }

    private sealed class EncounterSensors(CampaignMassiveEncounter encounter, bool scanningCapability) : IMassiveCombatSensorView
    {
        public float Confidence(int observer, long formation) => .65f;
        public bool IdentifiesImportantVessels(int observer, long formation) => scanningCapability || Engaged(observer, formation);
        public bool CanEstimateCombatPower(int observer, long formation) => scanningCapability || Engaged(observer, formation);
        private bool Engaged(int observer, long formation) => encounter.EngagedFormationPairs.Any(pair =>
            (pair.FirstFormationId == formation && Owns(observer, pair.SecondFormationId)) ||
            (pair.SecondFormationId == formation && Owns(observer, pair.FirstFormationId)));
        private bool Owns(int observer, long formation) => encounter.Battle.Formations.Any(x => x.Id == formation && x.CivilizationId == observer);
    }
}
