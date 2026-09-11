using System;
using Game.Simulation.Colonization;
using Game.Simulation.Exploration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Game.Simulation.AI;
using Game.Simulation.Combat;
using Game.Simulation.Construction;
using Game.Simulation.Economy;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;
using Game.Simulation.Species;

namespace Game.Persistence;

public sealed class CampaignSaveService
{
    public const int LegacyFormatVersion = 8;
    public const int PresetFormatVersion = 10;
    public const int CurrentFormatVersion = 12; // Odd versions belong to the campaign Diplomacy wrapper.

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
    };

    public void Save(string path, GalaxyState galaxy, double simulationDays)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (galaxy.DeveloperSession is not null)
            throw new InvalidOperationException("A Developer campaign cannot be written as a Player save. Use Developer campaign persistence.");
        SaveCore(path, galaxy, simulationDays);
    }

    // Only the Developer envelope serializer may use this canonical payload path. The live
    // provenance marker remains attached throughout validation and serialization.
    internal void SaveDeveloperPayload(string path, GalaxyState galaxy, double simulationDays)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        if (galaxy.DeveloperSession is null)
            throw new InvalidOperationException("Developer payload serialization requires explicit Developer session provenance.");
        SaveCore(path, galaxy, simulationDays);
    }

    private void SaveCore(string path, GalaxyState galaxy, double simulationDays)
    {
        ValidatePlanetaryReferences(galaxy);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var envelope = new CampaignSaveEnvelope
        {
            FormatVersion = galaxy.Colonies.Any(colony => colony.SurfaceBuildings.Count > 0) ? CurrentFormatVersion :
                galaxy.Systems.Any(system => system.CatalogPresetId is not null) ? PresetFormatVersion : LegacyFormatVersion,
            GameVersion = GameVersion.Current,
            SavedAtUtc = DateTimeOffset.UtcNow,
            SimulationDays = simulationDays,
            Galaxy = new GalaxySaveDto
            {
                Seed = galaxy.Seed,
                GenerationMetadata = galaxy.GenerationMetadata,
                Systems = ToSystemDtos(galaxy.Systems),
                Civilizations = ToCivilizationDtos(galaxy.Civilizations),
                Fleets = ToFleetDtos(galaxy.Fleets),
                Colonies = ToColonyDtos(galaxy.Colonies),
                Economies = ToEconomyDtos(galaxy.Economies),
                Technologies = ToTechnologyDtos(galaxy.Technologies),
                ConstructionStates = ToConstructionDtos(galaxy.ConstructionStates),
                ShipyardStates = ToShipyardDtos(galaxy.ShipyardStates),
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
        using (var document = JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("DeveloperFormatVersion", out _))
                throw new InvalidDataException("Developer campaign envelopes cannot be opened as Player saves.");
        }
        var envelope = JsonSerializer.Deserialize<CampaignSaveEnvelope>(json, JsonOptions)
            ?? throw new InvalidDataException("Save file did not contain a campaign envelope.");

        if (envelope.FormatVersion < 1 || envelope.FormatVersion > CurrentFormatVersion || envelope.FormatVersion is 9 or 11)
        {
            throw new InvalidDataException(
                $"Unsupported save format {envelope.FormatVersion}; maximum supported is {CurrentFormatVersion}.");
        }

        var simulationDays = envelope.FormatVersion >= 5
            ? envelope.SimulationDays
            : envelope.SimulationSeconds;
        var systems = ToSystems(envelope.Galaxy.Systems);

        IList<CivilizationState> civilizations;
        CivilizationKnowledgeState knowledge;
        int playerCivilizationId;

        if (envelope.FormatVersion == 1 || envelope.Galaxy.Civilizations.Count == 0)
        {
            civilizations = new CivilizationSeeder().Seed(
                systems,
                Math.Min(8, systems.Count),
                Math.Min(2, Math.Max(0, systems.Count - 8)),
                envelope.Galaxy.Seed);
            playerCivilizationId = civilizations.First(c => c.IsPlayer).Id;
            knowledge = CreateInitialKnowledge(systems, civilizations);
        }
        else
        {
            civilizations = ToCivilizations(
                envelope.Galaxy.Civilizations,
                legacyAlreadyWarpCapable: envelope.FormatVersion < 5,
                saveFormatVersion: envelope.FormatVersion,
                campaignSeed: envelope.Galaxy.Seed);
            playerCivilizationId = envelope.Galaxy.PlayerCivilizationId;
            knowledge = ToKnowledge(envelope.Galaxy.Knowledge);

            if (knowledge.GetKnownSystems(playerCivilizationId).Count == 0)
            {
                var player = civilizations.First(c => c.Id == playerCivilizationId);
                knowledge.MarkSystemFullySurveyed(player.Id, player.HomeSystemId);
                knowledge.RevealWithinSensorRange(
                    player.Id,
                    player.HomeSystemId,
                    systems,
                    player.IsSeededAncient ? 420.0f : 95.0f);
            }
        }

        // An empty fleet list is valid in current campaigns: ships begin as paid shipyard
        // orders and only become fleets after construction completes. Seed prototype fleets
        // solely for formats that predate fleet persistence; otherwise loading would create a
        // free vessel and change the campaign merely because no ship has finished yet.
        IList<FleetState> fleets = envelope.FormatVersion < 3
            ? new FleetSeeder().Seed(systems, civilizations)
            : ToFleets(
                    envelope.Galaxy.Fleets,
                    civilizations,
                    envelope.FormatVersion,
                    restoreUnserializedShipbuildingPopulation: envelope.FormatVersion >= 7)
                .ToList();

        IList<ColonyState> colonies;
        IReadOnlyList<CivilizationEconomyState> economies;
        if (envelope.FormatVersion >= CurrentFormatVersion)
        {
            if (envelope.Galaxy.Colonies is null || envelope.Galaxy.Colonies.Count == 0 ||
                envelope.Galaxy.Economies is null || envelope.Galaxy.Economies.Count == 0)
                throw new InvalidDataException("Surface-aware saves require their authoritative colonies and economies; they cannot be reseeded.");
            foreach (var economy in envelope.Galaxy.Economies)
                ValidateEconomyStock(economy.CivilizationId, economy.Credits, economy.Industry, economy.Science);
            if (envelope.Galaxy.Economies.GroupBy(item => item.CivilizationId).Any(group => group.Count() != 1) ||
                civilizations.Any(civilization => !envelope.Galaxy.Economies.Any(item => item.CivilizationId == civilization.Id)))
                throw new InvalidDataException("Surface-aware saves require one economy for each civilization.");
        }

        if (envelope.FormatVersion < 4 ||
            envelope.Galaxy.Colonies.Count == 0 ||
            envelope.Galaxy.Economies.Count == 0)
        {
            var colonySeeder = new ColonySeeder();
            colonies = colonySeeder.Seed(civilizations);
            economies = colonySeeder.SeedEconomies(civilizations);
        }
        else
        {
            colonies = ToColonies(
                    envelope.Galaxy.Colonies,
                    civilizations,
                    envelope.FormatVersion)
                .ToList();
            economies = ToEconomies(envelope.Galaxy.Economies);
        }

        // Pre-shipbuilding saves had free prototype colony fleets. When migrating those
        // campaigns, reserve real population now so later colony founding cannot create it.
        if (envelope.FormatVersion < 7)
            EnsureLegacyExpansionFleets(fleets, systems, civilizations, colonies);

        IList<TechnologyState> technologies = envelope.FormatVersion < 5 ||
                                              envelope.Galaxy.Technologies.Count == 0
            ? CreateMigratedTechnologyStates(civilizations)
            : ToTechnologies(envelope.Galaxy.Technologies);

        IList<ConstructionState> construction = envelope.FormatVersion < 6 ||
                                                envelope.Galaxy.ConstructionStates.Count == 0
            ? CreateMigratedConstructionStates(civilizations)
            : ToConstructionStates(envelope.Galaxy.ConstructionStates);

        IList<ShipyardState> shipyards = envelope.FormatVersion < 7 ||
                                         envelope.Galaxy.ShipyardStates.Count == 0
            ? new ShipyardSeeder().Seed(civilizations)
            : ToShipyardStates(
                envelope.Galaxy.ShipyardStates,
                civilizations,
                envelope.FormatVersion);

        // Saves that predate explicit fleet-combat persistence are upgraded in memory to the
        // registry baseline. Explicit Combat DTO state is restored above when present.
        foreach (var fleet in fleets)
            CombatProfileRegistry.EnsureState(fleet);

        var galaxy = new GalaxyState
        {
            Seed = envelope.Galaxy.Seed,
            GenerationMetadata = ValidateGenerationMetadata(
                envelope.Galaxy.GenerationMetadata,
                envelope.Galaxy.Seed,
                systems.Count),
            Systems = systems,
            Civilizations = civilizations,
            Fleets = fleets,
            Colonies = colonies,
            Economies = economies,
            Technologies = technologies,
            ConstructionStates = construction,
            ShipyardStates = shipyards,
            PlayerCivilizationId = playerCivilizationId,
            Knowledge = knowledge,
        };

        ValidatePlanetaryReferences(galaxy);

        return new LoadedCampaign(
            galaxy,
            simulationDays,
            envelope.GameVersion,
            envelope.SavedAtUtc);
    }

    private static GalaxyGenerationMetadata? ValidateGenerationMetadata(
        GalaxyGenerationMetadata? metadata,
        long seed,
        int systemCount)
    {
        // Metadata was introduced after the existing save formats and is intentionally
        // optional so older campaigns continue to load unchanged.
        if (metadata is null)
            return null;
        if (string.IsNullOrWhiteSpace(metadata.EnteredSeed) ||
            string.IsNullOrWhiteSpace(metadata.GeneratorVersion) ||
            metadata.InternalSeed != seed ||
            metadata.SystemCount != systemCount ||
            metadata.SystemCount <= 0 ||
            metadata.OtherCivilizations < 0 ||
            metadata.GuaranteedNearbyHabitableWorlds < 0)
            throw new InvalidDataException("Campaign generation metadata is invalid or does not match the saved galaxy.");
        return metadata;
    }

    private static CivilizationKnowledgeState CreateInitialKnowledge(
        IReadOnlyList<StarSystemState> systems,
        IList<CivilizationState> civilizations)
    {
        var knowledge = new CivilizationKnowledgeState();
        foreach (var civilization in civilizations)
        {
            knowledge.MarkSystemFullySurveyed(civilization.Id, civilization.HomeSystemId);
            knowledge.RevealWithinSensorRange(
                civilization.Id,
                civilization.HomeSystemId,
                systems,
                civilization.IsSeededAncient ? 420.0f : 95.0f);
        }

        return knowledge;
    }

    private static IList<TechnologyState> CreateMigratedTechnologyStates(
        IList<CivilizationState> civilizations)
    {
        var states = new TechnologySeeder().Seed(civilizations);
        foreach (var civilization in civilizations)
        {
            if (civilization.DevelopmentStage != CivilizationDevelopmentStage.WarpCapable)
                continue;

            var state = states.First(t => t.CivilizationId == civilization.Id);
            foreach (var technology in TechnologyRegistry.All)
                state.CompletedTechnologyIds.Add(technology.Id);
        }

        return states;
    }

    private static IList<ConstructionState> CreateMigratedConstructionStates(
        IList<CivilizationState> civilizations)
    {
        var states = new ConstructionSeeder().Seed(civilizations);
        foreach (var civilization in civilizations)
        {
            if (civilization.DevelopmentStage != CivilizationDevelopmentStage.WarpCapable)
                continue;

            var state = states.First(c => c.CivilizationId == civilization.Id);
            foreach (var project in ConstructionRegistry.All)
                state.CompletedProjectIds.Add(project.Id);
        }

        return states;
    }

    private static void EnsureLegacyExpansionFleets(
        IList<FleetState> fleets,
        IReadOnlyList<StarSystemState> systems,
        IList<CivilizationState> civilizations,
        IList<ColonyState> colonies)
    {
        var colonyDesign = ShipDesignRegistry.All.FirstOrDefault(
            design => design.Role == FleetRole.Colony);
        if (colonyDesign is null || colonyDesign.PopulationCostMillions <= 0.0)
            return;

        var nextId = fleets.Count == 0 ? 0 : fleets.Max(f => f.Id) + 1;
        foreach (var civilization in civilizations)
        {
            if (civilization.DevelopmentStage != CivilizationDevelopmentStage.WarpCapable ||
                !civilization.ExpansionAllowed)
            {
                continue;
            }

            var existing = fleets.FirstOrDefault(f =>
                f.IsActive &&
                f.CivilizationId == civilization.Id &&
                f.Role == FleetRole.Colony);
            if (existing is not null && existing.EmbarkedPopulationMillions > 0.0)
                continue;

            var source = colonies
                .Where(colony => colony.CivilizationId == civilization.Id)
                .OrderByDescending(colony => colony.PopulationMillions)
                .FirstOrDefault();
            if (source is null ||
                source.PopulationMillions < colonyDesign.PopulationCostMillions + 500.0)
            {
                continue;
            }

            var sourceSpeciesId = RequireKnownPopulationSpeciesId(
                source.PopulationSpeciesId,
                $"source colony {source.Id}");
            source.PopulationMillions -= colonyDesign.PopulationCostMillions;

            if (existing is not null)
            {
                existing.EmbarkedPopulationMillions = colonyDesign.PopulationCostMillions;
                existing.EmbarkedPopulationSpeciesId = sourceSpeciesId;
                CombatProfileRegistry.EnsureState(existing);
                continue;
            }

            var home = systems.First(s => s.Id == civilization.HomeSystemId);
            var fleet = new FleetState
            {
                Id = nextId++,
                CivilizationId = civilization.Id,
                Name = civilization.IsPlayer
                    ? "Pioneer One"
                    : $"{civilization.Name} Pioneer",
                Role = FleetRole.Colony,
                Position = home.Position,
                CurrentSystemId = home.Id,
                StrategicSpeed = colonyDesign.StrategicSpeed,
                SensorRange = colonyDesign.SensorRange,
                IsActive = true,
                EmbarkedPopulationMillions = colonyDesign.PopulationCostMillions,
                EmbarkedPopulationSpeciesId = sourceSpeciesId,
                Combat = CombatProfileRegistry.CreateInitialState(
                    colonyDesign.CombatProfileId,
                    FleetRole.Colony),
            };
            fleets.Add(fleet);
        }
    }

    private static List<StarSystemState> ToSystems(
        IReadOnlyList<StarSystemSaveDto> dtos) =>
        dtos.Select(d => new StarSystemState(
                d.Id,
                d.Name,
                new Vector2(d.X, d.Y),
                d.Archetype,
                d.HasHabitableWorld,
                d.HasAnomaly,
                d.HasRareResource,
                d.HasPreWarpCivilization,
                d.CatalogPresetId,
                d.StellarClass))
            .ToList();

    private static IList<CivilizationState> ToCivilizations(
        IReadOnlyList<CivilizationSaveDto> dtos,
        bool legacyAlreadyWarpCapable,
        int saveFormatVersion,
        long campaignSeed)
    {
        return dtos.Select(d => new CivilizationState(
                d.Id,
                d.Name,
                d.HomeSystemId,
                d.Archetype,
                new CivilizationTraits(
                    d.Aggression,
                    d.Territoriality,
                    d.Greed,
                    d.ScientificCuriosity,
                    d.RiskTolerance,
                    d.SurvivalPriority,
                    d.HonorBound),
                d.IsPlayer,
                legacyAlreadyWarpCapable
                    ? CivilizationDevelopmentStage.WarpCapable
                    : d.DevelopmentStage,
                legacyAlreadyWarpCapable ? false : d.IsSeededAncient,
                legacyAlreadyWarpCapable ? true : d.ExpansionAllowed,
                legacyAlreadyWarpCapable ? false : d.NeutralUnlessProvoked,
                saveFormatVersion < 8
                    ? SpeciesAssignmentPolicy.Assign(campaignSeed, d.Id)
                    : RequireKnownSpeciesId(d.SpeciesId, d.Id))
            {
                Leadership = d.Leadership is null
                    ? CivilizationLeadershipState.CreateFoundingRoster(d.Id,
                        (saveFormatVersion < 8 ? SpeciesAssignmentPolicy.Assign(campaignSeed, d.Id) : d.SpeciesId) == SpeciesCatalog.TerranBaselineId)
                    : CivilizationLeadershipState.Restore(d.Leadership),
            })
            .ToList();
    }

    private static string RequireKnownSpeciesId(string speciesId, int civilizationId)
    {
        if (string.IsNullOrWhiteSpace(speciesId) ||
            !SpeciesCatalog.TryGet(speciesId, out _))
        {
            throw new InvalidDataException(
                $"Civilization {civilizationId} references unknown species ID '{speciesId}'.");
        }

        return speciesId;
    }

    private static string RequireKnownPopulationSpeciesId(
        string? speciesId,
        string owner)
    {
        if (string.IsNullOrWhiteSpace(speciesId) ||
            !SpeciesCatalog.TryGet(speciesId, out _))
        {
            throw new InvalidDataException(
                $"{owner} references unknown population species ID '{speciesId}'.");
        }

        return speciesId;
    }

    private static string GetCivilizationSpeciesId(
        IList<CivilizationState> civilizations,
        int civilizationId)
    {
        var civilization = civilizations.FirstOrDefault(c => c.Id == civilizationId)
            ?? throw new InvalidDataException(
                $"Population state references unknown civilization {civilizationId}.");

        return RequireKnownPopulationSpeciesId(
            civilization.SpeciesId,
            $"civilization {civilizationId}");
    }

    private static string ResolvePopulationSpeciesId(
        string? savedSpeciesId,
        int civilizationId,
        IList<CivilizationState> civilizations,
        int saveFormatVersion,
        string owner)
    {
        return saveFormatVersion < 8
            ? GetCivilizationSpeciesId(civilizations, civilizationId)
            : RequireKnownPopulationSpeciesId(savedSpeciesId, owner);
    }

    private static IReadOnlyList<FleetState> ToFleets(
        IReadOnlyList<FleetSaveDto> dtos,
        IList<CivilizationState> civilizations,
        int saveFormatVersion,
        bool restoreUnserializedShipbuildingPopulation)
    {
        var colonyPopulation = ShipDesignRegistry.All.FirstOrDefault(
                design => design.Role == FleetRole.Colony)
            ?.PopulationCostMillions ?? 0.0;

        var fleets = new List<FleetState>(dtos.Count);
        foreach (var dto in dtos)
        {
            var embarkedPopulation = Math.Max(
                0.0,
                dto.EmbarkedPopulationMillions ??
                (restoreUnserializedShipbuildingPopulation && dto.Role == FleetRole.Colony
                    ? colonyPopulation
                    : 0.0));

            var embarkedSpeciesId = embarkedPopulation > 0.0
                ? ResolvePopulationSpeciesId(
                    dto.EmbarkedPopulationSpeciesId,
                    dto.CivilizationId,
                    civilizations,
                    saveFormatVersion,
                    $"fleet {dto.Id}")
                : null;

            var fleet = new FleetState
            {
                Id = dto.Id,
                CivilizationId = dto.CivilizationId,
                Name = dto.Name,
                Role = dto.Role,
                DesignId = dto.DesignId,
                Position = new Vector2(dto.X, dto.Y),
                CurrentSystemId = dto.CurrentSystemId,
                DestinationSystemId = dto.DestinationSystemId,
                PlannedRouteSystemIds = dto.PlannedRouteSystemIds ?? new List<int>(),
                DestinationPlanetaryBodyId = saveFormatVersion >= 8
                    ? dto.DestinationPlanetaryBodyId
                    : null,
                SettlementBodyId = dto.SettlementBodyId,
                PreventAutomaticSettlement = dto.PreventAutomaticSettlement,
                SettlementDaysCompleted = dto.SettlementDaysCompleted,
                ReconnaissanceSystemId = dto.ReconnaissanceSystemId,
                ReconnaissanceDaysCompleted = dto.ReconnaissanceDaysCompleted,
                FreightTargetOutpostId = dto.FreightTargetOutpostId,
                FreightHomeColonyId = dto.FreightHomeColonyId,
                CargoMaterialCapacity = dto.CargoMaterialCapacity,
                CargoMaterials = dto.CargoMaterials,
                StrategicSpeed = dto.StrategicSpeed,
                MaximumLegRangeLightYears = dto.MaximumLegRangeLightYears > 0.0
                    ? dto.MaximumLegRangeLightYears
                    : 360.0,
                FuelCapacityLightYears = dto.FuelCapacityLightYears > 0.0
                    ? dto.FuelCapacityLightYears
                    : 1000.0,
                FuelRemainingLightYears = dto.FuelRemainingLightYears ??
                    (dto.FuelCapacityLightYears > 0.0 ? dto.FuelCapacityLightYears : 1000.0),
                SensorRange = dto.SensorRange,
                IsActive = dto.IsActive,
                EmbarkedPopulationMillions = embarkedPopulation,
                EmbarkedPopulationSpeciesId = embarkedSpeciesId,
                Combat = dto.Combat is null
                    ? null
                    : new FleetCombatState
                    {
                        ProfileId = dto.Combat.ProfileId,
                        Shields = dto.Combat.Shields,
                        Armor = dto.Combat.Armor,
                        Hull = dto.Combat.Hull,
                        WeaponCooldownRemainingDays = dto.Combat.WeaponCooldownRemainingDays,
                        Order = dto.Combat.Order,
                        TargetFleetId = dto.Combat.TargetFleetId,
                        DefendSystemId = dto.Combat.DefendSystemId,
                        RetreatProgressDays = dto.Combat.RetreatProgressDays,
                        RetreatStarted = dto.Combat.RetreatStarted,
                        IsDisengaged = dto.Combat.IsDisengaged,
                        DisengagedSystemId = dto.Combat.DisengagedSystemId,
                    },
            };

            CombatProfileRegistry.EnsureState(fleet);
            fleets.Add(fleet);
        }

        return fleets;
    }

    private static IReadOnlyList<ColonyState> ToColonies(
        IReadOnlyList<ColonySaveDto> dtos,
        IList<CivilizationState> civilizations,
        int saveFormatVersion)
    {
        return dtos.Select(d => new ColonyState
            {
                Id = d.Id,
                CivilizationId = d.CivilizationId,
                SystemId = d.SystemId,
                PlanetaryBodyId = saveFormatVersion >= 8 ? d.PlanetaryBodyId : null,
                Name = d.Name,
                Kind = d.Kind,
                PopulationSpeciesId = ResolvePopulationSpeciesId(
                    d.PopulationSpeciesId,
                    d.CivilizationId,
                    civilizations,
                    saveFormatVersion,
                    $"colony {d.Id}"),
                PopulationMillions = d.PopulationMillions,
                Infrastructure = d.Infrastructure,
                Stability = d.Stability,
                StoredFoodPopulationDaysMillions = d.StoredFoodPopulationDaysMillions,
                StoredWaterPopulationDaysMillions = d.StoredWaterPopulationDaysMillions,
                StoredExtractedMaterials = d.StoredExtractedMaterials,
                RemainingExtractableMaterials = d.RemainingExtractableMaterials,
                SurfaceHubLevel = d.SurfaceHubLevel ?? 3,
                SurfaceHubUpgradeDaysRemaining = d.SurfaceHubUpgradeDaysRemaining,
                SurfaceBuildings = RestoreSurfaceBuildings(d, saveFormatVersion),
            })
            .ToArray();
    }

    private static List<SurfaceBuildingState> RestoreSurfaceBuildings(ColonySaveDto dto, int version)
    {
        if (version < CurrentFormatVersion && dto.SurfaceBuildings is { Count: > 0 })
            throw new InvalidDataException($"Colony {dto.Id} surface construction requires save format {CurrentFormatVersion}.");
        if (version >= CurrentFormatVersion && dto.SurfaceBuildings is null)
            throw new InvalidDataException($"Colony {dto.Id} is missing its surface construction collection.");
        return dto.SurfaceBuildings ?? new List<SurfaceBuildingState>();
    }

    private static IReadOnlyList<CivilizationEconomyState> ToEconomies(
        IReadOnlyList<EconomySaveDto> dtos) =>
        dtos.Select(d =>
        {
            if (!double.IsFinite(d.LastResearchSpendingPerDay) || d.LastResearchSpendingPerDay < 0.0 ||
                !double.IsFinite(d.LastResearchFundingFraction) ||
                d.LastResearchFundingFraction is < 0.0 or > 1.0 ||
                !double.IsFinite(d.OperatingArrears) || d.OperatingArrears < 0.0 ||
                !double.IsFinite(d.LastBaseOperationsFundingFraction) ||
                d.LastBaseOperationsFundingFraction is < 0.0 or > 1.0)
            {
                throw new InvalidDataException(
                    $"Civilization {d.CivilizationId} has invalid research funding state.");
            }
            if (d.IndustryPriority is not null && !Enum.IsDefined(d.IndustryPriority.Value))
                throw new InvalidDataException($"Civilization {d.CivilizationId} has an unknown industry priority.");
            return new CivilizationEconomyState
            {
                CivilizationId = d.CivilizationId,
                Credits = d.Credits,
                Industry = d.Industry,
                Science = d.Science,
                LastCreditsPerSecond = d.LastCreditsPerSecond,
                LastIndustryPerSecond = d.LastIndustryPerSecond,
                LastSciencePerSecond = d.LastSciencePerSecond,
                LastResearchSpendingPerDay = d.LastResearchSpendingPerDay,
                LastResearchFundingFraction = d.LastResearchFundingFraction,
                OperatingArrears = d.OperatingArrears,
                LastBaseOperationsFundingFraction = d.LastBaseOperationsFundingFraction,
                IndustryPriority = d.IndustryPriority,
            };
        })
            .ToArray();

    private static IList<TechnologyState> ToTechnologies(
        IReadOnlyList<TechnologySaveDto> dtos)
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
            foreach (var id in dto.CompletedTechnologyIds)
                state.CompletedTechnologyIds.Add(id);
            result.Add(state);
        }

        return result;
    }

    private static IList<ConstructionState> ToConstructionStates(
        IReadOnlyList<ConstructionSaveDto> dtos)
    {
        var result = new List<ConstructionState>(dtos.Count);
        foreach (var dto in dtos)
        {
            var state = new ConstructionState
            {
                CivilizationId = dto.CivilizationId,
                ActiveProjectId = dto.ActiveProjectId,
                ActiveProjectProgress = dto.ActiveProjectProgress,
                ActiveProjectAuthorizationCredits = dto.ActiveProjectAuthorizationCredits,
            };
            ValidateConstructionStateDto(dto);
            foreach (var id in dto.CompletedProjectIds)
                state.CompletedProjectIds.Add(id);
            foreach (var order in dto.QueuedProjects)
                state.QueuedProjects.Add(new QueuedConstructionProject(order.ProjectId, order.AuthorizationCredits));
            result.Add(state);
        }

        return result;
    }

    private static void ValidateConstructionStateDto(ConstructionSaveDto dto)
    {
        var known = ConstructionRegistry.All.Select(project => project.Id).ToHashSet(StringComparer.Ordinal);
        if (dto.CompletedProjectIds is null || dto.QueuedProjects is null)
            throw new InvalidDataException($"Construction state {dto.CivilizationId} is missing required collections.");
        if (!double.IsFinite(dto.ActiveProjectProgress) || dto.ActiveProjectProgress < 0 ||
            !double.IsFinite(dto.ActiveProjectAuthorizationCredits) || dto.ActiveProjectAuthorizationCredits < 0)
            throw new InvalidDataException($"Construction state {dto.CivilizationId} has invalid active progress or authorization.");
        if (dto.ActiveProjectId is not null && !known.Contains(dto.ActiveProjectId))
            throw new InvalidDataException($"Construction state {dto.CivilizationId} references an unknown active project.");
        if (dto.ActiveProjectId is null && (dto.ActiveProjectProgress != 0 || dto.ActiveProjectAuthorizationCredits != 0))
            throw new InvalidDataException($"Construction state {dto.CivilizationId} has active state without a project.");
        if (dto.ActiveProjectId is { } activeId && dto.ActiveProjectProgress > ConstructionRegistry.Get(activeId).IndustryCost + 0.0001)
            throw new InvalidDataException($"Construction state {dto.CivilizationId} exceeds active project materials.");
        if (dto.CompletedProjectIds.Any(id => !known.Contains(id)) || dto.CompletedProjectIds.Distinct(StringComparer.Ordinal).Count() != dto.CompletedProjectIds.Count)
            throw new InvalidDataException($"Construction state {dto.CivilizationId} has invalid completed projects.");
        if (dto.ActiveProjectId is { } active && dto.CompletedProjectIds.Contains(active, StringComparer.Ordinal))
            throw new InvalidDataException($"Construction state {dto.CivilizationId} overlaps active and completed projects.");
        if (dto.QueuedProjects.Count > ConstructionState.MaxQueuedProjects ||
            dto.QueuedProjects.Any(order => order is null || string.IsNullOrWhiteSpace(order.ProjectId) || !known.Contains(order.ProjectId) || !double.IsFinite(order.AuthorizationCredits) || order.AuthorizationCredits < 0) ||
            dto.QueuedProjects.Select(order => order.ProjectId).Distinct(StringComparer.Ordinal).Count() != dto.QueuedProjects.Count ||
            dto.QueuedProjects.Any(order => dto.CompletedProjectIds.Contains(order.ProjectId, StringComparer.Ordinal) || order.ProjectId == dto.ActiveProjectId))
            throw new InvalidDataException($"Construction state {dto.CivilizationId} has an invalid queued project.");
    }

    private static IList<ShipyardState> ToShipyardStates(
        IReadOnlyList<ShipyardSaveDto> dtos,
        IList<CivilizationState> civilizations,
        int saveFormatVersion)
    {
        var knownDesignIds = ShipDesignRegistry.All
            .Select(design => design.Id)
            .ToHashSet(StringComparer.Ordinal);
        var result = new List<ShipyardState>(dtos.Count);

        foreach (var dto in dtos)
        {
            if (!double.IsFinite(dto.ActiveBuildProgress) || dto.ActiveBuildProgress < 0 || !double.IsFinite(dto.ActiveAuthorizationCredits) || dto.ActiveAuthorizationCredits < 0 || dto.NextOrderSequence <= 0 || dto.QueuedBuilds is null || dto.QueuedBuilds.Any(build => build is null || !double.IsFinite(build.AuthorizationCredits) || build.AuthorizationCredits < 0 || !double.IsFinite(build.ReservedPopulationMillions) || build.ReservedPopulationMillions < 0))
                throw new InvalidDataException($"Shipyard {dto.CivilizationId} has invalid order accounting.");
            var orderIds = dto.QueuedBuilds.Where(build => !string.IsNullOrWhiteSpace(build.OrderId)).Select(build => build.OrderId!).ToList();
            if (!string.IsNullOrWhiteSpace(dto.ActiveOrderId)) orderIds.Add(dto.ActiveOrderId);
            if (orderIds.Distinct(StringComparer.Ordinal).Count() != orderIds.Count)
                throw new InvalidDataException($"Shipyard {dto.CivilizationId} has duplicate order identities.");
            var reservedPopulation = Math.Max(0.0, dto.ReservedPopulationMillions);
            var activeDesignId = string.IsNullOrWhiteSpace(dto.ActiveDesignId)
                ? null
                : dto.ActiveDesignId;

            if (activeDesignId is not null && !knownDesignIds.Contains(activeDesignId))
            {
                if (reservedPopulation > 0.0)
                {
                    throw new InvalidDataException(
                        $"Shipyard {dto.CivilizationId} active build '{activeDesignId}' is unknown but retains {reservedPopulation:0.###} million reserved population; refusing to discard reserved colonists.");
                }

                // An unknown zero-population build is safe to discard as corrupt queue metadata.
                activeDesignId = null;
            }

            if (activeDesignId is null && reservedPopulation > 0.0)
            {
                throw new InvalidDataException(
                    $"Shipyard {dto.CivilizationId} retains {reservedPopulation:0.###} million reserved population without a valid active design.");
            }

            var state = new ShipyardState
            {
                CivilizationId = dto.CivilizationId,
                NextOrderSequence = dto.NextOrderSequence,
                ActiveDesignId = activeDesignId,
                ActiveOrderId = dto.ActiveOrderId,
                ActiveBuildProgress = activeDesignId is null ? 0.0 : dto.ActiveBuildProgress,
                ActiveAuthorizationCredits = Math.Max(0.0, dto.ActiveAuthorizationCredits),
                ReservedPopulationMillions = reservedPopulation,
                ReservedPopulationSpeciesId = reservedPopulation > 0.0
                    ? ResolvePopulationSpeciesId(
                        dto.ReservedPopulationSpeciesId,
                        dto.CivilizationId,
                        civilizations,
                        saveFormatVersion,
                        $"shipyard {dto.CivilizationId} active reservation")
                    : null,
                ReservedPopulationSourceColonyId = reservedPopulation > 0.0 ? dto.ReservedPopulationSourceColonyId : null,
            };
            if (state.ActiveDesignId is not null && string.IsNullOrWhiteSpace(state.ActiveOrderId))
                state.ActiveOrderId = $"legacy-{dto.CivilizationId}-active";

            var availableQueueSlots = ShipyardState.MaxPendingBuilds -
                                      (state.ActiveDesignId is null ? 0 : 1);
            var acceptedQueueEntries = 0;

            foreach (var queued in dto.QueuedBuilds)
            {
                var queuedPopulation = Math.Max(0.0, queued.ReservedPopulationMillions);
                var hasKnownDesign = !string.IsNullOrWhiteSpace(queued.DesignId) &&
                                     knownDesignIds.Contains(queued.DesignId);

                if (!hasKnownDesign)
                {
                    if (queuedPopulation > 0.0)
                    {
                        throw new InvalidDataException(
                            $"Shipyard {dto.CivilizationId} queued build '{queued.DesignId}' is invalid but retains {queuedPopulation:0.###} million reserved population; refusing to discard reserved colonists.");
                    }

                    // Invalid zero-population metadata can be dropped without changing people.
                    continue;
                }

                if (acceptedQueueEntries >= Math.Max(0, availableQueueSlots))
                {
                    if (queuedPopulation > 0.0)
                    {
                        throw new InvalidDataException(
                            $"Shipyard {dto.CivilizationId} queue exceeds the bounded maximum while overflow build '{queued.DesignId}' retains {queuedPopulation:0.###} million reserved population; refusing to truncate reserved colonists.");
                    }

                    // Overflow with no population payload is safe to clamp away.
                    continue;
                }

                state.QueuedBuilds.Add(new ShipBuildOrderState
                {
                    OrderId = string.IsNullOrWhiteSpace(queued.OrderId) ? $"legacy-{dto.CivilizationId}-queued-{acceptedQueueEntries + 1}" : queued.OrderId,
                    DesignId = queued.DesignId,
                    AuthorizationCredits = Math.Max(0.0, queued.AuthorizationCredits),
                    ReservedPopulationMillions = queuedPopulation,
                    ReservedPopulationSpeciesId = queuedPopulation > 0.0
                        ? ResolvePopulationSpeciesId(
                            queued.ReservedPopulationSpeciesId,
                            dto.CivilizationId,
                            civilizations,
                            saveFormatVersion,
                            $"shipyard {dto.CivilizationId} queued reservation")
                        : null,
                    ReservedPopulationSourceColonyId = queuedPopulation > 0.0 ? queued.ReservedPopulationSourceColonyId : null,
                });
                acceptedQueueEntries++;
            }

            result.Add(state);
        }

        return result;
    }

    private static CivilizationKnowledgeState ToKnowledge(
        IReadOnlyList<CivilizationKnowledgeSaveDto> dtos)
    {
        var knowledge = new CivilizationKnowledgeState();
        foreach (var dto in dtos)
        {
            if (dto.SystemSurveys.Count == 0)
            {
                // Legacy saves used "known" to mean all system facts were available.
                foreach (var systemId in dto.KnownSystemIds)
                    knowledge.MarkSystemFullySurveyed(dto.CivilizationId, systemId);
            }
            else
            {
                foreach (var systemId in dto.KnownSystemIds)
                    knowledge.RevealSystem(dto.CivilizationId, systemId);

                foreach (var survey in dto.SystemSurveys)
                {
                    switch (survey.Level)
                    {
                        case SystemSurveyLevel.Unknown:
                            break;
                        case SystemSurveyLevel.Detected:
                            knowledge.RevealSystem(dto.CivilizationId, survey.SystemId);
                            break;
                        case SystemSurveyLevel.PartiallySurveyed:
                            knowledge.AdvanceSystemSurvey(
                                dto.CivilizationId,
                                survey.SystemId,
                                Math.Clamp(survey.Progress, 0.000001, 0.999999));
                            break;
                        case SystemSurveyLevel.FullySurveyed:
                            knowledge.MarkSystemFullySurveyed(
                                dto.CivilizationId,
                                survey.SystemId);
                            break;
                    }
                }
            }

            foreach (var civilizationId in dto.KnownCivilizationIds)
                knowledge.RevealCivilization(dto.CivilizationId, civilizationId);
        }

        return knowledge;
    }

    private static void ValidatePlanetaryReferences(GalaxyState galaxy)
    {
        if (galaxy.Colonies.Any(colony => colony.SurfaceBuildings is { Count: > 0 }))
        {
            foreach (var economy in galaxy.Economies)
                ValidateEconomyStock(economy.CivilizationId, economy.Credits, economy.Industry, economy.Science);
            if (galaxy.Civilizations.Any(civilization => galaxy.Economies.Count(item => item.CivilizationId == civilization.Id) != 1))
                throw new InvalidDataException("Surface construction requires one authoritative economy for each civilization.");
        }
        var bodies = galaxy.PlanetaryBodies.ToDictionary(body => body.Id);
        var systemIds = galaxy.Systems.Select(system => system.Id).ToHashSet();

        foreach (var colony in galaxy.Colonies)
        {
            if (!Enum.IsDefined(colony.Kind))
                throw new InvalidDataException($"Settlement {colony.Id} has an unknown settlement kind.");
            if (!double.IsFinite(colony.StoredExtractedMaterials) || colony.StoredExtractedMaterials < 0.0)
                throw new InvalidDataException($"Settlement {colony.Id} has invalid extracted-material storage.");
            if (colony.RemainingExtractableMaterials is double remainingDeposit &&
                (!double.IsFinite(remainingDeposit) || remainingDeposit < 0.0))
                throw new InvalidDataException($"Settlement {colony.Id} has an invalid remaining resource deposit.");
            if (colony.SurfaceHubLevel is < 1 or > 3)
                throw new InvalidDataException($"Settlement {colony.Id} has an invalid surface hub level.");
            if (!double.IsFinite(colony.StoredFoodPopulationDaysMillions) || colony.StoredFoodPopulationDaysMillions < 0.0 ||
                !double.IsFinite(colony.StoredWaterPopulationDaysMillions) || colony.StoredWaterPopulationDaysMillions < 0.0)
                throw new InvalidDataException($"Settlement {colony.Id} has invalid food or potable-water reserves.");
            SurfaceConstruction.Validate(colony);
            if (colony.SurfaceBuildings.Count > SurfaceConstruction.GetBuildingCapacity(colony))
                throw new InvalidDataException($"Settlement {colony.Id} exceeds its represented hub module capacity.");
            if (colony.PlanetaryBodyId is not int bodyId)
            {
                if (colony.SurfaceBuildings.Count > 0)
                    throw new InvalidDataException($"Colony {colony.Id} has surface buildings without an exact planetary body.");
                continue;
            }

            if (!bodies.TryGetValue(bodyId, out var body) || body.SystemId != colony.SystemId)
            {
                throw new InvalidDataException(
                    $"Colony {colony.Id} references planetary body {bodyId} outside system {colony.SystemId}.");
            }
            if (colony.SurfaceBuildings.Count > 0 && !body.Environment.HasSolidSurface)
                throw new InvalidDataException($"Colony {colony.Id} has buildings on a body without solid ground.");
            var outpostOperations = ResourceOutpostOperations.GetSnapshot(galaxy, colony);
            if (outpostOperations.IsResourceOutpost && colony.StoredExtractedMaterials > outpostOperations.StorageCapacity + 0.000001)
                throw new InvalidDataException($"Settlement {colony.Id} stores more extracted material than its represented capacity.");
            if (outpostOperations.IsResourceOutpost && colony.RemainingExtractableMaterials is double remaining &&
                remaining + colony.StoredExtractedMaterials > outpostOperations.InitialDepositMaterials + 0.000001)
                throw new InvalidDataException($"Settlement {colony.Id} has more remaining and stored material than its represented deposit.");
        }

        foreach (var fleet in galaxy.Fleets)
        {
            if (fleet.DesignId is not null &&
                (!ShipDesignRegistry.TryGet(fleet.DesignId, out var design) || design!.Role != fleet.Role))
                throw new InvalidDataException($"Fleet {fleet.Id} references an unknown or role-incompatible ship design.");
            if (!double.IsFinite(fleet.MaximumLegRangeLightYears) || fleet.MaximumLegRangeLightYears <= 0.0)
                throw new InvalidDataException($"Fleet {fleet.Id} has an invalid maximum interstellar leg range.");
            if (!double.IsFinite(fleet.FuelCapacityLightYears) || fleet.FuelCapacityLightYears <= 0.0 ||
                !double.IsFinite(fleet.FuelRemainingLightYears) || fleet.FuelRemainingLightYears < 0.0 ||
                fleet.FuelRemainingLightYears > fleet.FuelCapacityLightYears + 0.000001)
                throw new InvalidDataException($"Fleet {fleet.Id} has invalid interstellar fuel endurance.");
            if (!double.IsFinite(fleet.CargoMaterialCapacity) || fleet.CargoMaterialCapacity < 0.0 ||
                !double.IsFinite(fleet.CargoMaterials) || fleet.CargoMaterials < 0.0 ||
                fleet.CargoMaterials > fleet.CargoMaterialCapacity + 0.000001)
                throw new InvalidDataException($"Fleet {fleet.Id} has invalid freight cargo state.");
            if ((fleet.FreightTargetOutpostId is not null || fleet.FreightHomeColonyId is not null || fleet.CargoMaterials > 0.0) &&
                fleet.Role != FleetRole.Logistics)
                throw new InvalidDataException($"Fleet {fleet.Id} carries freight mission state without a logistics role.");
            if (fleet.FreightTargetOutpostId is int outpostId && !galaxy.Colonies.Any(colony =>
                    colony.Id == outpostId && colony.CivilizationId == fleet.CivilizationId && colony.Kind == SettlementKind.ResourceOutpost))
                throw new InvalidDataException($"Fleet {fleet.Id} references an invalid freight outpost.");
            if (fleet.FreightHomeColonyId is int freightHomeId && !galaxy.Colonies.Any(colony =>
                    colony.Id == freightHomeId && colony.CivilizationId == fleet.CivilizationId && colony.Kind == SettlementKind.Colony))
                throw new InvalidDataException($"Fleet {fleet.Id} references an invalid freight home colony.");
            if (fleet.PlannedRouteSystemIds.Any(systemId => !systemIds.Contains(systemId)))
                throw new InvalidDataException($"Fleet {fleet.Id} has a route waypoint outside the generated galaxy.");
            if (fleet.DestinationSystemId is null && fleet.PlannedRouteSystemIds.Count > 0)
                throw new InvalidDataException($"Fleet {fleet.Id} has route waypoints without an active destination.");
            if (fleet.PlannedRouteSystemIds.Count > 0 &&
                fleet.PlannedRouteSystemIds[^1] != fleet.DestinationSystemId)
                throw new InvalidDataException($"Fleet {fleet.Id} route does not end at its mission destination.");

            if (!double.IsFinite(fleet.SettlementDaysCompleted) || fleet.SettlementDaysCompleted < 0 ||
                fleet.SettlementDaysCompleted > ColonizationSimulation.EstablishmentDays(fleet) ||
                !double.IsFinite(fleet.ReconnaissanceDaysCompleted) || fleet.ReconnaissanceDaysCompleted < 0 ||
                fleet.ReconnaissanceDaysCompleted > ExplorationSimulation.ScoutReconnaissanceDays)
                throw new InvalidDataException($"Fleet {fleet.Id} has invalid local-work progress.");
            if ((fleet.SettlementBodyId is null && fleet.SettlementDaysCompleted > 0) ||
                (fleet.ReconnaissanceSystemId is null && fleet.ReconnaissanceDaysCompleted > 0) ||
                (fleet.PreventAutomaticSettlement && (fleet.Role != FleetRole.Colony || fleet.SettlementBodyId is not null)))
                throw new InvalidDataException($"Fleet {fleet.Id} has local work without a valid order.");
            if (fleet.SettlementBodyId is int site &&
                (fleet.Role != FleetRole.Colony || !bodies.TryGetValue(site, out var siteBody) ||
                 fleet.CurrentSystemId != siteBody.SystemId || fleet.DestinationSystemId is not null))
                throw new InvalidDataException($"Fleet {fleet.Id} has an invalid settlement work site.");
            if (fleet.ReconnaissanceSystemId is int recon && (fleet.Role != FleetRole.Scout || !systemIds.Contains(recon)))
                throw new InvalidDataException($"Fleet {fleet.Id} has an invalid reconnaissance work site.");
            if (fleet.DestinationPlanetaryBodyId is not int bodyId)
                continue;

            var targetSystemId = fleet.DestinationSystemId ?? (fleet.SettlementBodyId == bodyId ? fleet.CurrentSystemId : null);
            if (fleet.Role != FleetRole.Colony || targetSystemId is not int systemId)
            {
                throw new InvalidDataException(
                    $"Fleet {fleet.Id} has a planetary-body target without an active colony-system destination.");
            }

            if (!bodies.TryGetValue(bodyId, out var body) || body.SystemId != systemId)
            {
                throw new InvalidDataException(
                    $"Fleet {fleet.Id} targets planetary body {bodyId} outside destination system {systemId}.");
            }
        }
    }

    private static void ValidateEconomyStock(int civilizationId, double credits, double industry, double science)
    {
        if (!double.IsFinite(credits) || credits < 0 || !double.IsFinite(industry) || industry < 0 || !double.IsFinite(science) || science < 0)
            throw new InvalidDataException($"Civilization {civilizationId} has invalid economy stock; resources must be finite and nonnegative.");
    }

    private static List<StarSystemSaveDto> ToSystemDtos(
        IReadOnlyList<StarSystemState> systems) =>
        systems.Select(s => new StarSystemSaveDto
            {
                Id = s.Id,
                Name = s.Name,
                X = s.Position.X,
                Y = s.Position.Y,
                Archetype = s.Archetype,
                HasHabitableWorld = s.HasHabitableWorld,
                HasAnomaly = s.HasAnomaly,
                HasRareResource = s.HasRareResource,
                HasPreWarpCivilization = s.HasPreWarpCivilization,
                CatalogPresetId = s.CatalogPresetId,
                StellarClass = s.StellarClass,
            })
            .ToList();

    private static List<CivilizationSaveDto> ToCivilizationDtos(
        IEnumerable<CivilizationState> civilizations) =>
        civilizations.Select(c => new CivilizationSaveDto
            {
                Id = c.Id,
                Name = c.Name,
                HomeSystemId = c.HomeSystemId,
                Archetype = c.Archetype,
                Aggression = c.Traits.Aggression,
                Territoriality = c.Traits.Territoriality,
                Greed = c.Traits.Greed,
                ScientificCuriosity = c.Traits.ScientificCuriosity,
                RiskTolerance = c.Traits.RiskTolerance,
                SurvivalPriority = c.Traits.SurvivalPriority,
                HonorBound = c.Traits.HonorBound,
                IsPlayer = c.IsPlayer,
                DevelopmentStage = c.DevelopmentStage,
                IsSeededAncient = c.IsSeededAncient,
                ExpansionAllowed = c.ExpansionAllowed,
                NeutralUnlessProvoked = c.NeutralUnlessProvoked,
                SpeciesId = RequireKnownSpeciesId(c.SpeciesId, c.Id),
                Leadership = new Dictionary<string, CivilizationCharacter>(c.Leadership.Offices),
            })
            .ToList();

    private static List<FleetSaveDto> ToFleetDtos(
        IEnumerable<FleetState> fleets)
    {
        return fleets.Select(fleet =>
        {
            var population = Math.Max(0.0, fleet.EmbarkedPopulationMillions);
            var combat = CombatProfileRegistry.EnsureState(fleet);

            return new FleetSaveDto
            {
                Id = fleet.Id,
                CivilizationId = fleet.CivilizationId,
                Name = fleet.Name,
                Role = fleet.Role,
                DesignId = fleet.DesignId,
                X = fleet.Position.X,
                Y = fleet.Position.Y,
                CurrentSystemId = fleet.CurrentSystemId,
                DestinationSystemId = fleet.DestinationSystemId,
                PlannedRouteSystemIds = fleet.PlannedRouteSystemIds.ToList(),
                DestinationPlanetaryBodyId = fleet.DestinationPlanetaryBodyId,
                SettlementBodyId = fleet.SettlementBodyId,
                PreventAutomaticSettlement = fleet.PreventAutomaticSettlement,
                SettlementDaysCompleted = fleet.SettlementDaysCompleted,
                ReconnaissanceSystemId = fleet.ReconnaissanceSystemId,
                ReconnaissanceDaysCompleted = fleet.ReconnaissanceDaysCompleted,
                FreightTargetOutpostId = fleet.FreightTargetOutpostId,
                FreightHomeColonyId = fleet.FreightHomeColonyId,
                CargoMaterialCapacity = fleet.CargoMaterialCapacity,
                CargoMaterials = fleet.CargoMaterials,
                StrategicSpeed = fleet.StrategicSpeed,
                MaximumLegRangeLightYears = fleet.MaximumLegRangeLightYears,
                FuelCapacityLightYears = fleet.FuelCapacityLightYears,
                FuelRemainingLightYears = fleet.FuelRemainingLightYears,
                SensorRange = fleet.SensorRange,
                IsActive = fleet.IsActive,
                EmbarkedPopulationMillions = population,
                EmbarkedPopulationSpeciesId = population > 0.0
                    ? RequireKnownPopulationSpeciesId(
                        fleet.EmbarkedPopulationSpeciesId,
                        $"fleet {fleet.Id}")
                    : null,
                Combat = new FleetCombatSaveDto
                {
                    ProfileId = combat.ProfileId,
                    Shields = combat.Shields,
                    Armor = combat.Armor,
                    Hull = combat.Hull,
                    WeaponCooldownRemainingDays = combat.WeaponCooldownRemainingDays,
                    Order = combat.Order,
                    TargetFleetId = combat.TargetFleetId,
                    DefendSystemId = combat.DefendSystemId,
                    RetreatProgressDays = combat.RetreatProgressDays,
                    RetreatStarted = combat.RetreatStarted,
                    IsDisengaged = combat.IsDisengaged,
                    DisengagedSystemId = combat.DisengagedSystemId,
                },
            };
        }).ToList();
    }

    private static List<ColonySaveDto> ToColonyDtos(
        IEnumerable<ColonyState> colonies) =>
        colonies.Select(c => new ColonySaveDto
            {
                Id = c.Id,
                CivilizationId = c.CivilizationId,
                SystemId = c.SystemId,
                PlanetaryBodyId = c.PlanetaryBodyId,
                Name = c.Name,
                Kind = c.Kind,
                PopulationSpeciesId = RequireKnownPopulationSpeciesId(
                    c.PopulationSpeciesId,
                    $"colony {c.Id}"),
                PopulationMillions = c.PopulationMillions,
                Infrastructure = c.Infrastructure,
                Stability = c.Stability,
                StoredFoodPopulationDaysMillions = c.StoredFoodPopulationDaysMillions,
                StoredWaterPopulationDaysMillions = c.StoredWaterPopulationDaysMillions,
                StoredExtractedMaterials = c.StoredExtractedMaterials,
                RemainingExtractableMaterials = c.RemainingExtractableMaterials,
                SurfaceHubLevel = c.SurfaceHubLevel,
                SurfaceHubUpgradeDaysRemaining = c.SurfaceHubUpgradeDaysRemaining,
                SurfaceBuildings = c.SurfaceBuildings,
            })
            .ToList();

    private static List<EconomySaveDto> ToEconomyDtos(
        IReadOnlyList<CivilizationEconomyState> economies) =>
        economies.Select(e =>
        {
            if (e.IndustryPriority is not null && !Enum.IsDefined(e.IndustryPriority.Value))
                throw new InvalidDataException($"Civilization {e.CivilizationId} has an unknown industry priority.");
            return new EconomySaveDto
            {
                CivilizationId = e.CivilizationId,
                Credits = e.Credits,
                Industry = e.Industry,
                Science = e.Science,
                LastCreditsPerSecond = e.LastCreditsPerSecond,
                LastIndustryPerSecond = e.LastIndustryPerSecond,
                LastSciencePerSecond = e.LastSciencePerSecond,
                LastResearchSpendingPerDay = e.LastResearchSpendingPerDay,
                LastResearchFundingFraction = e.LastResearchFundingFraction,
                OperatingArrears = e.OperatingArrears,
                LastBaseOperationsFundingFraction = e.LastBaseOperationsFundingFraction,
                IndustryPriority = e.IndustryPriority,
            };
        })
            .ToList();

    private static List<TechnologySaveDto> ToTechnologyDtos(
        IEnumerable<TechnologyState> technologies) =>
        technologies.Select(t => new TechnologySaveDto
            {
                CivilizationId = t.CivilizationId,
                CompletedTechnologyIds = t.CompletedTechnologyIds.OrderBy(id => id).ToList(),
                ActiveResearchId = t.ActiveResearchId,
                ActiveResearchProgress = t.ActiveResearchProgress,
            })
            .ToList();

    private static List<ConstructionSaveDto> ToConstructionDtos(
        IEnumerable<ConstructionState> states) =>
        states.Select(c => new ConstructionSaveDto
            {
                CivilizationId = c.CivilizationId,
                CompletedProjectIds = c.CompletedProjectIds.OrderBy(id => id).ToList(),
                ActiveProjectId = c.ActiveProjectId,
                ActiveProjectProgress = c.ActiveProjectProgress,
                ActiveProjectAuthorizationCredits = c.ActiveProjectAuthorizationCredits,
                QueuedProjects = c.QueuedProjects.Select(order => new QueuedConstructionProjectSaveDto
                {
                    ProjectId = order.ProjectId,
                    AuthorizationCredits = order.AuthorizationCredits,
                }).ToList(),
            })
            .ToList();

    private static List<ShipyardSaveDto> ToShipyardDtos(
        IEnumerable<ShipyardState> states) =>
        states.Select(s =>
        {
            var reservedPopulation = Math.Max(0.0, s.ReservedPopulationMillions);
            return new ShipyardSaveDto
            {
                CivilizationId = s.CivilizationId,
                ActiveDesignId = s.ActiveDesignId,
                NextOrderSequence = s.NextOrderSequence,
                ActiveOrderId = s.ActiveOrderId,
                ActiveBuildProgress = s.ActiveBuildProgress,
                ActiveAuthorizationCredits = s.ActiveAuthorizationCredits,
                ReservedPopulationMillions = reservedPopulation,
                ReservedPopulationSpeciesId = reservedPopulation > 0.0
                    ? RequireKnownPopulationSpeciesId(
                        s.ReservedPopulationSpeciesId,
                        $"shipyard {s.CivilizationId} active reservation")
                    : null,
                ReservedPopulationSourceColonyId = reservedPopulation > 0.0 ? s.ReservedPopulationSourceColonyId : null,
                QueuedBuilds = s.QueuedBuilds
                    .Take(ShipyardState.MaxPendingBuilds)
                    .Select(build =>
                    {
                        var queuedPopulation = Math.Max(0.0, build.ReservedPopulationMillions);
                        return new QueuedShipBuildSaveDto
                        {
                            OrderId = build.OrderId,
                            DesignId = build.DesignId,
                            AuthorizationCredits = build.AuthorizationCredits,
                            ReservedPopulationMillions = queuedPopulation,
                            ReservedPopulationSpeciesId = queuedPopulation > 0.0
                                ? RequireKnownPopulationSpeciesId(
                                    build.ReservedPopulationSpeciesId,
                                    $"shipyard {s.CivilizationId} queued reservation")
                                : null,
                            ReservedPopulationSourceColonyId = queuedPopulation > 0.0 ? build.ReservedPopulationSourceColonyId : null,
                        };
                    })
                    .ToList(),
            };
        }).ToList();

    private static List<CivilizationKnowledgeSaveDto> ToKnowledgeDtos(
        CivilizationKnowledgeState knowledge)
    {
        var snapshot = knowledge.Snapshot();
        var ids = snapshot.Systems.Keys
            .Concat(snapshot.Civilizations.Keys)
            .Distinct()
            .OrderBy(id => id);

        return ids.Select(id => new CivilizationKnowledgeSaveDto
            {
                CivilizationId = id,
                KnownSystemIds = snapshot.Systems.TryGetValue(id, out var systems)
                    ? systems.ToList()
                    : new List<int>(),
                KnownCivilizationIds = snapshot.Civilizations.TryGetValue(id, out var civilizations)
                    ? civilizations.ToList()
                    : new List<int>(),
                SystemSurveys = knowledge.GetSystemSurveyKnowledge(id)
                    .Select(survey => new SystemSurveySaveDto
                    {
                        SystemId = survey.SystemId,
                        Level = survey.Level,
                        Progress = survey.Progress,
                    })
                    .ToList(),
            })
            .ToList();
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
    public GalaxyGenerationMetadata? GenerationMetadata { get; set; }
    public List<StarSystemSaveDto> Systems { get; set; } = new();
    public List<CivilizationSaveDto> Civilizations { get; set; } = new();
    public List<FleetSaveDto> Fleets { get; set; } = new();
    public List<ColonySaveDto> Colonies { get; set; } = new();
    public List<EconomySaveDto> Economies { get; set; } = new();
    public List<TechnologySaveDto> Technologies { get; set; } = new();
    public List<ConstructionSaveDto> ConstructionStates { get; set; } = new();
    public List<ShipyardSaveDto> ShipyardStates { get; set; } = new();
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
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CatalogPresetId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public StellarPrimaryClass? StellarClass { get; set; }
}

