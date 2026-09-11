# Premium diplomacy handoff

The branch contains the integrated observer-safe Relations workspace. Pure shaping is exposed by
`DiplomacyWorkspacePresenter.Build(DiplomaticStateView, ...)`; `DiplomacyWorkspaceView` stores the
model and exposes stable node names plus callbacks for contact, proposal, and action integration.
Rendering is supplied by the workspace render partial and Main integration.

Implemented sections are the contact/search/filter rail, selected civilization presentation,
communication state, political status, Trust/Hostility/Fear/Respect/Cooperation meters, directional
access, agreements, incoming/outgoing proposals, recent history, and action bar. Filters are
observer-safe and per-contact. Unknown names and values stay unknown; stale/lost channels are
unavailable. Source contact indices survive filtering.

Commands remain authoritative in `ObserverDiplomacyCommandService`. Notifications use the existing
player feed; voice uses the observer-safe voice bridge/router; future Galactic News Network
integration must consume only known diplomatic events. Semantic navigation icons are available
through `VisualIconLibrary` under `assets/visual/ui/navigation/`; `CampaignSidebar` remains the
mapping owner.

CPU validation covers anonymous contacts, hidden relationships, proposal direction,
communication/proposal/agreement/war/ceasefire/peace lifecycles, directional access, history, and
hidden unrelated observer state. The previous maintained native 720-v3 run passed 19 images and
97 checks. That historical run is not current release approval: fresh exact-commit 720p/1080p
capture, packaging, and final visual review remain pending.

Extension seams include richer species/leader presentation, negotiation terms, voice styling, and
Galactic News Network display. No hidden AI state or unsupported treaty concepts belong in those
seams.
