# W6 Adjustment Report V0

Status: Accepted
Date: 2026-06-22
Depends on:
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/43-summoner-geomancer-concrete-v0.md`
- `docs/job-balance/44-time-mystic-concrete-v0.md`
- `docs/job-balance/46-bard-dancer-concrete-v0.md`
- `docs/job-balance/57-vanguard-ramza-concrete-v0.md`
- `docs/job-balance/60-prerequisite-tree-and-jp-cost-draft-v0.md`
- `docs/job-balance/61-jp-boost-removal-decision-v0.md`
- `docs/job-balance/63-w4-t21-populated-incidence-v0.md`
- `docs/job-balance/65-w5-f5-real-roster-sweep-schema-v0.md`
- `docs/job-balance/66-w5-f5-real-roster-sweep-v0.md`
- `work/w5-real-roster-sweep-v0.json`
- `work/w6-adjustment-report-v0.json`

## Purpose

W6 converts W5/F5 findings into bounded balance adjustments.

This document does not rewrite every upstream concrete-job file yet. It records the accepted W6
adjustment targets and the exact re-simulation gates that must pass when those mechanical patches
are applied. The purpose is to keep the next patch isolated: W5 found two failures, so W6 answers
those failures without flattening unrelated jobs, weapons, equipment, or caster identity.

Allowed W6 levers:

- raise JP cost;
- delay prerequisite depth;
- narrow skill scope;
- delay existing-equipment availability;
- cut a dangerous export;
- mark a row safe and preserve it.

Out of scope:

- Gil, shop price, reward, sell-value, or economy edits;
- new equipment;
- global fire, Faith, Summon, or magic nerfs;
- baseline-stat rewrites without a new dual-sim gate;
- weak-family or sword nerfs from W5 alone.

## Source Pins

| Artifact | SHA-256 |
| --- | --- |
| `docs/job-balance/65-w5-f5-real-roster-sweep-schema-v0.md` | `dc91fcdf8b29d077fd384825d2c32c782cfaa962a9c1adc236914ae199d80c30` |
| `docs/job-balance/66-w5-f5-real-roster-sweep-v0.md` | `0f28b9ecab44869580f62ce4cdbcfe0c717f7d8dc2477eb08c4f77f7620df936` |
| `work/w5_real_roster_sweep.py` | `341f0fde00108b1dc7f7460c0ff108e61d943fb5d4715e52604e2981e5c470e3` |
| `work/w5-real-roster-sweep-v0.json` | `51816babc2ceaa62bc4cde7b4b90a8f8a7167bd7c922f5db025a39d75a678d4e` |

W5 source commit: `3633df8bc4a2754266cbbf70848d3fb17b3f38bc`.

## W5 Findings Consumed

| W5 row | Result | Risk IDs | W6 read |
| --- | --- | --- | --- |
| `W5-P3-C-BELIEF-OIL` | fail | `1`, `4` | Primary numeric adjustment target. The Belief x Oil x fire-area vector reaches `681 / 415 = 1.641`. |
| `W5-P5-D-CONV` | fail | `3`, `4`, `7`, `9`, `12` | Same Belief/Oil vector remains live, with traced sustain/safety compression. |
| `W5-P5-E-LATE` | watch | `7`, `8`, `11` | Late breadth watch; no dominance proof. |
| `W5-RAMZA-C4-BREADTH` | watch | `11` | Ramza breadth watch; specialists still lead lanes. |
| Weak-family floor | pass | W5 floor lens | No weapon-family nerf or sword-specific correction from W5. |
| Equipment breakpoints | pass | `8` | Keep illegal support stacks illegal; no new equipment or Gil changes. |

## Adjustment Summary

| ID | Decision | Trigger | Lever | Re-sim required |
| --- | --- | --- | --- | --- |
| `W6-ADJ-01` | Remove the player-controlled `Oil` assembly from `Magma Surge`; keep `Oil x2.00`, `Belief x1.15`, and fire/Summon base values unchanged. | `W5-P3-C-BELIEF-OIL`, `W5-P5-D-CONV` | narrow skill/status assembly scope; cut dangerous export | yes |
| `W6-ADJ-02` | Preserve Belief, fire spells, and Summon base values; do not apply a global caster nerf. | Same rows | mark-safe with scope guard | yes, after `Magma Surge` assembly patch |
| `W6-ADJ-03` | Treat full-channel Bard/Dancer performance value as a protected ceiling, not the default pressured-roster expectation; keep action values unchanged. | `W5-P5-D-CONV` | normalize scenario assumption plus export guard | yes |
| `W6-ADJ-04` | Preserve Ramza C4 breadth with hard export boundaries. | `W5-P5-E-LATE`, `W5-RAMZA-C4-BREADTH` | mark-safe plus cut future export drift | yes, if Ramza R/S/M changes |
| `W6-ADJ-05` | Preserve weapon-family and equipment-breakpoint state. | W5 floor/equipment pass rows | mark-safe | no immediate re-sim |
| `W6-ADJ-06` | Keep late physical convergence pieces unassigned unless a dedicated R/S/M pass proves them safe. | W5 ceiling probes and doc 60 protected rows | cut dangerous export | yes, before assignment |

## `W6-ADJ-01` Magma Surge/Oil Assembly

Decision:

```text
Primary correction: Magma Surge no longer supplies player-controlled Oil setup
for same-party fire-area summon assembly.
Oil remains x2.00 where Oil exists.
Belief remains x1.15.
Fire spells and Summon base values remain unchanged.
```

This is the primary W6 correction.

It is intentionally surgical. The failed W5 vector is not "fire is too strong", "Summoner is too
strong", or "Faith is too strong." W5 proves the failure is the cross-job compound:

```text
Mystic Belief x Geomancer Magma Surge/Oil x Summoner Ifrit or Salamander area
```

The status still matters. A player can still build toward a high-payoff Oil mark when an existing
Oil source exists, and single-target fire can still exploit it hard. What W6 removes is the reliable
same-party route where Geomancer supplies the fire-amplifying setup and Summoner converts that setup
into a three-target ally-safe area spike.

`Magma Surge`'s Oil rider is an un-shipped deferred design rider from docs 43 and 44, not a vanilla
status mechanic. Removing it is therefore atlas-safe by default: W6 is retracting an un-canonized
rider, not changing Oil's vanilla status meaning. The mechanical patch should keep `Magma Surge` as
fire/earth terrain Geomancy damage, but it must not be the party's repeatable Oil setup for fire-area
summons. If `Magma Surge` needs a rider after the mechanical patch, the replacement must be an
existing non-fire-amplifying status that passes the vanilla status atlas and T4/T8 status gates.

No other repeatable player-controlled area `Oil` setup may be added as a replacement before a new W5
Belief/Oil re-sim.

### Lever Comparison

| Candidate | Change | Predicted named-risk math | Predicted `W5-P3-C-BELIEF-OIL` verdict | Predicted `W5-P5-D-CONV` verdict | W6 read |
| --- | --- | --- | --- | --- | --- |
| Oil magnitude trim | Change fire-on-Oil from `x2.00` to `x1.50` globally. | `Ifrit: 243 * 1.725 = 419`; `Salamander: 297 * 1.725 = 512`. | Watch/fail boundary: named risk improves, but `512 / 415 = 1.234` remains high. | Watch: Belief/Oil improves, but sustain/safety compression still needs the performer normalization gate. | Rejected as primary because it changes every Oil interaction to solve one constructed route. |
| `Magma Surge` assembly restriction | Remove `Oil` from the player-controlled `Magma Surge` setup route. | Synthetic Oil stack is no longer constructible from the tested source chain. Same summon without Oil is `Ifrit: 243 * 1.15 = 279`; `Salamander: 297 * 1.15 = 342`. | Pass predicted for the named Belief/Oil breach if no equivalent Oil setup is introduced. | Watch predicted: named risk removed; residual sustain/safety must be re-tested with pressured performer value. | Accepted primary lever. It targets the proved assembly route and preserves Oil, Belief, fire, and Summon values elsewhere. |
| Area Oil cap fallback | Keep single-target `Oil x2.00`, but cap ally-safe area fire to one Oiled target per cast. | Per-layer estimate: `Ifrit: 81 * 4.60 = 373`; `Salamander: 99 * 4.60 = 455`. Exact ratio-scaled: `Ifrit 372.0`; `Salamander 454.0`. | Watch boundary because `454 / 415 = 1.094`; cleaner than global Oil trim, but still changes a formula rule beyond `Magma Surge`. | Watch: same residual sustain/safety issue. | Reserved fallback only if the mechanical layer cannot remove/replace the `Magma Surge` Oil rider cleanly. |

The accepted first pass is the assembly restriction. The fallback path is deliberately not canonized
until the mechanical patch proves that `Magma Surge` cannot be separated from Oil with available data
or formula hooks.

The area-cap estimates above remain non-binding. The binding pass/watch/fail result must come from
the post-patch dual-sim majority/Pareto verdict, not from a ratio table alone.

Predicted primary-lever effect:

| Action | Current W5 vector | Current ratio vs `415` | Post-assembly vector | New ratio vs `415` |
| --- | ---: | ---: | ---: | ---: |
| `Ifrit` | `558` | `1.345` | `279` | `0.673` |
| `Salamander` | `681` | `1.641` | `342` | `0.823` |

Calculation:

```text
Ifrit: 243 * 1.15 = 279.45
Salamander: 297 * 1.15 = 341.55
```

Patch targets for the next mechanical pass:

| File | Required later edit |
| --- | --- |
| `docs/job-balance/43-summoner-geomancer-concrete-v0.md` | Replace the deferred `Magma Surge`/`Oil` into fire-summon area vector with the `Magma Surge` assembly restriction. Keep Summoner base values unchanged. |
| `docs/job-balance/44-time-mystic-concrete-v0.md` | Update the Belief x `Magma Surge`/Oil watch table so the player-controlled `Magma Surge` source chain is no longer valid. Keep Belief `1.15`. |
| `work/gpt-summoner-geomancer-concrete-v0.json` | Mirror the `Magma Surge` Oil-rider removal or replacement with a non-fire-amplifying existing status. |
| `work/gpt-time-mystic-concrete-v0.json` | Mirror the Belief/Oil source-chain update. |
| Formula/status bundle | Preserve `Oil x2.00`; add no area-cap rule unless the fallback path is explicitly activated by a failed mechanical proof. |

Acceptance gate:

- rerun the W5 Belief/Oil named-risk rows after the `Magma Surge` assembly patch;
- require the tested `Magma Surge` source chain to report no constructible Belief/Oil breach;
- keep any equivalent Oil setup below the named-risk bound before acceptance;
- confirm Geomancer `Magma Surge` and fire-Summoner rows still sit above the weak-family floor after
  rider removal;
- require `W5-P3-C-BELIEF-OIL` and `W5-P5-D-CONV` to return pass/watch by majority/Pareto verdict,
  not only by vector ratio;
- preserve non-Oil Black Magic, Summon, and Mystic rows byte-stable where possible;
- preserve single-target Oil `x2.00` rows unless a later, separate test proves they fail;
- get Claude 0-mismatch approval before accepting the mechanical patch.

## `W6-ADJ-02` Caster Values Mark-Safe

Decision:

- do not reduce `Ifrit`, `Salamander`, `Bahamut`, `Firaga`, `Flare`, or other fire/caster base
  values from W5 alone;
- do not reduce `Belief` from `1.15`;
- keep `Belief` battle-scoped;
- keep Bard/Dancer `Magickal Refrain` from stacking with `Belief`.

Reason:

W5 and Claude's independent checker reproduced caster constants exactly. The fail does not appear
in neutral caster rows; it appears only when Oil doubles every target in an area fire payoff and
Belief multiplies the result again.

Residual risk:

If the `Magma Surge` assembly patch still leaves an equivalent player-controlled Oil source above
bound, the next candidate should be Oil application timing or Oil counterplay, not a global Summoner,
Faith, or fire nerf.
`Field Salve` already clears Oil at 0 MP in doc 52, so counterplay should start by testing access,
timing, and action pressure around existing cleanup before changing damage constants.

## `W6-ADJ-03` Performer/Sustain Normalization

Decision:

Do not nerf `Seraph Song`, `Life's Anthem`, Bard/Dancer action values, or performer target caps in
W6.

