# 05 — White Mage · "The Threshold Keeper"

The dedicated white-magic healer, rebuilt as the job that owns **catastrophic thresholds** — the moment a
fight crosses into mass KO, a severe or magical status spike, a magic burst, undead, or a boss's big turn.
It **pre-empts** those moments with Faith-independent wards and timed Reraise, and **recovers** from them
with Faith-scaled magnitude: big heals, mass and area revival, severe cleanse, plus a single modest Holy. It
is no longer the safe-corner heal-bot whose curing three other jobs already cover — it is the miracle-scale
disaster job, with a real offensive turn so it is never dead weight at the front.

This doc records the design decision for the White Mage on the Deep Combat Layer (DCL). Mechanics it leans on
are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier C.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a
  **first-rank** unlock reached directly off a base job (`docs/job-balance/00-job-tree.md`). It is an
  **accessible, export-rich** job — it earns its keep by being useful *and* by what it lends to other builds
  (Liturgy, the White Magic secondary, Staff A), not by being a deep destination.

## The vanilla problems it solves

Vanilla White Mage (`docs/job-balance/vanilla/05-white-mage.md`) is "indispensable in the abstract" yet
held to tier B because its healing overlaps cheaper jobs and it contributes only from a safe corner. Four
concrete feel-bad problems are fixed, each mapped to a design move:

1. **Raw healing that three jobs already do.** Cure overlaps Chemist potions and Monk Chakra — its curing
   "isn't unique, so it competes for its own slot." **Fix:** White's healing is **Faith-scaled magnitude +
   party-scale**, on its own axis — it out-heals on devout allies and at area scale, and it **cedes the
   low-Faith and Faith-proof corner to the Chemist** via the heal-tax (`docs/job-balance/jobs/02-chemist.md`,
   SIM 1). Not a third HP-copy; a different recovery axis.
2. **The 100%-support dead turn.** A vanilla healer with nothing to heal wastes the turn and can't be fielded
   near the front — the classic support trap. **Fix:** a **minimum offensive kit** — the free range-3 Staff
   bolt floor plus one modest Holy — so the White Mage always has a usable action and can take a forward turn.
3. **Wards that don't engage Faith, healing that does — backwards.** Vanilla heals are Faith-gated (so a
   low-Faith bruiser heals poorly) while the buff suite is the part that actually carries the job. **Fix:** the
   design **leans into that split deliberately** — magnitude heals scale on Faith, but the **ward suite is
   Faith-INDEPENDENT** (`docs/deep-combat-layer/11`), so the White Mage protects the very low-Faith allies it
   *cannot* efficiently heal by wrapping them in Protect/Regen instead.
4. **Reraise as a pre-battle "I win" blanket.** Vanilla Reraise is best-in-class precisely because you
   pre-cast it and a death simply undoes itself. **Fix:** Reraise becomes a Tier-2, single-target, high-MP,
   charge-timed, **short-duration** insurance — "a death is coming" cover, not a mandatory splash that
   trivialises dying.

## Fantasy

The threshold keeper: a devout cleric who reads the fight for the moment it is about to break, and stands in
that gap. Before the catastrophe, it wards — Protect, Shell, Wall, a timed Reraise on the unit about to be
focused. When the catastrophe lands anyway, it answers at miracle scale — mass revival, a severe cleanse, a
heal that fills a devout ally outright — and turns its faith outward as a single shaft of Holy light against
the wicked and the undead. Its power is its devotion, and its devotion is also why it dies: open to the same
holy and unholy forces it channels.

## Chassis

- **Low HP (~75)** · **high MA** · **HIGH Faith** · Speed **moderate–high** · **Move 3** · **low Brave**.
- **Armour: Robes** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the cleric's robe; the mod
  draws no new art, so the chassis must match the art). Robes carry **~no physical DR** — the intended glass
  body: it lives behind the line or it dies.
- **High Faith = the deliberate two-sided counter.** Devotion is the engine of its Faith-scaled magnitude
  (heals and Holy land hard, `docs/deep-combat-layer/08`) — but Faith enters **both** sides of the magic
  contest, so the same high Faith makes the White Mage take **×1.30 magic damage** and **succumb to magical
  statuses** (`docs/deep-combat-layer/13`, magical statuses resist on *inverse* Faith). An enemy burst caster
  near-one-shots it (SIM 3). Its only magic defence is **Magic-Evade** (built from robes/anti-magic gear,
  capped ~50%, `docs/deep-combat-layer/11`) — a coin-flip, never immunity.
