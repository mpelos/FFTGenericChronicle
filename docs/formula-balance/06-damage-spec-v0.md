# Damage Calculation Spec V0

Status: Accepted
Spec version: `damage-spec-v0`
Date: 2026-06-20
Depends on:
- `docs/formula-balance/00-envelope.md`
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/02-variable-palette.md`
- `docs/formula-balance/03-family-taxonomy-and-viability.md`
- `docs/formula-balance/04-proof-and-baseline-plan.md`
- `docs/formula-balance/05-formula-proposal-protocol.md`
- `docs/modding/02-formula-id-catalog.md`
- `docs/modding/03-battle-data-map.md`
Review: Approved by Claude on 2026-06-20 with no required changes.

## Purpose

This document is the shared calculation spec for provisional formula simulations.

It does not accept any new damage formula. It defines the assumed arithmetic, modifier order,
rounding model, variance model, and accuracy model that formula proposals must reference until
the Windows proof session replaces assumptions with verified IVC behavior.

Use this spec only with an explicit status label:

```text
Damage spec: damage-spec-v0
Status: assumed / WotL-fallback, pending verified-baseline re-sim
```

After the proof session in `04-proof-and-baseline-plan.md`, this file should be superseded by
`damage-spec-v1` or amended with verified labels.

## Source Labels

Every rule in this spec uses one of these labels:

- `verified-IVC`: confirmed from local IVC data, committed dumps, or in-game proof.
- `WotL-fallback`: inherited from classic FFT/WotL references and local research, not yet proven
  in IVC.
- `assumed`: chosen as a common simulation assumption so GPT and Claude simulate the same model.
- `to-verify-in-04`: should be checked during the Windows proof session if practical.

Unless a row says `verified-IVC`, treat it as provisional.

## Canonical Routine Catalog

The canonical Formula id catalog is `docs/modding/02-formula-id-catalog.md`.

This spec inherits that catalog rather than copying the entire table. Formula proposals should
cite both this spec version and the exact routine id or weapon routine being simulated.

Known local facts:

| Fact | Label | Notes |
| --- | --- | --- |
| `OverrideAbilityActionData` exposes `Formula`, `X`, and `Y` override columns | verified-IVC | Behavior still needs damage proof. |
| `ItemWeaponData.xml` exposes weapon `Formula` and `Power` fields | verified-IVC | Values and in-game routing still need Windows baseline/proof. |
| Ability base Formula/X/Y values are hardcoded in `FFT_enhanced.exe` | verified-IVC | Use WotL fallback until extracted or proven. |
| Weapon routine meanings below match IVC exactly | WotL-fallback | Must be sampled in game before final formulas. |

### Weapon Attack Routines

These are the v0 weapon routine meanings for simulation. They are WotL fallback behavior unless
the Windows proof session verifies the specific family/routine.

| Weapon family | V0 routine | Label | Verification |
| --- | --- | --- | --- |
| Bare hands | `floor(PA * Brave / 100) * PA` | WotL-fallback | to-verify-in-04 if used as baseline |
| Knife / ninja blade / longbow | `floor((PA + Speed) / 2) * WP` | WotL-fallback | to-verify-in-04 |
| Sword / rod / polearm / crossbow | `PA * WP` | WotL-fallback | to-verify-in-04 |
| Knight sword / katana | `floor(PA * Brave / 100) * WP` | WotL-fallback | to-verify-in-04 |
| Staff / pole | `MA * WP` | WotL-fallback | to-verify-in-04 |
| Axe / flail / bag | `RdmInt(1, PA) * WP` | WotL-fallback | to-verify-in-04 |
| Physical gun | `WP * WP` | WotL-fallback | to-verify-in-04 |
| Instrument / book / cloth | `floor((PA + MA) / 2) * WP` | WotL-fallback | to-verify-in-04 |

### High-Value Ability Routines

These are common action routines likely to appear in early simulations. Use
`docs/modding/02-formula-id-catalog.md` for the full list.

| Formula id | V0 routine summary | Label | Verification |
| --- | --- | --- | --- |
| `0x01` | weapon damage | WotL-fallback | to-verify-in-04 |
| `0x02` | weapon damage with option/proc behavior | WotL-fallback | to-verify-in-04 |
| `0x03` | `WP * WP` style weapon damage | WotL-fallback | to-verify-in-04 |
| `0x08` | Faith-scaled `MA * Y` damage | WotL-fallback | to-verify-in-04 |
| `0x0A` | Faith-scaled offensive status hit | WotL-fallback | later proof |
| `0x0B` | Faith-scaled buff hit | WotL-fallback | later proof |
| `0x0C` | Faith-scaled `MA * Y` healing | WotL-fallback | later proof |
| `0x20` | non-Faith `MA * Y` damage | WotL-fallback | to-verify-in-04 if used |
| `0x24` | `floor((PA + Y) / 2) * MA` style damage | WotL-fallback | later proof |
| `0x2D` | `PA * (WP + Y)` plus status | WotL-fallback | to-verify-in-04 if used |
| `0x31` | `floor((PA + Y) / 2) * PA` | WotL-fallback | later proof |
| `0x37` | `RdmInt(1, Y) * PA` | WotL-fallback | later proof |
| `0x42` | `PA * Y` with caster recoil | WotL-fallback | later proof |
| `0x43` | caster missing HP damage | WotL-fallback | later proof |
| `0x4E` | non-Faith `MA * Y` damage variant | WotL-fallback | later proof |
| `0x63` | `Speed * WP` throw damage | WotL-fallback | later proof |
| `0x64` | Jump formula | WotL-fallback | later proof |

## Damage Pipeline V0

V0 assumes this order for actions that produce damage or healing:

1. Select the action routine.
2. Gather effective inputs.
3. Compute the routine base value.
4. Apply the main power term such as `WP` or `Y` if the routine has one.
5. Apply support and status damage multipliers.
6. Apply critical hit modifier when relevant.
7. Apply elemental strengthen, weakness, resistance, nullify, or absorb.
8. Apply target mitigation such as Protect or Shell.
9. Apply Zodiac compatibility modifier.
10. Propagate formula-specific random terms or random-hit aggregation.
11. Clamp and floor the final visible result.

This order is `assumed` and `to-verify-in-04`. It exists so simulations are comparable before
the exact IVC order is proven.

### Step 1 - Select Action Routine

| Rule | Label | Verification |
| --- | --- | --- |
| Ability simulations select one hardcoded Formula id plus `X`/`Y` when used. | verified-IVC for fields, WotL-fallback for behavior | to-verify-in-04 |
| Weapon simulations select one weapon routine through `ItemWeaponData.Formula`. | verified-IVC for field, assumed for routing | to-verify-in-04 |
| Slot-hardcoded side behavior is not assumed portable across unrelated actions. | WotL-fallback | proof per routine |

### Step 2 - Gather Effective Inputs

Use effective battle stats after job, equipment, and status effects:

```text
PA, MA, Speed, Brave, Faith, Level, current HP/MP, max HP/MP, WP, X, Y
```

Labels:

| Input class | Label | Verification |
| --- | --- | --- |
| Local data exposes job, item, ability, and weapon values | verified-IVC | already mapped locally |
| Runtime effective battle stats have known public offsets | source-derived | optional Tier-2 readiness check |
| Exact status-modified effective stat order | WotL-fallback / assumed | to-verify-in-04 when relevant |

### Step 3 - Compute Routine Base Value

Use the routine catalog above. Integer division floors immediately.

Examples:

```text
PA * WP routine:
base = PA
power_term = WP

