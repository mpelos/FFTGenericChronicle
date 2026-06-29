# 01 — Squire · "The Unbreakable"

The entry job, reframed as the **last-man-standing anchor that keeps acting through lockdown**. Its
power is *uptime*, not magnitude: the one body that cannot be Stunned, knocked flat, or ordered to
halt, and that drags a locked-down squad back into the fight.

This doc records the design decision for the Squire on the Deep Combat Layer (DCL). Mechanics it leans
on are owned by the DCL docs and cross-referenced inline; numbers are calibration (`docs/deep-combat-layer/12`).

## Tier & tree position

- **Tier D.** Tier here is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*):
  D = the most accessible jobs. The Squire is the **root of the tree** (`docs/job-balance/00-job-tree.md`)
  — every recruit starts here, no prerequisites — so it sits in the most-reachable tier and is balanced
  *within* that tier, not against capstones.
- Being Tier D does **not** mean "the job you leave behind" (its vanilla fate). It means a job whose
  edge is a **specific, always-available answer** rather than a high ceiling — see J1.

## The vanilla problem it solves

Vanilla Squire (`docs/job-balance/vanilla/01-squire.md`) is the canonical dead-end: below-baseline
growths, a thin *Fundaments* command, no innate worth fielding for, and a near-mandatory JP-economy
wart. It "exists to be left behind."

The DCL Squire keeps the humble chassis but gives it **one thing nothing else in the roster has** —
innate immunity to the lockdown-flag class — so there is a real, legible reason to field a Squire on
the right map at *any* point in the game. The vanilla keepers are preserved and sharpened: **Move+1**
and **Counter Tackle** return as real exports/reactions, and vanilla *Salve* (area status cleanse)
evolves into the focused **On Your Feet** lockdown-cleanse.

## Fantasy

The dogged cadet who simply will not go down and cannot be kept down — Stunned, knocked flat, ordered
to halt, and he just keeps soldiering. The body still taking turns when the rest of the squad is locked
on the floor.

## Chassis

- Balanced, PA-leaning, **solid base HP** (an endurance chassis; base HP is also the status-resist stat,
  `docs/deep-combat-layer/13`).
- **Armour: Clothes & Suits** — the light body class (`docs/deep-combat-layer/14`). Heavy Armor is the
  Knight's identity lever, so the entry job stays light: it survives by *being a body that keeps acting*
  (Counter Tackle + lockdown immunity + Move), **not** by armour DR.
- Broad **generic equip access** — the widest *access* in the roster, the shallowest *specialisation*
  (see aptitude).
- Low Faith, moderate Brave (two-sided at Core, `docs/deep-combat-layer/07`–`08`). Speed average.
  Unremarkable Dodge — it trades flashy evasion for **guaranteed uptime**.
