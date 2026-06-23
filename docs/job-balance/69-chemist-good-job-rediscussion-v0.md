# Chemist Good-Job Rediscussion V0

Status: GPT/Claude consensus for Marcelo validation
Date: 2026-06-23
Scope: Chemist only

Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/52-squire-chemist-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Purpose

This document revisits Chemist under the updated good-job premises:

- learned skills should feel useful and readable;
- item-based recovery may scale through item tier and stock progression;
- strong setups are healthy when they require real stock, action, support-slot, timing, or party cost;
- reactions should not all become Brave optimization problems;
- persistent combat effects should use visible status feedback;
- no Gil values are changed;
- no new equipment is added.

This pass supersedes the Chemist rows in docs 52 and 58 where they conflict. It does not change
Squire, final JP costs, prerequisites, item prices, sell values, rewards, or implementation data.

## Chemist Identity

Chemist is the inventory-bound certainty job: it turns stock into reliable single-target recovery,
revive, cleanup, MP support, and practical battlefield tricks.

The job should remain useful all game through item tiers and delivery tools. Its certainty is paid
for through item stock, action economy, mostly single-target scope, positioning, support-slot
pressure, leather durability, and modest personal damage until guns arrive.

Chemist is item-first in Band 0/A. Gun identity is real, but first meaningful gun access remains
Band B+ unless a later equipment pass proves an earlier timing is safe.

## Consensus Package

| Skill | Slot | Value | Guardrail |
| --- | --- | --- | --- |
| `Potion` | Action item | 30 HP | Real stock; single target; short base range. |
| `Hi-Potion` | Action item | 70 HP | Availability-tier gated; not Band A floor. |
| `X-Potion` | Action item | 150 HP | Late stock; active item use and late Auto-Potion tier only. |
| `Ether` | Action item | 20 MP | Real stock; caster support, not free MP sustain. |
| `Hi-Ether` | Action item | 50 MP | Mid/late caster support; stock-limited. |
| `Elixir` | Action item | late rare HP+MP emergency restore | Not Auto-Potion eligible; no routine sustain assumption. |
| `Phoenix Down` | Action item | revive at max(20 HP, floor(0.20 * max HP)) | Real stock; revived unit remains vulnerable. |
| Condition items | Action item | clear their matching conditions | Cleanup, not prevention or immunity. |
| `Remedy` | Action item | broad late condition cleanup | Single target; stock-limited. |
| `Holy Water` | Action item | status cleanup plus undead interaction | Preserve FFT item vocabulary. |
| `Field Salve` | Action | Poison/Oil cleanup plus modest scaling heal | Below Potion-line healing; no revive. |
| `Quick Draw` | Action | gun hit x0.70; target -15 CT on hit | Requires gun; Band B+; not a gun damage steroid. |
| `Smoke Bomb` | Action | visible short Blind/Smoke status | Stock/action bounded; no hidden tile smoke. |
| `Auto-Potion` | Reaction | tier-aware Potion-line reaction | No Brave; stock-consuming; post-damage survivor-only. |
| `Throw Item` | Support | item range +2 | Does not improve item power or Auto-Potion. |
| `Item Lore` | Support | stronger active item use | Active-use only; no Auto-Potion boost. |
| `Safeguard` | Support | blocks battle-scoped equipment break/steal | No generic mitigation. |
| `Reequip` | Support | action-cost in-battle swap | No new equipment; formula-affecting swaps must be tested. |
| `Move-Find Item` | Movement | campaign treasure identity | No combat mobility increase; no Gil edits. |

## Action Notes

### Potion Line

The Potion line is Chemist's primary HP scaling lane.

```text
Potion = 30 HP
Hi-Potion = 70 HP
X-Potion = 150 HP
```

Rules:

- consumes actual item stock;
- single target;
- short base range;
- `Throw Item` improves delivery range, not item power;
- `Item Lore` improves active use only;
- no Gil price, sell value, or reward edits are made.

The item-tier structure is the intended late-game scaling. The old flat `Auto-Potion` boundary is
superseded because it kept the reaction safe by making it irrelevant late.

### Ether Line And Elixir

The Ether line remains Chemist's reliable MP support lane.

```text
Ether = 20 MP
Hi-Ether = 50 MP
```

`Elixir` is restored as a late rare Chemist identity hook if the item exists in the final data set.
It is not eligible for `Auto-Potion`, and `Item Lore` does not improve it in this provisional pass.
The expectation is rare emergency use, not routine attrition sustain.

### Phoenix Down

