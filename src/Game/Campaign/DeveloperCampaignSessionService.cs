using System;
using System.Collections.Generic;
using System.IO;
using Game.Persistence;

namespace Game.Campaign;

/// <summary>Developer session lifecycle with a separate save slot and ordinary campaign rules.</summary>
public sealed class DeveloperCampaignSessionService
{
    public const string SaveFileName = "developer-autosave.json";
    private const string LegacyDemoSaveFileName = "demo-autosave.json";
    private readonly CampaignSessionService _playerSessions;
    private readonly DeveloperCampaignPersistenceService _saveService;
    private readonly CampaignStatePersistenceService _legacyPersistence;

    public DeveloperCampaignSessionService(
        CampaignSessionService? playerSessions = null,
        DeveloperCampaignPersistenceService? saveService = null,
        CampaignStatePersistenceService? legacyPersistence = null)
    {
        _playerSessions = playerSessions ?? new CampaignSessionService();
        _saveService = saveService ?? new DeveloperCampaignPersistenceService();
        _legacyPersistence = legacyPersistence ?? new CampaignStatePersistenceService();
    }

    public CampaignBootstrapResult CreateNew(long seed)
    {
        var created = _playerSessions.CreateNew(seed);
        created.Galaxy.DeveloperSession = new DeveloperSessionState(ToolsUsed: false);
        return created;
    }

    public CampaignBootstrapResult LoadOrCreate(string path, long fallbackSeed)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A save path is required.", nameof(path));
        if (File.Exists(path) || File.Exists(path + ".bak"))
            return LoadPairOrRecover(path, fallbackSeed, _saveService.Load, importingLegacy: false);

        // Import is attempted only before any Developer primary or backup exists. Broken
        // Developer saves never cause an older demo to silently replace later progress.
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var legacyPath = Path.Combine(directory, LegacyDemoSaveFileName);
        if (File.Exists(legacyPath) || File.Exists(legacyPath + ".bak"))
            return LoadPairOrRecover(legacyPath, fallbackSeed, _legacyPersistence.Load, importingLegacy: true);
        return CreateNew(fallbackSeed);
    }

    /// <summary>Loads the existing Developer slot without generating or writing a replacement.</summary>
    public CampaignBootstrapResult LoadExisting(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A save path is required.", nameof(path));
        if (File.Exists(path) || File.Exists(path + ".bak"))
            return LoadPairExisting(path, _saveService.Load, importingLegacy: false);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var legacyPath = Path.Combine(directory, LegacyDemoSaveFileName);
        if (File.Exists(legacyPath) || File.Exists(legacyPath + ".bak"))
            return LoadPairExisting(legacyPath, _legacyPersistence.Load, importingLegacy: true);
        throw new FileNotFoundException("No Developer campaign save or legacy demo save is available.", path);
    }

    private static CampaignBootstrapResult LoadPairExisting(string primaryPath,
        Func<string, LoadedCampaignState> load, bool importingLegacy)
    {
        var failures = new List<string>();
        var kind = importingLegacy ? "legacy demo" : "Developer";
        if (File.Exists(primaryPath))
        {
            try
            {
                return Bootstrap(load(primaryPath), CampaignBootstrapSource.LoadedSave, importingLegacy,
                    importingLegacy ? "Imported the legacy demo into Developer mode in memory; its original save files remain unchanged." : null);
            }
            catch (Exception failure) { failures.Add($"Primary {kind} save failed:\n{failure}"); }
        }
        else failures.Add($"Primary {kind} save was missing.");

        var backupPath = primaryPath + ".bak";
        if (File.Exists(backupPath))
        {
            try
            {
                return Bootstrap(load(backupPath), CampaignBootstrapSource.RecoveredFromBackup, importingLegacy,
                    string.Join("\n", failures) + (importingLegacy
                        ? "\nThe legacy demo backup was imported into Developer mode in memory; both original paths remain unchanged."
                        : "\nThe previous Developer backup was recovered."));
            }
            catch (Exception failure) { failures.Add($"Backup {kind} save also failed:\n{failure}"); }
        }
        else failures.Add("No backup save was available.");

        throw new InvalidDataException(string.Join("\n", failures));
    }

    private CampaignBootstrapResult LoadPairOrRecover(string primaryPath, long fallbackSeed,
        Func<string, LoadedCampaignState> load, bool importingLegacy)
    {
        var failures = new List<string>();
        var backupPath = primaryPath + ".bak";
        if (File.Exists(primaryPath))
        {
            try
            {
                return Bootstrap(load(primaryPath), CampaignBootstrapSource.LoadedSave, importingLegacy,
                    importingLegacy ? "Imported the legacy demo into Developer mode in memory; its original save files remain unchanged." : null);
            }
            catch (Exception failure)
            {
                failures.Add($"Primary {(importingLegacy ? "legacy demo" : "Developer")} save failed:\n{failure}");
            }
        }
        else
            failures.Add($"Primary {(importingLegacy ? "legacy demo" : "Developer")} save was missing.");

        if (File.Exists(backupPath))
        {
            try
            {
                return Bootstrap(load(backupPath), CampaignBootstrapSource.RecoveredFromBackup, importingLegacy,
                    string.Join("\n", failures) + (importingLegacy
                        ? "\nThe legacy demo backup was imported into Developer mode in memory; both original paths remain unchanged."
                        : "\nThe previous Developer backup was recovered."));
            }
            catch (Exception failure)
            {
                failures.Add($"Backup {(importingLegacy ? "legacy demo" : "Developer")} save also failed:\n{failure}");
            }
        }
        else
            failures.Add("No backup save was available.");

        return CreateNew(fallbackSeed) with
        {
            Source = CampaignBootstrapSource.RecoveredFromInvalidSave,
            LoadFailure = string.Join("\n", failures),
        };
    }

    private static CampaignBootstrapResult Bootstrap(LoadedCampaignState loaded,
        CampaignBootstrapSource source, bool importingLegacy, string? loadFailure)
    {
        // The old guided demo used ordinary rules and no Developer tool grants. The original
        // canonical file remains untouched; a later explicit save writes the Developer envelope.
        if (importingLegacy) loaded.Galaxy.DeveloperSession = new DeveloperSessionState(ToolsUsed: false);
        return new(loaded.Galaxy, loaded.Diplomacy, loaded.AdaptiveResearch, loaded.SimulationDays, source,
            loaded.GameVersion, loaded.SavedAtUtc, loadFailure);
    }
}
