using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Game.Simulation.AI;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;
using Game.Simulation.Research;

namespace Game.Persistence;

public sealed class CampaignSaveService
{
    public const int CurrentFormatVersion = 5;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
    };

    public void Save(string path, GalaxyState galaxy, double simulationDays)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var envelope = new CampaignSaveEnvelope
        {
            FormatVersion = CurrentFormatVersion,
            GameVersion = GameVersion.Current,
            SavedAtUtc = DateTimeOffset.UtcNow,
            SimulationDays = simulationDays,
            Galaxy = new GalaxySaveDto
            {
                Seed = galaxy.Seed,
                Systems = ToSystemDtos(galaxy.Systems),
                Civilizations = ToCivilizationDtos(galaxy.Civilizations),
                Fleets = ToFleetDtos(galaxy.Fleets),
                Colonies = ToColonyDtos(galaxy.Colonies),
                Economies = ToEconomyDtos(galaxy.Economies),
                Technologies = ToTechnologyDtos(galaxy.Technologies),
                PlayerCivilizationId = galaxy.PlayerCivilizationId,
                Knowledge = ToKnowledgeDtos(galaxy.Knowledge),
            },
        };

        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(path)) File.Replace(tempPath, path, path + ".bak", ignoreMetadataErrors: true);
        else File.Move(tempPath, path);
    }

    public LoadedCampaign Load(string path)
    {
        var json = File.ReadAllText(path);
        var envelope = JsonSerializer.Deserialize<CampaignSaveEnvelope>(json, JsonOptions)
            ?? throw new InvalidDataException("Save file did not contain a campaign envelope.");

        if (envelope.FormatVersion < 1 || envelope.FormatVersion > CurrentFormatVersion)
            throw new InvalidDataException($"Unsupported save format {envelope.FormatVersion}; maximum supported is {CurrentFormatVersion}.");

        var simulationDays = envelope.FormatVersion >= 5 ? envelope.SimulationDays : envelope.SimulationSeconds;
        var systems = ToSystems(envelope.Galaxy.Systems);

        IList<CivilizationState> civilizations;
        CivilizationKnowledgeState knowledge;
        int playerCivilizationId;

        if (envelope.FormatVersion == 1 || envelope.Galaxy.Civilizations.Count == 0)
        {
            civilizations = new CivilizationSeeder().Seed(systems, Math.Min(8, systems.Count), Math.Min(2, Math.Max(0, systems.Count - 8)), envelope.Galaxy.Seed);
            playerCivilizationId = civilizations.First(c => c.IsPlayer).Id;
            knowledge = CreateInitialKnowledge(systems, civilizations);
        }
        else
        {
            civilizations = ToCivilizations(envelope.Galaxy.Civilizations, legacyAlreadyWarpCapable: envelope.FormatVersion < 5);
            playerCivilizationId = envelope.Galaxy.PlayerCivilizationId;
            knowledge = ToKnowledge(envelope.Galaxy.Knowledge);
            if (knowledge.GetKnownSystems(playerCivilizationId).Count == 0)
            {
                var player = civilizations.First(c => c.Id == playerCivilizationId);
                knowledge.RevealSystem(player.Id, player.HomeSystemId);
                knowledge.RevealWithinSensorRange(player.Id, player.HomeSystemId, systems, player.IsSeededAncient ? 420.0f : 95.0f);
            }
        }

        IList<FleetState> fleets = envelope.FormatVersion < 3 || envelope.Galaxy.Fleets.Count == 0
            ? new FleetSeeder().Seed(systems, civilizations)
            : ToFleets(envelope.Galaxy.Fleets).ToList();

        if (envelope.FormatVersion < 4)
            EnsureLegacyExpansionFleets(fleets, systems, civilizations);

        IList<ColonyState> colonies;
        IReadOnlyList<CivilizationEconomyState> economies;
        if (envelope.FormatVersion < 4 || envelope.Galaxy.Colonies.Count == 0 || envelope.Galaxy.Economies.Count == 0)
        {
            var colonySeeder = new ColonySeeder();
            colonies = colonySeeder.Seed(civilizations);
            economies = colonySeeder.SeedEconomies(civilizations);
        }
        else
        {
            colonies = ToColonies(envelope.Galaxy.Colonies).ToList();
            economies = ToEconomies(envelope.Galaxy.Economies);
        }

        IList<TechnologyState> technologies = envelope.FormatVersion < 5 || envelope.Galaxy.Technologies.Count == 0
            ? CreateMigratedTechnologyStates(civilizations)
            : ToTechnologies(envelope.Galaxy.Technologies);

        var galaxy = new GalaxyState
        {
            Seed = envelope.Galaxy.Seed,
            Systems = systems,
            Civilizations = civilizations,
            Fleets = fleets,
            Colonies = colonies,
            Economies = economies,
            Technologies = technologies,
            PlayerCivilizationId = playerCivilizationId,
            Knowledge = knowledge,
        };

        return new LoadedCampaign(galaxy, simulationDays, envelope.GameVersion, envelope.SavedAtUtc);
    }

    private static CivilizationKnowledgeState CreateInitialKnowledge(IReadOnlyList<StarSystemState> systems, IList<CivilizationState> civilizations)
    {
        var knowledge = new CivilizationKnowledgeState();
        foreach (var civilization in civilizations)
        {
            knowledge.RevealSystem(civilization.Id, civilization.HomeSystemId);
            knowledge.RevealWithinSensorRange(civilization.Id, civilization.HomeSystemId, systems, civilization.IsSeededAncient ? 420.0f : 95.0f);
        }
        return knowledge;
    }

    private static IList<TechnologyState> CreateMigratedTechnologyStates(IList<CivilizationState> civilizations)
    {
        var states = new TechnologySeeder().Seed(civilizations);
        foreach (var civilization in civilizations)
        {
            if (civilization.DevelopmentStage != CivilizationDevelopmentStage.WarpCapable) continue;
            var state = states.First(t => t.CivilizationId == civilization.Id);
            foreach (var technology in TechnologyRegistry.All) state.CompletedTechnologyIds.Add(technology.Id);
        }
        return states;
    }

    private static void EnsureLegacyExpansionFleets(IList<FleetState> fleets, IReadOnlyList<StarSystemState> systems, IList<CivilizationState> civilizations)
    {
        var nextId = fleets.Count == 0 ? 0 : fleets.Max(f => f.Id) + 1;
        foreach (var civilization in civilizations)
        {
            if (civilization.DevelopmentStage != CivilizationDevelopmentStage.WarpCapable || !civilization.ExpansionAllowed) continue;
            if (fleets.Any(f => f.CivilizationId == civilization.Id && f.Role == FleetRole.Colony)) continue;
            var home = systems.First(s => s.Id == civilization.HomeSystemId);
            fleets.Add(new FleetState
            {
                Id = nextId++, CivilizationId = civilization.Id, Name = civilization.IsPlayer ? "Pioneer One" : $"{civilization.Name} Pioneer",
                Role = FleetRole.Colony, Position = home.Position, CurrentSystemId = home.Id, StrategicSpeed = 13.5, SensorRange = 75.0f, IsActive = true,
            });
        }
    }

    private static List<StarSystemState> ToSystems(IReadOnlyList<StarSystemSaveDto> dtos) =>
        dtos.Select(d => new StarSystemState(d.Id, d.Name, new Vector2(d.X, d.Y), d.Archetype, d.HasHabitableWorld, d.HasAnomaly, d.HasRareResource, d.HasPreWarpCivilization)).ToList();

    private static IList<CivilizationState> ToCivilizations(IReadOnlyList<CivilizationSaveDto> dtos, bool legacyAlreadyWarpCapable) =>
        dtos.Select(d => new CivilizationState(
            d.Id, d.Name, d.HomeSystemId, d.Archetype,
            new CivilizationTraits(d.Aggression, d.Territoriality, d.Greed, d.ScientificCuriosity, d.RiskTolerance, d.SurvivalPriority, d.HonorBound),
            d.IsPlayer,
            legacyAlreadyWarpCapable ? CivilizationDevelopmentStage.WarpCapable : d.DevelopmentStage,
            legacyAlreadyWarpCapable ? false : d.IsSeededAncient,
            legacyAlreadyWarpCapable ? true : d.ExpansionAllowed,
            legacyAlreadyWarpCapable ? false : d.NeutralUnlessProvoked)).ToList();

    private static IReadOnlyList<FleetState> ToFleets(IReadOnlyList<FleetSaveDto> dtos) => dtos.Select(d => new FleetState
    {
        Id = d.Id, CivilizationId = d.CivilizationId, Name = d.Name, Role = d.Role, Position = new Vector2(d.X, d.Y), CurrentSystemId = d.CurrentSystemId,
        DestinationSystemId = d.DestinationSystemId, StrategicSpeed = d.StrategicSpeed, SensorRange = d.SensorRange, IsActive = d.IsActive,
    }).ToArray();

    private static IReadOnlyList<ColonyState> ToColonies(IReadOnlyList<ColonySaveDto> dtos) => dtos.Select(d => new ColonyState
    {
        Id = d.Id, CivilizationId = d.CivilizationId, SystemId = d.SystemId, Name = d.Name, PopulationMillions = d.PopulationMillions,
        Infrastructure = d.Infrastructure, Stability = d.Stability,
    }).ToArray();

    private static IReadOnlyList<CivilizationEconomyState> ToEconomies(IReadOnlyList<EconomySaveDto> dtos) => dtos.Select(d => new CivilizationEconomyState
    {
        CivilizationId = d.CivilizationId, Credits = d.Credits, Industry = d.Industry, Science = d.Science,
        LastCreditsPerSecond = d.LastCreditsPerSecond, LastIndustryPerSecond = d.LastIndustryPerSecond, LastSciencePerSecond = d.LastSciencePerSecond,
    }).ToArray();

    private static IList<TechnologyState> ToTechnologies(IReadOnlyList<TechnologySaveDto> dtos)
    {
        var result = new List<TechnologyState>(dtos.Count);
        foreach (var dto in dtos)
        {
            var state = new TechnologyState
            {
                CivilizationId = dto.CivilizationId,
                ActiveResearchId = dto.ActiveResearchId,
                ActiveResearchProgress = dto.ActiveResearchProgress,
            };
            foreach (var id in dto.CompletedTechnologyIds) state.CompletedTechnologyIds.Add(id);
            result.Add(state);
        }
        return result;
    }

    private static CivilizationKnowledgeState ToKnowledge(IReadOnlyList<CivilizationKnowledgeSaveDto> dtos)
    {
        var knowledge = new CivilizationKnowledgeState();
        foreach (var d in dtos)
        {
            foreach (var systemId in d.KnownSystemIds) knowledge.RevealSystem(d.CivilizationId, systemId);
            foreach (var civilizationId in d.KnownCivilizationIds) knowledge.RevealCivilization(d.CivilizationId, civilizationId);
        }
        return knowledge;
    }

    private static List<StarSystemSaveDto> ToSystemDtos(IReadOnlyList<StarSystemState> systems) => systems.Select(s => new StarSystemSaveDto
    {
        Id = s.Id, Name = s.Name, X = s.Position.X, Y = s.Position.Y, Archetype = s.Archetype, HasHabitableWorld = s.HasHabitableWorld,
        HasAnomaly = s.HasAnomaly, HasRareResource = s.HasRareResource, HasPreWarpCivilization = s.HasPreWarpCivilization,
    }).ToList();

    private static List<CivilizationSaveDto> ToCivilizationDtos(IEnumerable<CivilizationState> civilizations) => civilizations.Select(c => new CivilizationSaveDto
    {
        Id = c.Id, Name = c.Name, HomeSystemId = c.HomeSystemId, Archetype = c.Archetype, Aggression = c.Traits.Aggression, Territoriality = c.Traits.Territoriality,
        Greed = c.Traits.Greed, ScientificCuriosity = c.Traits.ScientificCuriosity, RiskTolerance = c.Traits.RiskTolerance, SurvivalPriority = c.Traits.SurvivalPriority,
        HonorBound = c.Traits.HonorBound, IsPlayer = c.IsPlayer, DevelopmentStage = c.DevelopmentStage, IsSeededAncient = c.IsSeededAncient,
        ExpansionAllowed = c.ExpansionAllowed, NeutralUnlessProvoked = c.NeutralUnlessProvoked,
    }).ToList();

    private static List<FleetSaveDto> ToFleetDtos(IEnumerable<FleetState> fleets) => fleets.Select(f => new FleetSaveDto
    {
        Id = f.Id, CivilizationId = f.CivilizationId, Name = f.Name, Role = f.Role, X = f.Position.X, Y = f.Position.Y, CurrentSystemId = f.CurrentSystemId,
        DestinationSystemId = f.DestinationSystemId, StrategicSpeed = f.StrategicSpeed, SensorRange = f.SensorRange, IsActive = f.IsActive,
    }).ToList();

    private static List<ColonySaveDto> ToColonyDtos(IEnumerable<ColonyState> colonies) => colonies.Select(c => new ColonySaveDto
    {
        Id = c.Id, CivilizationId = c.CivilizationId, SystemId = c.SystemId, Name = c.Name, PopulationMillions = c.PopulationMillions,
        Infrastructure = c.Infrastructure, Stability = c.Stability,
    }).ToList();

    private static List<EconomySaveDto> ToEconomyDtos(IReadOnlyList<CivilizationEconomyState> economies) => economies.Select(e => new EconomySaveDto
    {
        CivilizationId = e.CivilizationId, Credits = e.Credits, Industry = e.Industry, Science = e.Science,
        LastCreditsPerSecond = e.LastCreditsPerSecond, LastIndustryPerSecond = e.LastIndustryPerSecond, LastSciencePerSecond = e.LastSciencePerSecond,
    }).ToList();

    private static List<TechnologySaveDto> ToTechnologyDtos(IEnumerable<TechnologyState> technologies) => technologies.Select(t => new TechnologySaveDto
    {
        CivilizationId = t.CivilizationId, CompletedTechnologyIds = t.CompletedTechnologyIds.OrderBy(id => id).ToList(),
        ActiveResearchId = t.ActiveResearchId, ActiveResearchProgress = t.ActiveResearchProgress,
    }).ToList();

    private static List<CivilizationKnowledgeSaveDto> ToKnowledgeDtos(CivilizationKnowledgeState knowledge)
    {
        var snapshot = knowledge.Snapshot();
        var ids = snapshot.Systems.Keys.Concat(snapshot.Civilizations.Keys).Distinct().OrderBy(id => id);
        return ids.Select(id => new CivilizationKnowledgeSaveDto
        {
            CivilizationId = id,
            KnownSystemIds = snapshot.Systems.TryGetValue(id, out var systems) ? systems.ToList() : new List<int>(),
            KnownCivilizationIds = snapshot.Civilizations.TryGetValue(id, out var civilizations) ? civilizations.ToList() : new List<int>(),
        }).ToList();
    }
}

