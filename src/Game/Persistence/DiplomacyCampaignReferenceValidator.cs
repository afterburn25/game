using System;
using System.IO;
using System.Linq;
using Game.Simulation.Diplomacy;
using Game.Simulation.Models;

namespace Game.Persistence;

/// <summary>
/// Cross-checks a structurally valid Diplomacy snapshot against the authoritative campaign
/// identities. DiplomacySnapshotInvariantValidator proves the snapshot is internally coherent;
/// this validator proves it does not point at civilizations or star systems that do not exist in
/// the campaign being saved/loaded.
/// </summary>
public static class DiplomacyCampaignReferenceValidator
{
    public static void Validate(GalaxyState galaxy, DiplomacyStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(snapshot);

        var civilizations = galaxy.Civilizations.Select(civilization => civilization.Id).ToHashSet();
        var systems = galaxy.Systems.Select(system => system.Id).ToHashSet();

        foreach (var contact in snapshot.Contacts)
        {
            RequireCivilization(civilizations, contact.ObserverCivilizationId, $"contact {contact.ContactId} observer");
            if (contact.TargetCivilizationId is int target)
                RequireCivilization(civilizations, target, $"contact {contact.ContactId} target");
            if (contact.LastObservedSystemId is int systemId)
                RequireSystem(systems, systemId, $"contact {contact.ContactId} observed system");
        }

        foreach (var relationship in snapshot.Relationships)
        {
            RequireCivilization(civilizations, relationship.CivilizationAId, "relationship civilization A");
            RequireCivilization(civilizations, relationship.CivilizationBId, "relationship civilization B");
            foreach (var grievance in relationship.Grievances)
                RequireCivilization(civilizations, grievance.SourceCivilizationId, "grievance source");
        }

        foreach (var access in snapshot.AccessPermissions)
        {
            RequireCivilization(civilizations, access.GrantorCivilizationId, "access grantor");
            RequireCivilization(civilizations, access.VisitorCivilizationId, "access visitor");
        }

        foreach (var claim in snapshot.Claims)
        {
            RequireCivilization(civilizations, claim.ClaimantCivilizationId, $"claim {claim.ClaimId} claimant");
            RequireSystem(systems, claim.SystemId, $"claim {claim.ClaimId} system");
            foreach (var audience in claim.KnownToCivilizationIds)
                RequireCivilization(civilizations, audience, $"claim {claim.ClaimId} audience");
        }

        foreach (var response in snapshot.ClaimResponses)
            RequireCivilization(civilizations, response.RespondingCivilizationId, $"claim {response.ClaimId} responder");

        foreach (var agreement in snapshot.Agreements)
        {
            RequireCivilization(civilizations, agreement.CivilizationAId, $"agreement {agreement.AgreementId} civilization A");
            RequireCivilization(civilizations, agreement.CivilizationBId, $"agreement {agreement.AgreementId} civilization B");
        }

        foreach (var proposal in snapshot.Proposals)
        {
            RequireCivilization(civilizations, proposal.ProposerCivilizationId, $"proposal {proposal.ProposalId} proposer");
            RequireCivilization(civilizations, proposal.RecipientCivilizationId, $"proposal {proposal.ProposalId} recipient");
        }

        foreach (var history in snapshot.RecentHistory)
        {
            RequireCivilization(civilizations, history.PrimaryCivilizationId, $"history event {history.EventId} primary civilization");
            if (history.SecondaryCivilizationId is int secondary)
                RequireCivilization(civilizations, secondary, $"history event {history.EventId} secondary civilization");
            if (history.SystemId is int systemId)
                RequireSystem(systems, systemId, $"history event {history.EventId} system");
            foreach (var audience in history.KnownToCivilizationIds)
                RequireCivilization(civilizations, audience, $"history event {history.EventId} audience");
        }
    }

    private static void RequireCivilization(
        System.Collections.Generic.HashSet<int> civilizations,
        int civilizationId,
        string owner)
    {
        if (!civilizations.Contains(civilizationId))
            throw new InvalidDataException($"Diplomacy {owner} references unknown civilization {civilizationId}.");
    }

    private static void RequireSystem(
        System.Collections.Generic.HashSet<int> systems,
        int systemId,
        string owner)
    {
        if (!systems.Contains(systemId))
            throw new InvalidDataException($"Diplomacy {owner} references unknown star system {systemId}.");
    }
}
