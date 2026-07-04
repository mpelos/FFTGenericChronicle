# 10 — Time Mage · "The Clockbreaker"

The turn-economy caster, rebuilt as the job that **weaponizes the clock**. It owns the DCL's
turn-frequency axis (`docs/deep-combat-layer/01`): it freezes and Slows enemies (denying their turns
*and* lagging their guard so the team cracks it), Hastes and Quickens its own champions into the gaps,
and strikes on its own with **Gravity** (percent-HP) and a **Comet** (single-target). Control is its
axis — but in the DCL every one of those tools is an *aggressive tempo play*, not a passive buff, so it
is no longer the force-multiplier that feels useless by itself: it is a self-sufficient combatant whose
weapon is **when**.

This doc records the design decision for the Time Mage on the Deep Combat Layer (DCL). Mechanics it
leans on are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier B.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a **mid**
  unlock reached past a first-rank job (`docs/job-balance/00-job-tree.md`). It is also the roster's
  **export hub** — Short Charge and Teleport are why other builds visit it — so it earns its keep both as a
  primary and as a donor (rationed; see *R / S / M*).

## The vanilla problems it solves

Vanilla Time Mage (`docs/job-balance/vanilla/10-time-mage.md`) is rated **A/S by optimizers** — tempo is
the strongest lever in FFT — yet it is the job Marcelo flags as *"too support, nearly useless by itself."*
That is the paradox: mechanically dominant, but it **plays** like a buff-bot. Four concrete problems, each
mapped to a design move:

1. **It plays like a support bot (the feel problem).** Its turns are spent making *other* units better;
   with no one to buff it has a dead turn, and it can't take a forward role. **Fix:** reframe tempo as
   **aggression** — Slow/Stop are an attack on the enemy's next turn (and crack its guard for the team,
   below) — and give it **standalone offense** (de-niched Gravity + Comet + the free bolt floor), so a Time
   turn is *always* an active play and the job is fieldable solo (SIM 7).
2. **Weak, niche direct damage.** Vanilla Gravity is capped (anti-giant only) and Meteor's charge is
   impractical — "it controls, it doesn't nuke." **Fix:** **Gravity de-niched** to percent-HP that chunks
   *any* healthy target (but cannot finish — a softener, not a deleter, SIM 3), **Comet** as a reliable
   single-target minimum offense (SIM 4), and **Meteor demoted** to a late capstone timing-puzzle, never the
   offense base.
3. **Degenerate tempo (the optimizer's tier-S).** Vanilla Quick + Short Charge enable "act again before
   the enemy can answer" and near-instant nukes — the warping combo. **Fix:** the **Telegraph Invariant**
   (below) — Time may improve timing windows but may **not** erase the readable charge window that all
   charged offense relies on (SIM 1).
4. **Export hub = mandatory-Time.** Vanilla Short Charge and Teleport are near-auto-includes that flatten
   every caster build. **Fix:** **ration the exports** — Short Charge is **moderate** (and additive-capped
   against Haste, by the Invariant); Teleport carries a distance-risk — so they are choices, not defaults.

## Fantasy

The clockbreaker bends the rhythm of battle. It freezes an enemy mid-stride; it drags a battle line out of
sync so it can't raise its guard in time; it hastens its own champions to strike in the seams and grants one
an extra heartbeat of action; and when the moment is right it pulls a comet down or crushes a giant under its
own weight. Time is the weapon — not a blessing it hands to others, but a lever it pulls *on the fight*.

## Chassis

- **Good MA** (below the Black Mage — for Comet and for landing control) · **HP ~75** · Speed **moderate**
  · **Move 3** · **NEUTRAL Faith** · **low Brave**.
- **Armour: Robes** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the mage's robe; the mod
  draws no new art). ~No physical DR — it folds to a dive (SIM 5) and lives behind the line.
- **Neutral Faith = the deliberate distinction from the other casters.** Its **control runs on MA, not its
  own Faith** (Stop/Slow resist on the *target's* inverse Faith, with the caster's MA as offense —
  `docs/deep-combat-layer/13`), so it does **not** need to be a high-Faith glass cannon to do its job.
  Neutral Faith makes it **less self-vulnerable than the Black/White Mage** (it takes ×1.0 magic, not ×1.30)
  and a less explosive nuker — a controller's body, not a cannon's. (Low Faith was rejected: it would
  double-dip — strong control *and* better magic/status resistance on top of robes/Teleport/Reflect.)
