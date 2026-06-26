# Knight

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Knight layers — `04`, `06`, `53` (Knight rows), `58`, and `70` — folded into
this single decision doc.

> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Knight is one of the **most engine-coupled**
> jobs: most of its kit is "weapon output ×N + an output-multiplier status." Importantly, the Deep
> Combat Layer provides **natively** much of what Knight hand-rolled here — Taunt, guard depletion,
> facing-as-vulnerability, and Parry are all DCL primitives (see *DCL rebase notes*).

## Identity / compass

Knight is the **armored control anchor**: a durable frontline that holds space, makes armed enemies
less dangerous, opens guarded targets for allies, and forces priority enemies to respect the melee
line.

It should feel good when the enemy has weapons, shields, armor, or a key attacker to contain. It is
deliberately **less dominant** into magic/status-heavy encounters, ranged kiting, enemies with no
meaningful equipment lane, and maps that punish slow armored positioning.

Knight is **not** a generic sword-DPS job, **not** the best crush job, and **not** a universal tank
template.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `melee-physical` |
| Secondary tags | `durable`, `weapon-break` |
| Growth profile | physical |
| Armor class | `plate` |
| Weapon families | knight_sword, sword, fists (swing / crush) |
| Role reason | Durable armed control; pressures equipment and holds ground without being only "the best sword user." |

**Good at:** containing armed/shielded/armored enemies, opening targets for allies, holding a line,
taunting priority threats.
**Bad at / countered by:** magic and status pressure, ranged kiting, equipment-less enemies, maps
that punish slow positioning.

## Shared status vocabulary

A small visible vocabulary the player learns once. *(Magnitudes are v0.2 provisional.)*

| Status | Meaning | Knight source |
|--------|---------|---------------|
| `Guarded` | self takes one eligible direct hit reduced *(×0.70)* | `Guarded Strike` |
| `Exposed` | all incoming damage up *(×1.25)* — **same as Squire's `Exposed`** | `Rend Armor` |
| `Disarmed` | target's outgoing weapon damage down *(×0.70)* | `Rend Weapon` |
| `Guard Broken` | target's shield & weapon guard/evasion halved *(×0.50)* | `Shield Break` |
| `Taunted` | target's next offensive action must target/approach the Knight if legal | `Taunt` |

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Guarded Strike** | Safe engagement: a slightly weaker strike that leaves the Knight guarded against one hit. | One eligible direct hit only, until next Knight turn; non-stacking; not all-damage immunity. *(v0.2: weapon ×0.85; `Guarded` ×0.70.)* |
| **Rend Weapon** | Contain an armed attacker. | Affects weapon/basic-weapon offense only — not spells/items/pure-status. No permanent destruction. *(v0.2: weapon ×0.50; `Disarmed`.)* |
| **Rend Armor** | Party setup window: open a target for ally follow-up. | Visible, non-stacking `Exposed`; same meaning/magnitude as Squire's. *(v0.2: weapon ×0.50.)* |
| **Shield Break** | Strip a turtle's guard. | Only shield & weapon guard/evasion — not class/accessory evade, rear-facing, or immunity. *(v0.2: weapon ×0.50; `Guard Broken` ×0.50.)* |
| **Rend Power / Magick / Speed** | Direct, legible battle stat breaks. | Battle-scoped; stack at most twice (PA/MA cap −4, Speed cap −2). Not statuses; `Rend Speed` is not `Slow`. *(v0.2: weapon ×0.35; PA −2 / MA −2 / Speed −1.)* |
| **Rend MP** | Anti-caster resource pressure. | No HP burst; no Silence replacement. *(v0.2: weapon ×0.35; MP damage min(30, 25% maxMP).)* |
| **Taunt** | Visible one-action target pressure (replaces `Challenge`). | Forces target to target/approach the Knight if legal; graceful fallback if not; **no** Berserk damage boost; respects immunity. *(v0.2: range 3.)* |

**Cut:** `Crushing Blow` — its anti-guard role overlaps `Shield Break`, bloats the kit, and crowds
Monk's protected crush identity. Knight can still use crush-capable equipment if the rules allow.

## Reaction / Support / Movement

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Parry** | Knight's priority reaction: turn aside a frontal weapon hit. | Frontal direct weapon hits only; needs shield/parry-capable weapon; **not** Brave-scaled as the main lever; no magic/status/rear/area coverage. *(v0.2: incoming ×0.60.)* |
| Reaction | **Brace** | (cut / deferred) | Returns only if a later pass needs a narrow hold-ground/anti-displacement identity; never generic damage reduction. |
| Support | **Equip Armor** | Heavy-armor build-shaping. | Unlock only; no free mitigation multiplier; watch so every fragile job doesn't patch into plate. |
| Support | **Equip Shield** | Shield build-shaping. | Unlock only; no free mitigation multiplier; needs evasion/incidence validation with Parry. |
| Support | **Defensive Training** | Explicit Knight-action upgrade table (extends `Guarded`/`Disarmed`/`Exposed`/`Guard Broken` by one action; +10 Taunt reliability). | Never improves ordinary attacks, weapons, spells, items, the stat-break caps, or `Rend MP`. |
| Movement | **Shield March** | Armored positioning tool. | *(v0.2: Move +1 while shielded/heavy posture.)* No terrain/elevation bypass; not free durable-and-mobile. |

## Open items / validation hooks

- Watch: `Exposed` becoming mandatory setup for every party; `Guarded Strike` + `Parry` + shield +
  armor stacking into practical immunity; `Taunt` locking too many enemies or breaking bosses; PA/MA/
  Speed breaks trivializing via repeated stacking; `Equip Armor`/`Equip Shield` as the default
  fragile-job fix.
- `T4 accuracy/evasion`: `Shield Break`, shield layers, `Parry`, `Equip Shield` stacks.
- `F5 real-roster`: active Knight vs magic-heavy, ranged, boss, and low-equipment enemy profiles.
- Control-identity sweep: `Taunt` vs immunity and unreachable-target fallbacks.
- Combo convergence: `Rend Armor` → ally burst (esp. mage follow-up).

## DCL rebase notes

Knight is the clearest case where the DCL **already provides natively** what v0.2 hand-rolled:

- **Exposed** → the DCL's facing/active-defense system *is* this (flank = −2 defense, back = no
  defense roll). `Rend Armor`/`Exposed` re-expresses as a temporary active-defense / DR penalty, not
  a flat ×1.25 incoming multiplier.
- **Guard Broken** → the DCL has **depleting Parry/Block**; `Shield Break` becomes depleting or
  penalizing the target's guard — exactly the DCL flail's *fura-guarda* (−to-be-parried/−to-be-blocked)
  primitive.
- **Guarded / Parry** → the DCL Parry is a core active defense (depleting, ≈ skill/2 + 3). `Parry`
  (reaction) and `Guarded Strike` re-express as DCL active-defense mechanics rather than ×0.60/×0.85.
- **Taunt** → **native in the DCL** (doc 13), with a twist that *helps* Knight: DCL taunt is
  Brave-inverted — high-Brave glass-cannons are vulnerable and the **low-Brave tank is the natural
  taunter**. Knight's `Taunt` fits this directly and sharpens the tank fantasy.
- **Disarmed** → reduce the target's weapon damage; under the DCL, a temporary weapon-skill or wmod
  penalty.
- **Rend Power/Magick/Speed/MP** are **engine-neutral** direct stat/resource breaks and carry over
  as-is.
