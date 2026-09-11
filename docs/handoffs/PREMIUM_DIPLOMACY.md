# Premium diplomacy handoff

The branch contains the integrated observer-safe Relations workspace. Pure shaping is exposed by
`DiplomacyWorkspacePresenter.Build(DiplomaticStateView, ...)`; `DiplomacyWorkspaceView` stores the
model and exposes stable node names plus callbacks for contact, proposal, and action integration.
Rendering is supplied by the workspace render partial and Main integration.

Implemented sections are the contact/search/filter rail, selected civilization presentation,
communication state, political status, Trust/Hostility/Fear/Respect/Cooperation meters, directional
access, agreements, incoming/outgoing proposals, recent history, and action bar. Filters are
observer-safe and per-contact. Unknown names and values stay unknown; stale/lost channels are
unavailable. Source contact indices survive filtering. The current alien framing keeps the entire
source figure visible with aspect-preserving fit and captions below the portrait.

Commands remain authoritative in `ObserverDiplomacyCommandService`. Notifications use the existing
player feed; voice uses the observer-safe voice bridge/router; future Galactic News Network
integration must consume only known diplomatic events. Semantic navigation icons are available
through `VisualIconLibrary` under `assets/visual/ui/navigation/`; `CampaignSidebar` remains the
mapping owner.

CPU validation covers anonymous contacts, hidden relationships, proposal direction,
communication/proposal/agreement/war/ceasefire/peace lifecycles, directional access, history, and
hidden unrelated observer state. Source `0974b6d30ccbb320a771742919997e1954ddc85a` completed
both current native captures: `work/diplomacy-full-figure-final-1280x720` and
`work/diplomacy-full-figure-final-1920x1080`, each exit 0 with 23 images and 111 checks (19 flow
screens plus four art-framing screens). Audio used Dummy, so no audibility claim is made. CI keeps
raw stderr and allowlists only the documented unsupported-V-Sync X11 warning; other runtime/.NET
errors remain fatal. Earlier Windows/source `87606e69` evidence passed; current Windows/CI awaits
the next push. These receipts are not a release or Windows approval.

Extension seams include richer species/leader presentation, negotiation terms, voice styling, and
Galactic News Network display. No hidden AI state or unsupported treaty concepts belong in those
seams.
