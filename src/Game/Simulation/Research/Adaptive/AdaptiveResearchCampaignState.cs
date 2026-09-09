using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Research.Adaptive;

public sealed record AdaptiveResearchCivilizationStart(
    int CivilizationId,
    string SpeciesId,
    string ReferenceProfileId,
    string ApplicabilityContextId);

/// <summary>
/// Campaign owner for each civilization's authoritative Adaptive Research state. Static catalog
/// data remains shared by the runtime; only bounded civilization state belongs to the campaign.
/// </summary>
public sealed class AdaptiveResearchCampaignState
{
    private readonly IReadOnlyDictionary<int, AdaptiveResearchCivilizationState> _civilizations;
    private readonly IReadOnlyDictionary<int, AdaptiveResearchCivilizationStart> _starts;

    internal AdaptiveResearchCampaignState(
        AdaptiveResearchStrategicRuntime runtime,
        IReadOnlyDictionary<int, AdaptiveResearchCivilizationState> civilizations,
        IReadOnlyDictionary<int, AdaptiveResearchCivilizationStart> starts)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _civilizations = new ReadOnlyDictionary<int, AdaptiveResearchCivilizationState>(
            new Dictionary<int, AdaptiveResearchCivilizationState>(civilizations));
        _starts = new ReadOnlyDictionary<int, AdaptiveResearchCivilizationStart>(
            new Dictionary<int, AdaptiveResearchCivilizationStart>(starts));
    }

    public AdaptiveResearchStrategicRuntime Runtime { get; }
    public IReadOnlyDictionary<int, AdaptiveResearchCivilizationState> Civilizations => _civilizations;
    public IReadOnlyDictionary<int, AdaptiveResearchCivilizationStart> Starts => _starts;

    public AdaptiveResearchCivilizationState GetCivilization(int civilizationId) =>
        _civilizations.TryGetValue(civilizationId, out var state)
            ? state
            : throw new KeyNotFoundException($"Campaign has no Adaptive Research state for civilization {civilizationId}.");
}

/// <summary>
/// Deterministically composes research history from species physiology without creating a
/// species-specific future technology tree. Every civilization continues through one shared graph.
/// </summary>
public sealed class AdaptiveResearchCampaignFactory
{
    public const string TerranProfileId = "reference_humanlike_solar_2050";
    public const string PelagicProfileId = "reference_pelagic_high_pressure_early_space";
    public const string HighGravityProfileId = "reference_high_gravity_metabolic_early_space";
    public const string CryogenicHydrocarbonProfileId = "reference_cryogenic_hydrocarbon_early_space";

    private readonly AdaptiveResearchStrategicRuntime _runtime;

    public AdaptiveResearchCampaignFactory(AdaptiveResearchStrategicRuntime runtime) =>
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    public AdaptiveResearchCampaignState Create(GalaxyState galaxy)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        var states = new Dictionary<int, AdaptiveResearchCivilizationState>();
        var starts = new Dictionary<int, AdaptiveResearchCivilizationStart>();

        foreach (var civilization in galaxy.Civilizations.OrderBy(value => value.Id))
        {
            if (!states.TryAdd(civilization.Id, null!))
                throw new InvalidOperationException($"Duplicate civilization ID {civilization.Id} cannot own research state.");

            _ = SpeciesCatalog.Get(civilization.SpeciesId);
            var profileId = SelectReferenceProfile(civilization.SpeciesId);
            var contextId = $"species:{civilization.SpeciesId}";
            var composition = _runtime.Authority.ComposeReferenceProfile(
                $"civilization:{civilization.Id}",
                profileId,
                contextId);
            states[civilization.Id] = composition.State;
            starts.Add(civilization.Id, new AdaptiveResearchCivilizationStart(
                civilization.Id,
                civilization.SpeciesId,
                profileId,
                contextId));
        }

        return new AdaptiveResearchCampaignState(_runtime, states, starts);
    }

    public static string SelectReferenceProfile(string speciesId) => speciesId switch
    {
        SpeciesCatalog.TerranBaselineId => TerranProfileId,
        SpeciesCatalog.PelagicHighPressureId => PelagicProfileId,
        SpeciesCatalog.CompactHighGravityId => HighGravityProfileId,
        SpeciesCatalog.CryogenicHydrocarbonId => CryogenicHydrocarbonProfileId,
        _ => throw new KeyNotFoundException($"No Adaptive Research starting profile is registered for species '{speciesId}'."),
    };
}