knife-style routine:
base = floor((PA + Speed) / 2)
power_term = WP

Faith magic routine:
base = MA
power_term = Y
faith_scale = caster Faith and target Faith modifier
```

Label: WotL-fallback, to-verify-in-04.

### Step 4 - Apply Main Power Term

When a routine has a separate power term, V0 multiplies the base value by that term before
global modifiers:

```text
raw = base * power_term
```

For formulas where the catalog already defines the whole expression, use the catalog expression
as `raw`.

Label: WotL-fallback, to-verify-in-04.

### Step 5 - Apply Support And Status Damage Multipliers

V0 models support and status damage multipliers after the raw routine value and before critical,
element, mitigation, Zodiac, and final distribution reporting.

Examples include:

```text
Attack Boost / Attack Up
Martial Arts / Brawler
Two Hands
Berserk
Defense Boost or similar defensive modifiers
```

Do not apply a support multiplier unless the tested routine is known or assumed to receive that
modifier. If uncertain, run two simulations:

```text
support excluded
support included as WotL-fallback
```

Label: WotL-fallback / assumed, to-verify-in-04 for any support-sensitive proposal.

### Step 6 - Apply Critical Hit Modifier

V0 treats critical hits as a separate multiplier after support/status multipliers.

Formula simulations should normally report non-critical damage. If critical behavior is part of
the proposal, report it separately.

Label: WotL-fallback / assumed, to-verify-in-04.

### Step 7 - Apply Elemental Modifiers

V0 applies elemental modifiers after support/critical and before Protect/Shell/Zodiac.

Use separate rows for at least:

```text
neutral
strengthened attacker element
target weak
target halves
target nullifies
target absorbs
```

Do not collapse absorb/nullify/halve into a generic damage multiplier in design verdicts, because
they create different tactical outcomes.

Label: WotL-fallback / assumed order, to-verify-in-04.

### Step 8 - Apply Target Mitigation

V0 applies Protect to physical damage and Shell to magical damage after element and before
Zodiac.

The exact ratio and classification of hybrid routines are WotL fallback until proven.

Label: WotL-fallback / assumed order, to-verify-in-04.

### Step 9 - Apply Zodiac Compatibility

V0 applies Zodiac after mitigation. If the routine has random terms, simulations report the
post-Zodiac result as a distribution.

Default simulations should use neutral compatibility unless the proposal depends on Zodiac.
If Zodiac matters, report neutral, good, bad, best, and worst cases separately.

Label: WotL-fallback / assumed order, to-verify-in-04.

### Step 10 - Propagate Variance Or Random Aggregation

V0 assumes no hidden global damage variance:

```text
global variance = 0%
```

Randomness is modeled only when the selected routine explicitly contains random terms. If the
routine defines randomness inside the base expression, such as `RdmInt(1, PA) * WP`, that random
term belongs to the routine base in Step 3. Step 10 reports the resulting distribution after the
full pipeline rather than adding a second random multiplier.

Examples:

```text
Axe / flail / bag: RdmInt(1, PA) * WP
Formula 0x37: RdmInt(1, Y) * PA
Multi-hit routines: report per-hit formula, hit count assumption, min, max, mode, and EV
```

Repeated identical attacks in the proof session should verify whether IVC has any hidden
variance not captured here.

Label: assumed for no hidden global variance; WotL-fallback for routine-specific random terms;
to-verify-in-04.

### Step 11 - Clamp And Floor Final Result

V0 floors after every integer division and after every multiplier represented as a fraction.

V0 clamps final damage/healing to the engine-visible 16-bit damage range:

```text
final = clamp(floor(value), 0, 65535)
```

The local data map identifies damage as a 16-bit value, but exact signedness, overflow handling,
and intermediate clamp behavior still need proof before any formula intentionally approaches the
limit.

Label: mixed; 16-bit storage is source-derived/locally mapped, exact clamp behavior is assumed.

## Rounding And Truncation Rules V0

Default rounding policy:

```text
integer division: floor immediately
fractional multiplier: multiply, divide, floor immediately
final result: floor before clamp
```

Formula proposals must state if they rely on a different rounding rule.

Open proof needs:

- whether IVC floors after every classic intermediate step or only at final write;
- whether float-backed remaster multipliers produce edge-case differences from WotL;
- whether negative or overflow values can occur in reachable formula paths.

Label: WotL-fallback / assumed, to-verify-in-04.

## Faith Scaling V0

Faith-scaled magic routines use a caster Faith and target Faith multiplier after the base magic
expression.

V0 model:

```text
raw = MA * Y
faith_scaled = floor(raw * casterFaith * targetFaith / 10000)
```

Some routines invert, bypass, or reinterpret Faith. Use the routine catalog and document the
assumption per formula.

Label: WotL-fallback, to-verify-in-04 for any magic proposal.

## Accuracy Pipeline V0

Accuracy is simulated separately from damage. A proposal must not hide bad damage balance behind
unreported hit-rate assumptions.

V0 physical accuracy order:

1. Select physical hit routine.
2. Establish base hit chance or auto-hit flag from the action.
3. Apply facing and evasion stack.
4. Apply direct, unevadable, or evasion-bypass flags.
5. Apply status modifiers such as Blind, Defending, Sleep, Stop, or similar if relevant.
6. Clamp to legal hit-rate range.

V0 magic/status accuracy order:

1. Select magic/status hit routine.
2. Establish base hit chance from formula id and `X`/`Y` when known.
3. Apply MA or formula-specific caster stat if the routine uses one.
4. Apply caster Faith and target Faith if the routine is Faith-affected.
5. Apply immunity, reflectability, Shell/protection, or status flags when relevant.
6. Clamp to legal hit-rate range.

V0 evasion handling is assumed, not verified:

```text
class, shield, weapon, and accessory evasion sources, filtered by facing and flags
```

The line above is an input list, not a formula. Do not treat evasion as additive unless a later
proof confirms that exact stack.

Any formula proposal that depends on accuracy must report:

```text
damage on hit
expected damage after hit rate
hit-rate assumptions
which evasion/accuracy facts are verified vs assumed
```

Labels:

| Accuracy rule | Label | Verification |
| --- | --- | --- |
| Ability and item data expose evasion/flags | verified-IVC for fields | behavior needs playtest |
| Physical evasion ordering | WotL-fallback / assumed | later proof |
| Magic/status Faith hit ordering | WotL-fallback | later proof |
| Direct/unevadable flag behavior | WotL-fallback | proof when used as family identity |

## Minimum Simulation Header

Every simulation using this spec should begin with:

```text
Damage spec: damage-spec-v0
Scenario set version:
Formula proposal:
Routine id or weapon routine:
Verified facts:
WotL fallback facts:
Assumptions:
Open proof needs:
```

## Suggested Proof Additions For The Windows Session

The `04` proof plan already requires controlled before/after damage tests. While running those
tests, capture these extra probes if practical:

1. Repeat identical non-random attacks several times to confirm whether hidden global variance
   exists.
2. Test a Protect or Shell case where rounding would differ if mitigation happens before or
   after another modifier.
3. Test one Faith-scaled spell with known caster and target Faith values.
4. Test one elemental strengthen or weakness case with a neutral control.
5. If an accuracy-sensitive proposal is likely, record one evadable and one Direct/unevadable
   sample with the same attacker/target/facing.

These probes are optional for the first data-pipeline proof, but they are the fastest path from
`damage-spec-v0` assumptions to a verified `damage-spec-v1`.

## What This Spec Blocks

Because this spec is accepted:

- formula simulations must cite this spec or a later accepted spec version;
- harness output is not consensus evidence unless it follows the dual-review model in `05`;
- proposals remain provisional unless their verified, WotL-fallback, and assumed facts are
  labeled explicitly.

Until the Windows proof session verifies enough of this spec:

- final formula acceptance remains blocked;
- simulations against this spec remain provisional;
- any IVC-specific conclusion must be labeled pending verified-baseline re-simulation.