Instead, W6 changes how real-roster pressure rows count performer value:

```text
Full-channel performer value is a protected ceiling.
Default pressured-roster expected value without a dedicated protection commitment = 0.65 * full value.
A row that claims full-channel value must also claim and expose the protection/positioning cost.
One performer cannot claim full sustain mode and full control/tempo mode in the same 24-tick window.
The 0.65 factor applies uniformly to every pressured performer row in W5/W7 until revalidated.
```

Predicted `W5-P5-D-CONV` sustain effect:

| Source | Current W5 count | W6 pressured expectation |
| --- | ---: | ---: |
| Ramza C3 hybrid sustain | `35` | `35` |
| `Seraph Song`, HP 390 cap 4 | `304` | `198` |
| Total | `339` | `233` |

Calculation:

```text
304 * 0.65 = 197.6
35 + 197.6 = 232.6
```

Reason:

Doc 46 already defines performance as interruptible sustained value: a protected performer gets
meaningful output, while a punished performer loses most of it. W5 correctly counted the full
accepted performance row as a ceiling, but `W5-P5-D-CONV` also used it as ordinary convergence-party
sustain while the same party retained high safety-defense. That compresses sustain and safety too
much without charging the party for protecting the performer.

The `0.65` coefficient is provisional scenario accounting, not a hidden skill nerf. It is grounded
in doc 46's performance uptime endpoints: a protected full channel is `1.00` value, while the named
tick-12 interruption example leaves `0.25` value. W6 uses a contested-uptime midpoint between those
endpoints, rounded from `0.625` to `0.65`, for rows that do not explicitly spend team resources to
protect the performer. Because it is a scenario rule, it must be applied to every pressured
performer row in W5/W7, not only to the failing convergence row.

