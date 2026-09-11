using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Campaign;
using Game.Simulation.Diplomacy;
using Game.Simulation.Models;
using Game.Simulation.Research.Adaptive;

namespace Game.Persistence;

/// <summary>Separate Developer envelope around the canonical, validated campaign payload.</summary>
public sealed class DeveloperCampaignPersistenceService
{
    public const int CurrentFormatVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly CampaignStatePersistenceService _campaignPersistence;

    public DeveloperCampaignPersistenceService(CampaignStatePersistenceService? campaignPersistence = null) =>
        _campaignPersistence = campaignPersistence ?? new CampaignStatePersistenceService();

    public void Save(string path, GalaxyState galaxy, double simulationDays, DiplomacyState diplomacy,
        bool preserveExistingBackup = false)
        => Save(path, galaxy, simulationDays, diplomacy,
            _campaignPersistence.CreateAdaptiveResearchState(galaxy), preserveExistingBackup);

    public void Save(string path, GalaxyState galaxy, double simulationDays, DiplomacyState diplomacy,
        AdaptiveResearchCampaignState adaptiveResearch, bool preserveExistingBackup = false)
    {
        ValidatePath(path);
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(diplomacy);
        ArgumentNullException.ThrowIfNull(adaptiveResearch);
        var provenance = galaxy.DeveloperSession
            ?? throw new InvalidOperationException("Developer saves require explicit Developer session provenance.");
        if (!double.IsFinite(simulationDays) || simulationDays < 0)
            throw new ArgumentOutOfRangeException(nameof(simulationDays), "Simulation time must be finite and non-negative.");
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var nonce = Guid.NewGuid().ToString("N");
        var campaignPath = path + $".{nonce}.developer-campaign";
        var finalPath = path + $".{nonce}.developer-tmp";
        var ownsCampaignPath = false;
        var ownsFinalPath = false;
        try
        {
            // Reserve our own intermediate file before allowing canonical atomic replacement.
            // This also makes cleanup ownership explicit if any later validation/write fails.
            using (new FileStream(campaignPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            ownsCampaignPath = true;
            _campaignPersistence.SaveDeveloperPayload(campaignPath, galaxy, simulationDays, diplomacy, adaptiveResearch);
            var campaign = JsonNode.Parse(File.ReadAllText(campaignPath)) as JsonObject
                ?? throw new InvalidDataException("Canonical persistence did not produce a campaign object.");
            var envelope = new JsonObject
            {
                ["DeveloperFormatVersion"] = CurrentFormatVersion,
                ["Mode"] = "Developer",
                ["ToolsUsed"] = provenance.ToolsUsed,
                ["Campaign"] = campaign,
            };
            using (var stream = new FileStream(finalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                ownsFinalPath = true;
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
                writer.Write(envelope.ToJsonString(JsonOptions));
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(path))
                File.Replace(finalPath, path, preserveExistingBackup ? null : path + ".bak", ignoreMetadataErrors: true);
            else
                File.Move(finalPath, path);
        }
        finally
        {
            if (ownsCampaignPath)
            {
                DeleteOwnedStage(campaignPath);
                DeleteOwnedStage(campaignPath + ".bak");
            }
            if (ownsFinalPath) DeleteOwnedStage(finalPath);
        }
    }

    public LoadedCampaignState Load(string path, Action<CampaignRestorationProgress>? progress = null)
    {
        ValidatePath(path);
        progress?.Invoke(new(.03, "Reading Developer save envelope"));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var envelope = document.RootElement;
        if (envelope.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Developer save must contain an envelope object.");
        var expected = new HashSet<string>(StringComparer.Ordinal)
            { "DeveloperFormatVersion", "Mode", "ToolsUsed", "Campaign" };
        foreach (var property in envelope.EnumerateObject())
            if (!expected.Remove(property.Name))
                throw new InvalidDataException($"Developer envelope has an unexpected or duplicate field: {property.Name}.");
        if (expected.Count != 0)
            throw new InvalidDataException($"Developer envelope is missing required fields: {string.Join(", ", expected)}.");
        var version = envelope.GetProperty("DeveloperFormatVersion");
        if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var format) || format != CurrentFormatVersion)
            throw new InvalidDataException("Unsupported Developer save format; expected DeveloperFormatVersion 1.");
        var mode = envelope.GetProperty("Mode");
        if (mode.ValueKind != JsonValueKind.String || mode.GetString() != "Developer")
            throw new InvalidDataException("Developer envelope must declare Mode exactly as Developer.");
        var toolsUsed = envelope.GetProperty("ToolsUsed");
        if (toolsUsed.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new InvalidDataException("Developer envelope ToolsUsed must be an explicit boolean.");
        var campaign = envelope.GetProperty("Campaign");
        if (campaign.ValueKind != JsonValueKind.Object ||
            !campaign.TryGetProperty("FormatVersion", out var canonicalVersion) ||
            canonicalVersion.ValueKind != JsonValueKind.Number || !canonicalVersion.TryGetInt32(out var canonicalFormat) ||
            canonicalFormat is not (CampaignStatePersistenceService.LegacyFormatVersion or
                CampaignStatePersistenceService.PresetFormatVersion or CampaignStatePersistenceService.SurfaceFormatVersion or
                CampaignStatePersistenceService.AdaptiveFormatVersion or
                CampaignStatePersistenceService.CurrentFormatVersion))
            throw new InvalidDataException("Developer Campaign must contain a canonical v9, v11, v13, v15 or v17 campaign payload.");
        RejectNestedSessionMetadata(campaign);
        progress?.Invoke(new(.14, "Developer save envelope verified"));

        var campaignPath = path + $".{Guid.NewGuid():N}.developer-load";
        var ownsCampaignPath = false;
        try
        {
            using (var stream = new FileStream(campaignPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                ownsCampaignPath = true;
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(campaign.GetRawText());
            }
            var loaded = _campaignPersistence.Load(campaignPath, update =>
                progress?.Invoke(new(.14 + update.Fraction * .83, update.Status)));
            loaded.Galaxy.DeveloperSession = new DeveloperSessionState(toolsUsed.GetBoolean());
            progress?.Invoke(new(.98, "Finalizing Developer campaign"));
            return loaded;
        }
        finally
        {
            if (ownsCampaignPath) DeleteOwnedStage(campaignPath);
        }
    }

    private static void RejectNestedSessionMetadata(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (property.Name is "DeveloperSession" or "DeveloperFormatVersion" or "Mode" or "ToolsUsed")
                    throw new InvalidDataException($"Canonical campaign payload contains contradictory nested session metadata: {property.Name}.");
                RejectNestedSessionMetadata(property.Value);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
            foreach (var element in node.EnumerateArray()) RejectNestedSessionMetadata(element);
    }

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A save path is required.", nameof(path));
    }

    private static void DeleteOwnedStage(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