`Phoenix Down` should not revive into a late-game death loop.

```text
active Phoenix Down revive HP = max(20, floor(0.20 * target_max_hp))
with Item Lore = floor(0.30 * target_max_hp)
```

Rules:

- consumes actual item stock;
- single target;
- no Auto-Phoenix behavior is accepted here;
- `Item Lore` improves only active Phoenix Down use;
- the revived unit remains vulnerable and usually still needs protection or follow-up healing.

The Phoenix Down into later `Auto-Potion` top-up pattern is allowed as a possible combo, but it must
be counted in attrition validation because it combines revive, reaction, and item stock.

### Condition Items, Remedy, And Holy Water

Chemist keeps reliable condition cleanup.

The baseline condition item lane remains recognizable:

- `Antidote`;
- `Eye Drops`;
- `Echo Herbs`;
- `Maiden's Kiss`;
- `Gold Needle`;
- `Holy Water`;
- `Remedy`.

These are cleanup tools, not prevention or immunity. `Remedy` is the broad late answer, while
individual cures remain cheaper and narrower. `Holy Water` keeps its undead/status identity.

### Field Salve

`Field Salve` changes from flat 15 HP into a modest scaling field patch.

```text
raw_heal = 10 + floor(Level / 4) + PA
effective_heal = min(raw_heal, floor(current_potion_tier_heal / 2), missing_hp)
```

Rules:

- adjacent or range 1;
- clears Poison and Oil;
- no revive;
- no broad status cleanup;
- never beats Potion-line healing for the current item tier.

The point is not to erase Oil combos. The point is to give Chemist a tactical cleanup button after a
Poison/Oil plan lands, while keeping the real recovery lane tied to items and stock.

### Quick Draw

`Quick Draw` must not remain a worse normal attack.

```text
requires gun
damage = normal gun attack output x0.70
on hit = target -15 CT
```

Rules:

- Band B+ because it requires real gun access;
- single target;
- no item interaction;
- no self CT gain;
- the CT loss affects the target, not the user;
- no persistent hidden status is required because the turn-order change is visible.

If `-15 CT` is not noticeable enough in later feel checks, `-20 CT` is the first adjustment direction.
It should still remain distinct from Orator's broader speech/gun control identity and Time Mage's
dedicated CT manipulation.

### Smoke Bomb

`Smoke Bomb` is accepted as visible short status utility, not as an invisible persistent tile field.

Provisional target:

```text
range = 3
area = small AoE
effect = Blind or Smoke-style visible accuracy disruption for 1 round
hit rate = moderate, final value pending T4 accuracy validation
```

Rules:

- no damage;
- visible status feedback required;
- no hidden evasion aura;
- does not bypass status immunity or boss immunity;
- duration must be bounded so it cannot stack into practical enemy helplessness;
- final data should bind a real item/action cost without adding equipment or changing Gil values.

Using vanilla Blind semantics is preferred because it is readable FFT vocabulary and already has
status feedback. If implementation cannot represent a visible short status, the skill must be
re-reviewed before final data.

## Reaction Notes

### Auto-Potion

`Auto-Potion` becomes tier-aware, stock-consuming, and non-Brave.

```text
trigger chance = fixed 70%
trigger stat = none; not Brave/Faith based
timing = post-damage, survivor-only
frequency = once per unit round
threshold = post-damage HP <= 50% max HP
eligible items = Potion-line only
Item Lore interaction = none
Elixir/Phoenix/Remedy interaction = none
```

Tier rules:

| Campaign / learned tier | Preferred reaction item | Heal |
| --- | --- | ---: |
| Potion tier | `Potion` | 30 HP |
| Hi-Potion tier | `Hi-Potion` if learned and stocked, otherwise lower fallback | 70 HP |
| X-Potion tier | `X-Potion` if learned and stocked, otherwise lower fallback | 150 HP |

The X-Potion tier is included provisionally because excluding it recreates the late-game irrelevance
problem. It is conditional on attrition validation: if X-Potion Auto-Potion creates practical
immunity or an unkillable sustain loop, the final cap direction is Hi-Potion.

This supersedes doc 52's blanket X-Potion exclusion.

## Support And Movement Notes

### Throw Item

`Throw Item` remains the delivery support.

```text
item range +2
```

It does not improve item power, does not affect `Auto-Potion`, and must not make positioning
irrelevant by itself.

### Item Lore

`Item Lore` is active-use only.

```text
active Potion-line HP items = x1.30
active Ether-line MP items = x1.20
active Phoenix Down revive = 30% max HP
```

