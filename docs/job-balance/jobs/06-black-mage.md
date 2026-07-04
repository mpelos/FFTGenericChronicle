# 06 — Black Mage · "The Arsenal"

The offensive caster, rebuilt as the roster's **damage problem-solver**. Its power is not one big shell — it
is the **widest attack spellbook on the board** and the skill to read which tool the board wants: which
**element** (exploit the Zodiac, or go non-elemental), which **tier** (each a distinct cost / charge / area
profile), which **shape**, and **where** to place it. Two throughlines anchor the breadth: Black is **the
anti-armor answer** (magic ignores physical DR) and **the burst ceiling**. It is no longer the glass cannon
whose wind-up gets it killed before the blast lands — the charge is now a readable commitment, not a lottery.

This doc records the design decision for the Black Mage on the Deep Combat Layer (DCL). Mechanics it leans on
are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier C.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a
  **first-rank** unlock reached directly off a base job (`docs/job-balance/00-job-tree.md`), the offensive
  sibling to the White Mage (`docs/job-balance/jobs/05-white-mage.md`). An **accessible, export-rich** job — it
  earns its keep early *and* lends Rod Attunement, the Black Magic secondary, and Rod A to other builds.

## The vanilla problems it solves

Vanilla Black Mage (`docs/job-balance/vanilla/06-black-mage.md`) is the iconic glass cannon held to tier B
(drifting to C the faster the fight) — "reliability, not power, is the limiter." Four concrete feel-bad
problems are fixed, each mapped to a design move:

1. **A wind-up that gets you killed or wasted.** Vanilla's core flaw: long charge times let fast enemies leave
   the blast or kill the caster mid-cast — "the wind-up, not the damage, is what fails," so Short Charge is
   near-mandatory just to function. **Fix:** in the DCL a charge is **not interrupted by damage**
   (`docs/deep-combat-layer/11`) — it resolves unless the caster is KO'd/incapacitated or hit by a dedicated,
   Brave-resisted interrupt skill. The charge becomes a **readable positional telegraph** (the enemy's counter
   is to *move*, or to KO the body), not a survival lottery — and Black no longer has to borrow Short Charge to
   exist (that lane stays the Time Mage's).
2. **Tiers that converge late.** With FFT's additive-ish scaling, only the top -ga is ever worth casting at
   high MA — the lower tiers become dead JP. **Fix:** DCL magic is **multiplicative and spell-centric**
   (`docs/deep-combat-layer/11`) — the Fire : Firaga ratio is constant at *every* MA, and each tier is a
   distinct **profile** (basic = cheap/fast/single or tiny; -ra = flexible mid; -ga = cluster burst, long CT,
   high MP, placement-risky). Tier is a per-board **choice** the whole game, never a ladder.
3. **Out-scaled late.** Vanilla Black is eclipsed by summons and the instant-cast Calculator. **Fix:** the
   Calculator is replaced by the Necromancer (`docs/job-balance/00-job-tree.md`), removing the instant map-nuke
   that obsoleted it; and **magic ignores physical DR** (`docs/deep-combat-layer/11`) gives Black a *permanent*
   niche no other job fills — it is the answer to plate (SIM 3).
4. **Damage walled by resist/absorb, with no out.** Vanilla element-gating turns a resistant or absorbing foe
   into a dead matchup. **Fix:** the element-gate becomes a **read**, not a wall — exploit a Zodiac weakness
   with the matching element, or fall back to **Flare** (non-elemental, resist-proof) for the target that must
   die (SIM 6); and the free Rod bolt means a depleted Black is never a dead turn.

## Fantasy

The arsenal: a robed magus who answers every board with the right destruction. Plate that laughs at swords is
just a target to a fireball; a clustered formation is a Firaga waiting to happen; a fire-immune horror dies to
Flare; the one unit that must not live eats a Death spell. It commits — plants a telegraphed cataclysm and
dares the enemy to be somewhere else when it lands — and it pays for that power with a body that dies the
instant anything reaches it.

## Chassis

- **Highest MA in the roster** · **HP ~75** · Speed **moderate** · **Move 3** · **HIGH Faith** · **low Brave**.
- **Armour: Robes** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the mage's robe; the mod draws
  no new art). Robes carry **~no physical DR** — the intended glass body: it lives behind the line or it dies
  (thief dive TTK ~1.4, SIM 4).
