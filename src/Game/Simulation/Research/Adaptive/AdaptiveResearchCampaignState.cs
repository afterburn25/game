using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Simulation.Models;
using Game.Simulation.Species;

namespace Game.Simulation.Research.Adaptive;

public sealed record AdaptiveResearchCivilizationStart(
    int CivilizationId,
    string SpeciesId,
    string ReferenceProfileId,
    string ApplicabilityContextId);

public sealed record AdaptiveResearchCampaignCivilizationSnapshot(
    int CivilizationId,
    string SpeciesId,
    string ReferenceProfileId,
    string ApplicabilityContextId,
    AdaptiveResearchStateSnapshotV5 Research);

public sealed record AdaptiveResearchCampaignSnapshot(
    int SchemaVersion,
    string CatalogId,
    IReadOnlyList<AdaptiveResearchCampaignCivilizationSnapshot> Civilizations);

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

public sealed class AdaptiveResearchCampaignSnapshotCodec
{
    public const int CurrentSchemaVersion = 1;
    private readonly AdaptiveResearchStrategicRuntime _runtime;
    private readonly AdaptiveResearchOutcomeSnapshotCodec _civilizationCodec;

    public AdaptiveResearchCampaignSnapshotCodec(AdaptiveResearchStrategicRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _civilizationCodec = new AdaptiveResearchOutcomeSnapshotCodec(runtime);
    }

    public AdaptiveResearchCampaignSnapshot Capture(AdaptiveResearchCampaignState campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (!ReferenceEquals(campaign.Runtime, _runtime))
            throw new InvalidOperationException("Adaptive Research campaign belongs to a different runtime catalog instance.");
        return new AdaptiveResearchCampaignSnapshot(
            CurrentSchemaVersion,
            _runtime.Authority.Catalog.Metadata.CatalogId,
            campaign.Civilizations.Keys.OrderBy(id => id).Select(id =>
            {
                var start = campaign.Starts[id];
                return new AdaptiveResearchCampaignCivilizationSnapshot(
                    id,
                    start.SpeciesId,
                    start.ReferenceProfileId,
                    start.ApplicabilityContextId,
                    _civilizationCodec.Capture(campaign.Civilizations[id]));
            }).ToArray());
    }

    public AdaptiveResearchCampaignState Restore(GalaxyState galaxy, AdaptiveResearchCampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported Adaptive Research campaign schema {snapshot.SchemaVersion}.");
        var catalogId = _runtime.Authority.Catalog.Metadata.CatalogId;
        if (!string.Equals(snapshot.CatalogId, catalogId, StringComparison.Ordinal))
            throw new InvalidDataException($"Adaptive Research campaign catalog '{snapshot.CatalogId}' does not match runtime catalog '{catalogId}'.");

        var galaxyCivilizations = galaxy.Civilizations.ToDictionary(value => value.Id);
        if (snapshot.Civilizations.Count != galaxyCivilizations.Count)
            throw new InvalidDataException("Adaptive Research campaign must contain exactly one state for every civilization.");
        var states = new Dictionary<int, AdaptiveResearchCivilizationState>();
        var starts = new Dictionary<int, AdaptiveResearchCivilizationStart>();
        foreach (var entry in snapshot.Civilizations)
        {
            if (!galaxyCivilizations.TryGetValue(entry.CivilizationId, out var civilization))
                throw new InvalidDataException($"Adaptive Research references unknown civilization {entry.CivilizationId}.");
            if (!states.TryAdd(entry.CivilizationId, null!))
                throw new InvalidDataException($"Adaptive Research duplicates civilization {entry.CivilizationId}.");
            var expectedProfile = AdaptiveResearchCampaignFactory.SelectReferenceProfile(civilization.SpeciesId);
            var expectedContext = $"species:{civilization.SpeciesId}";
            if (!string.Equals(entry.SpeciesId, civilization.SpeciesId, StringComparison.Ordinal) ||
                !string.Equals(entry.ReferenceProfileId, expectedProfile, StringComparison.Ordinal) ||
                !string.Equals(entry.ApplicabilityContextId, expectedContext, StringComparison.Ordinal))
                throw new InvalidDataException($"Adaptive Research identity metadata does not match civilization {entry.CivilizationId}.");
            var state = _civilizationCodec.Restore(entry.Research);
            if (!string.Equals(state.CivilizationId, $"civilization:{entry.CivilizationId}", StringComparison.Ordinal))
                throw new InvalidDataException($"Adaptive Research state identity does not match civilization {entry.CivilizationId}.");
            states[entry.CivilizationId] = state;
            starts.Add(entry.CivilizationId, new AdaptiveResearchCivilizationStart(
                entry.CivilizationId,
                entry.SpeciesId,
                entry.ReferenceProfileId,
                entry.ApplicabilityContextId));
        }
        return new AdaptiveResearchCampaignState(_runtime, states, starts);
    }
}

public static class AdaptiveResearchDataLocator
{
    public static string FindDataRoot(string? startDirectory = null)
    {
        foreach (var start in new[] { startDirectory, Directory.GetCurrentDirectory(), AppContext.BaseDirectory }
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var directory = new DirectoryInfo(Path.GetFullPath(start!));
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "data", "research", "v1");
                if (File.Exists(Path.Combine(candidate, "index.json")))
                    return candidate;
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException(
            "Adaptive Research data/research/v1 could not be located from the working or application directory.");
    }
}
