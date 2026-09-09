using System;
using System.Linq;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Compact player Relations overlay. It renders only Main's observer-safe Diplomacy presentation
/// state and dispatches all actions back through Main's ObserverDiplomacyCommandService adapter.
/// </summary>
public partial class RelationsPanel : CanvasLayer
{
    private Main _main = null!;
    private Label _contactName = null!;
    private TextureRect _contactPortrait = null!;
    private Label _contactStatus = null!;
    private Label _politicalStatus = null!;
    private Label _communicationStatus = null!;
    private GridContainer _relationshipMetrics = null!;
    private Label _trust = null!;
    private Label _hostility = null!;
    private Label _fear = null!;
    private Label _respect = null!;
    private Label _cooperation = null!;
    private Label _access = null!;
    private Label _agreements = null!;
    private Label _proposal = null!;
    private Label _recent = null!;
    private Label _actionStatus = null!;
    private Button _previousContact = null!;
    private Button _nextContact = null!;
    private Button _previousProposal = null!;
    private Button _nextProposal = null!;
    private Button _nonAggression = null!;
    private Button _accessRequest = null!;
    private Button _peaceOffer = null!;
    private Button _ceasefireOffer = null!;
    private Button _accept = null!;
    private Button _reject = null!;
    private Button _withdraw = null!;
    private Button _grantAccess = null!;
    private Button _denyAccess = null!;
    private Button _declareWar = null!;
    private int _contactIndex;
    private int _proposalIndex;
    private double _refreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("RelationsPanel must be a child of Main.");