public sealed class CivilizationSaveDto
{
    public Dictionary<string, CivilizationCharacter>? Leadership { get; set; }
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
    public CivilizationDevelopmentStage DevelopmentStage { get; set; }
    public bool IsSeededAncient { get; set; }
    public bool ExpansionAllowed { get; set; } = true;
    public bool NeutralUnlessProvoked { get; set; }
    public string SpeciesId { get; set; } = string.Empty;
}

public sealed class FleetSaveDto
{
    public int Id { get; set; }
    public int CivilizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public FleetRole Role { get; set; }
    public string? DesignId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public int? CurrentSystemId { get; set; }
    public int? DestinationSystemId { get; set; }
    public List<int>? PlannedRouteSystemIds { get; set; }
    public int? DestinationPlanetaryBodyId { get; set; }
    public bool PreventAutomaticSettlement { get; set; }
    public int? SettlementBodyId { get; set; }
    public double SettlementDaysCompleted { get; set; }
    public int? ReconnaissanceSystemId { get; set; }
    public double ReconnaissanceDaysCompleted { get; set; }
    public int? FreightTargetOutpostId { get; set; }
    public int? FreightHomeColonyId { get; set; }
    public double CargoMaterialCapacity { get; set; }
    public double CargoMaterials { get; set; }
    public double StrategicSpeed { get; set; }
    public double MaximumLegRangeLightYears { get; set; }
    public double FuelCapacityLightYears { get; set; }
    public double? FuelRemainingLightYears { get; set; }
    public float SensorRange { get; set; }
    public bool IsActive { get; set; } = true;
    public double? EmbarkedPopulationMillions { get; set; }
    public string? EmbarkedPopulationSpeciesId { get; set; }
    public FleetCombatSaveDto? Combat { get; set; }
}

