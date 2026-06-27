# Job Authoring

Depends on: 00–14 — this manual is how to use all of them to build a job.

This is the method for authoring (and rediscussing) a job on the DCL. A job must pass **two gates**:
the **essence gate** — it is a recognizable FFT job — and the **DCL-citizen gate** — it declares a
position on every DCL axis and survives the anti-dominance rubric. The central creative act is to
**map the classic FFT fantasy onto the DCL's axes**: that is where fidelity and balance stop fighting
and start reinforcing each other — the Knight's "break armour" becomes crush + guard-shred, the Dragoon
becomes reach, the Thief becomes Speed/tempo, the devout caster becomes two-sided Faith. This manual
**owns the job-authoring doctrine**; the per-job decision docs are its consumers.

## The five laws

Every job and every skill obeys these. They are the **contextual-differentiation** pillar (`00`)
pushed down to the job and the ability level.

- **J1 — Job non-obsolescence.** Every job is the best, or near-best, pick in **at least one realistic
  scenario** — the contextual-differentiation pillar (`00`) at job scope, the same "each wins a context"
  already proven for armour classes (B10, `14`) and the right/wrong-tool weapon matchup (`03`). **Test:**
  name the scenario where you would field this job over its alternatives. A job with no such scenario is
  not done.

- **J2 — No dead support skill.** Every Reaction / Support / Movement ability makes sense in **at least
  one build — and none is a universal auto-include.** **Test:** name the build that picks it on purpose,
  then confirm a sensible build that skips it. R/S/M carry the strictest scrutiny because they are
  always **portable** across jobs; *both* failure modes — a trap nobody takes, an auto-include everybody
  takes — violate contextual differentiation.

- **J3 — Tiered balance.** Jobs sit in **tiers by acquisition difficulty**; the **power budget rises
  with tier**; and **balance is enforced within a tier** — the "1.0" power baseline is **per-tier**. (How
  this coexists with J1 is the *Tiers* section below.)

- **J4 — Fantasy over nostalgia.** Fidelity is to the **fantasy**, not the literal vanilla skill list.
  Keep a vanilla skill only if it still serves the job's objectives; otherwise **modify it or author a
  new one** — the job's objectives are the master, the vanilla set is a starting palette, not a contract.
  ("Author" means within the existing ability infrastructure — reskin / retune / recombine effects the
  engine already has; this is *abilities*, not *equipment*, so it never touches the **no-new-equipment**
  rule (`00`). Genuinely new behaviour may fall to Tier-2 — see *Feasibility*.)

- **J5 — Flat-band JP.** Every job costs a **similar total JP** to learn all its skills — no job costs
  much more than another (a band, not an identical number). Within a job, per-ability JP is a **learning
  curve** (the core fantasy is cheap and early; capstones are the expensive tail), but the **sum** lands
  in the shared band. JP is **never a hidden power gate**; and because a cheap ability splashes onto
  other jobs more easily, **per-ability JP doubles as the portability gate** (ties to J2). Consequence:
  a job's *power* and its *JP-to-master* are decoupled — **tier difficulty is unlock difficulty, not
  grind.**

## Tiers — how J3 and J1 coexist

J3 is vertical (higher tier, stronger); J1 is horizontal (every job good somewhere). Held naively they
collide: if a high tier were *strictly* stronger, no one would field a low-tier job late. FFT avoids
exactly this — Squire and Chemist stay useful all game — and so does the DCL:

- **J1 is the global floor; J3 is a stricter parity on top.** Within a tier, no job is strictly better
  than a peer (contextual differentiation, `00`). Across tiers, the higher tier wins on raw budget — but
  every job, at any tier, must still satisfy **J1 against the whole roster**.
- **The mechanism that lets the floor survive the gradient is the secondary slot plus a kept niche.** A
  high tier buys a **higher ceiling and more specialization, not universal dominance**; a low-tier job
  stays relevant because its *abilities travel* onto high jobs through the secondary command, and because
  it keeps one scenario where it is the pick.
