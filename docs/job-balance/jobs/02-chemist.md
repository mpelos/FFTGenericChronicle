# Chemist

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Chemist layers — `04`, `05`, `52` (Chemist rows), `58`, and `69` — folded
into this single decision doc.

> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Chemist is the **least engine-coupled** job
> in the roster: its core is flat item values (Potion/Ether/Phoenix), which scale by item tier rather
> than by a damage formula, so most of it transfers to the DCL almost unchanged (see *DCL rebase
> notes*).

## Identity / compass

Chemist is the **inventory-bound certainty job**: it turns stock into reliable single-target
recovery, revive, condition cleanup, MP support, and practical battlefield tricks. Its promise is
**reliability** when magic's CT/Faith/MP would be too slow or too swingy.

That certainty is paid for through item stock, action economy, mostly single-target scope,
positioning, support-slot pressure, leather durability, and modest personal damage until guns arrive.
Chemist is **item-first** in the early bands; the gun identity is real but first meaningful gun
access stays **Band B+** unless a later equipment pass proves an earlier timing is safe.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `specialist` |
| Secondary tags | `item`, `gun` |
| Growth profile | hybrid |
| Armor class | `leather` |
| Weapon families | gun, knife, fists (missile / thrust / crush) |
| Role reason | Reliable item support plus a PA-independent ranged route; stays useful through action certainty and utility. |

**Good at:** guaranteed single-target recovery/revive/cleanup, MP support, low-stat reliability,
PA-independent gun pressure (Band B+).
**Bad at / countered by:** stock exhaustion, multi-target burst, raw damage before guns, leather
durability, anything that punishes spending a turn on a non-damage certainty.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Potion line** | Primary HP lane, scaling by item tier. | Real stock; single target; short base range. *(v0.2: Potion 30 / Hi-Potion 70 / X-Potion 150 HP, availability-tier gated.)* |
| **Ether line** | Reliable MP support lane. | Real stock; caster support, not free MP sustain. *(v0.2: Ether 20 / Hi-Ether 50 MP.)* |
| **Elixir** | Late, rare HP+MP emergency restore. | Not `Auto-Potion`-eligible; no routine-sustain assumption; only if the vanilla item exists in final data. |
| **Phoenix Down** | Revive without a death-loop. | Real stock; revived unit stays vulnerable. *(v0.2: max(20, 20% maxHP); Item Lore → 30%.)* |
| **Condition items / Remedy / Holy Water** | Reliable condition cleanup; `Holy Water` keeps its undead/status identity. | Cleanup, not prevention or immunity. `Remedy` = broad late answer; single cures cheaper/narrower. |
| **Field Salve** | Tactical Poison/Oil cleanup plus a modest scaling patch-heal. | Below Potion-line healing; no revive; no broad cleanup — keeps Oil combos alive. *(v0.2: raw = 10 + ⌊Level/4⌋ + PA, capped at potion-tier/2.)* |
| **Quick Draw** | A gun shot that denies enemy tempo. | Requires gun; Band B+; CT loss hits the **target**, not self; not a gun damage steroid. *(v0.2: gun output ×0.70; target −15 CT on hit.)* |
| **Smoke Bomb** | Visible short accuracy-disruption (Blind/Smoke), small AoE. | No damage; visible status; bounded duration; no hidden tile field; respects immunity. *(v0.2: range 3, small AoE, ~1 round.)* |

## Reaction / Support / Movement

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Auto-Potion** | Tier-aware, stock-consuming survival reaction. | **Off Brave**; post-damage, survivor-only; once per round; triggers at post-damage HP ≤ 50%; Potion-line only. X-Potion tier is conditional on attrition validation (falls back to Hi-Potion if it creates practical immunity). *(v0.2: fixed 70% trigger.)* |
| Support | **Throw Item** | Deliberate ranged item delivery. | Item **range** +2 only — not power, not `Auto-Potion`; must not make positioning irrelevant. |
| Support | **Item Lore** | Rewards committed active-item builds. | **Active-use only.** *(v0.2: Potion-line ×1.30, Ether-line ×1.20, Phoenix 30%.)* Never improves `Auto-Potion`, `Smoke Bomb`, `Quick Draw`, or (this pass) `Elixir`. |
| Support | **Safeguard** | Narrow anti-disruption. | Blocks battle-scoped equipment break/steal only; **not** generic damage mitigation. |
| Support | **Reequip** | Action-cost in-battle equipment swap. | Full action; owned items only; no new equipment; **first cut candidate** if it trivializes the weapon-vs-armor matchup game. Any swap that changes weapon family / damage mode / shield / armor class is engine-affecting and must be re-validated. |
| Movement | **Move-Find Item** | Campaign treasure identity. | No combat mobility; must not become a source of free `Auto-Potion` stock; no Gil edits. |

## Open items / validation hooks

- `I-ATTRITION`: Potion tiers, % Phoenix revive, Phoenix → `Auto-Potion` top-up, once-per-round
  behavior, and X-Potion eligibility.
- `M-RSM-COUNT` / `M-SECONDARY-COUNT`: incidence of `Auto-Potion`, `Throw Item`, `Item Lore`,
  `Safeguard`, `Reequip`, `Move-Find Item`, and Items-secondary.
- `T4 accuracy`: `Smoke Bomb` hit rate and short Blind/Smoke duration.
- Control-identity sweep: `Quick Draw` CT chip vs Orator and Time Mage control.
- Engine re-validation: any accepted `Reequip` swap that changes weapon family / damage mode /
  shield / armor class.
- Watch: `Auto-Potion` becoming mandatory on every durable unit; `Item Lore` over-centralizing
  support; `Reequip` trivializing matchup planning.

## DCL rebase notes

- **Item HP/MP/revive (Potion / Ether / Elixir / Phoenix) is essentially engine-neutral** — flat item
  values that scale by tier, not by a damage formula. They transfer to the DCL almost unchanged; this
  is what makes Chemist's certainty identity robust across engines.
- **Auto-Potion** is explicitly off-Brave and post-damage — it maps cleanly onto the DCL's **neutral
  reaction** category (flat trigger chance, not Brave-scaled).
- **Quick Draw**: "gun output ×0.70" re-expresses on the DCL gun pipeline (gun is **skill-primary**,
  skill → penetration in the DCL); the −15 CT is engine-neutral.
- **Smoke Bomb** Blind/Smoke: the % infliction becomes the DCL **3d6 status contest**; the effect
  (an accuracy penalty) maps onto a penalty to the enemy's 3d6 hit roll.
- **Field Salve** scaling heal re-derives on the DCL scale; the cleanup intent carries over directly.