It does not affect:

- `Auto-Potion`;
- `Smoke Bomb`;
- `Quick Draw`;
- `Elixir` in this provisional pass;
- condition-item success unless a later status pass explicitly accepts that.

This makes committed item builds feel stronger while preventing the support from becoming a passive
reaction-sustain engine.

### Safeguard

`Safeguard` remains a narrow anti-disruption support.

It blocks battle-scoped equipment break and steal effects. It does not reduce generic incoming
damage and should only become valuable when enemy kit or map pressure asks for it.

### Reequip

`Reequip` is kept provisionally as an action-cost in-battle swap support.

Rules:

- requires the support slot;
- swapping costs a full action;
- respects the unit's job equipment restrictions;
- swaps only among owned/equipped items;
- no new equipment is added;
- no Gil values are changed.

Any swap that changes weapon family, damage mode, shield profile, armor class, or target profile is
formula-affecting and must be tested before final implementation. `Reequip` is Chemist's first cut
candidate if W4/W5 shows it trivializes the weapon-vs-armor matchup game.

### Move-Find Item

`Move-Find Item` remains a campaign and treasure identity movement skill.

It gives no combat mobility increase. It must not become a hidden source of effectively free
`Auto-Potion` stock, and it does not justify Gil edits.

## Expected Healthy Builds

- active Chemist with Items and a gun, alternating reliable support and tactical `Quick Draw`;
- low-Faith unit carrying Items as emergency reliability;
- durable support unit using `Throw Item` for deliberate ranged item delivery;
- item specialist with `Item Lore`, paying the support slot for stronger active consumables;
- survival build using `Auto-Potion`, with stock cost, HP threshold, once-per-round timing, and no
  lethal prevention;
- party setup where revive, follow-up protection, and later Auto-Potion create a strong but
  resource-consuming recovery line.

## Watch Patterns

- `Auto-Potion` becoming mandatory on every durable unit once Hi-Potion or X-Potion tiers exist;
- `Phoenix Down` into `Auto-Potion` top-up becoming a low-cost death-loop answer;
- `Throw Item` making positioning irrelevant;
- `Item Lore` making every support caster prefer Items over White, Mystic, or Time support;
- `Quick Draw` CT chip erasing Orator or Time Mage control identity;
- `Smoke Bomb` stacking into practical enemy helplessness;
- `Reequip` trivializing weapon-family and armor-profile planning;
- `Move-Find Item` or item availability creating effectively free reaction stock.

## Validation Hooks

These are later validation hooks, not blockers to this provisional rediscussion artifact:

- `I-ATTRITION`: Potion tiers, Phoenix Down percentage revive, Phoenix into Auto-Potion top-up,
  Auto-Potion once-per-round behavior, and X-Potion eligibility.
- `M-RSM-COUNT`: incidence of `Auto-Potion`, `Throw Item`, `Item Lore`, `Safeguard`, `Reequip`, and
  `Move-Find Item`.
- `M-SECONDARY-COUNT`: how often Items secondary plus `Throw Item` or `Item Lore` outcompetes
  specialist support packages.
- `T4 accuracy`: `Smoke Bomb` hit rate and short Blind/Smoke duration.
- `F5 real-roster`: active Chemist with gun/item utility, item-secondary units, and parties without
  active Chemist.
- Formula re-sim: any accepted `Reequip` swap that changes weapon family, damage mode, shield
  profile, armor class, or target profile.
- Control identity sweep: compare `Quick Draw` CT chip against Orator and Time Mage control roles.

## Reviewer Notes

Claude approved the Chemist direction before this artifact was written:

- `Auto-Potion` should scale through Potion tiers and may include X-Potion late, conditional on
  attrition validation.
- `Phoenix Down` should revive by percentage instead of staying flat 20 HP.
- `Quick Draw` should be a target CT-denial gun shot, not a self-tempo loop.
- `Smoke Bomb` should use visible short Blind/Smoke status, not hidden tile smoke.
- `Reequip` may be kept with full action cost and hard gates; it remains the first cut if it
  trivializes equipment matchup planning.

Claude accepted this artifact as Chemist's provisional rediscussion record pending Marcelo
validation.

Non-blocking notes carried into validation:

- `Auto-Potion` item selection should compare best-available tier against tier-appropriate or
  cheapest-sufficient selection. If best-available wastes scarce X-Potions on small hits, prefer the
  cheapest-sufficient rule.
- `Elixir` is allowed only as a vanilla item already present in the data set. This pass does not
  authorize creating a new item record.
