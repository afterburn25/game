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
2. Port civilization state and snapshot import/export with revisions, project identities and ordered collections. Research uses string civilization IDs; the campaign bridge maps integer game IDs explicitly. Do not reinterpret identity types.
3. Port applicability, facilities, progress policy, eligibility and view generation. Observer-facing locked/hidden information must be derived from the original authority, not from direct catalog enumeration in presentation.
4. Port the runtime kernel and authority, then expertise, pressure, hypotheses/outcomes, agenda and foreign-technology features with their retained state and deterministic inputs. Preserve event-driven candidate wakeups; do not replace them with a full-tree scan every frame.
5. Port starting profiles, funding/escrow commands, facility synchronization, maturity/development progression and campaign capability adapters. Verify setup, operating and milestone costs, partial funding, paused projects and once-only commitment consumption across save/recovery.
6. Compose Adaptive Research and diplomacy with the native campaign coordinator, sensor contacts and accepted clock. The full configured player path requires a separate parity fixture and multi-step test; a passing legacy coordinator remains a narrower result.

Use one immutable shared catalog per content version. Native state stores stable IDs and owned values; short-lived queries borrow const definitions. Do not retain references into growable vectors or temporary JSON trees. Do not clone mutable capability/hostility policy state when constructing temporary world views. Keep research/game rules in Stellar Core, with generic asset location and file services in Stellar Engine.

## Validation and export

Follow `SOURCE_PARITY_ACCEPTANCE.md`. Build actual-source fixtures from the maintained C# console projects; catch only the requested production operation, retain complete before/after state and compare event order. Test actual current data and bounded malformed copies located under identified generated test directories. Never alter canonical research files for a negative test.

Catalog coverage includes required-file absence, malformed JSON/types, duplicate and unknown identifiers, mismatched catalog/domain metadata, invalid prerequisite/capability references, declared counts and index ordering. Runtime coverage includes hidden candidate wakeups, prerequisites, species context, lab assignment/readiness, pressure/evidence, capability implications, hypothesis resolution, funding interruptions, cancellation/recovery and duplicate-event prevention. Keep source-only null/runtime behaviors separate from native safety boundaries.

The export manifest must explicitly include the research data needed by the configured native runtime. Relocated execution must resolve those assets from the package, not the repository working directory. Missing or corrupt data must report the concrete path and a nonzero terminal exit through the maintained executable; no scratch executable retry loop or uncaught crash dialog.

Do not claim player-save compatibility until Adaptive Research state, pending outcomes, escrow, once-only grants and diplomacy state round-trip with the original save schema. Do not remove the Godot/C# path or merge the migration into integration before the broader gameplay and presentation parity gates pass.
