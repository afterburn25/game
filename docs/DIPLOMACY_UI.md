# Diplomacy workspace boundary

`DiplomacyWorkspaceView` is a presentation-only workspace. It consumes the observer-filtered
`DiplomaticStateView` and reuses `DiplomacyRelationsPresenter` for the selected contact. It does
not receive `DiplomacyState`, hidden AI state, foreign raw identities, or private proposals.

The model exposes generic unidentified contacts, observer-visible confidence and awareness,
political status when a relationship is known, directional access, active agreements, pending
proposals, and recent history. Relationship meters remain nullable; null means unknown rather
than zero. Contact filters are limited to all, identified, unidentified, cooperative, neutral,
hostile, at war, and pending proposal based on those safe fields.

The workspace owns stable presentation node names and callback seams. `Main` remains responsible
for command dispatch through `ObserverDiplomacyCommandService`; this component does not duplicate
legality or mutate diplomacy state. Rendering and application integration are supplied by the
owning presentation agent. Native GUI and screenshot acceptance are outside this CPU boundary.
