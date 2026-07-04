# White Mage (#05) — design convergence & validation gate

Date: 2026-06-30. Closes the White Mage design (first DCL caster). Registered doc:
`docs/job-balance/jobs/05-white-mage.md`. Sim harness: `work/1782829577-sim-white-mage.py`.

## Process

Designed from the vanilla White Mage (`docs/job-balance/vanilla/05-white-mage.md`) + the DCL magic system
(`docs/deep-combat-layer/11`, validated 2026-06-26), never anchored on a prior draft. GPT peer (thread
`019f1030-9ddc-7ab2-8179-0f90cf106a30`, gpt-5.5) participated per the divergence + every-decision directives:
it supplied the **"Threshold Keeper"** compass and, on the offense question, tightened the smite materially
(modest, iconic, no second offensive tool). Marcelo drove two mid-design changes — (a) "every job needs a
minimum offensive kit; lean on the free range-3 Staff bolt"; (b) "a single-target spell at Fire/Blizzard/
Thunder power?" — both routed back through GPT before registering, then approved ("pode registrar").

## Compass (converged)

The job for fights that **cross catastrophic thresholds** (mass KO, severe/magical status, magic burst,
undead, boss spike). **Pre-empts** with Faith-independent wards + timed Reraise; **recovers** with
Faith-scaled magnitude (big heals, mass/area revive, severe cleanse) + a modest Holy. Explicitly NOT the
flat Faith-proof triage (Chemist) nor self-sustain (Monk).

## Key decisions & the cross-job invariants they respect

| Decision | Rationale | Invariant checked |
|---|---|---|
| Robes / low HP / low Brave / **HIGH Faith** | devotion powers Faith-scaled magnitude **and** is the two-sided weakness (×1.30 magic taken, magical-status prone) | chassis bound by sprite (cleric robe); Faith two-sided (`08`); status three-axis (`13`) |
| Healing = Faith-scaled **magnitude**, not flat triage; heal-tax cedes low-Faith corner to Chemist | de-overlaps the three recovery jobs (vanilla problem #1) | no-strictly-better vs `02-chemist`, `07-monk` |
| Ward suite (Protect/Shell/Regen/Wall) **Faith-INDEPENDENT** | the *only* way it protects a low-Faith bruiser it can't out-heal; buffs are friendly no-resist magnitude+duration (`11`) | resolves vanilla's backwards Faith-gating |
| **Minimum offense** = free range-3 Staff bolt (~29% Firaga) + one Core Holy | no 100%-support dead turn (vanilla problem #2) | every-job-needs-minimum-offense |
| **Holy** = single-target, basic-Fire-tier (~43% Firaga), spiritual (Faith×2, ignores DR+Zodiac), Magic-Evadable; **NO ladder, NO AoE, EVER** | minimum offense without becoming a second burst caster | identity wall vs Black Mage; defence-bypass ration (Magic-Evade still rolls, `11`) |
| Reraise → Tier-2, single-target, high-MP, CT, **SHORT duration** | de-powers vanilla's pre-battle "I win" blanket (problem #4) | no mandatory-splash dominator |
| Liturgy innate **+** exported Support | moat = free economy on high-MA/Faith/Staff-A robes, not exclusivity | parasitic-innate-export (Landreader/Aerial Training precedent, `15`) |

## Sim read (`work/1782829577-sim-white-mage.py`, all gates pass; calibration GATES not breaks)

- **SIM 1 — heal differentiation + heal-tax:** WM out-heals Chemist on high/neutral-Faith allies; on a
  low-Faith ally WM Cure falls **below** Chemist's flat Hi-Potion. Distinct axis confirmed; Chemist/Monk keep
  the low-Faith corner.
- **SIM 2 — ★ buff-turtle gate:** 1 warded+Regen body is out-sustainable (intended hard turtle) but
  focus-fired open (2–3 attackers overwhelm; a spike hit > the Regen buffer). **CALIBRATION GATE: Regen must
  stay below one committed warded attacker's chip** (`12`, Hypothesis), else a single body is unkillable.
  Structure sound: wards/Regen are durations, party versions are Tier-2, WM is killable.
- **SIM 3 — fragility / two-sided:** folds to a Thief dive (TTK ~1.4) and is near one-shot by an enemy Firaga
  on its high Faith (×1.30). Disruptable on all three status axes. Strong, intended weakness.
- **SIM 4 — minimal offense:** Staff bolt ~29% Firaga (free floor), Holy ~43% (= basic Fire tier = the
  CEILING, since Holy ignores Zodiac). Identity wall vs Black holds.
- **SIM 5 — revive/Reraise distinct from Chemist:** two doors (flat Faith-proof Phoenix vs Faith-scaled
  big-HP + mass/area revive); Reraise duration flagged the hardest knob.
- **SIM 6 — lane check:** Chemist (flat/reactive) vs Monk (self-sustain) vs White (Faith-scaled
  pre-emptive miracle scale) — three distinct recovery axes.

## Calibration left to Windows / doc 12 (tagged Hypothesis in the registered doc)

- Regen-per-turn **< one warded attacker's chip** (the unkillable-by-one breakpoint).
- Heal magnitudes → ~single-target parity so the heal-tax genuinely cedes the low-Faith corner.
- Reraise duration (real but never mandatory).
- Holy ≤ basic-Fire power (ceiling, never exceeded).
- All numbers are frozen DCL placeholders (G_m=0.58, Faith band [0.70,1.30], MA 14) — real calibration is
  `docs/deep-combat-layer/12`. Magic M2 (AoE/caster-reachability) remains conditional on a Windows AI test
  per the magic-system validation; the White Mage carries **no AoE offense**, so it is not exposed to that
  residual risk.