- **The price of a high tier is the unlock journey, not JP** (J5): once unlocked, every job costs ~the
  same JP to master.

**The tiering model is hybrid:** the **vanilla job tree is the base tiering** — recognizable and already
play-tested as a progression — and the DCL **corrects it only where the DCL changes a job's intended
power.** Tier membership is set by **unlock prerequisites** (the vanilla-style "master enough of the
prior job(s)" gate, adjusted), not by JP-to-master.

This **refines pillar #1 of `00`** ("no build is strictly better than another") to be **tier-relative**:
strict non-domination holds *within* a tier; *across* tiers it becomes J1's global floor. (`00` should
absorb this refinement; tracked in `12`.)

## The authoring pipeline

Walk these steps to author or rediscuss a job; each fills a section of the job's decision doc.

### 0 · Frame — what a job is

A job is the FFT-standard package: a **primary command** (its signature skill set), **one secondary
command slot** (any unlocked job's command), and **one each of Reaction / Support / Movement** — all
learned with JP, all but the primary **portable** to other jobs. *(This is the assumed FFT-standard
frame; the exact portability freedom — how freely the secondary and R/S/M travel — is open calibration
(`12`) and is **load-bearing**, because it is also what keeps low tiers from going obsolete under tiered
power.)* You cannot budget a skill without knowing its slot and whether it travels.

A job also carries **innate traits / passives** — non-purchased, always-on properties bound to the job
itself: equip access (which armour class, weapon families, shield), granted off-hand modes (dual-wield /
Doublehand), a built-in weapon family (the Monk's **Martial Arts**, `14`), movement bonuses, and the
like. Several are load-bearing — the caster's **robe-only** restriction (`14`) is what makes magic
fragile, Martial Arts is the Monk's entire weapon — so a job **declares its innates explicitly**; they
are never assumed.

### 1 · Identity & essence

State, in a line or two: the **fantasy** (the power the job sells), the **vanilla identity** to honour
or deliberately diverge from, and the **player archetype** it serves. Then apply the **essence test**
(J4): would an FFT player recognize the *fantasy* — not the skill list?

### 2 · Map the fantasy onto the DCL axes — the chassis

The heart of the method. For each axis, **declare the job's position**. This table is mandatory in
every job doc:

| Axis | Owner | What the job declares |
|------|-------|------------------------|
| Attributes (PA / MA / HP / MP) | `01` | the stat profile that fits the fantasy — note **base HP doubles as physical-status resistance** (`13`): a high-HP job also shrugs off stun / knockdown / poison |
| **Speed** | `01`/`04` | **the mandatory B1 calibration** — turn-frequency and guard-refresh are *paid for in per-hit offense*; a fast + high-offense + high-mitigation job is forbidden |
| **Move / Jump** | `01`/`05`/`06` | the positioning budget — **more** decisive in the DCL (flanking, reach, kiting decide fights); interacts with armour Weight (`14`) |
| Brave **dependency** | `07` | which Brave value the kit **rewards** (Brave is unit-owned, not job-set). Brave is the **physical-offense + courage + Will** temperament, two-sided (its offense is paid in active-defense); **not a magic axis**; taunt inverts |
| Faith **dependency** | `08` | which Faith value the kit rewards — two-sided; a caster wants high Faith and is a glass cannon on both ends |
| Zodiac | `09` | usually roster-level; note only if the job leans on it |
| Armour-class access | `14` | the mitigation↔avoidance position (plate / mail / leather / robe), **job-gated** (B10) — a primary identity lever |
| **Defensive profile** | `01`/`04`/`14` | the **C-Ev / Dodge floor** (the light-build survival stat), **shield access** (Block — the top rung *and* the only strong anti-ranged defense), and the active-defense lean (Parry duelist / Block tank / Dodge skirmisher) |
| **Off-hand mode** | `14` | what the job's abilities grant in a free off-hand: **shield / dual-wield (guard-shredder, `04`) / Doublehand / none** |
| Weapon families + grades | `10`/`14` | the damage-type identity (thrust / swing / crush, reach, right/wrong-tool) **and** its **per-job-level skill growth** (the grade × job-level table — a progression axis *separate* from JP) |

Worked translations: *Knight* → swing/crush + guard-shred + plate (the "break things" fantasy as a
matchup, not a stat line); *Thief* → high Speed paid by low per-hit, leather, a utility kit; *Dragoon* →
the reach identity (`06`); *Monk* → unarmed fists (`14`) + self-contained sustain; *Black Mage* → high
MA/Faith, robe, the magic family (`11`), anti-armour by ignoring DR.

### 3 · The ability kit

**Shape.** A themed primary command of several abilities across a **power/cost curve** — the core
fantasy cheap and early (the job is fun before mastery), capstones as the expensive tail. **Author to
the objective, not the heritage (J4):** keep, modify, or create as the job's objectives demand.

For **every** ability decide these five things — this is what "map it to a DCL primitive" really means:

1. **What it does** — a damage formula (`02`/`03`/`10`), a status via the 3d6 contest (`13`), a spell
   (`11`), a buff, or a movement / utility effect.
2. **How it engages the roll system** (`04`) — does it roll to hit (≤ skill)? does it provoke a defense
   roll (so it can be Dodged / Parried / Blocked)? can it crit (bypass defense) or fumble? does facing
   modify it (`05`)? An **auto-hit / undefendable** ability is a different balance animal and must be
   costed as one.
3. **Range, area, targeting** — single / line / cross / burst, and the range band. **Uncosted AoE is a
   known breaker** (the magic validation: a free area multiplies output across a cluster), so any area
   ability must declare what pays for the area — MP, charge, or lower per-target power.
4. **Its cost currency** — the menu: a per-battle **MP budget** (casters, `11`), **CT / charge** (slow
   or powerful effects), **guard / position exposure**, an **HP cost**, **JP** (always, to learn), or
   **at-will** (the physical default). Pick the currency that fits the fantasy and the power.
5. **Resist category, if it inflicts status** (`13`) — by the status's **nature**: physical → base-HP,
   mental → Brave, taunt → inverted (low Brave), magical → inverse-Faith. Respect the **control-status
   ration** ("few jobs, real cost") — stun / fear / taunt / charm stay characterful only while rare.

**R/S/M doctrine.** Each must pass **J2** (a build wants it; none auto-includes). Build reactions from
the **taxonomy** (`13`): *courage* (fires ∝ Brave), *caution* (∝ inverse Brave), *neutral* (flat) — the
**job picks which**, which is exactly how the slot stays live at any Brave value and avoids a "universal
Brave problem." Because R/S/M always travel, they take the strictest scrutiny.

**Portability — the FFT trap, and the tool against it.** Judge every active *and* every R/S/M **as if
slotted on its most abusive host**, not only at home. The standard fix is **anti-splash design**: tie an
ability's power to the **home job's own stat or resource** so it weakens off-job (the Monk's `MA_wmod`
scales with Martial-Arts level → feeble on anyone else, `14`). **JP doubles as the portability gate**
(J5): a cheap ability splashes easily, so price the splash-worthy ones deliberately; per-ability JP is a
learning curve whose **sum** stays in the shared band.

### 4 · Tier placement

Place the job by the **hybrid model** (above): start from its vanilla-tree position and correct only if
the DCL changes its intended power. Record its **tier**, its **unlock prerequisites**, and the
**per-tier budget band** it is measured against. *(Tier count, the full 21-job map, and the per-tier
budget numbers are calibration — `12`.)*

### 5 · Balance & pillar validation — the checklist

A job is not done until it passes, on paper:

- [ ] **J1** — a named scenario where it is the pick, both against same-tier peers and as a global floor
  against higher tiers.
- [ ] **J2** — every R/S/M has a named wanting-build *and* a sensible build that skips it.
- [ ] **No strictly-better option** inside the kit and inside the tier (contextual differentiation,
  tier-relative).
- [ ] **No free lunch** — every advantage is paid on another axis (`00`); the Speed calibration (B1)
  holds.
- [ ] **Counterplay is real, not aspirational** — name the answers to this job *and confirm they are
  engine behaviour*, not hoped-for AI. (The magic validation learned this the hard way: a counter the
  engine does not execute does not count — `11`.)
- [ ] **Legibility** (`00`) — the kit reads clearly; the preview shows the deterministic result.
- [ ] **Declared weakness** — name the scenario where this job is the *wrong* pick (the negative space of
  J1; the two-sided pillar applies to jobs too).
- [ ] **Identity guards** — magic's DR-ignoring anti-armour stays with casters; a physical job answers
  armour only through the crush / penetration / divisor matchup (`03`/`14`), never a free DR-ignore. Any
  Brave-scaled offense must carry Brave's active-defense downside (no min-max escape — taunt is the
  closer, B9).

This checklist is the input to `/game-design-validation`; a job that cannot tick it is a draft.

### 6 · Feasibility & format

- **Tag each ability Tier-1 (data) or Tier-2 (code)** and respect **no new equipment SKUs** (`00`) —
  reskin/retune existing effects first.
- **Decision-doc format:** identity → the chassis declaration table → the ability table (each row: name
  · effect · DCL primitive · MP/JP/CT · Tier-1/2) → R/S/M → counterplay → open calibration. Tag
  uncertain facts **Proven / Strong / Hypothesis / Refuted**; keep *structure* in the doc and send
  *numbers* to `12`.

## Hard rules — the DCL-breakers

Forget one of these and the job breaks the engine:

- **Speed is calibrated per job (B1):** fast is paid in per-hit offense; never fast + offensive +
  mitigating.
- **Brave is the physical-offense temperament, not a magic axis (B9):** it scales **physical** offense
  (+ courage reactions + Will), two-sided — the offense is **paid by an active-defense penalty**. Never
  route *magic* through it; no reaction is a universal Brave problem; taunt inverts (high Brave is the
  vulnerable one), which is what makes Brave's downside reach even a min-maxing backliner.
- **Armour is job-gated (B10):** granting plate vs mail vs leather vs robe is an identity *and* a balance
  decision.
- **Faith is two-sided:** the devout hit harder *and* are hurt harder — both ends.
- **Anti-armour comes two ways (`11`/`03`/`14`):** magic's is **ignoring DR** — the casters' exclusive
  mechanism; a physical job's is the **crush / penetration (×2) / gun-divisor matchup**, type-specific
  and paid by weakness vs the unarmoured. Never hand a physical job a flat DR-ignore.

## The J1 scenario palette

"Good in some scenario" is only testable against a fixed list. Test every job across this spread (it
mirrors the `/game-design-validation` axes):

- **Progression stage:** early / mid / fully-developed late.
- **Enemy composition:** armoured / evasive / casters / swarm.
- **Encounter type:** open battle / boss / defend-the-point / assassination.
- **Team role:** frontline / backline / support / tempo.
- **Spatial context:** chokepoint / open field / elevation; who acts first.

A job needs at least one cell of this grid where it is the pick (J1); an R/S/M needs at least one
build × cell where it is wanted (J2).

## Open / calibration

Tracked in `12`: the **tier count and the 21-job → tier map**; the **per-tier power budget**; the **JP
band** value and per-ability curves; the **portability / secondary-command model** (how freely skills
travel — load-bearing for J3); the **ability-authoring feasibility envelope** (what "create a skill" can
be, Tier-1 vs Tier-2); and the refinement of `00`'s contextual-differentiation pillar to **tier-relative**.
