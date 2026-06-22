# JP Boost Removal Decision V0

Status: Accepted by Claude review
Date: 2026-06-22
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/32-campaign-artifacts-provisional-v0.md`
- `docs/job-balance/50-campaign-journey-bundle-v0.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/job-balance/52-squire-chemist-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/job-balance/60-prerequisite-tree-and-jp-cost-draft-v0.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-physical-foundation-rsm-concrete-v0.json`
- `work/gpt-prerequisite-tree-jp-cost-v0.json`

## Purpose

This document records the accepted design direction that `JP Boost` is removed from the mod.

It exists before the mechanical patch pass so every later edit has one authoritative policy:
`JP Boost` is not a learnable build piece, not a hidden progression tax, and not replaced by another
universal JP accelerator.

## Decision

`JP Boost` is removed completely from the mod design.

It should not appear as:

- a Squire support ability;
- a learnable support ability on any other job;
- a protected W4/T2.1 seed row;
- a formula-bundle constant such as `jp_boost_multiplier`;
- a campaign journey envelope;
- a with/without benchmark split;
- a hidden progression requirement;
- a replacement universal JP accelerator under another name.

Vanilla reference docs may still list ability id 463 because they describe the source game. The mod
intentionally does not use that row.

## Rationale

`JP Boost` creates a false choice. Even without combat stats, it pushes players to equip one support
slot for long stretches of the campaign because skipping it feels like wasting future build progress.

That is the opposite of the mod goal. Support slots should express a tactical build choice, not a
permanent training tax.

Removing `JP Boost` also removes an entire class of special-case campaign analysis. W4 no longer
needs "with JP Boost" and "without JP Boost" incidence splits. It still needs progression-pressure
rows, but under a fixed JP model.

## Squire Support Identity

Removing `JP Boost` does not leave Squire without a support identity.

Squire's intentional support export is `Basic Training`.

| Piece | Slot | Band | Depth | JP | Effect | Limits |
| --- | --- | --- | --- | ---: | --- | --- |
| `Basic Training` | Support | B/C | Lv3 | 350 | Squire/Fundaments-style action output x1.10 | Squire/Fundaments actions only. Excludes ordinary attacks, weapon formulas, spells, items, Martial Arts, Jump, Iaido, Throw, Steal, Speechcraft, and `Ultima`. |

This keeps Squire's identity anchored on starter utility actions and gateway progression. It rewards
a unit that actually keeps using Squire/Fundaments actions, without becoming mandatory
cross-job infrastructure.

`Basic Training` is intentionally narrow. If W4 later shows that it is too weak or never chosen, the
first fix direction is to improve Squire's underlying actions or action-side value, not to broaden
`Basic Training` into generic physical, weapon, spell, or item output. Broadening it would recreate
the support-tax problem that `JP Boost` caused.

W4/T2.1 must still check whether `Basic Training` appears in plausible builds. A niche flavor pick is
acceptable. A never-fielded support is a problem, but the fix remains action-side rather than scope
expansion.

Rejected replacements for `JP Boost`:

| Candidate | Decision |
| --- | --- |
| New universal JP accelerator | Rejected. It recreates the same support tax under another name. |
| Weapon Drill as support | Rejected. It risks overlapping Archer accuracy, Monk unarmed identity, and weapon-family support lanes. |
| Teamwork or follow-through support | Rejected for this pass. It invents a new subsystem and overlaps Bard, Dancer, Orator, Vanguard, or Ramza identity. |
| Equip Swords or broad weapon access | Rejected. It risks reintroducing sword dominance and generic physical convergence. |
| Grit-enhancing support | Rejected. Squire already has `Grit` as a reaction, and a support that mainly buffs one reaction is too narrow or awkward. |
| No Squire support | Rejected. `Basic Training` already exists and is the correct support identity. |

## JP Pacing Posture

The mod should not become grindier because `JP Boost` was removed.

Vanilla-style `JP Boost` effectively made a 25 percent JP acceleration feel like the real baseline
for players who cared about optimization. Deleting it without recalibration would slow ordinary
build exploration and punish the player for removing a bad support tax.

The accepted direction is:

1. do not accept slower exploration as the default outcome;
2. do not add another universal JP multiplier;
3. use JP costs and prerequisite depth as the primary pacing levers;
4. use baseline JP-gain changes only as a later global fallback if targeted JP-cost and depth tuning
   cannot preserve healthy progression;
5. keep all Gil, price, reward, sell-value, drop, and shop-economy levers out of scope.

W4 should therefore test ordinary, optimizer, and grind-heavy progression under a fixed JP model.
If ordinary routes become too slow, lower relevant JP costs or prerequisite depths before considering
any global JP gain adjustment.

A global JP-gain increase would accelerate every route, including deep convergence engines such as
`Doublehand`, `Dual Wield`, and `Equip Knight Swords`. Targeted JP-cost and prerequisite-depth
tuning can speed intended early build breadth while keeping dangerous late engines expensive.

## Grind-To-Break Reframe

The W9 grind-to-break risk remains live.

`JP Boost` was the accelerant, but raw grinding can still push deep jobs, support engines, movement
skills, or R/S/M packages into earlier campaign bands. The risk changes mechanism; it does not
disappear.

Replace old "with JP Boost / without JP Boost" rows with:

| Row type | Meaning |
| --- | --- |
| Ordinary progression | Expected non-optimized play without hidden routes or excessive grind. |
| Optimizer progression | Knowledgeable routing within normal campaign play, but no repeat-grind loop. |
| Grind-heavy progression | Deliberate overgrind to force deep job depth or expensive exports early. |

W4/T2.1 and W5/F5 should preserve the question:

```text
Can a player reach a deep job, support engine, movement default, or R/S/M convergence package while
still facing encounters whose campaign band was not meant for that power?
```

## Patch Map

The first patch pass after this decision should update active consumers before W4.

Mandatory active-consumer patches:

| File | Required change |
| --- | --- |
| `docs/job-balance/31-campaign-gameplay-validation-v1.md` | Remove active `JP Boost` design claims; reframe JP acquisition and W9 around fixed JP ordinary/optimizer/grind-heavy rows. |
| `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md` | Delete the `JP Boost` Squire support row; elevate `Basic Training` as Squire's sole intentional support export; reframe W4/W5 follow-up. |
| `work/gpt-physical-foundation-rsm-concrete-v0.json` | Remove Squire `JP Boost`; keep `Basic Training` unchanged. |
| `docs/job-balance/60-prerequisite-tree-and-jp-cost-draft-v0.md` | Remove `JP Boost` from Squire support table, protected rows, and W4 optimizer-lever text; keep `Basic Training` B/C Lv3 350. |
| `work/gpt-prerequisite-tree-jp-cost-v0.json` | Remove `JP Boost` from `rsm_export_costs` and `protected_convergence_rows`; keep other rows byte-stable where possible. |
| `docs/job-balance/51-progression-and-build-input-ledger-v0.md` | Replace with/without `JP Boost` routing with fixed-JP ordinary/optimizer/grind-heavy routing. |
| `docs/job-balance/50-campaign-journey-bundle-v0.md` | Remove `JP Boost` party-envelope assumptions and update A/B pacing anchors. |
| `work/gpt-campaign-journey-bundle-v0.json` | Mirror doc 50 removal of `JP Boost` envelopes and route labels. |
| `work/sim-inputs-v0.2.1.json` | Remove `rsm_constants.support_effects.jp_boost_multiplier`. This is a bundle re-pin, but not a damage-model change. |

Supersession-note patches:

| File | Required change |
| --- | --- |
| `docs/job-balance/32-campaign-artifacts-provisional-v0.md` | Add or adjust the active note that old `JP Boost` risk rows are superseded by fixed-JP grind-to-break rows. |
| `docs/job-balance/47-campaign-validation-readiness-v0.md` | Reframe risk register from `JP Boost acceleration` to fixed-JP grind-to-break pacing. |
| `work/gpt-campaign-validation-readiness-v0.json` | Mirror doc 47 risk wording. |
| `docs/job-balance/48-milestone-note-v0.md` | Reframe the Marcelo-facing risk summary. |
| `work/gpt-campaign-milestone-note-v0.json` | Mirror doc 48 risk wording. |
| `docs/job-balance/52-squire-chemist-concrete-v0.md` | Reframe deferred R/S/M references so Squire support identity is `Basic Training`, not `JP Boost`. |

Historical files to leave intact:

| File family | Policy |
| --- | --- |
| `docs/job-balance/04-foundation-physical-jobs-proposal.md` | Historical V1 proposal; do not rewrite. |
| `docs/job-balance/05-squire-chemist-v1-proposal.md` | Historical V1 proposal; do not rewrite. |
| `docs/job-balance/28-foundation-reconciliation-v1.md` | Historical reconciliation; do not rewrite unless a later alias note is needed. |

Vanilla reference files to leave intact:

| File family | Policy |
| --- | --- |
| `docs/reference/*` | Source-game atlas truth; may continue listing vanilla id 463 `JP Boost`. |
| `work/baseline_abilities.csv` | Source-game baseline extract; do not edit for mod design removal. |

## Review Discipline

This decision doc should be reviewed before the mechanical patch pass.

After approval:

1. patch active consumer docs and JSON sidecars in a coordinated pass;
2. treat `work/sim-inputs-v0.2.1.json` as a separate bundle re-pin;
3. prove that removing `jp_boost_multiplier` does not touch damage-model sections;
4. prove doc 58 damage rows remain unchanged except for `JP Boost` deletion and Squire support
   framing;
5. prove doc 60 keeps all non-`JP Boost` seed rows stable.

No damage resimulation is required solely for removing `JP Boost`, because it never entered the
damage pipeline. W4 still must validate progression and incidence under the fixed JP model.

## Claude Review Request

Claude should review:

- whether this document fully captures the accepted removal decision;
- whether `Basic Training` is correctly elevated as Squire's support identity without changing its
  numbers;
- whether the JP pacing posture is concrete enough for W4;
- whether the W9 grind-to-break risk is preserved under the fixed JP model;
- whether the mandatory patch set, supersession-note set, historical set, and vanilla-reference
  no-edit set are complete.