- **High Faith = the deliberate two-sided counter.** It powers the biggest magic output on the board, but
  Faith enters the magic contest **twice** (`docs/deep-combat-layer/08`, `docs/deep-combat-layer/11`): the same
  high Faith makes Black take **×1.30 magic** and **succumb to magical statuses** (`docs/deep-combat-layer/13`,
  magical status resists on *inverse* Faith). An enemy nuke one-shots it (SIM 4) — the mage-kills-mage mirror.
  Its only magic defence is **Magic-Evade** (built from robes/gear, capped ~50%, `docs/deep-combat-layer/11`) —
  a coin-flip, never a wall.
- **Off the weapon axis:** low PA — its damage is **magic**, not melee. The caster weapon is the **Rod** — a
  free, range-3, MA-scaled **elemental** bolt whose element is set by the equipped Rod SKU
  (`docs/deep-combat-layer/11`, the weapon-bolt floor).
- **Low Brave** fits the backline and resists interrupt poorly is *not* the point — Brave does not touch magic
  output (`docs/deep-combat-layer/11`); but low Brave is a **mental-status** vulnerability. With low HP
  (physical) and high Faith (magical), Black is **disruptable on all three status axes**
  (`docs/deep-combat-layer/13`).

## Innate — Rod Attunement (free, and exported)

The discipline of a committed elementalist: **Rod Attunement** makes the Black Mage's **equipped-element**
work go further — a **stronger Rod bolt** and **cheaper *basic*-tier** casts of the matching element. It
touches **only the floor and the basic tier** — never -ra, -ga, Flare, or Death; **no charge-time cut**; **no
raw burst amplification** (SIM 2). So it smooths *sustain* in your specialty and beefs the *anti-armor chip*,
while the burst budget — the real brake — stays untouched.

It is a **pre-battle commitment with a built-in two-sided cost**: a Fire rod means a better, cheaper fire
plan, and a *worse* day against a fire-resistant board, where your cheap casts crater and you must pay
off-element or go to Flare (SIM 2, SIM 6). You main one element and **splash** the others for coverage — an
arsenal with a specialty, not a mono-caster.

Per the portability rules (`docs/deep-combat-layer/15`), it is **also a learnable Support**, the Black analogue
of the White Mage's Liturgy (`docs/job-balance/jobs/05-white-mage.md`): the **Black Magic command** travels
(any caster can slot it), but the matching-element economy needs the support. The moat is **not** exclusivity —
it is getting Attunement **free** on the **highest-MA, high-Faith, Rod-A** chassis with the primary command
slot open. *(If sim later shows even the floor/basic discount loosens the budget brake, the fallback is a pure
Rod/spellbook proficiency innate — Hypothesis, `docs/deep-combat-layer/12`.)*

## Command — Black Magic

**The design thesis (why Black is allowed to be the best damage caster):** its strongest choices are
**self-limiting by context** — -ga is *inefficient* into one target, an element read punishes autopilot, burst
MP is finite, the charge telegraphs placement, friendly fire is load-bearing (below), and the body dies when
reached. Power is fine because every spell has a *wrong* board.

The **always-on action** is the **free Rod bolt** — a range-3, MA-scaled elemental Attack
(`docs/deep-combat-layer/11`), weak but never a dead turn and, because it ignores DR, the **anti-armor
chipper** even at empty MP (SIM 3). The MP **budget** gates the bursts above it; the decision is *which bursts
this fight* (SIM 7).

**Core — the elemental ladder (three elements × three tiers, each a distinct profile):**

- **Fire / Blizzard / Thunder (basic)** — cheap, fast, single-target or a tiny area. The **most MP-efficient**
  choice and the everyday workhorse (SIM 1).
- **Fira / Blizzara / Thundara (-ra)** — the flexible mid: a small area at moderate cost/CT.
- **Firaga / Blizzaga / Thundaga (-ga)** — the cluster burst: bigger area, **long CT, high MP**, highest
  placement risk. It is the **worst** pick into a single target (dmg/MP 5.2 vs basic 7.8, SIM 1) and only earns
  its cost at **k ≥ 2** — a cluster tool, never the single-target button.

The three elements interact with the target's **Zodiac** (weakness ×1.30 / resist ×0.70,
`docs/deep-combat-layer/09`) — exploiting a weakness is the highest damage on the board; a resistant board is
where the read bites (SIM 6).

**Tier-2 (costed):**

- **Flare** — heavy **non-elemental**, single-target reliability: it ignores Zodiac entirely
  (`docs/deep-combat-layer/09`), so it is the answer to a resistant or **absorbing** foe — the "this one must
  die" button, at **high MP/CT and NO AoE** so it never becomes the everyday spell (SIM 6).
- **Death** — magical instant-KO, run as a **3d6 status** on *inverse* Faith (`docs/deep-combat-layer/13`): the
  devout are vulnerable, the atheist resists, and **bosses/immunes shrug it** (SIM 9). Single-target,
  expensive, swingy — a **late cruelty button**, never a rotation.

