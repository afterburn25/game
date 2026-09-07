using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Game.Simulation.Models;

namespace Game.Persistence;

public sealed class CampaignSaveService
{
    public const int CurrentFormatVersion = 1;
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
            GameVersion = "0.0.1-dev.1",
            SavedAtUtc = DateTimeOffset.UtcNow,
            SimulationSeconds = simulationSeconds,
            Galaxy = new GalaxySaveDto
            {
                Seed = galaxy.Seed,
                Systems = ToSystemDtos(galaxy.Systems),
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

        if (envelope.FormatVersion != CurrentFormatVersion)
            throw new InvalidDataException($"Unsupported save format {envelope.FormatVersion}; expected {CurrentFormatVersion}.");

        var systems = new List<StarSystemState>(envelope.Galaxy.Systems.Count);
        foreach (var dto in envelope.Galaxy.Systems)
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

        return new LoadedCampaign(
            new GalaxyState { Seed = envelope.Galaxy.Seed, Systems = systems },
            envelope.SimulationSeconds,
            envelope.GameVersion,
            envelope.SavedAtUtc);
    }

    private static List<StarSystemSaveDto> ToSystemDtos(IReadOnlyList<StarSystemState> systems)
    {
        var result = new List<StarSystemSaveDto>(systems.Count);
        foreach (var system in systems)
        {
            result.Add(new StarSystemSaveDto
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
            });
        }

        return result;
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

public sealed record LoadedCampaign(
    GalaxyState Galaxy,
    double SimulationSeconds,
    string GameVersion,
    DateTimeOffset SavedAtUtc
);
