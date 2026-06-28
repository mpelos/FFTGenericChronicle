# DCL Damage Model — vertical slice, offline proof

Date: 2026-06-26
Status: offline 11/11 PASS; live dry-run deployed, awaiting in-game confirmation.

First implementation slice of the Deep Combat Layer (the chosen first build, see
`work/1782514832-dcl-implementation-readiness-gap-analysis.md` and [[dcl-implementation-target]]).
Implements the DCL deterministic subtractive-DR damage pipeline (docs/deep-combat-layer/02,03) in the
runtime formula DSL and proves it in the engine's real formula path via the offline simulator.

## Files
- Profile: `work/battle-runtime-settings.dcl-damage-slice.json` (also deployed as the live
  dry-run `battle-runtime-settings.json`).
- Scenarios: `work/dcl-damage-slice-sim-scenarios.json` (11 scenarios, expectations locked).
- Run: `dotnet run --project codemod/fftivc.generic.chronicle.codemod.settingssimulate/... -c Release --
  work/battle-runtime-settings.dcl-damage-slice.json work/dcl-damage-slice-sim-scenarios.json`
  → all 11 `expect=pass`, exit 0.

## The pipeline (model "d", docs 02/03)
`injury = max(pen_floor, max(0, base(basePA) + wmod − DR_type)) × wound_mult × trait(Brave)`
- `base(basePA)`: GURPS-shaped table, re-ranged `ST = a.rawPa + 4`, type-split (`gurpsThr` for thrust,
  `gurpsSw` otherwise). Read from `a.rawPa` (+0x38, base PA — excludes the weapon's PA bonus, per doc 02).
- `wmod`: `aslot.weapon.weaponPower` (catalog).
- `DR_type`: subtractive `matrixClamp(drMatrix, armorClassIndex, typeIndex)`.
- `wound_mult`: cut 3/2, thrust 2/1, crush 1/1, missile 1/1.
- `trait`: Brave permille `760 + a.brave*590/100` (≈0.76–1.35), physical only.
- Damage type from attacker weapon family via `ActionSignalRules` (cut/thrust/crush/missile).
- Armor class from target body slot via `FormulaPreResponseVariables` (plate≥60 / mail 40–59 / robe / leather).

## Branching proven (the core project thesis: atk+tgt attributes AND equipment of both sides)
| Axis | Evidence (finalDamage) |
|---|---|
| Attacker base-PA | A=29 → B=46 (basePA 10→20) |
| Attacker Brave | A=29 → C=20 (Brave 70→10) |
| Weapon power | A=29 → K=45 (Longsword wp5 → Runeblade wp14) |
| Type × armor | vs plate: sword D=17 < axe E=23 ≈ spear G=23; vs leather: sword A=29 > axe F=24 |
| Armor class | leather A=29 / mail J=24 / plate D=17 (same Longsword); robe H=28 (dagger ×2, 0 DR) |

The matchup thesis holds: crush/thrust answer plate; cut beats light; right tool ≈ +35%-ish, wrong tool ≈ half.

## PROVISIONAL (calibration deferred per Marcelo — "deixa a calibragem fina para depois")
GURPS table is a linear placeholder (sw=ST, thr=floor(0.7·ST)); DR Plate 9/8/3/8, Mail 5, Leather 2,
Robe 0; pen_floor 20%; G folded to 1; wmod = raw vanilla weaponPower (DCL wants it inflated). These are
structure-correct, value-placeholder. Real GURPS table, G, pen_floor, PA→ST offset, DR-scaling curve,
and inflated per-weapon wmod are the `12-open-questions.md` calibration bucket.

## Known slice limitations (by design, deferred)
- Crossbow/gun use PA base here; they are skill-primary (need weapon-skill / Job Level — BLOCKED).
- Missile uses the sw base + no armor divisor yet (bow-vs-plate has no penetration answer in the slice).
- Reconciler (post-hit) delivery; pre-clamp same-hit delivery (better, KO-correct) is a follow-up.
- Brave multiplier is the only trait; no facing/reach/skill/crit yet (those are later DCL systems).

## Live test (deployed, dry-run / observe-only)
`battle-runtime-settings.dcl-damage-slice.json` is deployed with `DryRunRewrites=true` → it computes and
logs the DCL result via `[RUNTIME ...]` + `trace.*` but does NOT change HP. Goal: confirm live context
resolution (attacker-by-CT, both-sides equipment read, armor-class detection) and sane computed numbers.
After it passes, flip `DryRunRewrites=false` (+ data-layer neuter active) for the real-apply test.

Next: live dry-run confirm → real-apply test → then calibrate, or move to the hit/defense contest.