**Identity wall vs the White Mage (canon, `docs/job-balance/jobs/05-white-mage.md`):** Black **owns** the
ladder + AoE + element play + burst; White's offensive ceiling is **one** basic-Fire-tier single-target Holy
with no ladder and no AoE. White tops out exactly where Black *starts* (Holy 63 ≈ Black's basic Fire 63), then
Black keeps climbing (SIM 8). **Boundary vs the future Summoner:** Black is the **faster, flexible, smaller**
AoE + single-target burst; the **big, slow, committed barrage** is reserved for the Summoner — do not let
Black's -ga grow into that space. **Boundary vs the future Time Mage:** Black is the damage **arsenal** and
the burst **ceiling**; **the clock and the HP-axis belong to the Time Mage** — CT/Speed manipulation
(Haste/Slow/Stop/Quick), Gravity / %-HP softening (DR/Faith-independent), Reflect/Float/Teleport, and any
Meteor-style delayed prediction spell. The Time Mage also carries **one** modest single-target non-elemental
nuke (**Comet**) as its **minimum direct offense — not a damage plan**: it sits below Black's exploited-element
burst, below Flare, and never gains AoE, a tier ladder, weakness spikes, or Rod Attunement efficiency. So
Black stays **the** damage caster (volume, flexibility, AoE, the anti-armor arsenal, the Flare finisher) while
Time stays support/control-primary with just enough offense to be self-sufficient — the two never compete on
the same axis. **Flare remains Black's** premium resist-proof finisher.

> **System dependency (Hypothesis, owned by `docs/deep-combat-layer/11`):** Black's large AoE assumes
> **friendly fire** — an area blast damages your *own* units in it (the FFT-native default), which is the
> load-bearing brake on "blanket the scrum with -ga" (SIM 5). Doc 11 does not yet state it; this must be
> **confirmed/promoted there**. If the DCL rejects friendly fire, the -ga **radius / CT / MP must be re-simmed
> immediately** against the no-FF backup brakes (inefficient at k=1, modest radius, long CT, per-target
> Magic-Evade).

## R / S / M

- **Reaction — Rod Counter** — when the Black Mage **survives a direct hostile action** and the source is in
  Rod-bolt range/LoS, retaliate with **one basic Rod bolt** (`docs/deep-combat-layer/11`). It returns *only*
  the bolt — **no spell-copy, no AoE, no status** (full Magick Counter would be a free burst); it does **not**
  trigger from reactions, damage-over-time ticks, field ticks, or self-damage; **once per attacker action**;
  requires a caster weapon; **Magic-Evade applies**. It gives the glass body a little bite against the divers
  that kill it, without making it durable. Portable but naturally gated (only useful holding a Rod and wanting
  a modest MA bolt back).
- **Support — Rod Attunement** (the innate, learnable — see *Innate*): exports the matching-element
  floor/basic economy to whatever job pairs it with a Black Magic secondary.
- **Support — Rod Training** (weapon-proficiency export, `docs/deep-combat-layer/15`): grants **Rod A** — the
  caster weapon and its range-3 elemental bolt — to whatever job equips it (one weapon lane per support).
- **Movement — none (resolved).** Float is the **Time Mage's** command utility (a status spell) and Teleport
  is the Time Mage's movement export (`docs/job-balance/jobs/10-time-mage.md`) — neither is Black's. Black
  takes **no signature movement** on purpose: free repositioning would erase its "dies when reached" weakness,
  and the only "safe" caster movement (pure terrain utility) is Time's. In a build it borrows a generic
  movement; the slot is intentionally empty, not padded.

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot;
a job need not define every slot.)*

## Equipment & weapon aptitude

Pool from `docs/deep-combat-layer/15` (*Weapon aptitude*; mechanic owned by `docs/deep-combat-layer/10`),
spent **lean** — a caster, not a weapon user:

| Slot | Grant |
|------|-------|
| Armour | **Robes** (sprite-mandated — ~no physical DR) |
| Off-hand | **none** — no shield; it is a backline caster |
| Rod | **A** — the caster weapon: the free range-3 MA-scaled **elemental** bolt, and the spell-delivery weapon |

*(No martial weapons — its damage is **magic**, not melee. One A (Rod), per "one A below the capstone tier",
`docs/deep-combat-layer/15`. The underspent pool is deliberate: the Black Magic command, not a weapon, is the
job.)*

## Early / mid / late

- **Early.** Already a complete, fieldable attacker: the basic elemental ladder, the free Rod bolt (so a turn
  is never wasted, and plate is never safe), and Rod Attunement smoothing your chosen element. No dependence on
  a late unlock, no dead tier ladder.