- **Off the weapon axis** (like the Chemist, `docs/job-balance/jobs/02-chemist.md`): low PA, its damage is
  **magic**, not the staff's melee. The caster weapon is the **Staff** — a free, range-3, MA-scaled bolt
  (`docs/deep-combat-layer/11`, the weapon-bolt floor).
- **Low Brave** puts it in the backline and gives it a fitting Caution-category reaction (`13`) — but it is
  also a **mental-status** vulnerability. Combined with low HP (physical) and high Faith (magical), the White
  Mage is **disruptable on all three status axes** (`docs/deep-combat-layer/13`).

## Innate — Liturgy (free, and exported)

The discipline that makes a ward-and-recover game affordable: **Liturgy** makes the White Mage's white-magic
casts **cheaper and faster** — reduced MP cost and shorter charge time. It is the engine of the whole kit:
the reason the White Mage can stack the ward suite *and* still hold MP for a big heal, a revive, or a Holy.
The White Mage has it **free** (innate).

Per the portability rules (`docs/deep-combat-layer/15`), it is **also a learnable Support**. The **White
Magic command** itself travels (any caster can slot it as a secondary), but **without Liturgy that off-job
white magic is full price and full charge** — only the discounted, accelerated version needs the support. So
an off-job healer pays **two slots** (White Magic secondary **+** Liturgy support) for the economy the White
Mage gets native, and still brings its own MA and Faith. This parasitic-innate-export pattern is the same one
the Geomancer's Landreader and the Dragoon's Aerial Training use
(`docs/job-balance/jobs/11-geomancer.md`, `docs/job-balance/jobs/12-dragoon.md`): the support is the *key*
that unlocks the secondary command's full function, not a stand-alone effect.

The White Mage's moat is **not** exclusivity — it is getting Liturgy **free** on a **high-MA, high-Faith,
Staff-A, robes** chassis with the primary command slot open. A splashed Liturgy + White Magic on a low-MA,
low-Faith host brings the *utility* at low *magnitude* (small heals, weak Holy) — a welcome splash, never a
strictly-better White Mage (`docs/job-balance/job-design-process.md`).

## Command — White Magic

The **always-on action** is the **free Staff bolt** — a range-3, MA-scaled white-light bolt
(`docs/deep-combat-layer/11`, the weapon-bolt floor), weak (~29% of a Black Mage Firaga, SIM 4) but never a
dead turn: with nothing to heal, the White Mage still contributes a ranged poke. No white spell below
"replaces the bolt with a better free attack"; the bolt is the floor, and **Holy** is the single costed smite
above it.

**Core:**

- **Cure** — a Faith-scaled single-target heal. **Magnitude, not flat triage:** on a high- or neutral-Faith
  ally it **out-heals** the Chemist's flat potion (and Tier-2 scales to area), but the **target-Faith heal
  tax** (target Faith scales healing *received*, `docs/deep-combat-layer/08`, `11`) drops it **below** the
  Chemist's flat Hi-Potion on a **low-Faith** ally (SIM 1). The White Mage owns devout-ally and party-scale
  recovery; the Chemist/Monk own the low-Faith and Faith-proof corner.
- **Raise** — a Faith-scaled, big-HP revive: the **disaster door**, distinct from the Chemist's flat,
  Faith-proof, cheap, instant Phoenix Down (`docs/job-balance/jobs/02-chemist.md`). Two doors, not the magic
  copy (SIM 5).
- **Esuna** — the proactive **severe / magical cleanse**: it clears the heavy and magical statuses
  (`docs/deep-combat-layer/13`), deliberately distinct from the Chemist's cheap **common**-status items. The
  answer to a status spike before it compounds.
- **Protect / Shell / Regen (single-target)** — the **ward suite**, and it is **Faith-INDEPENDENT** (buffs
  are magnitude + duration, friendly, no-resist, **not** Faith-scaled, `docs/deep-combat-layer/11`). This is
  *how* the White Mage helps a **low-Faith** bruiser it can't efficiently heal: it can't out-heal a 0-Faith
  tank, but it can wrap it in Protect + Regen, which land on anyone. (Calibration — Regen-per-turn must stay
  **below one committed warded attacker's chip**, `docs/deep-combat-layer/12`, *Hypothesis* — else a buffed +
  Regen body is unkillable by a single attacker; SIM 2. The guardrails are structural: wards/Regen are
  **durations** that expire, the party-wide versions are **Tier-2**, and the White Mage itself is killable.)
