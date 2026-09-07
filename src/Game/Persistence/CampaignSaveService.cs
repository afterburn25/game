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
    public const int CurrentFormatVersion = 2;
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
            // 0.0.1 saves did not contain civilizations or fog of war. Migrate them
            // deterministically from the saved galaxy seed rather than invalidating the save.
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

            // Recovery for an incomplete/corrupt knowledge section: never grant full-map knowledge.
            if (knowledge.GetKnownSystems(playerCivilizationId).Count == 0)
            {
                var player = civilizations.First(c => c.Id == playerCivilizationId);
                knowledge.RevealWithinSensorRange(player.Id, player.HomeSystemId, systems, 230.0f);
            }
        }

        var galaxy = new GalaxyState
        {
            Seed = envelope.Galaxy.Seed,
            Systems = systems,
            Civilizations = civilizations,
            PlayerCivilizationId = playerCivilizationId,
            Knowledge = knowledge,
        };

        return new LoadedCampaign(galaxy, envelope.SimulationSeconds, envelope.GameVersion, envelope.SavedAtUtc);
    }

    private static List<StarSystemState> ToSystems(IReadOnlyList<StarSystemSaveDto> dtos)
    {
        var systems = new List<StarSystemState>(dtos.Count);
        foreach (var dto in dtos)
        {
            systems.Add(new StarSystemState(
                dto.Id,
                dto.Name,
                new Vector2(dto.X, dto.Y),
                dto.Archetype,
                dto.HasHabitableWorld,
                dto.HasAnomaly,
                dto.HasRareResource,
                dto.HasPreWarpCivilization));
        }

        return systems;
    }

    private static IReadOnlyList<CivilizationState> ToCivilizations(IReadOnlyList<CivilizationSaveDto> dtos)
    {
        return dtos.Select(dto => new CivilizationState(
            dto.Id,
            dto.Name,
            dto.HomeSystemId,
            dto.Archetype,
            new CivilizationTraits(
                dto.Aggression,
                dto.Territoriality,
                dto.Greed,
                dto.ScientificCuriosity,
                dto.RiskTolerance,
                dto.SurvivalPriority,
                dto.HonorBound),
            dto.IsPlayer)).ToArray();
    }

    private static CivilizationKnowledgeState ToKnowledge(IReadOnlyList<CivilizationKnowledgeSaveDto> dtos)
    {
        var knowledge = new CivilizationKnowledgeState();
        foreach (var dto in dtos)
        foreach (var systemId in dto.KnownSystemIds)
            knowledge.RevealSystem(dto.CivilizationId, systemId);

        return knowledge;
    }

    private static List<StarSystemSaveDto> ToSystemDtos(IReadOnlyList<StarSystemState> systems)
    {
        return systems.Select(system => new StarSystemSaveDto
        {
            Id = system.Id,
            Name = system.Name,
            X = system.Position.X,
            Y = system.Position.Y,
            Archetype = system.Archetype,
            HasHabitableWorld = system.HasHabitableWorld,
            HasAnomaly = system.HasAnomaly,
            HasRareResource = system.HasRareResource,
            HasPreWarpCivilization = system.HasPreWarpCivilization,
        }).ToList();
    }

    private static List<CivilizationSaveDto> ToCivilizationDtos(IReadOnlyList<CivilizationState> civilizations)
    {
        return civilizations.Select(civilization => new CivilizationSaveDto
        {
            Id = civilization.Id,
            Name = civilization.Name,
            HomeSystemId = civilization.HomeSystemId,
            Archetype = civilization.Archetype,
            Aggression = civilization.Traits.Aggression,
            Territoriality = civilization.Traits.Territoriality,
            Greed = civilization.Traits.Greed,
            ScientificCuriosity = civilization.Traits.ScientificCuriosity,
            RiskTolerance = civilization.Traits.RiskTolerance,
            SurvivalPriority = civilization.Traits.SurvivalPriority,
            HonorBound = civilization.Traits.HonorBound,
            IsPlayer = civilization.IsPlayer,
        }).ToList();
    }

    private static List<CivilizationKnowledgeSaveDto> ToKnowledgeDtos(CivilizationKnowledgeState knowledge)
    {
        return knowledge.Snapshot()
            .OrderBy(pair => pair.Key)
            .Select(pair => new CivilizationKnowledgeSaveDto
            {
                CivilizationId = pair.Key,
                KnownSystemIds = pair.Value.ToList(),
            })
            .ToList();
    }
}

public sealed class CampaignSaveEnvelope
{
    public int FormatVersion { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public DateTimeOffset SavedAtUtc { get; set; }
    public double SimulationSeconds { get; set; }
    public GalaxySaveDto Galaxy { get; set; } = new();
}

public sealed class GalaxySaveDto
{
    public long Seed { get; set; }
    public List<StarSystemSaveDto> Systems { get; set; } = new();
    public List<CivilizationSaveDto> Civilizations { get; set; } = new();
    public int PlayerCivilizationId { get; set; }
    public List<CivilizationKnowledgeSaveDto> Knowledge { get; set; } = new();
}

public sealed class StarSystemSaveDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public StarArchetype Archetype { get; set; }
    public bool HasHabitableWorld { get; set; }
    public bool HasAnomaly { get; set; }
    public bool HasRareResource { get; set; }
    public bool HasPreWarpCivilization { get; set; }
}

public sealed class CivilizationSaveDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int HomeSystemId { get; set; }
    public CivilizationArchetype Archetype { get; set; }
    public double Aggression { get; set; }
    public double Territoriality { get; set; }
    public double Greed { get; set; }
    public double ScientificCuriosity { get; set; }
    public double RiskTolerance { get; set; }
    public double SurvivalPriority { get; set; }
    public bool HonorBound { get; set; }
    public bool IsPlayer { get; set; }
}

public sealed class CivilizationKnowledgeSaveDto
{
    public int CivilizationId { get; set; }
    public List<int> KnownSystemIds { get; set; } = new();
}

public sealed record LoadedCampaign(
    GalaxyState Galaxy,
    double SimulationSeconds,
    string GameVersion,
    DateTimeOffset SavedAtUtc
);
