# Adaptive Research and integrated campaign migration boundary

Status: implementation pending. The native legacy research port and campaign step (038) do not establish Adaptive Research parity. Preserve the C# authority and `data/research/v1` unchanged while building the native implementation.

## Actual player composition

`src/Game/Presentation/Main.CoreIntegration.cs` is the composition reference in addition to `GalaxySimulationStepCoordinator.cs`. Its configured game differs from the latter's default constructor:

- Construction and shipbuilding query the live Adaptive Research campaign for capabilities.
- The strategic input builder shares that shipbuilding capability view and reads diplomacy knowledge.
- Combat preview, commands and simulation share the diplomacy runtime's hostility policy.
- The injected shipbuilding simulation does not receive a strategic preference provider. Native configuration must support that absence rather than automatically supplying published AI preferences.
- Legacy research accrual and advancement are disabled.

An integrated accepted step first advances the Core coordinator, then records sensor contacts for civilizations in ID order, advances Adaptive Research, and processes diplomacy with that step's exploration/combat events and absolute campaign day. Preserve the source's zero-day behavior separately for each operation. Autosave occurs after all bounded substeps, not during one of these phases. Notifications, voice and drawing consume results without controlling simulation time.

## Migration order and ownership

1. Port the immutable definitions, actual JSON catalog loader and wake-up indexes. Preserve ID case sensitivity, domain and insertion order where observable, validation order, node requirement defaults, rounding, implications and grants. Load the existing assets rather than inventing a substitute research tree.
2. Port civilization state and snapshot DTO/capture with revisions, project identities and ordered collections. Complete restore only after the applicability and facility catalogs exist: the source codec validates both. Research uses string civilization IDs; the campaign bridge maps integer game IDs explicitly. Do not reinterpret identity types.
3. Port applicability, facilities, progress policy, eligibility and view generation. Observer-facing locked/hidden information must be derived from the original authority, not from direct catalog enumeration in presentation.
4. Port the runtime kernel and authority, then expertise, pressure, hypotheses/outcomes, agenda and foreign-technology features with their retained state and deterministic inputs. Preserve event-driven candidate wakeups; do not replace them with a full-tree scan every frame.
5. Port starting profiles, funding/escrow commands, facility synchronization, maturity/development progression and campaign capability adapters. Verify setup, operating and milestone costs, partial funding, paused projects and once-only commitment consumption across save/recovery.
6. Compose Adaptive Research and diplomacy with the native campaign coordinator, sensor contacts and accepted clock. The full configured player path requires a separate parity fixture and multi-step test; a passing legacy coordinator remains a narrower result.

Use one immutable shared catalog per content version. Native state stores stable IDs and owned values; short-lived queries borrow const definitions. Do not retain references into growable vectors or temporary JSON trees. Do not clone mutable capability/hostility policy state when constructing temporary world views. Keep research/game rules in Stellar Core, with generic asset location and file services in Stellar Engine.

Low-level eligibility and standalone snapshot codecs may borrow the immutable catalogs they actually query; their owners must outlive the consumer and remain unmoved. The integrated runtime should own a shared immutable content bundle at a stable address, with its eligibility/view helpers in stable implementation storage. Load the base catalog, applicability, facilities and progress policy in explicit source order before constructing that bundle: C++ function-argument evaluation order must not choose which invalid file fails first. Snapshot DTOs and returned state own their values; the schema-1 codec does not require a placeholder runtime merely to access definitions.

## Runtime kernel sequencing

The kernel ports `AdaptiveResearchRuntime.cs` after eligibility and view generation. Its content bundle owns the four immutable catalogs; runtime moves must leave that bundle and the borrowed evaluator/view dependencies at stable addresses. Civilization state remains independently owned by its campaign. Use explicit copied project/node values across writer calls, and capture project IDs before iterating advancement: state writers can rebuild their collection's query cache.

Keep the source's mutations and events in order, including these less obvious cases:

