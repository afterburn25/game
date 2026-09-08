using System;
using Godot;

namespace Game.Presentation;

/// <summary>
/// Compact player Relations overlay. It renders only Main's observer-safe Diplomacy presentation
/// state and dispatches all actions back through Main's ObserverDiplomacyCommandService adapter.
/// </summary>
public partial class RelationsPanel : CanvasLayer
{
    private Main _main = null!;
    private Label _content = null!;
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
    private int _contactIndex;
    private int _proposalIndex;
    private double _refreshTimer;

    public override void _Ready()
    {
        _main = GetParent() as Main
            ?? throw new InvalidOperationException("RelationsPanel must be a child of Main.");

        var panel = new PanelContainer
        {
            AnchorLeft = 1.0f,
            AnchorRight = 1.0f,
            OffsetLeft = -500.0f,
            OffsetRight = -16.0f,
            OffsetTop = 16.0f,
            OffsetBottom = 690.0f,
        };

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        root.AddChild(header);
        header.AddChild(new Label
        {
            Text = "RELATIONS",
            TooltipText = "Shows only diplomatic contacts, relationships, agreements, proposals and history visible to your civilization.",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
        AddButton(header, "Close", "Close the Relations overlay.", () => Visible = false, 68.0f);

        _content = new Label
        {
            Text = "Diplomatic contacts are initializing…",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(448, 365),
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.AddChild(_content);

        var navigation = new HBoxContainer();
        navigation.AddThemeConstantOverride("separation", 5);
        root.AddChild(navigation);
        _previousContact = AddButton(navigation, "← Contact", "Previous observer-visible contact.", () =>
        {
            _contactIndex--;
            _proposalIndex = 0;
            ClearActionAndRefresh();
        }, 92.0f);
        _nextContact = AddButton(navigation, "Contact →", "Next observer-visible contact.", () =>
        {
            _contactIndex++;
            _proposalIndex = 0;
            ClearActionAndRefresh();
        }, 92.0f);
        _previousProposal = AddButton(navigation, "← Proposal", "Previous pending proposal involving the selected contact.", () =>
        {
            _proposalIndex--;
            ClearActionAndRefresh();
        }, 98.0f);
        _nextProposal = AddButton(navigation, "Proposal →", "Next pending proposal involving the selected contact.", () =>
        {
            _proposalIndex++;
            ClearActionAndRefresh();
        }, 98.0f);

        var proposalRow = new HBoxContainer();
        proposalRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(proposalRow);
        _nonAggression = AddButton(proposalRow, "Non-Aggression", "Propose a non-aggression agreement through the active diplomatic channel.", () => SendProposal(UiDiplomacyProposalAction.NonAggression), 112.0f);
        _accessRequest = AddButton(proposalRow, "Request Access", "Request transit access from the selected civilization.", () => SendProposal(UiDiplomacyProposalAction.AccessRequest), 104.0f);
        _peaceOffer = AddButton(proposalRow, "Peace", "Offer formal peace when current relations are hostile, at war, or under ceasefire.", () => SendProposal(UiDiplomacyProposalAction.PeaceOffer), 68.0f);
        _ceasefireOffer = AddButton(proposalRow, "Ceasefire", "Offer a ceasefire during hostile or wartime relations.", () => SendProposal(UiDiplomacyProposalAction.CeasefireOffer), 82.0f);

        var responseRow = new HBoxContainer();
        responseRow.AddThemeConstantOverride("separation", 4);
        root.AddChild(responseRow);
        _accept = AddButton(responseRow, "Accept", "Accept the selected incoming pending proposal.", () => RespondToProposal(true), 72.0f);
        _reject = AddButton(responseRow, "Reject", "Reject the selected incoming pending proposal.", () => RespondToProposal(false), 72.0f);
        _withdraw = AddButton(responseRow, "Withdraw", "Withdraw the selected outgoing pending proposal.", WithdrawProposal, 84.0f);
        _grantAccess = AddButton(responseRow, "Grant Access", "Grant the selected civilization political transit access through your territory.", () => SetAccess(true), 96.0f);
        _denyAccess = AddButton(responseRow, "Deny Access", "Deny the selected civilization political transit access through your territory.", () => SetAccess(false), 96.0f);

        _actionStatus = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(448, 42),
        };
        root.AddChild(_actionStatus);

        AddChild(panel);
        Visible = false;
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

    private static Button AddButton(Container parent, string text, string tooltip, Action action, float width)
    {
        var button = new Button
        {
            Text = text,
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
        if (_main is null || _content is null)
            return;

        var state = CurrentState();
        _content.Text = state.Details;

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
    }
}