- **Holy** — the one costed offensive turn: a **single-target spiritual smite** that, being spiritual,
  scales on Faith twice and **ignores both physical DR and Zodiac** (`docs/deep-combat-layer/08`,
  `docs/deep-combat-layer/09`), and is **Magic-Evadable**. It is calibrated to **basic single-target Black
  "Fire" tier** (~43% of a Firaga burst, SIM 4) — and that is the **ceiling, not the target**: because Holy
  ignores Zodiac it is *more consistent* than Fire, so it must never *exceed* basic Fire power
  (`docs/deep-combat-layer/12`, *Hypothesis*). It doubles as the roster's clean **anti-undead** answer. Its
  MP competes directly with the support kit — smiting is a turn *not* spent warding.

**Identity wall vs the Black Mage (hard rule).** Holy has **no tier ladder (no Holyra / Holyga) and no AoE,
ever.** White's offensive ceiling stays one basic single-target smite; **scaling burst, AoE, elemental play,
and the burst chassis are the Black Mage's lane** — never the White Mage's. The minimum-offense kit exists so
the White Mage is fieldable, not so it becomes a second damage caster.

**Tier-2 (costed):**

- **Curaja** — an **area heal**: party-scale recovery the Chemist cannot match.
- **Arise / Raiseja** — **mass / area revive**: the disaster door at scale.
- **Protectja / Shellja / Wall** — **party / combined** mitigation. The full stack is a hard wall, but a
  *costed* one — it takes many turns and a large slice of the MP budget to raise, and it is overwhelmed by
  focus-fire (2–3 attackers beat ward + Regen, SIM 2).
- **Reraise** — single-target, **high-MP**, charge-timed, **short duration**: "a death is coming" insurance,
  **not** a pre-battle blanket. Its duration is the hardest knob — long enough to feel real, short enough to
  never be a mandatory splash (`docs/deep-combat-layer/12`, *Hypothesis*). This is the deliberate de-power of
  the vanilla best-in-class Reraise.

## R / S / M

- **Reaction — Regenerator** — gain **Regen when hit** (Caution category, low-Brave, `docs/deep-combat-layer/13`):
  keeps a squishy back-liner alive through chip. **Portable** but naturally gated — it wants a body that sits
  in chip range and has the low Brave to fit a Caution reaction — so a splash brings a situational version,
  not a free auto-include.
- **Support — Liturgy** (the innate, learnable — see *Innate*): exports white-magic **economy** (cheaper /
  faster casts) to whatever job pairs it with a White Magic secondary.
- **Support — Staff Training** (weapon-proficiency export, `docs/deep-combat-layer/15`): grants **Staff A** —
  the caster weapon and its range-3 MA bolt — to whatever job equips it (one weapon lane per support).
- **Movement — Move-MP-Up** — regenerate MP by moving: tops up the per-battle MP budget just by
  repositioning, strong on a long-fight healer that lives off its MP pool.

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per
slot.)*

## Equipment & weapon aptitude

Pool from `docs/deep-combat-layer/15` (*Weapon aptitude*; mechanic owned by `docs/deep-combat-layer/10`),
spent **lean** — a caster, not a weapon user:

| Slot | Grant |
|------|-------|
| Armour | **Robes** (sprite-mandated — ~no physical DR) |
| Off-hand | **none** — no shield (no Block); it is a backline caster |
| Staff | **A** — the caster weapon: the free range-3 MA-scaled bolt, and the Holy/heal delivery |

*(No martial weapons — its damage is **magic** (the bolt and Holy), not melee. One A (Staff), per "one A
below the capstone tier", `docs/deep-combat-layer/15`. The underspent pool is deliberate: the White Magic
command, not a weapon, is the job.)*

## Early / mid / late

- **Early.** Already a complete support and a fieldable unit: Cure, the single-target ward suite, Esuna, and
  the **free Staff bolt** so a forward turn is never wasted. It protects a low-Faith bruiser with Protect +
  Regen and tops up devout allies — no dependence on a late unlock.
- **Mid.** The threshold keeper comes online: **pre-empt** a known burst with Shell/Wall, **cleanse** a
  status spike with Esuna, **revive** at big HP with Raise, and turn a spare turn into a Holy smite (or the
  anti-undead answer).
