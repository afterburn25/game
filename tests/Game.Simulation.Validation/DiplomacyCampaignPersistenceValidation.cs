using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Persistence;
using Game.Simulation.Diplomacy;
using Game.Simulation.Generation;

namespace Game.Simulation.Validation;

internal static class DiplomacyCampaignPersistenceValidation
{
    [ModuleInitializer]
    internal static void RunDiplomacyCampaignPersistenceChecks()
    {
        ValidateFormatEightDiplomacyPersistence();
        Console.WriteLine("PASS: format-v8 campaign Diplomacy persistence and compatibility");
    }

    private static void ValidateFormatEightDiplomacyPersistence()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"steller-diplomacy-save-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        var currentPath = Path.Combine(directory, "current-v8.json");
        var legacyV8Path = Path.Combine(directory, "legacy-v8-without-diplomacy.json");
        var invalidPath = Path.Combine(directory, "invalid-v8-diplomacy.json");

        try
        {
            var service = new CampaignSaveService();
            Require(CampaignSaveService.CurrentFormatVersion == 8,
                "Diplomacy persistence unexpectedly changed the accepted save-format version");

            var galaxy = new GalaxyGenerator().Generate(
                0x4450_4C4F_4D41_4359L,
                new GalaxyGenerationSettings
                {
                    SystemCount = 40,
                    PreWarpCivilizationCount = 5,
                    AncientCivilizationCount = 1,
                    Radius = 480.0f,
                });

            var civilizationIds = galaxy.Civilizations
                .OrderBy(civilization => civilization.Id)
                .Take(3)
                .Select(civilization => civilization.Id)
                .ToArray();
            Require(civilizationIds.Length == 3,
                "campaign persistence validation requires three civilizations");

            var first = civilizationIds[0];
            var second = civilizationIds[1];
            var third = civilizationIds[2];
            EstablishMutualCommunication(galaxy.Diplomacy, first, second, tick: 10);
            EstablishMutualCommunication(galaxy.Diplomacy, second, third, tick: 20);

            var diplomacy = new DiplomacySimulation(galaxy.Diplomacy);
            var privateAgreementProposal = diplomacy.SendProposal(
                second,
                third,
                DiplomaticProposalKind.Agreement,
                tick: 30,
                summary: "Private cooperation framework.",
                agreementType: DiplomaticAgreementType.Cooperation);
            diplomacy.RespondToProposal(
                privateAgreementProposal,
                responder: third,
                accept: true,
                tick: 31);

            diplomacy.SendProposal(
                first,
                second,
                DiplomaticProposalKind.Demand,
                tick: 32,
                summary: "Withdraw patrols from the disputed frontier.");

            var claimSystemId = galaxy.Civilizations
                .First(civilization => civilization.Id == second)
                .HomeSystemId;
            var privateClaim = diplomacy.AssertTerritorialClaim(second, claimSystemId, tick: 33);
            diplomacy.CommunicateTerritorialClaim(privateClaim, recipient: third, tick: 34);
            diplomacy.RespondToTerritorialClaim(
                privateClaim,
                responder: third,
                TerritorialClaimResponse.Disputed,
                tick: 35);

            var originalSnapshot = galaxy.Diplomacy.Snapshot();
            DiplomacySnapshotInvariantValidator.Validate(originalSnapshot);
            var originalObserverView = galaxy.Diplomacy.BuildViewFor(first);
            Require(originalObserverView.Contacts.All(contact => contact.TargetCivilizationId != third),
                "validation setup leaked the third civilization before persistence");
            Require(originalObserverView.Agreements.All(agreement =>
                    agreement.CivilizationAId != third && agreement.CivilizationBId != third),
                "validation setup leaked the private second/third agreement before persistence");
            Require(originalObserverView.Claims.All(claim => claim.ClaimId != privateClaim),
                "validation setup leaked the private territorial claim before persistence");

            service.Save(currentPath, galaxy, simulationDays: 42.25);

            var persistedJson = File.ReadAllText(currentPath);
            var persistedNode = JsonNode.Parse(persistedJson)?.AsObject()
                ?? throw new InvalidOperationException("saved campaign JSON was not an object");
            var persistedGalaxyNode = persistedNode["Galaxy"]?.AsObject()
                ?? throw new InvalidOperationException("saved campaign JSON had no Galaxy object");
            Require(persistedGalaxyNode["Diplomacy"] is not null,
                "new format-v8 save omitted authoritative Diplomacy state");

            var loaded = service.Load(currentPath);
            Require(Math.Abs(loaded.SimulationDays - 42.25) < 0.000001,
                "campaign load changed simulation time while restoring Diplomacy");
            var loadedSnapshot = loaded.Galaxy.Diplomacy.Snapshot();
            DiplomacySnapshotInvariantValidator.Validate(loadedSnapshot);
            Require(
                JsonSerializer.Serialize(loadedSnapshot) == JsonSerializer.Serialize(originalSnapshot),
                "campaign save/load did not round-trip the authoritative Diplomacy snapshot exactly");
            Require(
                JsonSerializer.Serialize(loaded.Galaxy.Diplomacy.BuildViewFor(first)) ==
                JsonSerializer.Serialize(originalObserverView),
                "campaign save/load changed the observer-specific Diplomacy view");

            var loadedObserverView = loaded.Galaxy.Diplomacy.BuildViewFor(first);
            Require(loadedObserverView.Contacts.All(contact => contact.TargetCivilizationId != third),
                "campaign load leaked a third-party civilization through Diplomacy persistence");
            Require(loadedObserverView.Agreements.All(agreement =>
                    agreement.CivilizationAId != third && agreement.CivilizationBId != third),
                "campaign load leaked a private third-party agreement");
            Require(loadedObserverView.Claims.All(claim => claim.ClaimId != privateClaim),
                "campaign load leaked a private territorial claim");

            var legacyRoot = JsonNode.Parse(persistedJson)?.AsObject()
                ?? throw new InvalidOperationException("saved campaign JSON was not an object");
            var legacyGalaxy = legacyRoot["Galaxy"]?.AsObject()
                ?? throw new InvalidOperationException("saved campaign JSON had no Galaxy object");
            Require(legacyGalaxy.Remove("Diplomacy"),
                "could not construct a pre-Diplomacy format-v8 compatibility fixture");
            File.WriteAllText(legacyV8Path, legacyRoot.ToJsonString());

            var legacyLoaded = service.Load(legacyV8Path);
            var legacySnapshot = legacyLoaded.Galaxy.Diplomacy.Snapshot();
            DiplomacySnapshotInvariantValidator.Validate(legacySnapshot);
            Require(legacySnapshot.Contacts.Length == 0 &&
                    legacySnapshot.Relationships.Length == 0 &&
                    legacySnapshot.Proposals.Length == 0 &&
                    legacySnapshot.RecentHistory.Length == 0,
                "format-v8 save without a Diplomacy field did not migrate to empty Diplomacy state");
            Require(legacySnapshot.NextClaimId == 1 &&
                    legacySnapshot.NextAgreementId == 1 &&
                    legacySnapshot.NextProposalId == 1 &&
                    legacySnapshot.NextEventId == 1,
                "pre-Diplomacy format-v8 migration did not initialize clean identity allocators");

            var invalidEnvelope = JsonSerializer.Deserialize<CampaignSaveEnvelope>(persistedJson)
                ?? throw new InvalidOperationException("could not deserialize current save for corruption fixture");
            var persistedDiplomacy = invalidEnvelope.Galaxy.Diplomacy
                ?? throw new InvalidOperationException("current save unexpectedly lacked Diplomacy state");
            var maximumProposalId = persistedDiplomacy.Proposals.Max(proposal => proposal.ProposalId);
            invalidEnvelope.Galaxy.Diplomacy = persistedDiplomacy with
            {
                NextProposalId = maximumProposalId,
            };
            File.WriteAllText(invalidPath, JsonSerializer.Serialize(invalidEnvelope));

            var rejectedInvalidDiplomacy = false;
            try
            {
                _ = service.Load(invalidPath);
            }
            catch (InvalidDataException)
            {
                rejectedInvalidDiplomacy = true;
            }

            Require(rejectedInvalidDiplomacy,
                "campaign load accepted corrupt current Diplomacy identity state");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static void EstablishMutualCommunication(
        DiplomacyState state,
        int first,
        int second,
        long tick)
    {
        var diplomacy = new DiplomacySimulation(state);
        diplomacy.ProcessContactOpportunity(CommunicatingContact(first, second, tick));
        diplomacy.ProcessContactOpportunity(CommunicatingContact(second, first, tick));
    }

    private static FirstContactOpportunity CommunicatingContact(
        int observer,
        int target,
        long tick) => new(
        ObserverCivilizationId: observer,
        ContactId: $"campaign-save-contact-{observer}-{target}",
        TargetCivilizationId: target,
        ObservedAtTick: tick,
        ObservedSystemId: null,
        Awareness: ContactAwareness.CommunicationAvailable,
        Condition: ContactCondition.Active,
        CommunicationAvailable: true,
        Confidence: 1.0);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
