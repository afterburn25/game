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

namespace Game.Persistence;

public sealed class CampaignSaveService
{
    public const int CurrentFormatVersion = 4;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
    };

    public void Save(string path, GalaxyState galaxy, double simulationSeconds)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var envelope = new CampaignSaveEnvelope
        {
            FormatVersion = CurrentFormatVersion,
            GameVersion = GameVersion.Current,
            SavedAtUtc = DateTimeOffset.UtcNow,
            SimulationSeconds = simulationSeconds,
            Galaxy = new GalaxySaveDto
            {
                Seed = galaxy.Seed,
                Systems = ToSystemDtos(galaxy.Systems),
                Civilizations = ToCivilizationDtos(galaxy.Civilizations),
                Fleets = ToFleetDtos(galaxy.Fleets),
                Colonies = ToColonyDtos(galaxy.Colonies),
                Economies = ToEconomyDtos(galaxy.Economies),
                PlayerCivilizationId = galaxy.PlayerCivilizationId,
                Knowledge = ToKnowledgeDtos(galaxy.Knowledge),
            },
        };

        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
            File.Replace(tempPath, path, path + ".bak", ignoreMetadataErrors: true);
        else
            File.Move(tempPath, path);
    }

    public LoadedCampaign Load(string path)
    {
        var json = File.ReadAllText(path);
        var envelope = JsonSerializer.Deserialize<CampaignSaveEnvelope>(json, JsonOptions)
            ?? throw new InvalidDataException("Save file did not contain a campaign envelope.");

        if (envelope.FormatVersion < 1 || envelope.FormatVersion > CurrentFormatVersion)
            throw new InvalidDataException($"Unsupported save format {envelope.FormatVersion}; maximum supported is {CurrentFormatVersion}.");

        var systems = ToSystems(envelope.Galaxy.Systems);

        IReadOnlyList<CivilizationState> civilizations;
        CivilizationKnowledgeState knowledge;
        int playerCivilizationId;

        if (envelope.FormatVersion == 1 || envelope.Galaxy.Civilizations.Count == 0)
        {
            var civilizationCount = Math.Min(8, systems.Count);
            civilizations = new CivilizationSeeder().Seed(systems, civilizationCount, envelope.Galaxy.Seed);
            playerCivilizationId = civilizations.First(c => c.IsPlayer).Id;
            knowledge = CivilizationKnowledgeState.CreateInitial(systems, civilizations, 230.0f);
        }
        else
        {
            civilizations = ToCivilizations(envelope.Galaxy.Civilizations);
            playerCivilizationId = envelope.Galaxy.PlayerCivilizationId;
            knowledge = ToKnowledge(envelope.Galaxy.Knowledge);

            if (knowledge.GetKnownSystems(playerCivilizationId).Count == 0)
            {
                var player = civilizations.First(c => c.Id == playerCivilizationId);
                knowledge.RevealWithinSensorRange(player.Id, player.HomeSystemId, systems, 230.0f);
            }
        }

        var fleets = envelope.FormatVersion < 3 || envelope.Galaxy.Fleets.Count == 0
            ? new FleetSeeder().Seed(systems, civilizations).ToList()
            : ToFleets(envelope.Galaxy.Fleets).ToList();

        if (envelope.FormatVersion < 4)
            EnsureLegacyColonyShips(fleets, systems, civilizations);

        IList<ColonyState> colonies;
        IReadOnlyList<CivilizationEconomyState> economies;
        if (envelope.FormatVersion < 4 || envelope.Galaxy.Colonies.Count == 0 || envelope.Galaxy.Economies.Count == 0)
        {
            var seeder = new ColonySeeder();
            colonies = seeder.Seed(civilizations).ToList();
            economies = seeder.SeedEconomies(civilizations);
        }
        else
        {
            colonies = ToColonies(envelope.Galaxy.Colonies).ToList();
            economies = ToEconomies(envelope.Galaxy.Economies);
        }

        var galaxy = new GalaxyState
        {
            Seed = envelope.Galaxy.Seed,
            Systems = systems,
            Civilizations = civilizations,
            Fleets = fleets,
            Colonies = colonies,
            Economies = economies,
            PlayerCivilizationId = playerCivilizationId,
            Knowledge = knowledge,
        };

        return new LoadedCampaign(galaxy, envelope.SimulationSeconds, envelope.GameVersion, envelope.SavedAtUtc);
    }

    private static void EnsureLegacyColonyShips(
        IList<FleetState> fleets,
        IReadOnlyList<StarSystemState> systems,
        IReadOnlyList<CivilizationState> civilizations)
    {
        var nextId = fleets.Count == 0 ? 0 : fleets.Max(f => f.Id) + 1;
        foreach (var civilization in civilizations)
        {
            if (fleets.Any(f => f.CivilizationId == civilization.Id && f.Role == FleetRole.Colony))
                continue;

            var home = systems.First(s => s.Id == civilization.HomeSystemId);
            fleets.Add(new FleetState
            {
                Id = nextId++,
                CivilizationId = civilization.Id,
                Name = civilization.IsPlayer ? "Pioneer One" : $"{civilization.Name} Pioneer",
                Role = FleetRole.Colony,
                Position = home.Position,
                CurrentSystemId = home.Id,
                DestinationSystemId = null,
                StrategicSpeed = 13.5,
                SensorRange = 75.0f,
                IsActive = true,
            });
        }
    }

    private static List<StarSystemState> ToSystems(IReadOnlyList<StarSystemSaveDto> dtos) =>
        dtos.Select(dto => new StarSystemState(dto.Id, dto.Name, new Vector2(dto.X, dto.Y), dto.Archetype, dto.HasHabitableWorld, dto.HasAnomaly, dto.HasRareResource, dto.HasPreWarpCivilization)).ToList();

    private static IReadOnlyList<CivilizationState> ToCivilizations(IReadOnlyList<CivilizationSaveDto> dtos) =>
        dtos.Select(dto => new CivilizationState(dto.Id, dto.Name, dto.HomeSystemId, dto.Archetype,
            new CivilizationTraits(dto.Aggression, dto.Territoriality, dto.Greed, dto.ScientificCuriosity, dto.RiskTolerance, dto.SurvivalPriority, dto.HonorBound), dto.IsPlayer)).ToArray();

    private static IReadOnlyList<FleetState> ToFleets(IReadOnlyList<FleetSaveDto> dtos) =>
        dtos.Select(dto => new FleetState
        {
            Id = dto.Id,
            CivilizationId = dto.CivilizationId,
            Name = dto.Name,
            Role = dto.Role,
            Position = new Vector2(dto.X, dto.Y),
            CurrentSystemId = dto.CurrentSystemId,
            DestinationSystemId = dto.DestinationSystemId,
            StrategicSpeed = dto.StrategicSpeed,
            SensorRange = dto.SensorRange,
            IsActive = dto.IsActive,
        }).ToArray();

    private static IReadOnlyList<ColonyState> ToColonies(IReadOnlyList<ColonySaveDto> dtos) =>
        dtos.Select(dto => new ColonyState
        {
            Id = dto.Id,
            CivilizationId = dto.CivilizationId,
            SystemId = dto.SystemId,
            Name = dto.Name,
            PopulationMillions = dto.PopulationMillions,
            Infrastructure = dto.Infrastructure,
            Stability = dto.Stability,
        }).ToArray();

    private static IReadOnlyList<CivilizationEconomyState> ToEconomies(IReadOnlyList<EconomySaveDto> dtos) =>
        dtos.Select(dto => new CivilizationEconomyState
        {
            CivilizationId = dto.CivilizationId,
            Credits = dto.Credits,
            Industry = dto.Industry,
            Science = dto.Science,
            LastCreditsPerSecond = dto.LastCreditsPerSecond,
            LastIndustryPerSecond = dto.LastIndustryPerSecond,
            LastSciencePerSecond = dto.LastSciencePerSecond,
        }).ToArray();

    private static CivilizationKnowledgeState ToKnowledge(IReadOnlyList<CivilizationKnowledgeSaveDto> dtos)
    {
        var knowledge = new CivilizationKnowledgeState();
        foreach (var dto in dtos)
        {
            foreach (var systemId in dto.KnownSystemIds)
                knowledge.RevealSystem(dto.CivilizationId, systemId);
            foreach (var civilizationId in dto.KnownCivilizationIds)
                knowledge.RevealCivilization(dto.CivilizationId, civilizationId);
        }
        return knowledge;
    }

    private static List<StarSystemSaveDto> ToSystemDtos(IReadOnlyList<StarSystemState> systems) => systems.Select(system => new StarSystemSaveDto
    {
        Id = system.Id, Name = system.Name, X = system.Position.X, Y = system.Position.Y, Archetype = system.Archetype,
        HasHabitableWorld = system.HasHabitableWorld, HasAnomaly = system.HasAnomaly, HasRareResource = system.HasRareResource, HasPreWarpCivilization = system.HasPreWarpCivilization,
    }).ToList();

    private static List<CivilizationSaveDto> ToCivilizationDtos(IReadOnlyList<CivilizationState> civilizations) => civilizations.Select(c => new CivilizationSaveDto
    {
        Id = c.Id, Name = c.Name, HomeSystemId = c.HomeSystemId, Archetype = c.Archetype, Aggression = c.Traits.Aggression,
        Territoriality = c.Traits.Territoriality, Greed = c.Traits.Greed, ScientificCuriosity = c.Traits.ScientificCuriosity,
        RiskTolerance = c.Traits.RiskTolerance, SurvivalPriority = c.Traits.SurvivalPriority, HonorBound = c.Traits.HonorBound, IsPlayer = c.IsPlayer,
    }).ToList();

    private static List<FleetSaveDto> ToFleetDtos(IReadOnlyList<FleetState> fleets) => fleets.Select(f => new FleetSaveDto
    {
        Id = f.Id, CivilizationId = f.CivilizationId, Name = f.Name, Role = f.Role, X = f.Position.X, Y = f.Position.Y,
        CurrentSystemId = f.CurrentSystemId, DestinationSystemId = f.DestinationSystemId, StrategicSpeed = f.StrategicSpeed, SensorRange = f.SensorRange, IsActive = f.IsActive,
    }).ToList();

    private static List<ColonySaveDto> ToColonyDtos(IList<ColonyState> colonies) => colonies.Select(c => new ColonySaveDto
    {
        Id = c.Id, CivilizationId = c.CivilizationId, SystemId = c.SystemId, Name = c.Name, PopulationMillions = c.PopulationMillions,
        Infrastructure = c.Infrastructure, Stability = c.Stability,
    }).ToList();

    private static List<EconomySaveDto> ToEconomyDtos(IReadOnlyList<CivilizationEconomyState> economies) => economies.Select(e => new EconomySaveDto
    {
        CivilizationId = e.CivilizationId, Credits = e.Credits, Industry = e.Industry, Science = e.Science,
        LastCreditsPerSecond = e.LastCreditsPerSecond, LastIndustryPerSecond = e.LastIndustryPerSecond, LastSciencePerSecond = e.LastSciencePerSecond,
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

public sealed class CampaignSaveEnvelope { public int FormatVersion { get; set; } public string GameVersion { get; set; } = string.Empty; public DateTimeOffset SavedAtUtc { get; set; } public double SimulationSeconds { get; set; } public GalaxySaveDto Galaxy { get; set; } = new(); }
public sealed class GalaxySaveDto { public long Seed { get; set; } public List<StarSystemSaveDto> Systems { get; set; } = new(); public List<CivilizationSaveDto> Civilizations { get; set; } = new(); public List<FleetSaveDto> Fleets { get; set; } = new(); public List<ColonySaveDto> Colonies { get; set; } = new(); public List<EconomySaveDto> Economies { get; set; } = new(); public int PlayerCivilizationId { get; set; } public List<CivilizationKnowledgeSaveDto> Knowledge { get; set; } = new(); }
public sealed class StarSystemSaveDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public float X { get; set; } public float Y { get; set; } public StarArchetype Archetype { get; set; } public bool HasHabitableWorld { get; set; } public bool HasAnomaly { get; set; } public bool HasRareResource { get; set; } public bool HasPreWarpCivilization { get; set; } }
public sealed class CivilizationSaveDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public int HomeSystemId { get; set; } public CivilizationArchetype Archetype { get; set; } public double Aggression { get; set; } public double Territoriality { get; set; } public double Greed { get; set; } public double ScientificCuriosity { get; set; } public double RiskTolerance { get; set; } public double SurvivalPriority { get; set; } public bool HonorBound { get; set; } public bool IsPlayer { get; set; } }
public sealed class FleetSaveDto { public int Id { get; set; } public int CivilizationId { get; set; } public string Name { get; set; } = string.Empty; public FleetRole Role { get; set; } public float X { get; set; } public float Y { get; set; } public int? CurrentSystemId { get; set; } public int? DestinationSystemId { get; set; } public double StrategicSpeed { get; set; } public float SensorRange { get; set; } public bool IsActive { get; set; } = true; }
public sealed class ColonySaveDto { public int Id { get; set; } public int CivilizationId { get; set; } public int SystemId { get; set; } public string Name { get; set; } = string.Empty; public double PopulationMillions { get; set; } public double Infrastructure { get; set; } public double Stability { get; set; } }
public sealed class EconomySaveDto { public int CivilizationId { get; set; } public double Credits { get; set; } public double Industry { get; set; } public double Science { get; set; } public double LastCreditsPerSecond { get; set; } public double LastIndustryPerSecond { get; set; } public double LastSciencePerSecond { get; set; } }
public sealed class CivilizationKnowledgeSaveDto { public int CivilizationId { get; set; } public List<int> KnownSystemIds { get; set; } = new(); public List<int> KnownCivilizationIds { get; set; } = new(); }
public sealed record LoadedCampaign(GalaxyState Galaxy, double SimulationSeconds, string GameVersion, DateTimeOffset SavedAtUtc);