public sealed class CampaignSaveEnvelope
{
    public int FormatVersion { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public DateTimeOffset SavedAtUtc { get; set; }
    public double SimulationDays { get; set; }
    public double SimulationSeconds { get; set; }
    public GalaxySaveDto Galaxy { get; set; } = new();
}

public sealed class GalaxySaveDto
{
    public long Seed { get; set; }
    public List<StarSystemSaveDto> Systems { get; set; } = new();
    public List<CivilizationSaveDto> Civilizations { get; set; } = new();
    public List<FleetSaveDto> Fleets { get; set; } = new();
    public List<ColonySaveDto> Colonies { get; set; } = new();
    public List<EconomySaveDto> Economies { get; set; } = new();
    public List<TechnologySaveDto> Technologies { get; set; } = new();
    public int PlayerCivilizationId { get; set; }
    public List<CivilizationKnowledgeSaveDto> Knowledge { get; set; } = new();
}

public sealed class StarSystemSaveDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public float X { get; set; } public float Y { get; set; } public StarArchetype Archetype { get; set; } public bool HasHabitableWorld { get; set; } public bool HasAnomaly { get; set; } public bool HasRareResource { get; set; } public bool HasPreWarpCivilization { get; set; } }
public sealed class CivilizationSaveDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public int HomeSystemId { get; set; } public CivilizationArchetype Archetype { get; set; } public double Aggression { get; set; } public double Territoriality { get; set; } public double Greed { get; set; } public double ScientificCuriosity { get; set; } public double RiskTolerance { get; set; } public double SurvivalPriority { get; set; } public bool HonorBound { get; set; } public bool IsPlayer { get; set; } public CivilizationDevelopmentStage DevelopmentStage { get; set; } public bool IsSeededAncient { get; set; } public bool ExpansionAllowed { get; set; } = true; public bool NeutralUnlessProvoked { get; set; } }
public sealed class FleetSaveDto { public int Id { get; set; } public int CivilizationId { get; set; } public string Name { get; set; } = string.Empty; public FleetRole Role { get; set; } public float X { get; set; } public float Y { get; set; } public int? CurrentSystemId { get; set; } public int? DestinationSystemId { get; set; } public double StrategicSpeed { get; set; } public float SensorRange { get; set; } public bool IsActive { get; set; } = true; }
public sealed class ColonySaveDto { public int Id { get; set; } public int CivilizationId { get; set; } public int SystemId { get; set; } public string Name { get; set; } = string.Empty; public double PopulationMillions { get; set; } public double Infrastructure { get; set; } public double Stability { get; set; } }
public sealed class EconomySaveDto { public int CivilizationId { get; set; } public double Credits { get; set; } public double Industry { get; set; } public double Science { get; set; } public double LastCreditsPerSecond { get; set; } public double LastIndustryPerSecond { get; set; } public double LastSciencePerSecond { get; set; } }
public sealed class TechnologySaveDto { public int CivilizationId { get; set; } public List<string> CompletedTechnologyIds { get; set; } = new(); public string? ActiveResearchId { get; set; } public double ActiveResearchProgress { get; set; } }
public sealed class CivilizationKnowledgeSaveDto { public int CivilizationId { get; set; } public List<int> KnownSystemIds { get; set; } = new(); public List<int> KnownCivilizationIds { get; set; } = new(); }
public sealed record LoadedCampaign(GalaxyState Galaxy, double SimulationDays, string GameVersion, DateTimeOffset SavedAtUtc);