Guardrails for later R/S/M implementation:

- `Encore` must not become earlier than its current D/E Lv4, 1000 JP posture;
- `Performance Mastery` and `Stagecraft` must remain performance-only, not generic sustain,
  defense, or caster support exports;
- `Performance Step` must remain narrow performer positioning, not a late universal movement answer;
- Bard and Dancer reaction/support/movement access must stay byte-identical across gender-linked
  active jobs.

Post-adjustment gate:

- rerun `W5-P5-D-CONV` with the `Magma Surge` assembly patch and pressured performer expectation;
- apply the same `0.65` pressured performer factor to every W5/W7 pressured performer row;
- keep the full-channel performance row as a separate protected-ceiling proof row;
- require `W5-P3-C-BELIEF-OIL` and `W5-P5-D-CONV` to return pass/watch by majority/Pareto verdict,
  not only by vector ratio;
- if sustain/safety still compresses after this, the next candidate is performer R/S/M pacing or
  protection-counterplay, not immediate `Seraph Song` value cuts.

## `W6-ADJ-04` Ramza Breadth Boundaries

Decision:

No Ramza nerf from W5.

Hard boundaries for future docs:

- do not add exportable Ramza-only R/S/M from W5 evidence;
- do not expand Ramza native premium equipment access from W5 evidence;
- do not give C4 Ramza a broad party support that replaces protected specialist lanes;
- keep final Ramza strong as a hybrid, but ensure specialists still lead their own lanes.