        var panel = new PanelContainer { Name = "RelationsOverlay" };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);
        header.AddChild(new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = VisualIconLibrary.Relations,
            CustomMinimumSize = new Vector2(22, 22),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        header.AddChild(new Label
        {
            Text = "RELATIONS",
            TooltipText = "Shows only diplomatic contacts, relationships, agreements, proposals and history visible to your civilization.",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
        AddButton(header, "Close", "Close the Relations overlay.", () => _main.GetNode<CampaignSidebar>("CampaignSidebar").CloseDrawer(), 68.0f);

        // Keep Close visible while wrapped action rows remain reachable at short heights.
        var scroll = new VBoxContainer
        {
            Name = "RelationsScroll",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        root.AddChild(scroll);
        var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(body);

        var contactCard = new PanelContainer { Name = "DiplomacyContactCard" };
        contactCard.AddThemeStyleboxOverride("panel", VisualUi.Surface(margin: 13));
        var contactRow = new HBoxContainer();
        contactRow.AddThemeConstantOverride("separation", 14);
        contactCard.AddChild(contactRow);
        _contactPortrait = new TextureRect
        {
            Name = "ContactPortrait",
            Texture = VisualIconLibrary.DiplomacyContact,
            CustomMinimumSize = new Vector2(118, 118),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        contactRow.AddChild(_contactPortrait);
        var contactDetails = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        contactDetails.AddThemeConstantOverride("separation", 4);
        _contactName = VisualUi.Text("NO FOREIGN CONTACTS", 19, Colors.White);
        _contactStatus = VisualUi.Text("Awaiting first contact", 11, VisualUi.Muted, wrap: true);
        contactDetails.AddChild(_contactName);
        contactDetails.AddChild(_contactStatus);
        contactRow.AddChild(contactDetails);
        var stateColumn = new VBoxContainer();
        _politicalStatus = VisualUi.Text("NO FORMAL RELATIONSHIP", 11, VisualUi.Gold);
        _politicalStatus.HorizontalAlignment = HorizontalAlignment.Right;
        _communicationStatus = VisualUi.Text("CHANNEL UNAVAILABLE", 10, VisualUi.Muted);
        _communicationStatus.HorizontalAlignment = HorizontalAlignment.Right;
        stateColumn.AddChild(_politicalStatus);
        stateColumn.AddChild(_communicationStatus);
        contactRow.AddChild(stateColumn);
        body.AddChild(contactCard);

        _relationshipMetrics = new GridContainer { Name = "DiplomacyMetrics", Columns = 5 };
        _relationshipMetrics.AddThemeConstantOverride("h_separation", 6);
        _relationshipMetrics.AddThemeConstantOverride("v_separation", 6);
        _trust = AddRelationMetric(_relationshipMetrics, "TRUST", new Color("8fe5b1"));
        _hostility = AddRelationMetric(_relationshipMetrics, "HOSTILITY", new Color("ee9a91"));
        _fear = AddRelationMetric(_relationshipMetrics, "FEAR", VisualUi.Gold);
        _respect = AddRelationMetric(_relationshipMetrics, "RESPECT", VisualUi.Accent);
        _cooperation = AddRelationMetric(_relationshipMetrics, "COOPERATION", new Color("b4a0e4"));
        body.AddChild(_relationshipMetrics);

        var standing = new GridContainer { Columns = 2 };
        standing.AddThemeConstantOverride("h_separation", 8);
        standing.AddThemeConstantOverride("v_separation", 8);
        _access = AddInformationCard(standing, "TRANSIT ACCESS", VisualIconLibrary.DiplomacyAccessGranted);
        _agreements = AddInformationCard(standing, "ACTIVE AGREEMENTS", VisualIconLibrary.DiplomacyAgreement);
        _access.Name = "DiplomacyAccess";
        _agreements.Name = "DiplomacyAgreements";
        body.AddChild(standing);
        _proposal = AddWideInformationCard(body, "PENDING PROPOSAL", VisualIconLibrary.DiplomacyAgreement);
        _recent = AddWideInformationCard(body, "RECENT DIPLOMACY", VisualIconLibrary.Info);
        _proposal.Name = "DiplomacyProposal";
        _recent.Name = "DiplomacyRecent";

        body.AddChild(VisualUi.Text("DIPLOMATIC ACTIONS", 11, VisualUi.Accent));

        var navigation = AddActionRow(body);
        _previousContact = AddButton(navigation, "← Contact", "Previous observer-visible contact.", () =>
        {
            _contactIndex--;
            _proposalIndex = 0;
            ClearActionAndRefresh();
        }, 100.0f, VisualIconLibrary.DiplomacyContact);
        _nextContact = AddButton(navigation, "Contact →", "Next observer-visible contact.", () =>
        {
            _contactIndex++;
            _proposalIndex = 0;
            ClearActionAndRefresh();
        }, 100.0f, VisualIconLibrary.DiplomacyContact);
        _previousProposal = AddButton(navigation, "← Proposal", "Previous pending proposal involving the selected contact.", () =>
        {
            _proposalIndex--;
            ClearActionAndRefresh();
        }, 108.0f, VisualIconLibrary.DiplomacyAgreement);
        _nextProposal = AddButton(navigation, "Proposal →", "Next pending proposal involving the selected contact.", () =>
        {
            _proposalIndex++;
            ClearActionAndRefresh();
        }, 108.0f, VisualIconLibrary.DiplomacyAgreement);

        var proposalRow = AddActionRow(body);
        _nonAggression = AddButton(proposalRow, "Non-Aggression", "Propose a non-aggression agreement through the active diplomatic channel.", () => SendProposal(UiDiplomacyProposalAction.NonAggression), 112.0f);
        _accessRequest = AddButton(proposalRow, "Request Access", "Request transit access from the selected civilization.", () => SendProposal(UiDiplomacyProposalAction.AccessRequest), 104.0f);
        _peaceOffer = AddButton(proposalRow, "Peace", "Offer formal peace when current relations are hostile, at war, or under ceasefire.", () => SendProposal(UiDiplomacyProposalAction.PeaceOffer), 84.0f, VisualIconLibrary.DiplomacyPeace);
        _ceasefireOffer = AddButton(proposalRow, "Ceasefire", "Offer a ceasefire during hostile or wartime relations.", () => SendProposal(UiDiplomacyProposalAction.CeasefireOffer), 98.0f, VisualIconLibrary.DiplomacyCeasefire);

        var responseRow = AddActionRow(body);
        _accept = AddButton(responseRow, "Accept", "Accept the selected incoming pending proposal.", () => RespondToProposal(true), 72.0f, VisualIconLibrary.Success);
        _reject = AddButton(responseRow, "Reject", "Reject the selected incoming pending proposal.", () => RespondToProposal(false), 72.0f, VisualIconLibrary.DiplomacyAccessDenied);
        _withdraw = AddButton(responseRow, "Withdraw", "Withdraw the selected outgoing pending proposal.", WithdrawProposal, 84.0f);
        _grantAccess = AddButton(responseRow, "Grant Access", "Grant the selected civilization political transit access through your territory.", () => SetAccess(true), 96.0f);
        _denyAccess = AddButton(responseRow, "Deny Access", "Deny the selected civilization political transit access through your territory.", () => SetAccess(false), 96.0f);

        var conflictRow = AddActionRow(body);
        _declareWar = AddButton(conflictRow, "Declare War", "Declare war on the selected identified civilization. This changes the diplomatic and combat relationship immediately.", DeclareWar, 112.0f, VisualIconLibrary.PatrolCorvette);

        _actionStatus = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(0, 42),
        };
        body.AddChild(_actionStatus);

        _main.GetNode<CampaignSidebar>("CampaignSidebar").AddPanel(panel);
        RefreshContent();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        _refreshTimer += delta;
        if (_refreshTimer < 0.5)
            return;

        _refreshTimer = 0.0;
        RefreshContent();
    }

    private static HFlowContainer AddActionRow(Container parent)
    {
        var row = new HFlowContainer();
        row.AddThemeConstantOverride("h_separation", 4);
        row.AddThemeConstantOverride("v_separation", 4);
        parent.AddChild(row);
        return row;
    }

    private static Button AddButton(
        Container parent,
        string text,
        string tooltip,
        Action action,
        float width,
        Texture2D? icon = null)
    {
        var button = new Button
        {
            Text = text,
            Icon = icon,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(width, 28),
            FocusMode = Control.FocusModeEnum.All,
        };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    private void SendProposal(UiDiplomacyProposalAction action)
    {
        var state = CurrentState();
        if (state.TargetCivilizationId is not int targetId)
            return;

        _actionStatus.Text = _main.IssueUiDiplomacyProposal(targetId, action);
        RefreshContent();
    }

    private void RespondToProposal(bool accept)
    {
        var state = CurrentState();
        if (state.PendingProposalId is not long proposalId)
            return;

        _actionStatus.Text = _main.IssueUiDiplomacyProposalResponse(proposalId, accept);
        RefreshContent();
    }

    private void WithdrawProposal()
    {
        var state = CurrentState();
        if (state.PendingProposalId is not long proposalId)
            return;

        _actionStatus.Text = _main.IssueUiDiplomacyProposalWithdrawal(proposalId);
        RefreshContent();
    }

    private void SetAccess(bool grant)
    {
        var state = CurrentState();
        if (state.TargetCivilizationId is not int targetId)
            return;

        _actionStatus.Text = _main.IssueUiDiplomacyAccess(targetId, grant);
        RefreshContent();
    }

    private static Label AddRelationMetric(GridContainer parent, string title, Color color)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(105, 66), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", VisualUi.Surface(margin: 8));
        var body = new VBoxContainer();
        body.AddChild(VisualUi.Text(title, 9, VisualUi.Muted));
        var value = VisualUi.Text("—", 17, color);
        body.AddChild(value);
        panel.AddChild(body);
        parent.AddChild(panel);
        return value;
    }

    private static Label AddInformationCard(GridContainer parent, string title, Texture2D icon)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(270, 78), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", VisualUi.Surface(margin: 9));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 9);
        card.AddChild(row);
        row.AddChild(VisualUi.Icon(icon, 30));
        var values = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        values.AddChild(VisualUi.Text(title, 9, VisualUi.Muted));
        var value = VisualUi.Text("—", 11, Colors.White, wrap: true);
        values.AddChild(value);
        row.AddChild(values);
        parent.AddChild(card);
        return value;
    }

    private static Label AddWideInformationCard(Container parent, string title, Texture2D icon)
    {
        var grid = new GridContainer { Columns = 1 };
        parent.AddChild(grid);
        return AddInformationCard(grid, title, icon);
    }

    private void DeclareWar()
    {
        var state = CurrentState();
        if (state.TargetCivilizationId is not int targetId) return;
        _actionStatus.Text = _main.IssueUiWarDeclaration(targetId);
        RefreshContent();
    }

    private RelationsPresentationState CurrentState()
    {
        var state = _main.GetUiRelationsState(_contactIndex, _proposalIndex);
        _contactIndex = state.ContactIndex;
        _proposalIndex = state.ProposalIndex;
        return state;
    }

    private void ClearActionAndRefresh()
    {
        _actionStatus.Text = string.Empty;
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (_main is null || _contactName is null)
            return;

        var state = CurrentState();
        _contactName.Text = state.ContactName;
        _contactPortrait.Texture = state.SpeciesId is { } speciesId
            ? VisualIconLibrary.Get(CivilizationArtworkLibrary.PathForSpecies(speciesId))
            : VisualIconLibrary.DiplomacyContact;
        _contactStatus.Text = state.ContactStatus;
        _politicalStatus.Text = state.PoliticalStatus.ToUpperInvariant();
        _communicationStatus.Text = state.CommunicationStatus.ToUpperInvariant();
        _communicationStatus.Modulate = state.HasVisibleCommunication ? new Color("8fe5b1") : VisualUi.Muted;
        _relationshipMetrics.Visible = state.Trust.HasValue;
        _trust.Text = Percent(state.Trust);
        _hostility.Text = Percent(state.Hostility);
        _fear.Text = Percent(state.Fear);
        _respect.Text = Percent(state.Respect);
        _cooperation.Text = Percent(state.Cooperation);
        _access.Text = state.AccessSummary;
        _agreements.Text = state.AgreementsSummary;
        _proposal.Text = state.ProposalSummary;
        _recent.Text = state.RecentEvents.Length == 0 ? "No recent diplomatic events" :
            string.Join("\n", state.RecentEvents.Select(message => "• " + message));

        _previousContact.Disabled = state.ContactCount <= 1 || state.ContactIndex <= 0;
        _nextContact.Disabled = state.ContactCount <= 1 || state.ContactIndex >= state.ContactCount - 1;
        _previousProposal.Disabled = state.ProposalCount <= 1 || state.ProposalIndex <= 0;
        _nextProposal.Disabled = state.ProposalCount <= 1 || state.ProposalIndex >= state.ProposalCount - 1;

        _nonAggression.Disabled = !state.CanOfferNonAggression;
        _accessRequest.Disabled = !state.CanRequestAccess;
        _peaceOffer.Disabled = !state.CanOfferPeace;
        _ceasefireOffer.Disabled = !state.CanOfferCeasefire;
        _accept.Disabled = !state.CanAcceptProposal;
        _reject.Disabled = !state.CanRejectProposal;
        _withdraw.Disabled = !state.CanWithdrawProposal;
        _grantAccess.Disabled = !state.CanSetAccess;
        _denyAccess.Disabled = !state.CanSetAccess;
        _declareWar.Disabled = !state.CanDeclareWar;
    }

    private static string Percent(double? value) => value.HasValue ? $"{value.Value:P0}" : "—";
}
