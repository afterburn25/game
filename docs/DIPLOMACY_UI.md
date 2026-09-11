# Diplomacy workspace

The implemented `DiplomacyWorkspaceView` is a full Relations workspace built on the existing
observer-filtered `DiplomaticStateView`. `DiplomacyWorkspacePresenter` shapes that view into a
pure model; the Godot view owns stable nodes, callbacks, and rendering.

The screen contains a searchable/filterable contact rail, selected-contact transmission and
identity area, political status and communication state, five relationship meters, directional
transit access, active agreements, pending incoming/outgoing proposals, recent diplomatic
history, and context-sensitive action controls. Unknown contacts remain generic. Unknown meter
values remain unknown rather than zero. Stale/lost contacts cannot appear as available channels.
Filters cover all, identified, unidentified, cooperative, neutral, hostile, at war, pending
proposal, and communication available.

Proposal, agreement, access, ceasefire, peace, and war actions dispatch through
`ObserverDiplomacyCommandService` and its existing availability contracts. The workspace does not
duplicate legality rules or receive authoritative hidden state. Per-contact pending counts,
source indices, observer-visible confidence, and known political state are preserved in the DTO.
Hidden names are never requested for unidentified contacts.

Diplomatic history is published through the existing player notification feed. Voice presentation
uses the observer-safe voice bridge/router, and future Galactic News Network consumption must use
only observer-visible events. Voice and news remain optional and cannot expose private negotiations.

Reusable panels and scroll containers reflow at 720p without shrinking essential text, while the
same model supports 1080p and larger layouts. The current full-figure implementation keeps the
entire alien portrait visible with aspect-preserving framing and places captions below it.
`VisualIconLibrary` exposes original color-coded
semantic SVGs for research, economy, construction, shipyard, exploration, colonization, logistics,
relations, inspection, home, galaxy, and settings.

Portraits fit the entire original image while preserving its aspect ratio. No crop or zoom
removes parts of the alien. Channel state sits above the image; names and subtitles occupy
their own panel below it, so they never cover the character. The native capture also checks
all four registered species at both resolutions, including the non-humanoid composition.

Detailed species/leader art, richer negotiation terms, full Galactic News Network presentation,
and native voice styling remain extension seams over existing systems. Native GUI acceptance,
Windows packaging, and final visual approval require current evidence.

Validation requires a game build, focused diplomacy validation, and maintained native capture at
the exact source revision. Review unknown contacts and hidden third-party agreements, directional
access, proposal flags, stale communication, responsive 720p/1080p layout, full alien framing,
and notifications. Source `0974b6d30ccbb320a771742919997e1954ddc85a` has completed both current
native receipts: `work/diplomacy-full-figure-final-1280x720` and
`work/diplomacy-full-figure-final-1920x1080`, each with exit 0, 23 images, and 111 checks.
The 23 images comprise 19 flow screens plus four full-art framing screens. The capture uses the
Dummy audio driver, so it establishes no audibility claim. The CI X11 allowlist accepts only the
documented unsupported-V-Sync warning while retaining raw stderr and failing other runtime errors.
Earlier Windows/source `87606e69` workflow evidence passed; current Windows/CI status remains
pending the next push. These receipts do not by themselves approve release or Windows packaging.