Reason:

`W5-P5-E-LATE` and `W5-RAMZA-C4-BREADTH` are watches, not fails. In the direct breadth row, Black
Mage leads damage, Vanguard leads protection/control, and Ninja leads mobility.

Acceptance gate:

Any future Ramza R/S/M, equipment expansion, or support export must rerun the Ramza breadth row.

## `W6-ADJ-05` Weapon And Equipment Mark-Safe

Decision:

- no weapon-family nerf;
- no sword nerf;
- no new equipment;
- no Gil or economy changes;
- keep `Equip Knight Swords` optional and cuttable;
- keep illegal learned-support stacks illegal.

Reason:

W5 proves the combat weak-family floor holds. The lowest combat weak-family ratio is `0.600`, above
the provisional `0.55` floor. Equipment breakpoint rows pass as long as the existing support-slot
rules remain enforced.

This is important for the mod's core goal: do not "fix" sword dominance by flattening or nerfing
weapon identities that W5 already shows are viable.

## `W6-ADJ-06` Late Physical Export Guard

Decision:

Keep high-convergence late physical pieces unassigned until a dedicated R/S/M placement pass proves
them safe in real roster rows.

Binding guard:

- `Attack Boost` remains unassigned/non-canon in W6;
- if assigned later, it must be D/E paced, high JP, support-slot exclusive, and re-simulated against
  native dual-wield, Doublehand, and premium equipment rows;
- do not use W5 stress-probe values as accepted player-facing ceilings by themselves.

Reason:

W5 floor rows pass and do not justify physical nerfs. The remaining risk is export placement:
stacking a generic physical boost onto already-good late weapon packages could recreate the old FFT
"best weapon plus best support" collapse under a different name.

## Mechanical Patch Order

Recommended next sequence:

1. Patch `Magma Surge` so it no longer supplies player-controlled `Oil` for fire-area summon assembly,
   and mirror that in docs 43 and 44 plus their JSON sidecars.
2. Patch W5/W7 scenario normalization so pressured performer rows use the `0.65` expected factor
   unless a row explicitly pays for protected channeling.
3. Rerun `W5-P3-C-BELIEF-OIL` and `W5-P5-D-CONV`.
4. Ask Claude for a new dual-sim check.
5. Only then decide whether performer R/S/M pacing still needs adjustment.

Do not batch global caster changes, Ramza changes, weapon changes, equipment changes, Gil changes,
or JP-table rewrites into the Oil/performance patch. W5 identified narrow failures; the next pass
should isolate those failures.

## Claude Review Request

Claude should review:

- whether the `Magma Surge` assembly restriction is the right primary surgical lever;
- whether preserving single-target Oil `x2.00`, Belief `1.15`, fire spells, and Summon base values
  is correct;
- whether the performer `0.65` pressured expectation is a fair W5/W7 row normalization;
- whether the Ramza, equipment, weapon, and late physical export mark-safe decisions are sufficiently
  bounded;
- whether the mechanical patch order is narrow enough for a clean re-simulation gate.