- **Off the weapon axis:** low PA — its output is magic and tempo, not melee. The caster weapon is the
  **Rod** — a free, range-3, MA-scaled bolt (`docs/deep-combat-layer/11`, the weapon-bolt floor) under its
  real offense.
- **Low Brave** fits the backline; with low HP and neutral Faith it is the *least* status-disruptable of the
  casters (neutral Faith resists magical statuses better than the high-Faith mages), but mental statuses
  (low Brave) and physical pressure (low HP) still bite (`docs/deep-combat-layer/13`).

## Innate — Short Charge (free, and exported)

The chronomancer's craft: **Short Charge** trims the charge time of the Time Mage's charged actions, free
(innate) — and it is **the** signature learnable Support, the reason other casters visit the job (the
White-Liturgy / Black-Rod-Attunement pattern, `docs/job-balance/jobs/05-white-mage.md`,
`docs/job-balance/jobs/06-black-mage.md`: one quality that is both the innate moat and the export).

Its magnitude is **rationed and bound by the Telegraph Invariant** (below): it is a **moderate** CT trim
that always leaves a real, repositionable charge window, and it is **additive-capped against Haste** — the
two never multiply a spell toward instant (SIM 1). The moat is **not** exclusivity — it is Short Charge
**free** on the body that *also* owns the tempo command. *(The exact reduction is calibration,
`docs/deep-combat-layer/12`, **Hypothesis** — and it is **not free tuning**: it must preserve at least one
enemy reposition window under the worst legal Haste stack; if that fails, Short Charge is reduced before any
damage job is touched.)*

## Command — Time Magic

**The roster rule this job is built around — the Telegraph Invariant:** *Time may improve timing windows,
but it may not erase the readable charge window of charged offense.* Charged bursts (the Black Mage's -ga,
the Summoner's barrage, Meteor itself) keep a window in which the enemy can reposition or answer; no stack of
Short Charge + Haste + Quick may collapse it (SIM 1). This is what lets Time be the strongest tempo job
without breaking the caster system.

**The always-on action** is the **free Rod bolt** (range 3, MA-scaled, `docs/deep-combat-layer/11`); above
it sit the tempo command and a small, real offense — so a Time turn is never dead (SIM 7).

**Core:**

- **Haste** (single-target ally buff — friendly, no-resist, `docs/deep-combat-layer/11`): raises an ally's
  turn frequency, and — because guard resets on a unit's own turn (`docs/deep-combat-layer/01`) — lets a
  **tank refresh its Parry/Block sooner** (it holds the line harder). Short duration, **no stacking**
  (reapply refreshes).
- **Slow** (single-target enemy — magical status, resists on **inverse Faith**, caster MA = offense,
  `docs/deep-combat-layer/13`): the **active offensive button**. It delays the enemy's next turn — *and*
  because the enemy now acts less often it **refreshes its guard less often, so the team cracks its Block/
  Parry** (SIM 2). The **obvious** reward is the visible turn-order-bar shift; the guard-crack is the deep
  bonus. Framed as an *attack on the enemy's next turn*, not "a debuff."
- **Gravity** (percent-current-HP, ignores DR and Faith): a **softener, not a deleter** — a real chunk off
  *any* healthy target (de-niched: it is not anti-giant-only), but it **cannot finish** (it leaves the target
  alive; Comet or the bolt closes the kill — SIM 3). The reliable opener that works through plate and through
  a faithless body alike.
- **Comet** (single-target, **non-elemental**): Time's **minimum direct offense, not its damage plan**
  (`docs/job-balance/jobs/06-black-mage.md`, the locked Black/Time boundary). Calibrated to **~-ra effective
  output** after the Time Mage's lower MA (SIM 4): it may beat the Black Mage's *neutral basic Fire* per cast
  (it is reliable, never resisted), but it **loses to** Black exploiting weakness, Black's -ga at k≥2, Black's
  Flare, and Black's damage-per-MP sustain. One button — **never AoE, never a tier ladder, never a rider**.
- **Float** (utility, casts the Float status on an ally): hover — negate earth/Geomancy interactions and
  ground traps. Time's command utility (this is *why* the Black Mage takes no signature movement —
  `docs/job-balance/jobs/06-black-mage.md`).