- Start writes the project before the node and enters Experimental with zero stage work, retaining accumulated total work. Pausing retains work and frees assigned labs; resuming tests scientific requirements, stage facilities, lab minimum, free labs and program capacity in that order.
- Advance rejects non-finite/negative elapsed years before its zero-time return. It processes the initial project-ID sequence, skips paused projects, and pauses changed requirements before spending research points. A project can cross several stages in one call; a hypothesis evidence boundary pauses it and discards the unused budget for that call.
- Stage changes write project and node before applying grants. Missing facilities for the next stage pause at the completed current stage. Mature completion removes the project, sets total work to the definition's base cost, grants declared capabilities and maturity grants, emits the maturity event, then wakes children.
- Capability implications use a breadth-first queue and a visited `(capability, optional context)` key. Already-held capabilities still traverse implications; new capability events precede their indexed candidate wakeups. Preserve partial state when a later implied grant fails; do not add an unrequested transaction rollback.
- Candidate review deduplicates in input order and consults eligibility only for absent or pre-Investigable nodes. It preserves previous total work, clears resolution/stage work, and never scans the entire catalog as a substitute for the source indexes.
- `DeploymentEventUnlocked` emitted by a stage grant is permission for the owning subsystem to deploy. The kernel does not add that ID to state `EnabledDeploymentEventIds` in this method. Preserve this distinction when porting the later deployment bridge.

Kernel evidence must retain complete state and ordered events after sequential commands, rejected commands and exceptions. Include repeated grants, competing contextual requirements, paused capacity, changed facilities, hypothesis support/disproof, multi-stage advancement, and duplicate-event prevention. Source-accepted NaN lab allocations and overflow-derived research budgets are compatibility observations, not permission to silently invent new simulation rules. Record exceptional native safety boundaries separately.

## State and recovery details

`AdaptiveResearchState.cs` keeps sparse node/project/evidence state and increments both state and materialized-view revisions on mutation. Duplicate evidence is rejected without a revision change; replacing node/project records writes the next revision even when other values match. No-op pressure, facility, trait and applicability operations have different revision rules. Keep mutation behind the research authority rather than exposing mutable maps to presentation.

The standalone schema-1 snapshot deliberately sorts its collections and omits runtime revision counters. Restore rebuilds state through its mutators, so it does not preserve the original numeric revisions. It also does not include the expertise, pressure/outcome/agenda/foreign-technology sidecars, campaign funding or save-v16 envelope. A passing standalone snapshot round trip is therefore a narrower milestone than restoring the player's campaign.

Test remove/reinsert and replacement sequences against actual C# state enumeration before selecting a native ordered-container implementation. Source dictionary/set slot reuse can affect observable iteration and floating sums; do not assume that sorting every runtime collection preserves behavior. Keep explicit sorting only where the source sorts. Test null versus empty context IDs, contextual evidence matching, archived established-knowledge resolutions, paused-project lab totals, and failed restore ordering. Any native revision-overflow policy must be defined without signed overflow and recorded separately from normal source parity.

## Validation and export

Follow `SOURCE_PARITY_ACCEPTANCE.md`. Build actual-source fixtures from the maintained C# console projects; catch only the requested production operation, retain complete before/after state and compare event order. Test actual current data and bounded malformed copies located under identified generated test directories. Never alter canonical research files for a negative test.

Catalog coverage includes required-file absence, malformed JSON/types, duplicate and unknown identifiers, mismatched catalog/domain metadata, invalid prerequisite/capability references, declared counts and index ordering. Runtime coverage includes hidden candidate wakeups, prerequisites, species context, lab assignment/readiness, pressure/evidence, capability implications, hypothesis resolution, funding interruptions, cancellation/recovery and duplicate-event prevention. Keep source-only null/runtime behaviors separate from native safety boundaries.

The export manifest must explicitly include the research data needed by the configured native runtime. Relocated execution must resolve those assets from the package, not the repository working directory. Missing or corrupt data must report the concrete path and a nonzero terminal exit through the maintained executable; no scratch executable retry loop or uncaught crash dialog.

Do not claim player-save compatibility until Adaptive Research state, pending outcomes, escrow, once-only grants and diplomacy state round-trip with the original save schema. Do not remove the Godot/C# path or merge the migration into integration before the broader gameplay and presentation parity gates pass.
