using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Game.Simulation.AI;
using Game.Simulation.Combat;
using Game.Simulation.Construction;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;
using Game.Simulation.Knowledge;
using Game.Simulation.Models;
using Game.Simulation.Research;
using Game.Simulation.Shipbuilding;
using Game.Simulation.Species;

namespace Game.Persistence;

public sealed class CampaignSaveService
{
    public const int CurrentFormatVersion = 8;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false,
    };

    public void Save(string path, GalaxyState galaxy, double simulationDays)
    {
        ValidatePlanetaryReferences(galaxy);
        var diplomacySnapshot = galaxy.Diplomacy.Snapshot();
        DiplomacySnapshotInvariantValidator.Validate(diplomacySnapshot);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

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
                ConstructionStates = ToConstructionDtos(galaxy.ConstructionStates),
                ShipyardStates = ToShipyardDtos(galaxy.ShipyardStates),
                PlayerCivilizationId = galaxy.PlayerCivilizationId,
                Knowledge = ToKnowledgeDtos(galaxy.Knowledge),
                Diplomacy = diplomacySnapshot,
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

        IList<FleetState> fleets = envelope.FormatVersion < 3 || envelope.Galaxy.Fleets.Count == 0
            ? new FleetSeeder().Seed(systems, civilizations)
            : ToFleets(
                    envelope.Galaxy.Fleets,
                    civilizations,
                    envelope.FormatVersion,
                    restoreUnserializedShipbuildingPopulation: envelope.FormatVersion >= 7)
                .ToList();

        IList<ColonyState> colonies;
        IReadOnlyList<CivilizationEconomyState> economies;
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

        var diplomacy = RestoreDiplomacy(envelope.Galaxy.Diplomacy);

        var galaxy = new GalaxyState
        {
            Seed = envelope.Galaxy.Seed,
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
            Diplomacy = diplomacy,
        };

        ValidatePlanetaryReferences(galaxy);

        return new LoadedCampaign(
            galaxy,
            simulationDays,
            envelope.GameVersion,
            envelope.SavedAtUtc);
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
                d.HasPreWarpCivilization))
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
                    : RequireKnownSpeciesId(d.SpeciesId, d.Id)))
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
                Position = new Vector2(dto.X, dto.Y),
                CurrentSystemId = dto.CurrentSystemId,
                DestinationSystemId = dto.DestinationSystemId,
                DestinationPlanetaryBodyId = saveFormatVersion >= 8
                    ? dto.DestinationPlanetaryBodyId
                    : null,
                StrategicSpeed = dto.StrategicSpeed,
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
                PopulationSpeciesId = ResolvePopulationSpeciesId(
                    d.PopulationSpeciesId,
                    d.CivilizationId,
                    civilizations,
                    saveFormatVersion,
                    $"colony {d.Id}"),
                PopulationMillions = d.PopulationMillions,
                Infrastructure = d.Infrastructure,
                Stability = d.Stability,
            })
            .ToArray();
    }

    private static IReadOnlyList<CivilizationEconomyState> ToEconomies(
        IReadOnlyList<EconomySaveDto> dtos) =>
        dtos.Select(d => new CivilizationEconomyState
            {
                CivilizationId = d.CivilizationId,
                Credits = d.Credits,
                Industry = d.Industry,
                Science = d.Science,
                LastCreditsPerSecond = d.LastCreditsPerSecond,
                LastIndustryPerSecond = d.LastIndustryPerSecond,
                LastSciencePerSecond = d.LastSciencePerSecond,
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
            };
            foreach (var id in dto.CompletedProjectIds)
                state.CompletedProjectIds.Add(id);
            result.Add(state);
        }

        return result;
    }

    private static IList<ShipyardState> ToShipyardStates(
        IReadOnlyList<ShipyardSaveDto> dtos,
        IList<CivilizationState> civilizations,
        int saveFormatVersion)
    {
        var result = new List<ShipyardState>(dtos.Count);
        foreach (var dto in dtos)
        {
            var reservedPopulation = Math.Max(0.0, dto.ReservedPopulationMillions);
            var state = new ShipyardState
            {
                CivilizationId = dto.CivilizationId,
                ActiveDesignId = dto.ActiveDesignId,
                ActiveBuildProgress = dto.ActiveBuildProgress,
                ReservedPopulationMillions = reservedPopulation,
                ReservedPopulationSpeciesId = reservedPopulation > 0.0
                    ? ResolvePopulationSpeciesId(
                        dto.ReservedPopulationSpeciesId,
                        dto.CivilizationId,
                        civilizations,
                        saveFormatVersion,
                        $"shipyard {dto.CivilizationId} active reservation")
                    : null,
            };

            var availableQueueSlots = ShipyardState.MaxPendingBuilds -
                                      (state.ActiveDesignId is null ? 0 : 1);
            foreach (var queued in dto.QueuedBuilds
                         .Where(build => !string.IsNullOrWhiteSpace(build.DesignId))
                         .Take(Math.Max(0, availableQueueSlots)))
            {
                if (!ShipDesignRegistry.All.Any(design => design.Id == queued.DesignId))
                    continue;

                var queuedPopulation = Math.Max(0.0, queued.ReservedPopulationMillions);
                state.QueuedBuilds.Add(new ShipBuildOrderState
                {
                    DesignId = queued.DesignId,
                    ReservedPopulationMillions = queuedPopulation,
                    ReservedPopulationSpeciesId = queuedPopulation > 0.0
                        ? ResolvePopulationSpeciesId(
                            queued.ReservedPopulationSpeciesId,
                            dto.CivilizationId,
                            civilizations,
                            saveFormatVersion,
                            $"shipyard {dto.CivilizationId} queued reservation")
                        : null,
                });
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

    private static DiplomacyState RestoreDiplomacy(DiplomacyStateSnapshot? snapshot)
    {
        if (snapshot is null)
            return new DiplomacyState();

        try
        {
            // A current format-v8 snapshot must already be strict-valid. Restore remains tolerant
            // for explicit migration/recovery use elsewhere, but campaign loading must not silently
            // repair corrupt current authoritative Diplomacy state.
            DiplomacySnapshotInvariantValidator.Validate(snapshot);
            var diplomacy = DiplomacyState.Restore(snapshot);
            DiplomacySnapshotInvariantValidator.Validate(diplomacy.Snapshot());
            return diplomacy;
        }
        catch (DiplomacySnapshotValidationException exception)
        {
            throw new InvalidDataException("Save contains invalid current Diplomacy state.", exception);
        }
    }

    private static void ValidatePlanetaryReferences(GalaxyState galaxy)
    {
        var bodies = galaxy.PlanetaryBodies.ToDictionary(body => body.Id);

        foreach (var colony in galaxy.Colonies)
        {
            if (colony.PlanetaryBodyId is not int bodyId)
                continue;

            if (!bodies.TryGetValue(bodyId, out var body) || body.SystemId != colony.SystemId)
            {
                throw new InvalidDataException(
                    $"Colony {colony.Id} references planetary body {bodyId} outside system {colony.SystemId}.");
            }
        }

        foreach (var fleet in galaxy.Fleets)
        {
            if (fleet.DestinationPlanetaryBodyId is not int bodyId)
                continue;

            if (fleet.Role != FleetRole.Colony || fleet.DestinationSystemId is not int systemId)
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
                X = fleet.Position.X,
                Y = fleet.Position.Y,
                CurrentSystemId = fleet.CurrentSystemId,
                DestinationSystemId = fleet.DestinationSystemId,
                DestinationPlanetaryBodyId = fleet.DestinationPlanetaryBodyId,
                StrategicSpeed = fleet.StrategicSpeed,
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
                PopulationSpeciesId = RequireKnownPopulationSpeciesId(
                    c.PopulationSpeciesId,
                    $"colony {c.Id}"),
                PopulationMillions = c.PopulationMillions,
                Infrastructure = c.Infrastructure,
                Stability = c.Stability,
            })
            .ToList();

    private static List<EconomySaveDto> ToEconomyDtos(
        IReadOnlyList<CivilizationEconomyState> economies) =>
        economies.Select(e => new EconomySaveDto
            {
                CivilizationId = e.CivilizationId,
                Credits = e.Credits,
                Industry = e.Industry,
                Science = e.Science,
                LastCreditsPerSecond = e.LastCreditsPerSecond,
                LastIndustryPerSecond = e.LastIndustryPerSecond,
                LastSciencePerSecond = e.LastSciencePerSecond,
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
                ActiveBuildProgress = s.ActiveBuildProgress,
                ReservedPopulationMillions = reservedPopulation,
                ReservedPopulationSpeciesId = reservedPopulation > 0.0
                    ? RequireKnownPopulationSpeciesId(
                        s.ReservedPopulationSpeciesId,
                        $"shipyard {s.CivilizationId} active reservation")
                    : null,
                QueuedBuilds = s.QueuedBuilds
                    .Take(ShipyardState.MaxPendingBuilds)
                    .Select(build =>
                    {
                        var queuedPopulation = Math.Max(0.0, build.ReservedPopulationMillions);
                        return new QueuedShipBuildSaveDto
                        {
                            DesignId = build.DesignId,
                            ReservedPopulationMillions = queuedPopulation,
                            ReservedPopulationSpeciesId = queuedPopulation > 0.0
                                ? RequireKnownPopulationSpeciesId(
                                    build.ReservedPopulationSpeciesId,
                                    $"shipyard {s.CivilizationId} queued reservation")
                                : null,
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
    public DiplomacyStateSnapshot? Diplomacy { get; set; }
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
    public float X { get; set; }
    public float Y { get; set; }
    public int? CurrentSystemId { get; set; }
    public int? DestinationSystemId { get; set; }
    public int? DestinationPlanetaryBodyId { get; set; }
    public double StrategicSpeed { get; set; }
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
    public string? PopulationSpeciesId { get; set; }
    public double PopulationMillions { get; set; }
    public double Infrastructure { get; set; }
    public double Stability { get; set; }
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
}

public sealed class ShipyardSaveDto
{
    public int CivilizationId { get; set; }
    public string? ActiveDesignId { get; set; }
    public double ActiveBuildProgress { get; set; }
    public double ReservedPopulationMillions { get; set; }
    public string? ReservedPopulationSpeciesId { get; set; }
    public List<QueuedShipBuildSaveDto> QueuedBuilds { get; set; } = new();
}

public sealed class QueuedShipBuildSaveDto
{
    public string DesignId { get; set; } = string.Empty;
    public double ReservedPopulationMillions { get; set; }
    public string? ReservedPopulationSpeciesId { get; set; }
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