**Tier-2 (costed):**

- **Stop** (single-target enemy — magical status, inverse Faith, **boss/immune-respecting**, short duration,
  no Stopja): Time's **one hard-denial door** — it removes a unit from the fight for the duration. It lands
  on the devout/casters and is **resisted by faithless bruisers** (SIM 2), so it is never a universal lock.
- **Quick**: a **costed extra-turn spell that cannot resolve or erase charged telegraphs.** Single-target
  ally, high MP and a meaningful CT, no Quickja. It grants an extra **turn**, so it can double *instant*
  actions (a melee, Gravity, the bolt) — but a charged spell started on that turn **still charges and still
  telegraphs**, and Quick on an already-charging unit does **not** resolve the charge (SIM 1, SIM 6). *(If
  sim later finds a residual loop, it downgrades to a bounded CT-advance rather than a full turn —
  `docs/deep-combat-layer/12`, Hypothesis.)*
- **Hasteja / Slowja**: the party / area versions of Haste / Slow — high MP/CT, small area.
- **Graviga**: area percent-HP softening (same "cannot finish" rule).
- **Reflect** (mid-cost, **not a starter**): bounce a spell back at its caster — a sharp anti-caster tool
  that is **self-limiting** (it also bounces *your own* buffs and heals onto the wrong side, so it must be
  placed with care).
- **Meteor**: the **capstone timing-puzzle**, not a damage base — a very long charge, a large telegraph,
  high MP, non-elemental area, with lower practical reliability than the Summoner's committed barrage. Its
  fantasy is *"I controlled the clock so hard the impossible spell landed."*

## R / S / M

- **Reaction — Mana Shield** (spend MP to absorb a hit): turns the Time Mage's MP pool into survivability —
  but the **MP is real fuel**: every point spent staying alive is a point **not** spent on Stop / Quick /
  Hasteja. A Time Mage that tanks with Mana Shield has stopped controlling the fight, and focus-fire still
  kills it once the pool runs dry (SIM 5). *(Calibration gate, `docs/deep-combat-layer/12`, Hypothesis: the
  MP→HP ratio must keep the choice real — if full shielding still leaves budget for the tempo kit, the ratio
  is too generous.)* A Caution-category reaction (low-Brave, `docs/deep-combat-layer/13`).
- **Support — Short Charge** (the innate, learnable — see *Innate*): exports the moderate, Invariant-bound CT
  trim. The signature donation; rationed, never near-instant.
- **Support — Rod Training** (weapon-proficiency export, `docs/deep-combat-layer/15`): grants **Rod A** — the
  caster weapon and its range-3 bolt — to whatever job equips it.
- **Movement — Teleport** (the premium movement export): move anywhere on the map — but with a **vanilla-style
  distance-risk** (the farther the jump, the greater the chance to fail/scatter), so it is excellent without
  being the free universal movement. Repositions a fragile caster out of a dive or onto a perfect angle.

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot.)*

## Equipment & weapon aptitude

Pool from `docs/deep-combat-layer/15` (*Weapon aptitude*; mechanic owned by `docs/deep-combat-layer/10`),
spent **lean** — a caster, not a weapon user:

| Slot | Grant |
|------|-------|
| Armour | **Robes** (sprite-mandated — ~no physical DR) |
| Off-hand | **none** — no shield; a backline caster |
| Rod | **A** — the caster weapon: the free range-3 MA-scaled bolt under its real offense |

*(No martial weapons — its output is **magic + tempo**, not melee. One A (Rod), per "one A below the capstone
tier", `docs/deep-combat-layer/15`. The underspent pool is deliberate: the Time Magic command, not a weapon,
is the job.)*

## Early / mid / late

- **Early.** Already an active controller that can fight: Haste/Slow (the bread-and-butter, with the visible
  turn-order swing), Gravity (an opener vs anything), Comet (a reliable nuke), Float, and the free bolt — no
  dead turns, no dependence on a late unlock.
- **Mid.** The clockbreaker proper: **Stop** an enemy caster before it casts, **Quick** a champion for a
  timing window, **Slow** a frontline so the team cracks its guard, **Reflect** to turn enemy magic back.