public sealed class FleetCombatSaveDto
{
    public string ProfileId { get; set; } = string.Empty;
    public double Shields { get; set; }
    public double Armor { get; set; }
    public double Hull { get; set; }
    public double WeaponCooldownRemainingDays { get; set; }
    public MilitaryOrderType Order { get; set; }
    public int? TargetFleetId { get; set; }
    public int? DefendSystemId { get; set; }
    public double RetreatProgressDays { get; set; }
    public bool RetreatStarted { get; set; }
    public bool IsDisengaged { get; set; }
    public int? DisengagedSystemId { get; set; }
}

public sealed class ColonySaveDto
{
    public int Id { get; set; }
    public int CivilizationId { get; set; }
    public int SystemId { get; set; }
    public int? PlanetaryBodyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SettlementKind Kind { get; set; }
    public string? PopulationSpeciesId { get; set; }
    public double PopulationMillions { get; set; }
    public double Infrastructure { get; set; }
    public double Stability { get; set; }
    public double StoredFoodPopulationDaysMillions { get; set; }
    public double StoredWaterPopulationDaysMillions { get; set; }
    public double StoredExtractedMaterials { get; set; }
    public double? RemainingExtractableMaterials { get; set; }
    public int? SurfaceHubLevel { get; set; }
    public double SurfaceHubUpgradeDaysRemaining { get; set; }
    public List<SurfaceBuildingState>? SurfaceBuildings { get; set; }
}

