# Foreign Technology, Transfer & Brokerage

Stellar Continuum treats foreign technology as **knowledge, physical artifacts, institutions, people, and processes** rather than an instant universal unlock.

A civilization can capture something enormously valuable and still be unable to use it. It can understand a foreign principle but be unable to manufacture it. It can operate a captured system only while alien specialists and tooling remain. Or it can possess technology that is useless to itself but extremely valuable to another civilization.

This document defines the research-side model. Diplomacy/economy systems own negotiation, payment, contracts, sanctions, trust, political consequences, and enforcement actions.

## There is no single reverse-engineering percentage

Foreign technology is assessed on four separate axes.

### Understanding

- **Unknown** — holder does not yet recognize a meaningful technological phenomenon.
- **Observed** — effect/capability has been legitimately observed.
- **Characterized** — major components/materials/interfaces/effects are identified.
- **Principle Understood** — underlying science is substantially understood.
- **Engineering Understood** — design/process detail is understood well enough to reason about reproduction/adaptation.

### Operability

- **Unknown**
- **Unusable**
- **Origin Only** — works only with source civilization infrastructure/biology/environment/personnel/control system.
- **Supported Operation** — holder can operate it while depending on foreign experts/tooling/infrastructure/consumables.
- **Adapted Operation** — native adapters/modifications permit operation.
- **Native Operation** — compatible implementations can be operated through the holder's own normal institutions.

### Reproduction

- **None**
- **Component Replication**
- **Subsystem Replication**
- **Foreign-Process Replication** — complete system can be reproduced only by preserving/imitating source tooling/process/environment.
- **Native-Process Replication** — holder can manufacture a functional equivalent using its own industrial system.

### Adaptation

- **None**
- **Conceptual Inspiration** — foreign science changes native research without producing a copy.
- **Interface Adaptation** — native support/interface systems make source technology usable.
- **Native Derivative** — holder develops its own implementation from foreign principles.
- **Hybrid Lineage** — native and foreign traditions combine into a genuinely new technological lineage.

These axes are deliberately non-linear.

A civilization can:

- operate a device without fully understanding it
- understand a principle without being able to reproduce it
- reproduce a foreign process only while captured tooling survives
- develop a native derivative without ever cloning the source device
- find a technology biologically useless yet scientifically or commercially valuable

## Compatibility constraints

A foreign system can be limited by one or several real constraints:

- scientific gap
- unavailable materials
- unavailable power/energy systems
- manufacturing-process dependency
- infrastructure dependency
- biological incompatibility
- environmental incompatibility
- cognitive/interface incompatibility
- software/identity dependency
- specialized consumables
- expert/tacit-knowledge dependency
- hazardous unknown behavior

Constraints are discovered through evidence and research. The UI can show uncertainty when unknown constraints probably remain, but it must not invent fake precision.

## Existing xenoscience research nodes matter

The foreign-technology model uses the existing Adaptive Research graph rather than creating a separate minigame.

Examples:

- **Foreign Device Forensics** — characterization and interface/component identification
- **Alien Material Analysis** — material dependency and component understanding
- **Reverse-Engineering Methodology** — disciplined subsystem reproduction
- **Technology Compatibility Science** — explicit compatibility assessment
- **Alien Power-System Analysis**
- **Alien Propulsion Analysis**
- **Foreign Scientific Translation**
- **Foreign Manufacturing Analysis**
- **Xenobiological Compatibility Science**
- **Cross-Lineage Engineering** — native derivatives
- **Hybrid Technology Design** — deliberately mixed technological lineages
- **Hazardous Foreign-Technology Protocols** — safer study of dangerous/incomprehensible assets

The source data is `data/research/v1/foreign_technology_model.json`.

## Technology transfer is a package, not a button

A technology deal is composed from actual things being transferred.

Possible components include:

- observation dossier
- theory/scientific records
- experimental datasets
- engineering blueprints
- manufacturing/process documentation
- reference hardware
- production tooling
- expert assistance
- training program
- operating laboratory/factory/shipyard/institution

Different packages can therefore have dramatically different value.

### Example

Civilization A sells Civilization B a propulsion **theory dossier**.

B gains codified science but no prototype, tooling, manufacturing process, or expert support.

