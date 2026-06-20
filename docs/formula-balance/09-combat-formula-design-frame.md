# Combat Formula Design Frame

Status: Accepted as design frame
Date: 2026-06-20
Review: Approved by Claude on 2026-06-20 as a frame. The central defense fork and Brave/Faith
policy remain pending decisions, not accepted formula rules.
Depends on:
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/02-variable-palette.md`
- `docs/formula-balance/03-family-taxonomy-and-viability.md`
- `docs/formula-balance/05-formula-proposal-protocol.md`
- `docs/formula-balance/06-damage-spec-v0.md`
- `docs/formula-balance/07-player-feedback-signals.md`
- `docs/formula-balance/08-scenario-set-v0.md`

## Purpose

This document starts actual combat-formula design.

Earlier docs were too conservative about technical implementation proof. Marcelo clarified the
design scope: formula planning should assume the mod can access attacker data and target data
whenever needed. Therefore, this document treats `04-proof-and-baseline-plan.md` as a future
implementation/verification track, not a blocker for design exploration.

This document still does not accept final numeric formulas. It defines the shared formula
architecture that future family-specific formula proposals should instantiate and simulate.

## Scope Correction

Use this rule for formula design from this point onward:

```text
Assume formulas can read the full attacker state, target state, equipment state, status state,
position context, and action metadata.
```

The project still cares about implementation reality later, but design should not be limited to
existing FFT data-only routine routing while we are deciding the best combat model.

Practical consequence:

- `04` remains useful for implementation proof and IVC validation.
- `05`, `06`, and `08` still define simulation discipline.
- Formula proposals may now be written as ideal formulas, not only as Tier-1 data rewrites.
- Implementation dependency should be recorded honestly, but it is not a veto during design.
- Tier labels remain required as cost/proof labels for later implementation planning.

## External FFT Reference Baseline

The formula design should start from real FFT behavior, then improve it.

External references checked:

- AeroStar's Battle Mechanics Guide on GameFAQs:
  https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
- FFHacktics formula catalog:
  https://ffhacktics.com/wiki/Formulas
- Final Fantasy Wiki weapon and armor lists:
  https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_weapons
  https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_armor
- WotL equipment guide on GameFAQs:
  https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070
- Game8 IVC weapon and armor guides:
  https://game8.co/games/Final-Fantasy-Tactics/archives/555215
  https://game8.co/games/Final-Fantasy-Tactics/archives/541913

Useful facts from those references:

- FFT weapon formulas already vary by family: `PA * WP`, `floor((PA + Speed) / 2) * WP`,
  `floor(PA * Brave / 100) * WP`, `MA * WP`, `WP * WP`, random `1..PA * WP`, and
  `floor((PA + MA) / 2) * WP`.
- Faith magic commonly uses caster Faith, target Faith, caster MA, and a spell constant.
- Vanilla FFT has no general point-defense stat. Armor primarily adds HP/MP, while mitigation
  comes from Protect/Shell, evasion, elements, Zodiac, status, and equipment effects.
- Equipment is not just weapon power. It includes block/evasion, dual-wield/doublehand flags,
  range, element, status procs, auto-status, HP/MP, PA/MA/Speed/Move/Jump bonuses, elemental
  absorb/halve/weakness, and status immunities.
- Player-facing meta signals point to Knight's Swords and Guns as high-performing families, with
  several other families perceived as outclassed, unreliable, or job-locked.

## Design Principles For New Formulas

### 1. Preserve FFT Inputs

New formulas should still read like FFT:

```text
PA, MA, Speed, Brave, Faith, WP, CT, MP, range, height, element, status, equipment, job identity
```

New derived values are allowed, but they should be built from FFT-native concepts.

### 2. Use GURPS As Inspiration, Not Law

The useful borrowed idea is not a table. It is the separation of:

- how the blow is delivered;
- how much force it creates;
- how armor or resistance reduces it;
- what damage type does after penetration.

Generic Chronicle should use thrust/swing-like ideas only where they make FFT weapon families
more distinct and readable.

### 3. Separate Gross Pressure From Mitigation

Classic FFT often goes straight from attacker expression to damage. Generic Chronicle should
generally use two stages:

```text
gross pressure = attacker's action output
mitigation = target's relevant defense after penetration
final damage = gross pressure after mitigation and type modifiers
```

This gives armor, target build, and weapon shape a real role.

### 4. Do Not Make Every Weapon Solve Every Problem

Each family should have a preferred target profile and an awkward target profile.

Examples:

- thrust/pierce families should like armored targets more than light swarms;
- swing/cut families should like exposed targets more than heavy armor;
- crushing families should punish armor or shields but carry reliability, speed, or accuracy
  costs;
- magic should bypass some physical defense but pay through CT, MP, Faith, Shell, element, or
  status interaction.

### 5. Buff Floors Before Cutting Ceilings

The default design posture is to raise weak families into relevance before nerfing loved strong
tools.

Use ceiling reductions only when buffing the ecosystem would break the FFT-like damage band or
make encounter pacing collapse.

## Proposed Formula Architecture

Every damaging action should be modeled as an `Action Package`:

```text
Action Package:
- source: weapon family / spell family / skill family
- delivery mode: thrust / swing / crush / shot / arc / channel / technique
- damage type: cut / pierce / crush / missile / magic / spirit / drain / pure
- attacker expression: which attacker stats create pressure
- power expression: how WP, spell power, or skill rank contributes
- target defense channel: armor / guard / resistance / faith / evasion / status immunity
- penetration expression: how much defense the action bypasses
- reliability expression: hit rate, evasion behavior, variance, CT, MP, or cooldown
- after-effect: element, status, drain, knockback, break, self-risk, or none
- implementation label: data-only / hook-required / unknown
```

The candidate damage pipeline:

```text
attacker_pressure = f(attacker stats, action power, delivery mode)
target_mitigation = mitigation_model(target, action, attacker)
post_mitigation = max(chip_floor, attacker_pressure - target_mitigation)
typed_damage = post_mitigation * type_response(target, damage type, element, status)
final_damage = floor(typed_damage after FFT-style modifiers)
```

This is the shared shape if the project accepts a mitigation model. If the project chooses to
stay closer to vanilla FFT with no new defense axis, `target_mitigation` becomes zero or limited
to existing FFT concepts such as Protect/Shell, evasion, element, Zodiac, and status.

The first architectural decision is therefore not a number. It is whether Generic Chronicle
introduces a target mitigation axis at all.

## Central Fork - Target Defense And Mitigation

Vanilla FFT does not have a general defense stat that reduces damage point by point. This is the
central design fork.

### Option A - No New Defense Axis

Keep the combat model close to FFT:

```text
final_damage = attacker_pressure * existing FFT responses
```

Existing responses include evasion, Protect/Shell, element, Zodiac, status, CT/MP, range, and
equipment effects.

Pros:

- closest to FFT feel;
- easier for players to understand because armor remains HP/status/evasion/stat identity;
- lower risk of making the game feel like a different RPG;
- weapon-family differentiation focuses on scaling, range, reliability, status, element, and
  job/equipment access.

Cons:

- thrust/swing/penetration cannot literally mean armor penetration;
- armor remains mostly survivability and utility, not damage texture;
- heavily armored targets do not create a distinct formula problem unless HP, Protect, evasion,
  or status does that work.

### Option B - Light Readable DR

Introduce a small `ArmorRating` or `GuardRating` derived from target equipment and state:

```text
target_mitigation = max(0, ArmorRating - Penetration)
final_damage = max(chip_floor, attacker_pressure - target_mitigation)
```

Pros:

- makes armor and shields more meaningful;
- gives thrust/pierce/crush/penetration a real mechanical role;
- creates matchup space where different families like different targets;
- directly attacks the "all weapons are just damage numbers" problem.

Cons:

- larger departure from vanilla FFT;
- requires strong UI/legibility so players understand why damage changed;
- risks making low-hit-count or low-gross families feel bad if DR is too high;
- needs careful simulation to avoid slowing combat or making armor mandatory.

### Option C - Type Resistance Without Point DR

Avoid point DR, but add type-response tables:

```text
final_damage = attacker_pressure * type_response(target_equipment, damage_type)
```

Examples:

- heavy armor resists `cut` but is less resistant to `pierce` or `crush`;
- robes resist `magic` or `spirit`;
- shields improve `Guard` against missile/cut but not all magic;
- accessories add narrow resistance or vulnerability.

Pros:

- keeps numbers closer to FFT percent-style modifiers;
- makes damage types matter without full subtractive armor math;
- easier to message as "armor matchup" rather than hidden point subtraction.

Cons:

- can become opaque if too many hidden type rules exist;
- penetration becomes less literal;
- percent stacking can create large damage swings if combined with elements, Zodiac, Shell, and
  support effects.

### Decision Needed

Claude and GPT must explicitly choose one of:

```text
A. no new defense axis;
B. light readable DR;
C. type resistance without point DR;
D. hybrid model, with exact boundaries.
```

No family-specific formula should be accepted until this fork is resolved, because it changes
what thrust, swing, crush, armor, and penetration mean.

## Derived Design Values

These derived values are allowed in design proposals.

| Derived value | Built from | Purpose |
| --- | --- | --- |
| `BodyPower` | PA, Brave, status, support effects | physical force and martial commitment |
| `Technique` | Speed, PA, Brave, job/skill context | precision, timing, and weapon handling |
| `SpellPower` | MA, Faith, spell rank, equipment boosts | magical output |
| `Guard` | shield/weapon evasion, class evasion, facing, Defend/status | active avoidance and parry pressure |
| `ArmorRating` | body/head armor class, shield class, armor HP tier, equipment type | optional physical mitigation if Option B or D is chosen |
| `SpiritResistance` | Faith, Shell, status, robe/accessory properties, element | magical and spiritual resistance |
| `Penetration` | weapon family, delivery mode, attacker stats, skill rank | how much mitigation is bypassed if mitigation exists |
| `Reliability` | accuracy, CT, MP, variance, range limits, line/arc constraints | cost paid for stronger output |

These are design abstractions. Later implementation can map them to data, hooks, or explicit
tables.

## Delivery Modes

| Mode | Design meaning | Typical families | Formula tendency |
| --- | --- | --- | --- |
| `thrust` | focused force through a narrow point | knives, polearms, some swords | lower gross, higher penetration, good accuracy |
| `swing` | broad cutting or slashing momentum | swords, katanas, axes | higher gross, lower penetration, target-type sensitive |
| `crush` | impact that defeats guard and armor by force | flails, axes, fists, bags | high mitigation pressure, lower reliability or speed |
| `shot` | direct projectile force | guns, crossbows | high reliability/range, limited stat scaling or ammo-like constraints |
| `arc` | arcing projectile or terrain-dependent attack | bows, thrown attacks | range/height identity, more positional variance |
| `channel` | weapon as magical conduit | rods, staves, books, instruments | MA/Faith/status/element identity |
| `technique` | skill expression beyond the basic strike | katanas, ninja blades, swordskills | stat blend plus status/crit/after-effect risk |

## Damage Types

| Type | Mitigation channel | Design role |
| --- | --- | --- |
| `cut` | armor plus Guard | strong vs light targets, weaker vs heavy armor |
| `pierce` | armor after penetration plus facing/Guard | reliable armor interaction, lower raw damage |
| `crush` | armor and shield stress | good vs armored/guarding targets, less precise |
| `missile` | range, evasion, shield/cover, armor | positioning and reliability family |
| `magic` | SpiritResistance, Faith, Shell, element | caster identity and target susceptibility |
| `spirit` | Faith/Brave/status resistance | non-elemental mystical effects and status-adjacent damage |
| `drain` | special resistance and HP/MP state | sustain identity with strict caps |
| `pure` | minimal mitigation | rare, expensive, CT/MP/status-gated |

## First Family Identity Pass

This table is not final numeric design. It defines the initial identity target for future
family-specific formula proposals.

| Family | Proposed mode/type | Primary stats | Defense interaction | Identity target |
| --- | --- | --- | --- | --- |
| Sword | swing/cut | PA + WP | normal armor, normal Guard | stable baseline, accessible, never universal best |
| Knight Sword | swing/cut + technique | PA + Brave + WP | normal armor, support-risk cap needed | premium stable damage with clear ceiling controls |
| Knife | thrust/pierce | Speed + PA + WP | higher penetration, high accuracy | low gross, reliable precision, good vs armor for its size |
| Ninja Blade | technique/cut | Speed + PA + WP | low mitigation, multi-hit risk | fast burst with strict support/dual-wield checks |
| Katana | technique/cut/spirit | PA + Brave + MA accent | moderate armor, status/crit flavor | disciplined high-skill weapon, not just worse knight sword |
| Polearm | thrust/pierce | PA + WP + reach | high penetration, range 2 | armored-target specialist with positional reach |
| Bow | arc/missile | PA + Speed + WP | evasion, range, height | positional ranged damage with map identity |
| Crossbow | shot/pierce | PA + WP | moderate penetration, direct line | reliable mid-range armor pressure |
| Gun | shot/missile | WP + user handling | low stat scaling, high reliability | ranged consistency without universal scaling dominance |
| Axe | swing/crush | PA + WP | high armor pressure | heavy volatile damage made trustworthy, not random trash |
| Flail | crush | PA + Speed or PA + WP | guard/shield pressure | anti-guard weapon with controlled variance |
| Bag | crush/chaos | PA/MA/Speed blend | inconsistent but bounded | oddball weapon with bounded high-risk payoff |
| Fist | crush/technique | PA + Brave + Speed | low equipment reliance | martial baseline with support risk controlled |
| Rod | channel/magic | MA + spell context | SpiritResistance, element | caster amplifier, not physical stick |
| Staff | channel/spirit | MA + Faith/support | SpiritResistance, healing/status | support/healing conduit with defensive utility |
| Pole | channel/pierce | MA + WP or MA + target state | mixed armor/spirit | magical reach weapon distinct from staff |
| Book | channel/spirit | MA + PA blend | SpiritResistance/status | knowledge/status hybrid, range utility |
| Instrument | channel/spirit | MA + Brave/Faith or party state | SpiritResistance | performance/support damage, job-lock payoff |
| Cloth | technique/cut | Speed + PA/MA blend | evasion/Guard sensitive | dancer weapon with mobility/precision identity |

## Dominance Engines Must Be Designed In

Family formulas must be designed against dominant FFT engines from the start, not patched after
simulation failures.

| Engine | Design risk | Frame response |
| --- | --- | --- |
| Two Hands / Doublehand | doubles or heavily amplifies already high WP routes | heavy and premium families need ceiling checks before accepting buffs |
| Two Swords / Dual Wield | doubles on-hit and high-efficiency weapons | fast and proc/status families need per-hit scaling or proc controls |
| Brave stacking | boosts Brave-scaling damage and reactions with little downside | Brave-based offense needs either saturation, risk, or opportunity cost |
| Martial Arts / Brawler | turns unarmed and PA scaling into low-equipment dominance | fist and crush formulas need support-aware baselines |
| Attack Boost | raises many physical routes at once | simulations must include no-support and boosted cases |
| Faith extremes | makes magic high-risk/high-reward or near irrelevant | magic formulas must report normal, high, and low Faith cases |

This frame prefers formulas that stay distinct under these engines. If a family only works when
one support ability is banned or ignored, the formula is not stable enough.

## Brave/Faith Asymmetry

FFT's Brave and Faith are not symmetric.

- Faith increases magical output and magical vulnerability.
- Brave improves many physical/reaction routes without an equally obvious defensive downside.

This contributes to physical damage feeling safer than magic. Generic Chronicle must decide what
to do with that asymmetry.

Options:

### Option F1 - Preserve Asymmetry

Keep Brave mostly upside and Faith double-edged.

Pros: closest to FFT feel.

Cons: physical remains structurally safer unless formulas compensate elsewhere.

### Option F2 - Soften Faith Downside

Reduce how strongly target Faith increases incoming magic, or make caster Faith matter more than
target Faith.

Pros: makes magic less punishing to build around.

Cons: moves away from classic FFT and may make magic too easy to optimize.

### Option F3 - Add Brave Risk Or Saturation

Keep Brave as identity, but make high-Brave offense saturate or carry a risk/opportunity cost.

Examples:

```text
Brave contribution has diminishing returns above a threshold.
High-Brave physical routes lose some Guard or become more vulnerable to spirit/status effects.
Brave scaling affects reliability rather than only raw damage.
```

Pros: makes Brave-based physical tools less free.

Cons: can feel punitive if not clearly messaged.

### Decision Needed

This frame does not choose F1/F2/F3 yet. Family formulas that use Brave or Faith must name which
Brave/Faith policy they assume.

## Magic Formula Direction

Magic should not simply be `MA * constant` with Faith as a large hidden swing.

Proposed architecture:

```text
spell_pressure = spell_rank_power + SpellPower(caster) * spell_scaling
spirit_mitigation = mitigation_or_response(target, caster, spell family)
post_mitigation = max(spell_floor, spell_pressure - spirit_mitigation)
final = post_mitigation * element_response * faith_response * shell_response
```

Design goals:

- Faith remains meaningful but not the only magic identity.
- CT and MP remain visible costs.
- Elemental equipment and Shell matter.
- Offensive magic should compete with physical damage over a tactical window, not only per hit.
- Support/healing magic can use different pressure and mitigation curves than direct damage.

## Armor And Equipment Direction

FFT armor traditionally adds HP/MP, status, element, evasion, and stat bonuses rather than
point-by-point damage reduction. Under Marcelo's corrected design assumption, Generic Chronicle
may introduce derived mitigation from equipment, but only after the central fork above is
approved.

Potential split if the project chooses Option B or D:

```text
ArmorRating: reduces cut/pierce/crush/missile after penetration
Guard: avoids or reduces attacks through evasion, shield, weapon block, facing, and Defend
SpiritResistance: reduces magic/spirit through Faith, Shell, robe/accessory/status/element
Vitality: HP/MP pool and recovery economy
```

If the project chooses Option C, armor and equipment should use the same categories, but express
them as coarse type responses instead of point reduction.

If accepted, this should make armor useful without turning FFT into a tabletop simulator:

- heavy armor gives stable physical mitigation and HP;
- clothing gives mobility/stat identity but lower mitigation;
- robes give spirit/magic resistance and MP/MA identity;
- shields improve Guard and sometimes elemental/status defense;
- accessories specialize builds through stats, status, movement, or elements.

If the project chooses Option A, equipment identity should instead rely on existing FFT-like
levers: HP/MP, PA/MA/Speed, Move/Jump, evasion, element, status, auto-status, and support access.

## Simulation Requirements For This Architecture

Every family-specific formula proposal must still follow `05` and `08`.

Minimum first-pass simulation set:

- one light target;
- one durable armored target;
- one evasive target if accuracy matters;
- one magic-relevant target if Faith/Shell/element matters;
- no-support, normal-support, and stress-support variants;
- comparison against WotL/FFT reference formulas where available;
- player-signal check from `07`.

Because the design now assumes full attacker/target access, missing IVC weapon baseline does not
block formula ideation. It only limits claims about exact IVC parity and implementation.

## Open Design Questions For Claude

1. Which central defense fork should the project choose: A, B, C, or D?
2. Should `Penetration` be mostly weapon-family fixed, attacker-stat derived, or skill-rank
   derived if mitigation exists?
3. Should thrust/swing/crush be visible player-facing labels, or only internal formula modes?
4. How hard should the default be toward buffing weak-family floors before lowering dominant
   ceilings?
5. Which Brave/Faith policy should the project choose: F1, F2, F3, or a hybrid?
6. Should magic use subtractive `SpiritResistance`, percent Faith/Shell response, or both?

## What This Changes

This document changes the workflow interpretation:

- design no longer waits for `work/baseline_weapons.csv` before proposing formula architecture;
- `04` becomes validation/implementation proof, not a design lock;
- future docs should start proposing and simulating actual family formulas;
- technical dependency is recorded but does not veto design exploration.
- target defense/mitigation becomes the first explicit design fork before family formulas.

This document does not change the final acceptance bar:

- Claude approval is still required;
- formulas still require simulation;
- final implementation still needs proof;
- FFT feel and family viability remain the primary goals.