- **Mid.** The board-reader comes online: exploit a Zodiac weakness with the right element, drop a -ra/-ga on a
  cluster (placed off your own line), and chip plate the rest of the team can't crack.
- **Late.** A destination by matchup — the **anti-armor and burst answer**: delete a plate wall, blow up a
  clustered formation, **Flare** the fire-immune horror, and **Death** the one unit that must not live. Not the
  big committed barrage (Summoner), not support (White) — the surgical artillery.

## Battle dynamics

**What the player does with it.** Field the Black Mage to **solve damage problems the rest of the team can't**:
read the board and pick the **element** (exploit Zodiac, or Flare a resistant target), the **tier** (basic to
sustain, -ga to punish a cluster), and the **placement** (a -ga that catches enemies but **not your own line** —
friendly fire is real). Open plate with magic where swords bounce, manage the **MP budget** (burst-then-floor,
or stretch the cheap tier), and fall to the free bolt when empty. As a donor it lends **Rod Attunement + Black
Magic** (an anti-armor/element package to an MA-statted job) and **Rod A** — but a low-MA, low-Faith host
brings the breadth at low magnitude.

**How an enemy version harms the player.** An enemy Black Mage **ignores your armour** (plate is no defence)
and **blows up your clusters** — and on a high-Faith target it one-shots. Counterplay is clear and
multi-layered: **close on it** — it is robes, low HP, and folds to a single diver (TTK ~1.4, SIM 4); **spread
out** so its -ga catches one unit, not three (the positional game, `docs/deep-combat-layer/11`); **move during
its telegraphed charge** so the blast lands on empty tiles; **build Magic-Evade** (a coin-flip save) and field
**low-Faith** bodies (they take ×0.70 and resist its Death); and **interrupt** it with a dedicated skill
(Brave-resisted) or simply KO it before the cast resolves.

## Two-sided cost (why it is not strictly-better)

- **Dies when reached** — robes, low HP, no shield: a single diver folds it (SIM 4). Its safety is **position**,
  which a mobile attacker takes away.
- **Magic glass cannon** — high Faith means **×1.30 magic taken** and vulnerability to **magical statuses**
  (`docs/deep-combat-layer/08`, `13`); Magic-Evade (capped ~50%, `11`) is a coin-flip, not a wall.
- **The charge is a commitment** — it resolves through damage, but it **telegraphs placement** (the enemy
  relocates out of the blast), and a **KO before it resolves still eats the cast**: the fragile body is the
  timer.
- **-ga is self-limiting** — the cluster burst is the **worst** single-target pick (SIM 1) and, with **friendly
  fire**, cannot be blanketed into a mixed scrum without hitting your own units (SIM 5).
- **Element-gated** — a resistant/absorbing board craters the cheap plan and forces the costly Flare or
  off-element (SIM 6); a committed rod-element is a bet that can be punished (SIM 2).
- **Finite burst** — the MP budget is a per-battle pool; out of it, Black falls to the bolt floor (still
  anti-armor, but no burst) (SIM 7).
- **Pure offense** — no heal, no ward, no sustain beyond the bolt, no hard control (Death is a swingy,
  boss-immune Tier-2; Toad and Poison are *not* Black's — they belong to the Mystic/Oracle and Necromancer
  lanes).

Distinct from **White Mage** (support + miracle recovery; single-target basic-Fire offensive ceiling —
`docs/job-balance/jobs/05-white-mage.md`), the future **Summoner** (the big slow committed barrage), the future
**Mystic/Oracle** (disable/control), the future **Necromancer** (attrition/state/undead), and the future
**Time Mage** (tempo control + %-HP gravity + one modest Comet for self-sufficiency — the clock and the
HP-axis, not the damage arsenal). Black is the **immediate-damage, anti-armor, flexible-burst** caster —
damage *now*.

## J1 — the pick / wrong-pick

- **The pick:** **high-DR / plate** enemies (magic ignores armour — the answer no one else has),
  **Faith-normal priority targets** to burst, **small/medium clusters** where element/shape/placement pay off,
  **element-exploitable** boards (a Zodiac weakness to hammer), and the **one target that must die** (Flare /
  Death). Slow or boxed-in enemies that can't leave the telegraph are ideal.
- **Wrong pick (two-sided):** a **fast melee-dive** roster (it folds before it casts), a **magic-heavy** enemy
  (the high-Faith body is the most magic-vulnerable you can field), a **resistant/absorbing or element-poor**
  board (the read collapses to costly Flare), a **spread, mobile** enemy (no clusters, and they walk out of
  every -ga), and **your own units in the scrum** (friendly fire — you can't blanket safely).