public sealed class EconomySaveDto
{
    public int CivilizationId { get; set; }
    public double Credits { get; set; }
    public double Industry { get; set; }
    public double Science { get; set; }
    public double LastCreditsPerSecond { get; set; }
    public double LastIndustryPerSecond { get; set; }
    public double LastSciencePerSecond { get; set; }
    public double LastResearchSpendingPerDay { get; set; }
    public double LastResearchFundingFraction { get; set; } = 1.0;
    public double OperatingArrears { get; set; }
    public double LastBaseOperationsFundingFraction { get; set; } = 1.0;
    public IndustryPriority? IndustryPriority { get; set; }
}

public sealed class TechnologySaveDto
{
    public int CivilizationId { get; set; }
    public List<string> CompletedTechnologyIds { get; set; } = new();
    public string? ActiveResearchId { get; set; }
    public double ActiveResearchProgress { get; set; }
}

public sealed class ConstructionSaveDto
{
    public int CivilizationId { get; set; }
    public List<string> CompletedProjectIds { get; set; } = new();
    public string? ActiveProjectId { get; set; }
    public double ActiveProjectProgress { get; set; }
    public double ActiveProjectAuthorizationCredits { get; set; }
    public List<QueuedConstructionProjectSaveDto> QueuedProjects { get; set; } = new();
}

