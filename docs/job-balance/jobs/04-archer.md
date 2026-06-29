# 04 — Archer · "The Zone-Control Marksman"

The ranged physical job, reimagined as a **vertical zone-controller**: it dictates engagement range —
kites from high ground, pins by the leg, drops volleys, and returns fire on anyone who shoots it. The
abilities are mined from **GURPS archery** (a primary DCL reference): the Aim maneuver, called shots
(leg-cripple), Rapid Strike, and Wait/overwatch.

This doc records the design decision for the Archer on the Deep Combat Layer (DCL). Mechanics it leans
on are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`).

## Tier & tree position

- **Tier C.** Tier here is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*):
  an early first-rank unlock from the Squire, alongside the Knight (`docs/job-balance/00-job-tree.md`).

## The vanilla problems it solves

Vanilla Archer (`docs/job-balance/vanilla/04-archer.md`) is Tier C as a fielded job but A for its
*Concentration* export — the textbook **stepping-stone, not a destination**: *Aim/Charge* is slow and
telegraphed (the tempo cost almost always beats the payoff), and the real reason to level it is to mine
ignore-evasion. Two fixes:

1. **The charge-trap** — a per-turn attacker out-tempos the vanilla wind-up. The DCL splits the Archer's
   offence into a patient shot (**Aim**) and a fast shot (**Rapid Shot**), so the charge is a *choice*,
   not the only plan.
2. **Mine-don't-field** — **Concentration is narrowed** from a universal accuracy patch into a
   conditional pick, and the Archer is given a real fielded zone-control kit (pin, volley, return-fire,
   high-ground), so you field it instead of mining it.

## Fantasy

The marksman who owns the high ground and the spacing: patient when it aims, fast when it must, and a
genuine threat to anyone — melee *or* caster — who tries to shoot it.

## Chassis

- Moderate PA, low HP · Speed moderate · **Move 4 (kiter)** · Faith none.
- **Brave-choice** (a real lever, two-sided per B9): high Brave = damage / low Brave = keep Dodge.
- **Clothes & Suits** · defence = Dodge + distance (no Parry against its own missiles).
- **2H ranged → no off-hand.**
- Movement **Vantage** — see below.

## Innate — Marksmanship (free, and exported)

The trained eye: on **basic single-target bow / crossbow shots**, the Archer ignores **range and height**
accuracy penalties for free. The Archer has it **innate** (no slot). Per the portability rules
(`docs/deep-combat-layer/15`), Marksmanship is **also a learnable Support** so any ranged splash can shoot
accurately at distance / elevation. It is deliberately narrow: it does **not** ignore **cover**, Dodge,
Block, Parry, DR, Magic-Evade, status contests, or immunity, and does **not** help Arc Volley, Rapid Shot,
Pinning's contest, item throws, spells, or guns. **Aim supersedes it** rather than stacking — Aim is the
charged version that adds effective skill *and* solves cover, so cover stays counterplay unless the Archer
spends the charge. The Archer's moat is free Marksmanship + native Bow A / Crossbow B + the whole Aim
suite + the kiter chassis — not exclusivity (`docs/job-balance/job-design-process.md`).

## Command — Aim

**Core:**

- **Aim** — one ability (no per-weapon variants). Spend a short charge → it adds **effective weapon
  skill** and ignores range / height / cover penalties. **Defence still rolls, armour still applies** —
  it is never a guaranteed hit. The result is naturally weapon-shaped because each weapon's own
  over-cap rules apply: on a bow it reads as range / arc / crit pressure; on a crossbow as straight-line
  / armour pressure. Aim carries **no penetration** of its own — anti-armour means bringing a crossbow
  (its innate armour-divisor, `docs/deep-combat-layer/03`).
- **Arc Volley** — an arcing shot down a line (costed if it is real AoE). Heroic area pressure.
- **Rapid Shot** — **bow-only**: two basic bow shots at a to-hit penalty and reduced per-shot damage;
  each target rolls defence; **no riders, no Pinning Shot, no Arc Volley, no Concentration** (it is
  multi-hit); target reactions trigger at most once. This is the tempo answer to the slow Aim — fast and
  inaccurate vs patient and accurate.

**Tier-2 (costed):**

- **Pinning Shot** — a leg shot → **Immobilize / Don't-Move** (the target still acts and still rolls
  Dodge). The kite-lock / zone-keeper, and the **first of the ≤2 pin doors** (the Chemist's Snare is the
  second; differentiated — Archer = long-range weapon pin with LoS/height; Chemist = short thrown
  reagent). The Squire's Stalwart is immune (Don't-Move).
- **Overwatch** — hold an action and fire at the first enemy entering the firing arc (a Wait/overwatch
  maneuver; needs delayed-trigger machinery, so it lands later).

## R / S / M

- **Reaction — Countershot** — when the Archer is targeted by or included in **any ranged attack,
  including magic**, and the source is visible, in weapon range, and in LoS, it returns a **basic shot**
  (no riders / Aim / Rapid / Pin). It does **not** interrupt or cancel the attack, and triggers **once
  per Archer turn-cycle** (not once per attacker on the map). This makes casters think twice before
  shooting the Archer. *(The magic-trigger portion may itself be Tier-2 if the engine lacks a clean
  trigger; J1 does not depend on punishing spells.)*
- **Support — Concentration** (export, narrowed): ignores **Dodge only** — never Block / Parry /
  Magic-Evade / DR — and applies to **single-target, single-hit weapon attacks only** (not multi-hit,
  AoE/line, Jump, Iaido, item throws, spells, status shots, or reactions); no immunity bypass. A
  conditional pick (dead vs armoured / low-Dodge / casters), not the old universal accuracy patch.
- **Support — Marksmanship** — the innate, learnable (see *Innate*): ignore range + height on basic
  ranged shots.
- **Support — Bow Training** (weapon-proficiency export, `15`): grants **Bow A** to whatever job equips
  it (a single weapon lane per support, so a splash cannot buy the whole arsenal at once).
- **Support — Field Arms** (weapon-proficiency export): grants **Crossbow B + Knife D**.
- **Movement — Vantage** — vertical mobility: **+Jump / climb sheer terrain**, to take and hold
  high-ground perches enemies cannot easily follow onto (high ground is the DCL range / accuracy /
  damage edge).

*(R/S/M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot.)*

## Equipment & weapon aptitude

Pool **8** (`docs/deep-combat-layer/15`, *Weapon aptitude*; mechanic owned by `10`).

| Slot | Grant |
|------|-------|
| Armour | **Clothes & Suits** |
| Off-hand | — (2H ranged) |
| Bow | **A** — PA-scaling, arc, terrain, kiting, soft-target pressure — the Archer's one A |
| Crossbow | **B** — skill-primary, straight line, anti-armour (innate divisor) |
| Knife | **D** — melee fallback |

*(Crossbow tops at **B** — there is no A crossbow-specialist by design; the Archer's single A is the
Bow. The roster's anti-armour answers are elsewhere: Chemist alchemy, magic, Monk crush, Knight
Flail / guard-shred, Mystic Knight.)*

## Early / mid / late

- **Early.** Safe ranged pressure immediately (at-will bow, short Aim, Rapid Shot for tempo).
- **Mid.** The vertical lane-controller — kite, pin, volley, return-fire; the crossbow for whatever
  armours up.
- **Late.** A fielded destination, not a stop — the zone-control marksman; narrowed Concentration still
  helps vs evasive targets but is no longer the sole reason to visit the job.

## Battle dynamics

**What the player does with it.** Field the Archer to **own the spacing**: take high ground (Vantage),
kite at Move 4, and dictate the engagement. Open patient with **Aim** for accurate, cover-piercing hits;
switch to **Rapid Shot** when a mobile target out-tempos the charge; **Arc Volley** a cluster; **Pin** a
runner to hold it for a finisher; **crossbow** whatever armours up. **Countershot** makes enemy casters
and shooters pay for targeting it. As a donor it hands out Concentration (anti-Dodge), Marksmanship
(range/height accuracy), and the weapon lanes (Bow A; Crossbow B + Knife D). *(Sim: Concentration takes a
35%→91% shot vs an evasive target; the crossbow ~doubles bow damage into Heavy via its divisor.)* Note
(grade-budget reconciliation, pending): a higher-PA host with Bow A + Aim out-damages the Archer's raw
shot by ~22%, but only as a single-target sidegrade (no Concentration / Volley / Pin / Countershot, two
slots) — the Archer stays the best *package*.

**How an enemy version harms the player.** An enemy Archer **kites, pins (Immobilize), and returns fire
(Countershot)** from high ground — it punishes you for advancing in the open and for shooting it.
Counterplay: **close under cover** (Marksmanship doesn't ignore cover; only its Aim charge does),
**Stalwart** against the pin, deny its high ground or **flank** it, and **magic / focus** the low-HP body
once you reach it (TTK ~4 in melee).

## J1 — the pick / wrong-pick

- **The pick:** open / vertical maps (Vantage + kite), vs **evasive** targets (Aim / Concentration), vs
  **armour** (crossbow), pinning to hold a finisher's target, and **deterring ranged / casters**
  (Countershot).
- **Wrong pick (two-sided):** cramped melee scrums where the gap collapses (light, low HP, folds), and a
  full Aim into mobile targets (use Rapid Shot instead).
