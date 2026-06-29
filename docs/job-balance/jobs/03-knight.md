# 03 — Knight · "The Master-at-Arms"

The armoured melee, reimagined as a **Battle Master**: the most weapon-skilled body on the roster,
whose skill itself unlocks a suite of **martial maneuvers**. It protects by **geometry and control**,
and opens the enemy by **breaking their guard** — not by destroying their gear.

This doc records the design decision for the Knight on the Deep Combat Layer (DCL). Mechanics it leans
on are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`).

## Tier & tree position

- **Tier C.** Tier here is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*):
  an early foundational job, one step past the Squire/Chemist entry pair
  (`docs/job-balance/00-job-tree.md`). Balanced within its tier; its weapon pool (8) is high-for-tier
  because the Knight is a pure on-axis weapon-and-armour job, but it stays below the capstones.

## The vanilla problems it solves

Vanilla Knight (`docs/job-balance/vanilla/03-knight.md`) is "a great base, a mediocre destination."
*Arts of War / Rend* is accuracy-gated denial — whiff-prone, **useless vs monsters**, redundant once
damage snowballs — and the job's real value is the chassis plus the **Equip Heavy Armor / Equip
Shields** donor supports. The classic mine-don't-field job.

Two design problems are addressed at once:

1. **Permanent equipment destruction is removed entirely.** It is near-useless against one-off enemies
   you never meet again, and it is a **feel-bad** mechanic when an enemy Knight permanently breaks the
   *player's* gear. The "open the target for the team" fantasy is kept, but rebuilt on **guard-shred**
   (reversible, universal, never feel-bad).
2. **Donor → destination.** The signature maneuvers are **primary-gated** (innate *Weapon Master*), and
   on the DCL armour is job-gated (B10) so **Equip Heavy Armor / Equip Shields do not exist as
   splashes** — you cannot mine the Knight's durability onto another body.

## Fantasy

The master-at-arms / medieval knight: so skilled with weapons that the skill itself enables battlefield
maneuvers — break a guard, trip, shove, goad, punish a whiff — while standing as the wall that must be
dealt with first.

## Chassis

- High HP, **high PA** · Speed low (Heavy Armor −1 Move, `docs/deep-combat-layer/14`) · Move 3 ·
  Faith none.
- **Heavy Armor + shield Block** — but Block **depletes** (`docs/deep-combat-layer/04`), so the Knight
  is a **soft-tank**, not an unkillable anvil (focus-fire drains Block; flank/back ignores it; crit
  bypasses it).
- **Brave fork (a real build choice, two-sided per B9):**
  - **Low Brave → shield-wall:** better active defence, taunt-resistant — the anvil.
  - **High Brave → Doublehand line-breaker:** the open-target damage build.
  - High PA carries weapon damage either way.
- Off-hand: **Shield OR Doublehand** (the fork). Movement **Hold Ground** — immovable (cannot be
  shoved/knocked back); the **displacement-immunity** lane, distinct from the Squire's
  lockdown-immunity.

## Innate — Weapon Master (free, and exported)

The Knight's deep weapon skill is the **maneuver gate**: it unlocks the **master maneuvers** (the
team-scale versions of Guard Break, Trip, Shove, Taunt, Cover). The Knight has Weapon Master **free**
(innate). Per the portability rules (`docs/deep-combat-layer/15`), it is **also a learnable Support**:
an off-job running **Arts of War secondary + Weapon Master support** gets the master maneuvers too —
paying **two slots**. Without the support, off-job Arts of War gives only the **basic** maneuvers (Power
Strike and a weak, self-only Feint).

The Knight stays the best home not by exclusivity but by its **moat**: **Heavy Armor is job-gated and
does NOT export** (B10, `14`), so a splashed maneuver-user keeps its own fragile chassis — it gets the
openers but not the wall (no shield-wall, no Hold Ground / Bulwark line-hold). The maneuvers are control
/ openers, not raw damage, so a faster body wielding them is a welcome splash, never a strictly-better
home (`docs/job-balance/job-design-process.md`).

Weapon Master is **not** a flat Parry bonus: the Knight's top Parry comes from its **skill-scaled
weapon Parry** (`docs/deep-combat-layer/04`), so defence does not over-stack (Heavy Armor + Block +
Parry).

## Command — Arts of War (maneuvers)

**Core (engine-real):**

- **Power Strike** — a committed heavy PA blow at the cost of **−own guard this turn** (the aggressive
  button; feeds the high-Brave Doublehand fork). Available off-job as a basic maneuver.
- **Guard Break** — strike that shreds the target's **Block + Parry only — never Dodge** — leaving a
  persistent token. The team opener against turtles, shield users, and parry duelists; **evasive**
  targets remain an Archer / flank problem (no overlap with Concentration). Universal (works vs
  monsters), reversible. **Guardrails:** melee-only, single-target, one action — **no Dual-Wield
  double-application, no Throw / ranged delivery**; defence still rolls. *The master (persistent,
  team-relevant) version needs Weapon Master (free on Knight, or the support off-job); plain Arts of War
  gets only a weak self-only Feint.* This is the flagship — it replaces gear destruction.
- **Sweep / Cleave** — front-arc / adjacent AoE melee: **reduced damage, each target rolls defence, no
  riders, costed** (the area rule, `docs/deep-combat-layer/15`). Anti-cluster.
- **Bulwark** — brace as a **hard obstacle**: raise own Block/DR, become costly to pass/displace;
  anchors a chokepoint.

**Tier-2 (costed maneuvers via unbuilt engine hooks; master versions Knight-primary):**

- **Trip / Knockdown** — knock the target prone (a lockdown flag, costed per the control-status ration;
  the Squire's Stalwart is immune — a clean interaction).
- **Shove** — **line-control** displacement of one tile (off a ledge, out of position) — distinct from
  the Squire's rescue/peel Tackle.
- **Bind Weapon** — suppress the target's weapon **Parry / reaction / weapon-skill bonus** until its
  next turn. The reversible replacement for "disarm" — no inventory object, no permanent loss.
- **Goad / Taunt** — draw aggression; the Knight is the sanctioned Taunt door (control-status ration,
  rule 8; directed-taunt AI).
- **Cover / Bodyguard** — intercept a hit aimed at an adjacent ally (needs a confirmed redirect
  primitive).

## R / S / M

- **Reaction — Riposte** — counter an adjacent melee attacker whose attack misses or is defended.
  Melee-only · once per attacker action · no ranged/magic. The skilled-fighter's punish.
- **Support — Doublehand** — 1H → +weapon-mod; the open-target damage lane, homed to a 1H job per the
  cross-job rule.
- **Support — Weapon Master** — the maneuver gate, learnable (see *Innate*); with Arts of War secondary
  it grants the master maneuvers at a two-slot cost.
- **Support — Arms Training** (weapon-proficiency export, `15`) — grants the Knight's **Sword B + Flail
  D** to whatever job equips it. **Knight Sword A does NOT export** (signature/exclusive family).
- **Movement — Hold Ground** — immovable.

*(R/S/M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot.)*

## Equipment & weapon aptitude

Pool **8** (`docs/deep-combat-layer/15`, *Weapon aptitude*; mechanic owned by `10`).

| Slot | Grant |
|------|-------|
| Armour | **Heavy Armor** (not splashable — B10) |
| Off-hand | **Shield / Doublehand** (the fork) |
| Knight Sword | **A** — signature, job-exclusive |
| Sword | **B** — the bread-and-butter swing |
| Flail | **D** — access to crush / **fura-guarda** (value = −2 to be blocked, not raw flail damage) |

*(Axe dropped — redundant with the Flail's crush. Pool trimmed from 10→8 to sit in Tier C; Knight Sword
stays exclusive.)*

## Why it is a destination, not a donor

Under the portability philosophy the maneuvers *travel* — Arts of War + Weapon Master gives an off-job
the master maneuvers at a two-slot cost. What does **not** travel is the **moat**: **Heavy Armor is
job-gated (B10, no Equip-armour splashes)**, the Knight gets Weapon Master **free** (slots open), and the
wall kit (Hold Ground, Bulwark, Cover) only pays off on a durable body. A splashed maneuver-user gets the
openers but keeps its own fragile chassis — strong, but never a strictly-better Knight. Durability on the
roster comes from **choosing a durable job**, not from mining Knight gear.

## Early / mid / late

- **Early.** A reliable tank that already opens targets (Guard Break) and lands an honest hit
  (Power Strike).
- **Mid.** The chokepoint anchor and the protector of a glass backline; the Battle Master who trips,
  shoves, goads, and punishes whiffs; exports Doublehand.
- **Late.** A destination, not a donor — it opens turtles for the finishers and holds objectives. Not a
  pure-burst race (Monk/Ninja/Samurai out-scale raw damage) — by design.

## Battle dynamics

**What the player does with it.** Field the Knight as the **line-holder and team-opener**: stand in the
lane to bodyblock a glass backline (Cover, Bulwark, Hold Ground — a chokepoint nothing shoves it off),
and **Guard Break** the high-guard threats so the team's chip becomes real damage. *(Sim: Guard Break is
~3.1× a high-guard target's incoming damage; it never touches Dodge, so evasive targets stay an
Archer/flank job — no overlap.)* Against plate, pair Guard Break with its own **Flail crush** or a
crusher ally. Build the Brave fork to the map: **low-Brave wall** (active-def +3, taunt-resist) to anchor,
or **high-Brave Doublehand** to break open targets. As a donor it hands out Doublehand, the master
maneuvers (Weapon Master + Arts of War, 2 slots), and Sword B / Flail D — but **never Heavy Armor**.

**How an enemy version harms the player.** An enemy Knight **Guard-Breaks your turtle** (your defensive
units stop turning hits aside), **Taunts your aggressive units out of position**, and **holds a
chokepoint** you can't shove it off. Counterplay: bring **evasive** units (Guard Break can't touch
Dodge), **crush or magic** for its Heavy Armor, **flank / focus-fire** to drain its Block, and **kite**
it (Move 3, no ranged answer).

## J1 — the pick / wrong-pick

- **The pick:** vs armed melee and **high-guard comps** (turtles / shield users / parry duelists) —
  break their guard and bodyblock the lane to shield a glass backline (needs no AI, no specific comp);
  control the field with trip / shove / taunt; hold a chokepoint nothing can shove it off.
- **Wrong pick (two-sided):** very **evasive** targets (Guard Break does not touch Dodge — bring
  Archer / flank); pure **burst** races (out-scaled); and **kite-heavy** maps with no chokepoint (slow,
  no ranged answer — by design).