- **Late.** Party-scale tempo (**Hasteja/Slowja**), area softening (**Graviga**), and **Meteor** as the
  capstone payoff — plus the export hub (Short Charge, Teleport) lifting the whole squad. Not a damage caster
  (Black) — the master of **when**.

## Battle dynamics

**What the player does with it.** **Weaponize the clock.** Read the turn order: **Slow** the frontline (it
refreshes guard slower → the team cracks its Block/Parry) and **Stop** the enemy caster before its spell
resolves; **Haste** your Knight or Dragoon for a guard/timing window and **Quick** a champion for an extra
strike; **Gravity** a healthy target to soften it and finish with **Comet** or the bolt. As a donor it lends
the rationed **Short Charge**, the **Teleport** movement, and **Haste/Slow** — the export hub — but never an
unreactable burst (the Invariant binds Short Charge; Teleport carries distance-risk).

**How an enemy version harms the player.** An enemy Time Mage **steals your tempo** — Slows your line (your
guard lags and you get cracked), Stops your key unit, Hastes/Quickens its own threats, and chunks you with
Gravity. Counterplay is clear: **focus-fire it** (robes, low HP — it folds to one diver, SIM 5); **field
low-Faith bodies** (the faithless bruiser **resists its Stop/Slow and its Comet** — its control simply
bounces, `docs/deep-combat-layer/13`); **win fast** (tempo compounds over a long fight — deny it the time);
and **pressure its casters' positioning** rather than fearing an instant nuke (the telegraph always holds).

## Two-sided cost (why it is not strictly-better)

- **Physically fragile** — robes, low HP: a single diver folds it (SIM 5). Its safety is **position** (and
  Teleport), which pressure takes away.
- **Control is inverse-Faith** — a **low-Faith / faithless** enemy **resists** Stop / Slow (and Comet),
  `docs/deep-combat-layer/13`: the atheist bruiser is its hard matchup, and its control is best against the
  devout/casters.
- **Neutral Faith = modest damage** — no devout burst; Comet and Gravity are a self-sufficiency floor, **not**
  a damage lane. The Black Mage out-damages it massively (SIM 4).
- **Charge-gated** — Comet, Meteor, and Stop have CT windows (not damage-interruptible, but a dedicated
  Interrupt skill — Brave-resisted — or a fast enemy disrupts, `docs/deep-combat-layer/13`).
- **Bound by the Telegraph Invariant** — it can **not** manufacture an unreactable burst (Short Charge stays
  moderate; Quick can't resolve or erase a charge, SIM 1).
- **Mana Shield is a real tradeoff** — MP spent surviving is MP not spent controlling (SIM 5).
- **Tempo needs time** — in a short, decisive fight its compounding advantage never materializes; bring burst
  instead.

Distinct from **Black Mage** (magnitude / burst / AoE — `docs/job-balance/jobs/06-black-mage.md`), **White
Mage** (heal / ward / revive — `docs/job-balance/jobs/05-white-mage.md`), the future **Summoner** (the heavy
committed barrage), and the **Mystic/Oracle** (the **spiritual-affliction** suite — Faith/Brave tuning,
Sleep, Confuse, Frog, Blind, Silence; **not** Charm/Berserk, which are Orator's —
`docs/job-balance/jobs/09-mystic.md`). **Time owns the CLOCK** (Slow, Stop, Haste, Quick) and the **HP-axis**
(Gravity), never the afflictions and never magnitude. Two different kinds of "control," two different axes.

## J1 — the pick / wrong-pick

- **The pick:** fights where **tempo decides** — enemy **casters/devout** to Stop/Slow (high Faith → its
  control lands), an **entrenched line** to Slow (crack its guard for the team), **your own champions** to
  Haste/Quick (timing windows), **drawn-out attritional** fights where tempo compounds, **high-HP** targets to
  Gravity-soften, and maps where the **export hub** (Short Charge/Teleport) lifts the whole party.
- **Wrong pick (two-sided):** a **low-Faith / faithless** enemy (it resists Stop/Slow/Comet — the atheist
  bruiser walks through its control), a **fast-dive** roster (it folds before it controls), a **short,
  decisive** fight (tempo never compounds — bring burst), and a board that **just needs raw damage** (that is
  the Black Mage's job; Time only chips).