- Movement ordinary, but owns and exports **Move+1** (the team's sole raw +Move).

## Innate — Stalwart (free, and exported)

Immunity to the **lockdown-flag class only**: **Stun / Knockdown / Don't-Act / Don't-Move**, regardless
of physical *or* magical source. The Squire has it **free** (innate, no support slot spent). Per the
portability rules (`docs/deep-combat-layer/15`), Stalwart is **also a learnable Support** — any job can
equip the same **full** immunity, paying its support slot. This does **not** nullify the control doors
(Knight Trip, Throw-Stone riders): it is a slot-cost counter-pick (the unit forgoes Doublehand /
Concentration / Throw Items / Light Fingers / etc.), so the doors stay meaningful on everyone who hasn't
spent the slot. The Squire's moat is getting it **free** (slot open) on a durable chassis — not
exclusivity (`docs/job-balance/job-design-process.md`).

Stalwart deliberately does **not** cover displacement (being shoved/knocked back is the Knight's
*Hold Ground* lane, `docs/job-balance` Knight entry) and does **not** cover the mind/transform/halt
family (Sleep / Stop / Petrify / Frog / Slow / Charm / Confuse / Fear). Those are distinct flags with
their own counters — keeping Stalwart narrow is what makes the Squire a *situational answer*, not a
universal immunity.

## Command kit

**Core** (always-on identity):

- **Throw Stone** — free, infinite, ranged physical chip: no MP, no weapon, no ammo, any tile including
  height. The immune body keeps contributing at range while it holds the line.
- **Wish** — Faith-proof HP-transfer heal: sacrifice own HP to heal an ally a larger amount. A heal lane
  **no Faith roll can blank** (part of the roster's multi-lane healing census, `docs/deep-combat-layer/11`;
  none dominates everywhere).
- **On Your Feet** — Faith-proof cleanse of the **lockdown flags only** (Stun / Knockdown / Don't-Act /
  Don't-Move), adjacent / small area. It is the *export* of the Squire's own resilience: the immune body
  un-locks the squad around it. Deliberately narrow so it **complements White Mage's Esuna** (the broad
  cleanser) rather than replacing it.

**Tier-2** (upside via unbuilt engine hooks — never a floor, `docs/deep-combat-layer/15`):

- **Tackle** — knockback / peel: shove an enemy disabler off an ally or out of position (Tier-2
  displacement).
- **Throw Stone riders** — costed Knockdown / Stun on the thrown rock (Tier-2 status, paid in a real
  currency per the control-status ration).

## R / S / M

- **Reaction — Counter Tackle (Core) + Grit (Tier-2).** Counter Tackle (the vanilla keeper) is the
  honest Core reaction — a free retaliatory shove when struck in melee. Grit (low-HP clutch) is a
  condition-gated Tier-2 bonus, minor by design.
- **Support — Stalwart.** The exported lockdown-immunity (see *Innate*); the Squire's innate, learnable
  by any job at the cost of its support slot.
- **Support — Field Equip / Basic Arms.** The weapon-proficiency export (`15`): grants the Squire's
  basic-arms grades — **Sword C · Knife C · Axe C · Crossbow D** — to whatever job equips it (aptitude
  travels at the source grade now; **no armour / shield / job-exclusive families**). A genuine
  cross-build enabler; the modest C-grades keep it from donating specialist power.
- **Movement — Move+1.** The team's sole raw +Move export; the roster's chassis donor.

*(R/S/M is a **set**, not one-of-each: two reactions, two supports, one movement — the unit equips one
per slot.)*

## Equipment & weapon aptitude

Grade budget pool **7**, **A in no family** — breadth *is* the cost (`docs/deep-combat-layer/15`,
*Weapon aptitude*; mechanic owned by `10`). Exact letters/pool are calibration (`12`).

| Slot | Grant |
|------|-------|
| Armour | **Clothes & Suits** |
| Off-hand | **Shield** (Block — the top active-defense rung and the only strong anti-ranged defense at this tier, `04`) |
| Sword | **C** — reliable swing; the best 1H parry among its options |
| Knife | **C** — thrust (×2 vs no-DR), light, pairs with the shield |
| Axe | **C** — crush, the answer to Heavy Armor / guard |
| Crossbow | **D** — minimal skill-primary reach (a touch of ranged, not a specialist) |

It covers all three physical damage types (cut / thrust / crush) plus a touch of reach, but tops out at
C — **any specialist beats it on that specialist's weapon**. That is exactly the entry job's shape:
amplitude paid for in depth.

## Early / mid / late

- **Early.** A competent, flexible first body: covers any damage type, heals Faith-free (Wish), throws
  rocks for free. Move+1 and Field Equip already feed the squad.
- **Mid.** As specialists come online the Squire stops being a default front-liner and becomes a
  **situational pick**: brought *because* the map or the enemy comp rewards Stalwart / On Your Feet, or
  as the chassis donor for another build.
- **Late.** Never a magnitude job — its endgame value is **uptime against lockdown/attrition comps and
  on hold-the-point / survive maps**, plus its always-relevant exports. It stays in the toolbox for the
  matchup, not the power.

## Signature builds & synergy

- **The Lockdown Anchor** (Squire + a borrowed cleanse such as White's Esuna): an immune body that
  un-disables the team while it itself cannot be locked down. With On Your Feet it already self-rescues
  the squad's lockdown flags; with a borrowed broad cleanse it answers the wider status family too.
- **Chassis donor.** Move+1 and Field Equip lend mobility and off-class weapon access across the whole
  export web. The Squire borrows any trait-/family-appropriate secondary to *act on* while it is immune.

## Battle dynamics

**What the player does with it.** Field the Squire when the enemy plan is lockdown / attrition or the
map is hold-the-point: it is the body that keeps acting (Stalwart) and un-locks the squad (On Your Feet)
while preserving its support slot for something else. Throw Stone chips for free at range while it holds;
Wish keeps a low-Faith ally up where a White heal would be blanked; Axe C is its anti-armour answer
(crush vs Heavy). As a **donor** it hands out Stalwart (immunity counter-pick), Move+1, and Basic-Arms
access across the squad. Three-case read: *Squire (innate)* — immune free, slot open; *White + Stalwart*
— a better pure cleanser, slot spent, still robe-fragile; *finisher + Stalwart* — can't be locked off
its kill turn, slot spent.

**How an enemy version harms the player.** An enemy Squire **ignores your lockdown** — Trip / Stun /
Don't-Act / Don't-Move whiff on it, so it keeps acting and un-locks its allies. Counterplay is legible:
it is a **low-output** body, so answer it with **damage, displacement, mind/halt status** (Sleep / Stop /
Charm / Petrify — Stalwart doesn't cover those), **magic, or focus-fire**. Don't expect your lockdown
tools to solve it; just out-damage it.

## J1 — the pick / wrong-pick

- **The pick:** vs a **lockdown / attrition team**, and on **hold-the-point / survive** maps. The Squire
  is the **cheapest, most slot-efficient lockdown anchor**: immune for **free** (support slot open),
  durable enough to stay in the scrum, and still running a real Support plus a secondary on top — and it
  **recovers the team** with On Your Feet. A White Mage can equip Stalwart to keep cleansing under
  lockdown, but it pays the support slot and stays robe-fragile; the Squire gets immunity free on a
  sturdier body. *(Sim: in a ~40%-lockdown comp it raises team action-economy +58%.)* It needs no
  specific AI behaviour and no specific enemy comp to work.
- **Wrong pick (two-sided):** a straight **damage race** (immunity is dead weight — output and defence
  are the budget there); a **mind / transform / halt-disable** team (Sleep / Stop / Petrify / Frog /
  Slow / Charm / Confuse / Fear — Stalwart does not cover those; bring magic-resistance or a broad
  cleanser); any fight that needs real damage, tanking, or healing *magnitude*. Its value is **uptime,
  not power**.
