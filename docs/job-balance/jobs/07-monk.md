# 07 — Monk · "The Self-Sufficient Diver"

The bare-handed bruiser, reframed as a **melee diver that needs no healer**: it dives the scrum,
outlasts a physical brawl, punches through armour with crush fists, and sustains itself and its
neighbours — but it is no longer the do-everything ranged-medic-bruiser of vanilla.

This doc records the design decision for the Monk on the Deep Combat Layer (DCL). Mechanics it leans on
are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`).

## Tier & tree position

- **Tier B.** Tier here is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*):
  mid-tree, unlocked past the Knight (`docs/job-balance/00-job-tree.md`).

## The vanilla problem it solves

The problem here is the inverse of the earlier jobs. Vanilla Monk (`docs/job-balance/vanilla/07-monk.md`)
is a consensus **top job** (tier A, many say S) because it does **everything** in one chassis: highest
HP + near-highest PA + high Speed + high evade; Brawler (full bare-handed, free weapon slot); ranged
fists *and* AoE; and MP-free Chakra / Revive / Purification. The design risk is **omnicapability /
no-strictly-better**, not weakness.

The DCL keeps the Monk powerful and fun but carves **real negative space** so it is excellent without
being all-purpose (see the levers below).

## Fantasy

The ascetic bruiser — the body that does not need a healer: it dives in, takes the hits, heals itself,
and lands crush blows that punch through armour.

## Chassis

- **Highest HP** on the roster (the buffer) · high PA · Speed moderate · Move 4 · Faith none.
- **Brave HIGH** — paid via the active-defence penalty (B9): the HP buffer is the **only** real defence
  (no shield, no Block, poor Dodge), and high Brave makes it **taunt-vulnerable**.
- **Clothes & Suits but effectively NO DR** (unarmed/light — it takes full hits; magic and big single
  blows hurt).
- **No off-hand, no shield, no Block.** Fists = **crush, reach-1**.
- Movement **Sure Footing** — ignore terrain move-penalties; the anti-kite tool (it solves range by
  moving, not by shooting).

## The anti-omnipotence levers (the honest costs)

1. **No DR** → magic and big single hits land in full.
2. **High Brave → −active-defence + taunt-vulnerable** → HP is the only defence.
3. **Range-capped sustain** (self + adjacent / adjacent) → **not a backline medic**; White owns ranged
   team heal.
4. **Reach-1 with no Core ranged answer** → kitable; no free ranged-nuke rotation (kills vanilla
   Shockwave-spam).
5. **Crush fists** → the melee **anti-armour** bruiser, distinct from Knight guard-shred / crossbow /
   Chemist alchemy.

## Innate — Martial Arts (free, and exported)

For the Monk the **innate and the weapon-proficiency export collapse into one** (`docs/deep-combat-layer/15`):
Martial Arts **is** the Monk's built-in weapon (crush, reach-1, no off-hand) *and* its innate, and the
Monk is off the weapon-grade axis, so there is no separate weapon export. The Monk has Martial Arts
**free** (innate, on the best body for it). Per the portability rules it is **also a learnable Support**:
an off-job running **Martial Arts secondary + Martial Arts support** punches with the full unarmed weapon
and its scaling.

The moat is **not** exclusivity — it is the **scaling, the chassis, and the combat-mode cost**:

- **Unarmed damage scales with the holder's Monk job-level**, not its active job (`docs/deep-combat-layer/14`).
  A host that never leveled Monk punches with a noodle fist; matching the native Monk means actually
  grinding Monk to the top — and even then a higher-PA native still edges it.
- **The support occupies the unarmed/no-shield combat mode.** While using Martial Arts the holder has
  **no weapon, no shield/off-hand**, and no Dual-Wield / Doublehand / weapon-proc / Jump / Throw / Iaido
  interaction — so a splasher gives up its own wall (no shield-wall, no Hold Ground line-hold) to borrow
  the fists.
- The Monk gets it **free** (slots open) on the **highest-HP / high-PA** body with **Chakra / Revive
  native** — and the real donor prize is that sustain, not the fists (which a host's own swung weapon
  out-damages into soft targets; borrowed fists only win vs **plate**).

A sturdier or faster body throwing fists is therefore a costly, welcome splash, never a strictly-better
Monk (`docs/job-balance/job-design-process.md`).

## Command — Martial Arts

**Core:**

- **Pummel** — multi-hit unarmed crush; a personal anti-armour burst that **chips** guard by volume but
  does not **strip** it (no overlap with the Knight's one-action Guard Break, which opens a target for the
  whole team — Pummel is self-only burst). Travels with Martial Arts as a secondary; full Pummel scaling
  needs the Martial Arts support (or Monk primary), off-job-without-support gets only weak common-unarmed
  output. No Dual-Wield interaction; target reactions trigger at most once per Pummel action.
- **Chakra** — restore HP (+MP) to **self + adjacent only** — MP-free, range-capped.
- **Revive** — raise an **adjacent** fallen ally (MP-free).
- **Cyclone** — a small crush AoE around the self.

No broad cleanse — *Purification* is omitted; cleanse belongs to White / Chemist / Squire.

**Tier-2:**

- **Earth Wave** — a short-line crush wave: reduced damage, no riders, cannot hit elevated targets,
  strictly worse than closing. A limited pressure valve, never a ranged rotation — Core Monk has no free
  ranged answer and reach-1 stays a real weakness.

## R / S / M

- **Reaction — Adrenaline Rush** (export): when damaged or starting a turn below a HP threshold, gain a
  short self-buff (+PA or a Speed/CT nudge) for ~1 turn — non-stack, capped at one visible tier,
  **resets after battle**. **No permanent stat growth** (no farm-the-hits grind, no in-fight runaway
  snowball). Portable: bruisers want it, backliners skip it, and the reset keeps it honest off-job.
- **Support — Martial Arts** (export, full — the innate, see *Innate*): grants the unarmed weapon family
  and its scaling, but the scaling reads the **holder's Monk job-level** (the moat — a low-Monk host
  punches weakly), and it forces the **unarmed / no-shield combat mode** (no weapon, no shield/off-hand,
  no Dual-Wield / Doublehand / weapon-proc / Jump / Throw / Iaido). It affects unarmed attacks and Monk
  techniques only — never a generic "make my whole kit stronger" support.
- **Movement — Sure Footing.**

*(R/S/M is a **set**, not one-of-each: one reaction, one support, one movement — equip one per slot.
The Monk's set is lean because its innate, weapon, and one of its supports are the same thing.)*

## Equipment & weapon aptitude

**Off the weapon-grade axis** — the Monk fights unarmed, and its fists scale with the **Monk job-level**
(`docs/deep-combat-layer/14`), not a weapon grade. **No shield / no Block.** Armour: Clothes & Suits,
but with effectively no DR.

## Early / mid / late

- **Early.** Already a strong self-sufficient bruiser (HP, fists, in-scrum Chakra).
- **Mid.** The scrum diver — Pummel crush (anti-armour), Cyclone, front-line sustain, clutch adjacent
  Revive.
- **Late.** Still excellent *in the brawl*, but with real negative space — kited, burned by magic, no
  backline utility. Powerful, not omnipotent.

## Battle dynamics

**What the player does with it.** Field the Monk as the **healer-free diver**: close on the scrum (Sure
Footing beats kite-terrain), soak on the HP buffer, **Chakra** yourself and a neighbour mid-fight, and
**crush** through whatever armours up (fists ignore plate's weakness — *Sim: 45/hit into Heavy vs a
walled Sword's 21*). Clutch **adjacent Revive** means a wipe-threat line keeps standing with no backline
medic. As a donor the prize is that **MP-free sustain** (Chakra/Revive) on a body that didn't bring a
White Mage — and the **Martial Arts support** for a crush option, though a host's own swung weapon
out-damages borrowed fists into soft targets, so it's a situational anti-armour pick, not a free upgrade.

**How an enemy version harms the player.** An enemy Monk **dives your backline, self-heals, and crushes
your plate** — focus-firing it is slow because of the HP buffer + Chakra. Counterplay leans on its honest
negative space: **kite it** (reach-1, no ranged answer), **magic-burst** it (no DR — *Sim: a spell TTKs
it in ~5*), **taunt** it (high Brave), or **charm / confuse / fear** it (high Brave, and it can't ignore
the flags). An enemy armoured-martial splash has to drop its shield to punch, so answer it like any
bruiser.

## J1 — the pick / wrong-pick

- **The pick:** self-sustaining **in the scrum** — diving and outlasting a physical brawl, **anti-armour**
  fights (crush), no-MP teams, long fights (Adrenaline Rush).
- **Wrong pick (two-sided, explicit):** ranged focus-fire; **magic burst** (no DR); taunt / kite;
  **charm / confuse / fear** (high Brave plus the control flags it cannot ignore); and maps where
  **adjacency is hard** (its sustain and its reach both evaporate).