Another deal might include:

- theory dossier
- experimental data
- blueprints
- one reference engine
- production tooling
- five-year expert-assistance program
- local training program

That is a far more complete transfer and can be worth vastly more.

## Package components map to the tacit-knowledge system

Technology exchange reuses the canonical knowledge assets.

Examples:

- theory/blueprints -> `codified_records`
- experimental data -> `experimental_dataset`
- manufacturing/process documentation -> `experimental_protocols`
- reference hardware -> `intact_prototype`
- production tooling -> `manufacturing_tooling`
- experts -> `expert_cohort`
- training -> `training_pipeline`
- operating institution -> `operating_institution`

This prevents trade, conquest, archaeology, and reverse engineering from each inventing a different representation of the same knowledge.

## Legal rights and technical ability are different

Technology packages can carry legal rights such as:

- inspect
- internal research
- operate supplied units
- manufacture
- modify
- civilian use
- military use
- share with allies
- export to third parties
- sublicense
- resell package

Licenses can also constrain duration, production quantity, application, territory, recipient, exclusivity, derivative rights, and reporting.

But:

> **A license is law, not physics.**

If a civilization possesses the technical knowledge and can manufacture a prohibited military derivative, the research system does not magically stop it because the contract says no.

Diplomacy/law/intelligence systems determine whether the breach is discovered and what happens afterward: sanctions, trust loss, compensation demands, embargo, covert action, retaliation, or war.

Real copy protection can exist only if implemented by actual technology such as encryption, authentication, machine identity, or biological locks—and those protections can themselves become technical targets.

## Possession does not mean permission

Captured/stolen data may carry no lawful rights whatsoever.

A civilization may still technically study or use what it physically possesses.

Likewise, a civilization can legally purchase manufacturing rights but discover that its industry, biology, materials, or scientific competence cannot actually use them yet.

So:

- **rights do not equal technical ability**
- **technical ability does not equal legal rights**

## Technology value is buyer-specific

There is no universal:

> Technology Value: 500

Research-side value depends on the buyer.

Important inputs include:

- capability novelty
- current recognized need / Research Pressure
- existing alternatives
- compatibility assessment
- native research work/time likely saved
- field competence/readiness
- available facilities/materials/populations
- package completeness
- expert/tooling support
- dependency risk
- hazard risk
- legal rights included
- scarcity/exclusivity
- legitimately known third-party demand
- value of denying the technology to rivals

The research model outputs assessments such as:

- research utility
- operational utility
- strategic-denial value
- resale/brokerage utility
- dependency risk
- hazard risk
- expected assimilation horizon

Diplomacy/economy can then turn those into offers/demands according to each civilization's actual interests and knowledge.

## Brokerage is a real playstyle

A technology can be commercially valuable even when the current holder cannot use it.

Example:

Humans acquire high-pressure aquatic life-support technology.

Human compatibility:

- direct use: essentially none
- scientific value: moderate
- reproduction: maybe limited

Another aquatic civilization might evaluate the exact same package as civilization-changing.

Humans can sell or broker it without ever integrating it into human ships.

This creates technological arbitrage based on legitimate knowledge of other civilizations' needs rather than an arbitrary trade-price table.

## Conquest does not grant an instant technology list

Conquest can yield:

- devices
- samples
- factories
- laboratories
- tooling
- records
- expert populations
- training institutions

The conqueror still has to assess and assimilate them.

A captured factory may immediately support `foreign_process_replication` while its original staff and processes remain, but the conqueror can still lack native-process reproduction.

Destroying, driving away, or failing to train from those specialists can permanently reduce the value of the conquest.

## Performance

Foreign assessments are stored only for strategically relevant technology assets/lineages.

They are reevaluated when something meaningful changes:

- new evidence/device/sample/records
- field competence changes materially
- experts/tooling/institutions are gained/lost
- xenoscience/reverse-engineering research matures
- native material/energy/manufacturing capability changes
- a newly compatible population/species becomes available

They are not recomputed every simulation tick.

## Canonical files

- `data/research/v1/foreign_technology_model.json`
- `data/research/v1/technology_exchange_model.json`
- `data/research/v1/tacit_knowledge_model.json`

These extend the normal Adaptive Research graph; they do not bypass it.
