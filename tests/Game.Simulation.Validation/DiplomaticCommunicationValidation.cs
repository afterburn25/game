using System.Runtime.CompilerServices;
using Game.Simulation.Diplomacy;

namespace Game.Simulation.Validation;

internal static class DiplomaticCommunicationValidation
{
    [ModuleInitializer]
    internal static void RunDiplomaticCommunicationChecks()
    {
        ValidateMutualIdentificationRequiredBeforeNegotiation();
        Console.WriteLine("PASS: mutual diplomatic communication and access negotiation");
    }

    private static void ValidateMutualIdentificationRequiredBeforeNegotiation()
    {
        var state = new DiplomacyState();
        var diplomacy = new DiplomacySimulation(state);
        var communication = new DiplomaticCommunicationService(state);

        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            ObserverCivilizationId: 11,
            ContactId: "civilization:22",
            TargetCivilizationId: 22,
            ObservedAtTick: 5,
            ObservedSystemId: 7,
            Awareness: ContactAwareness.ContactEstablished,
            Condition: ContactCondition.Active,
            CommunicationAvailable: false,
            Confidence: 0.95));

        var blocked = false;
        try
        {
            communication.EstablishMutualCommunication(11, 22, tick: 6);
        }
        catch (InvalidOperationException)
        {
            blocked = true;
        }

        Require(blocked, "one-way identification incorrectly established mutual communication");
        Require(!state.BuildViewFor(11).Contacts.Single().CommunicationAvailable,
            "failed mutual-communication attempt partially mutated the informed side");
        Require(state.BuildViewFor(22).Contacts.Count == 0,
            "failed communication attempt revealed the informed civilization to an unaware target");

        diplomacy.ProcessContactOpportunity(new FirstContactOpportunity(
            ObserverCivilizationId: 22,
            ContactId: "civilization:11",
            TargetCivilizationId: 11,
            ObservedAtTick: 9,
            ObservedSystemId: 7,
            Awareness: ContactAwareness.ContactEstablished,
            Condition: ContactCondition.Active,
            CommunicationAvailable: false,
            Confidence: 0.90));

        communication.EstablishMutualCommunication(11, 22, tick: 10);

        var contact11 = state.BuildViewFor(11).Contacts.Single();
        var contact22 = state.BuildViewFor(22).Contacts.Single();
        Require(contact11.CommunicationAvailable && contact22.CommunicationAvailable,
            "mutually identified civilizations did not receive a two-way communication channel");
        Require(contact11.Awareness == ContactAwareness.CommunicationAvailable &&
                contact22.Awareness == ContactAwareness.CommunicationAvailable,
            "communication establishment did not advance both contact-awareness states");

        var proposalId = diplomacy.SendProposal(
            proposer: 11,
            recipient: 22,
            kind: DiplomaticProposalKind.AccessRequest,
            tick: 11,
            summary: "Request transit access through claimed space.");
        diplomacy.RespondToProposal(proposalId, responder: 22, accept: true, tick: 12);

        Require(state.GetAccessPermission(grantor: 22, visitor: 11) == AccessPermission.Granted,
            "accepted access request did not create directional transit permission");
        Require(state.GetAccessPermission(grantor: 11, visitor: 22) != AccessPermission.Granted,
            "directional access request incorrectly granted reciprocal transit permission");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