- **Late.** A destination by matchup — the **disaster job** for fights that cross catastrophic thresholds
  (mass KO, severe status, magic burst, undead, a boss spike): Curaja, Arise/Raiseja mass revival, party
  Wall, and a timed Reraise on the unit about to be focused. Not flat triage (Chemist), not self-sustain
  (Monk) — miracle-scale recovery and pre-emption.

## Battle dynamics

**What the player does with it.** Field the White Mage to **pre-empt and recover thresholds**. Read the
incoming burst and **pre-cast** Protect/Shell/Wall — Faith-independent, so they land on the low-Faith tanks
your heals can't reach — and a timed Reraise on the unit about to be focused. Between support turns, keep the
**free Staff bolt** firing so no turn is dead. When the catastrophe lands, answer at scale: big-HP **Raise**
or mass-revive, **Esuna** a severe status, fill a devout ally with **Cure/Curaja**, and spend the occasional
turn on **Holy** (especially into undead). As a donor it lends **Liturgy + White Magic** (party Protect/Shell
or an emergency Raise to a durable job) and **Staff A** — but a low-MA, low-Faith host brings the utility at
low magnitude.

**How an enemy version harms the player.** An enemy White Mage **wards its own line** (Protect/Shell/Wall)
and **un-kills your kills** (Raise/Reraise), dragging the fight long. Counterplay is clear and multi-layered:
**focus-fire the White Mage itself** — robes, low HP, it folds to a dive (Thief TTK ~1.4, SIM 3,
`docs/job-balance/jobs/08-thief.md`); **burst it with magic** (high Faith → ×1.30 taken, near one-shot, SIM
3); **Silence / Interrupt / pressure its MP** (charges and a finite MP budget, `docs/deep-combat-layer/11`,
`docs/deep-combat-layer/13`); and **overwhelm the turtle** (2–3 attackers beat ward + Regen, SIM 2). Do not
try to out-damage a single warded + Regen body head-on — break the caster instead.

## Two-sided cost (why it is not strictly-better)

- **Physically fragile** — robes, low HP, no shield: folds to a fast physical dive (SIM 3). Its safety is
  **position**, which a mobile attacker can take away.
- **Magic glass cannon** — high Faith means **×1.30 magic taken** and vulnerability to **magical statuses**
  (`docs/deep-combat-layer/08`, `13`); Magic-Evade (capped ~50%, `11`) is a coin-flip, not a wall.
- **Disruptable on all three status axes** — low Brave (mental), low HP (physical), high Faith (magical),
  `docs/deep-combat-layer/13`.
- **The heal-tax cedes the low-Faith corner** — on a low-Faith ally its Cure falls **below** the Chemist's
  flat potion (SIM 1): for a low-Faith party, bring a Chemist.
- **MP is a budget and charges can be cut** — the big spells draw on a finite per-battle MP pool over the
  free bolt floor and have CT windows; Silence/Interrupt/MP-pressure shut the premium kit off
  (`docs/deep-combat-layer/11`).
- **Minimal offense by design** — the Staff bolt is weak (~29% of a Firaga) and Holy is a single
  basic-Fire-tier smite with **no ladder and no AoE, ever**; burst and area damage are the **Black Mage's**
  lane.

Distinct from **Chemist** (flat, Faith-proof, finite-stock, instant, reactive triage —
`docs/job-balance/jobs/02-chemist.md`), **Monk** (self-sustain, no MP/Faith, frontline —
`docs/job-balance/jobs/07-monk.md`), and the **Black Mage** (scaling burst + AoE + elements — the offensive
caster). Three recovery jobs, three axes; the White Mage is the **Faith-scaled, pre-emptive, miracle-scale**
one.

## J1 — the pick / wrong-pick

- **The pick:** fights that **cross catastrophic thresholds** — a known **magic burst** to pre-empt
  (Shell/Wall), a **severe / magical status** spike to Esuna, a **devout / high-Faith party** to out-heal on,
  **undead** to smite with Holy, **mass-KO** disasters to mass-revive, and **boss spikes** to time Reraise
  against. Long, attritional fights reward the ward suite and MP economy.
- **Wrong pick (two-sided):** a **low-Faith party** (healing inefficient — bring Chemist), a **magic-heavy**
  enemy (the high-Faith White Mage is the most magic-vulnerable body you can field), a **fast physical-dive**
  roster (it folds before it casts), **Silence / Interrupt / MP-pressure** enemies (the premium kit shuts
  off), and **short, decisive** fights (wards and Reraise are wasted — bring damage).
