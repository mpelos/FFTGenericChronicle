# Formula Balance Technical Envelope

Status: Accepted
Date: 2026-06-20
Owner: GPT-5 writes; Claude reviews and must approve before accepted guidance changes.
Review: Approved by Claude on 2026-06-20.

## Purpose

This document defines the technical envelope for Generic Chronicle formula-balance work. It is
not a balance design by itself. It states what the current modding surface can express, what
requires code hooks, and how every future decision must record confidence and implementation
dependency.

The goal of the mod remains design-led: make every weapon family meaningfully useful in endgame
while preserving the feel, rhythm, scale, and spirit of FFT. Technical constraints guide the
design, but they do not replace the design goal.

## Source Documents

This document inherits the current technical map from:

- `docs/modding/00-overview.md`
- `docs/modding/02-formula-id-catalog.md`
- `docs/modding/03-custom-formula-feasibility.md`
- `docs/modding/04-re-strategy.md`
- `docs/modding/05-battle-data-map.md`

If this document conflicts with those research files, treat this document as the balance-facing
summary and revisit the source research before making a final decision.

## Baseline Target

The design target is FINAL FANTASY TACTICS - The Ivalice Chronicles, Steam Enhanced v1.5.0.
Classic FFT and WotL data are references only when the IVC value has not been captured yet.

WotL/FFHacktics references are useful because IVC appears to preserve the same broad formula
language, but they are not automatically final truth for this build. Where IVC base
Formula/X/Y or weapon values have not been captured, use WotL/FFHacktics as the provisional
reference and apply known IVC rebalance notes from `docs/modding/` until in-game proof replaces
the fallback.

## Machine Reality

This checkout is running on Linux. The game install, Reloaded-II, mod loader templates, and
FF16Tools.CLI live on the Windows game machine paths documented in `docs/modding/00-overview.md`.

Work that requires extracting fresh game data, converting NXD files, deploying proof patches, or
testing the running game must happen on that Windows machine. Until then, new facts about live
weapon data or proof patches must be labeled `needs data capture on game machine` or
`needs in-game proof`.

## Access Tiers

### Tier 1 - Data-Only Baseline

Tier 1 is the required baseline for the mod. It uses existing data surfaces:

- `OverrideAbilityActionData` for per-ability `Formula`, `X`, `Y`, element, status, range, AoE,
  vertical tolerance, CT, and MP cost.
- `ItemWeaponData.xml` for weapon formula, power, range, elements, evasion, attack flags, and
  option ability.
- TableData XML for jobs, skillsets, items, armor, status, shops, spawns, and related hardcoded
  tables.
- ENTD encounter binaries for battle roster, level, job, equipment, skills, and placement.

Tier 1 cannot write arbitrary math. It can choose an existing hardcoded formula routine and feed
that routine the limited parameters exposed by data. For ability action formulas, the practical
constraint is a fixed formula id plus two byte-sized numeric parameters, `X` and `Y`, plus
metadata such as element, status, CT, and MP.

For weapon attacks, `ItemWeaponData.xml` exposes a much smaller weapon-formula vocabulary than
the full ability catalog. Future family design must map each family against the actual weapon
formula ids available in the data, not against an imagined free-form weapon formula system.

Tier 1 design must be able to stand on its own. A weapon-family identity is not accepted unless
there is a plausible Tier-1 version or an explicit decision that the family depends on Tier 2.

### Tier 2 - Code-Hook Extension

Tier 2 means writing a Reloaded-II code mod that locates and hooks battle logic inside
`FFT_enhanced.exe`. It can potentially compute arbitrary custom math over live battle-unit
state, such as PA, MA, Speed, Brave, Faith, HP/MP, level, status, position, element, and computed
damage.

Tier 2 is feasible in principle, but it is not proven as a finished Generic Chronicle gameplay
pipeline yet. It requires reverse engineering, signature scanning, hook validation, and live
testing. No balance rule should make the whole project dependent on Tier 2 until the relevant
hook has been proven in this build.

Tier 2 is allowed as an extension path when it materially improves the final balance goal. It is
not allowed to become a hidden assumption in documents that claim to be data-only.

## Design Policy: Graceful Degradation

Every weapon-family concept should be written in two layers when possible:

1. Ideal identity: the best expression of the family if all needed mechanics are available.
2. Tier-1 fallback: the strongest version expressible with existing data-only tools.

If the ideal identity requires Tier 2, the document must say so directly. If the Tier-1 fallback
is too weak to meet the mod's goals, that becomes an explicit risk, not an implicit promise.

Example structure for future family notes:

```text
Family: example weapon
Ideal identity: what the family should feel like.
Tier-1 expression: existing formulas / data levers that can approximate it.
Tier-2 dependency: what extra math or state would be needed.
Confidence: high / medium / low.
Proof needed: data test, hook test, playtest, or none.
```

## Confidence And Proof Labels

Every design decision must include these labels:

- Confidence: `high`, `medium`, or `low`.
- Dependency: `Tier-1`, `Tier-2`, `Mixed`, or `Research-only`.
- Proof state: `verified in local files`, `external/source-derived`, `needs in-game proof`,
  `needs reverse engineering`, or `needs playtest`.

These labels prevent a conceptual goal, a known data field, and an unproven hook from being
treated as the same kind of fact.

## Current Hard Constraints

- Base per-ability Formula/X/Y values are hardcoded in `FFT_enhanced.exe`; the data table is a
  sparse override layer.
- Data-only ability formulas can select existing routines, not define new routines.
- Ability `X` and `Y` are byte-sized values.
- Many formula behaviors are slot- or command-hardcoded; changing only a Formula byte may not
  carry every behavior to every action.
- The classic FFT weapon formula families already encode part of weapon identity.
- The repo does not yet contain a committed `ItemWeaponData.xml` CSV baseline by weapon family,
  formula id, power, range, element, and related weapon fields. That dump is required before
  final weapon-family taxonomy.
- Generic point-by-point armor DR is not currently a proven FFT data concept. Any armor
  penetration model inspired by another system is a hypothesis until mapped to Tier 1 or proven
  through Tier 2.
- The full data pipeline still needs an end-to-end proof patch in the running game before any
  concrete formula numbers are final.

## Gate Rules

These are process gates, not balance hard rules:

- Conceptual design may proceed before proof patches.
- No numeric formula set becomes final until the relevant data path or hook path is proven.
- No weapon-family taxonomy becomes final until the weapon data baseline is captured or its
  absence is explicitly accepted as a risk.
- Tier-1 is the baseline implementation target until a Tier-2 hook is proven.
- GURPS and any other external system are references only. They can suggest useful models, but
  they do not override FFT feel, readability, or balance.
- A Claude approval is required before moving a document from proposed guidance to accepted
  guidance.