public sealed class QueuedConstructionProjectSaveDto
{
    public string ProjectId { get; set; } = string.Empty;
    public double AuthorizationCredits { get; set; }
}

public sealed class ShipyardSaveDto
{
    public int CivilizationId { get; set; }
    public long NextOrderSequence { get; set; } = 1;
    public string? ActiveDesignId { get; set; }
    public string? ActiveOrderId { get; set; }
    public double ActiveBuildProgress { get; set; }
    public double ActiveAuthorizationCredits { get; set; }
    public double ReservedPopulationMillions { get; set; }
    public string? ReservedPopulationSpeciesId { get; set; }
    public int? ReservedPopulationSourceColonyId { get; set; }
    public List<QueuedShipBuildSaveDto> QueuedBuilds { get; set; } = new();
}

public sealed class QueuedShipBuildSaveDto
{
    public string? OrderId { get; set; }
    public string DesignId { get; set; } = string.Empty;
    public double AuthorizationCredits { get; set; }
    public double ReservedPopulationMillions { get; set; }
    public string? ReservedPopulationSpeciesId { get; set; }
    public int? ReservedPopulationSourceColonyId { get; set; }
}

public sealed class CivilizationKnowledgeSaveDto
{
    public int CivilizationId { get; set; }
    public List<int> KnownSystemIds { get; set; } = new();
    public List<int> KnownCivilizationIds { get; set; } = new();
    public List<SystemSurveySaveDto> SystemSurveys { get; set; } = new();
}

public sealed class SystemSurveySaveDto
{
    public int SystemId { get; set; }
    public SystemSurveyLevel Level { get; set; }
    public double Progress { get; set; }
}

public sealed record LoadedCampaign(
    GalaxyState Galaxy,
    double SimulationDays,
    string GameVersion,
    DateTimeOffset SavedAtUtc);
