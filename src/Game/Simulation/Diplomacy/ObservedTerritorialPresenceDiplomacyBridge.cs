using System;
using System.Collections.Generic;
using System.Linq;
using Game.Simulation.Models;

namespace Game.Simulation.Diplomacy;

/// <summary>
/// Explicit subsystem handoff for a legitimately observed foreign civilization physically present
/// in a system. Producers own sensing/current-presence truth; Diplomacy must never reconstruct
/// these observations by scanning authoritative foreign fleets.
/// </summary>
public sealed record TerritorialPresenceObservation(
    int TerritorialCivilizationId,
    int IntruderCivilizationId,
    int SystemId,
    long ObservedAtTick);

public sealed record TerritorialPresenceDiplomacyResult(
    int ObservationsReceived,
    int TrespassesRecorded,
    int SkippedUnidentified,
    int SkippedNoTerritorialBasis,
    int SkippedAuthorizedAccess,
    int SkippedInvalidOrUnknownSystem,
    int SkippedDuplicate);

/// <summary>
/// Converts explicit, legitimately observed current-presence facts into Diplomacy trespass history.
/// It validates only campaign/Diplomacy facts that this subsystem owns: system knowledge,
/// territorial basis, identified intruder identity and access permission. It never blocks movement
/// and never manufactures observations from GalaxyState's authoritative foreign fleet collection.
/// </summary>
public sealed class ObservedTerritorialPresenceDiplomacyBridge
{
    private readonly GalaxyState _galaxy;
    private readonly DiplomacyCampaignRuntimeCoordinator _runtime;
    private readonly DiplomacySimulation _diplomacy;

    public ObservedTerritorialPresenceDiplomacyBridge(
        GalaxyState galaxy,
        DiplomacyCampaignRuntimeCoordinator runtime)
    {
        _galaxy = galaxy ?? throw new ArgumentNullException(nameof(galaxy));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _diplomacy = new DiplomacySimulation(_runtime.State);
    }

    public TerritorialPresenceDiplomacyResult Process(
        IReadOnlyList<TerritorialPresenceObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        var recorded = 0;
        var unidentified = 0;
        var noTerritorialBasis = 0;
        var authorized = 0;
        var invalidSystem = 0;
        var duplicate = 0;

        foreach (var observation in observations)
        {
            if (observation.TerritorialCivilizationId < 0 ||
                observation.IntruderCivilizationId < 0 ||
                observation.TerritorialCivilizationId == observation.IntruderCivilizationId ||
                observation.SystemId < 0 ||
                observation.ObservedAtTick < 0)
            {
                invalidSystem++;
                continue;
            }

            if (!_galaxy.Civilizations.Any(civilization =>
                    civilization.Id == observation.TerritorialCivilizationId) ||
                !_galaxy.Civilizations.Any(civilization =>
                    civilization.Id == observation.IntruderCivilizationId) ||
                !_galaxy.Systems.Any(system => system.Id == observation.SystemId) ||
                !_galaxy.Knowledge.IsSystemKnown(
                    observation.TerritorialCivilizationId,
                    observation.SystemId))
            {
                invalidSystem++;
                continue;
            }

            var territorialView = _runtime.BuildView(observation.TerritorialCivilizationId);
            var identifiedIntruder = territorialView.Contacts.Any(contact =>
                contact.TargetCivilizationId == observation.IntruderCivilizationId &&
                contact.Awareness >= ContactAwareness.Identified);
            if (!identifiedIntruder)
            {
                unidentified++;
                continue;
            }

            var hasActiveClaim = territorialView.Claims.Any(claim =>
                claim.Active &&
                claim.ClaimantCivilizationId == observation.TerritorialCivilizationId &&
                claim.SystemId == observation.SystemId);
            var hasColony = _galaxy.Colonies.Any(colony =>
                colony.CivilizationId == observation.TerritorialCivilizationId &&
                colony.SystemId == observation.SystemId);
            if (!hasActiveClaim && !hasColony)
            {
                noTerritorialBasis++;
                continue;
            }

            if (_runtime.State.GetAccessPermission(
                    observation.TerritorialCivilizationId,
                    observation.IntruderCivilizationId) == AccessPermission.Granted)
            {
                authorized++;
                continue;
            }

            var alreadyRecorded = territorialView.RecentEvents.Any(evt =>
                evt.Kind == DiplomaticEventKind.TrespassRecorded &&
                evt.PrimaryCivilizationId == observation.TerritorialCivilizationId &&
                evt.SecondaryCivilizationId == observation.IntruderCivilizationId &&
                evt.SystemId == observation.SystemId &&
                evt.Tick == observation.ObservedAtTick);
            if (alreadyRecorded)
            {
                duplicate++;
                continue;
            }

            _diplomacy.RecordTrespass(
                observation.TerritorialCivilizationId,
                observation.IntruderCivilizationId,
                observation.SystemId,
                observation.ObservedAtTick);
            recorded++;
        }

        return new TerritorialPresenceDiplomacyResult(
            observations.Count,
            recorded,
            unidentified,
            noTerritorialBasis,
            authorized,
            invalidSystem,
            duplicate);
    }
}

public static class ObservedTerritorialPresenceDiplomacyBridgeExtensions
{
    public static ObservedTerritorialPresenceDiplomacyBridge CreateTerritorialPresenceBridge(
        this DiplomacyCampaignRuntimeCoordinator runtime,
        GalaxyState galaxy)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(galaxy);
        return new ObservedTerritorialPresenceDiplomacyBridge(galaxy, runtime);
    }
}
