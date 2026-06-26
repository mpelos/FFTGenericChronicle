# Magic

Status: Draft (option c locked; tuning open)
Date: 2026-06-25
Depends on: 08-trait-faith, 09-trait-zodiac, 04-hit-and-defense.
Review: Pending.

## Decision: hybrid, on its own axis (option c)

Magic stays **FFT-native and runs on its own axis** — it does **not** go through the physical damage
pipeline. The GURPS-derived machinery (thrust/swing, subtractive DR, wound multipliers, Parry/Block)
is for *physical* combat. Magic keeps FFT's own model:

- Driven by **MA / MP / Faith** plus the spell's **element** and the target's **Shell / elemental
  affinity (Zodiac)**.
- **Deterministic** damage, like the physical side (preview = result; randomness only in landing).
- **Does not pass through physical DR or wound multipliers** — armor that stops swords does not stop
  fireballs (that's what Shell and Faith are for).

This was an explicit hybrid choice: keep magic recognizably FFT, but give it *one* borrowed idea
from the physical layer — dodgeable bolts (below).

## What magic must cover — the taxonomy

Magic spans six categories, but only **three need a damage equation**, and those three share **one
spine**:

- **Elemental damage** (Fire/Ice/Lightning/…) — numeric; interacts with Zodiac + Shell.
- **Spiritual damage** (Holy/Dark) — numeric; Faith-scaled, *outside* the Zodiac wheel.
- **Healing / restoration** (Cure/Raise) — numeric; "negative damage" on HP.
- **Magical status** (sleep/petrify/silence/…) — *not* a damage equation; runs the `13` 3d6 contest
  (inverse-Faith resist, caster MA = offense).
- **Buffs / enhancement** (Haste/Protect/Shell/Regen) — effects with magnitude + duration, not damage.
- **Utility / movement** (Teleport/Float) and **summons** (= a large elemental-damage AoE, not a
  separate spine).

So the system needs **one** magic damage formula, not five: the three numeric categories share the
spine below, status reuses `13`, and buffs/utility are calibrated effects.

## Magic damage — the shape (multiplicative, spell-centric)

Magic damage is **multiplicative and spell-centric** — the *conceptual* (not literal) mirror of the
physical pipeline (`02`):

```
dmg = base(MA) × spell_power × faith_mult × element_mult × G_m
```

| term | meaning |
|------|---------|
| `base(MA)` | the caster's magic base — **linear in MA** (magic does *not* inherit the GURPS thrust/swing table; GURPS has no magic damage spine, which is *why* magic is FFT-native, not GURPS-derived). |
| `spell_power` | the **per-spell tier** (Fire < Fira < Firaga). The spell carries the damage identity — magic is **spell-centric** where physical is **weapon-centric**. |
| `faith_mult` | the two-sided **Faith** multiplier (`08`) — magic's *one* big trait multiplier. Applied **twice**: caster output × target vulnerability, each a *bounded band centered at 1.0* (see below). |
| `element_mult` | the **Zodiac** affinity/weakness (`09`) + **Shell** resist — *modest bounded bands* (provisional weak ×1.30 / resist ×0.70, Shell ×0.50), multiplicative and commutative (see *Zodiac, Shell, and how the bands stack* below). |
| `G_m` | magic bridge constant to FFT's HP scale (calibration). |

**Physical subtracts, magic multiplies — a deliberate paradigm split.** Physical mitigates by
*subtraction* (`base − DR`, then wound mult, floored by `pen_floor`); magic mitigates by
*multiplication* (Shell/Zodiac/Faith cut a *fraction*). Two legible, distinct ways not to die — the
reason the two axes stay separate. Consequences:

- **Magic ignores physical DR** → it is the **anti-armor answer** (a plate knight's DR does nothing to
  a fireball — `sim_magic_shape`: physical cut collapses vs plate while magic is flat across armor).
- **A multiplicative resist never chip-zeros** → magic has no physical-style "weak vs heavy = 0" wall
  (but also, see risk, no natural ceiling).

**Why multiplicative, not additive** (spell as a flat `+mod` like `wmod`): an additive spell term
**compresses spell choice at high MA** — the Firaga/Fire ratio decays toward 1 as MA grows, so by
end-game every tier converges. Multiplicative keeps the ratio = `spell_power` at *every* MA, so
picking the right spell stays meaningful the whole game. This is **structural, not calibration**
(`sim_magic_shape` Part A — confidence **Strong**).

**The three numeric categories differ only in their resist term:** elemental carries Zodiac + Shell;
spiritual (Holy/Dark) carries Faith only (no Zodiac); healing carries no resist. One spine, three skins.

### Risk: magic has no structural damper (the #1 magic calibration risk)

Unlike physical — whose subtractive DR + `pen_floor` cap both ends — multiplicative magic has **no
natural floor or ceiling**; its safe band is held **entirely by calibration**, and **stacked
multipliers compound** (`MA × spell_power × Faith × Zodiac-weakness` can multiply into a one-shot —
the magic flavor of the `02` / B7 compounding-multiplier risk; `sim_magic_shape` Part C). The
structural guardrail: **keep the count of *large* multipliers small and bounded** — **Faith is the
single big two-sided multiplier**, Zodiac/Shell are *modest bounded bands*, and a **soft cap is held
in reserve**. The magnitudes (`G_m`, spell tiers, the Faith/Zodiac/Shell bands) are calibration; the
*shape* is what stands here.

### Faith enters twice — bounded and centered

`faith_mult` is **two applications**: the **caster's** Faith (output) × the **target's** Faith
(vulnerability). This is not vanilla nostalgia — it is the only shape consistent with the locked
two-sided Faith (`08`: high Faith = more magic output *and* more magic taken) and **A2** (magical status
resists on *inverse* Faith). A single application would erase one side: caster-only makes every target
take the same magic (no atheist-tank, no devout-glass-cannon); target-only makes every caster equally
good. The two-sidedness *is* the two applications. Confidence **Strong** (`sim_magic_faith`).

Vanilla's mistake was the **curve**, not the count. FFT maps Faith with `F/100` (0…1), so two
applications give a buildable swing over **10×** and Faith dominates magic; a faith-25 atheist takes
~14% (near-immune — the degenerate "atheist party trivialises magic"). The DCL instead puts each
application on a **bounded band centered at 1.0** — mid-Faith is *neutral*, deviation runs both ways
(provisional **[0.70, 1.30]**, a clean ±30%):

- **Centered, so Faith spreads rather than shifts.** Averaged over a roster, the mean multiplier is
  ~1.0 — Faith changes *who* magic punishes, not the global magic power level (that is `G_m`).
- **Bounded, so the devout/devout corner is ~1.69×** — about the size of a Zodiac weakness, *not* a
  runaway. Faith is a **bounded spreader, not the stack's explosion source** (the big number is the
  `MA × spell_power × G_m` spine — the `02`/Q2 calibration target, not Faith).
- **A real resist that is never immunity.** A low-Faith target takes ~70% from a neutral caster (an
  atheist-tank identity), but a zealot caster still lands ~91% — a lever, not a wall (multiplicative
  resist never chip-zeros). The band *width* is the calibration knob: wider-low (e.g. `[0.65, 1.30]`)
  buys a beefier atheist tank at the **same** bounded high corner; narrower (`[0.80, 1.20]`) is the
  safety valve if magic plays too swingy.

The exact band is calibration; the **shape — twice, centered, bounded — stands here**.

### Zodiac, Shell, and how the bands stack

Faith is the big lever; **Zodiac** (the target's elemental affinity, `09`) and **Shell** (a magic-armor
buff) are the *modest* ones. The structural rules for how everything combines (confidence **Strong**,
`sim_magic_stack`):

- **Everyday bands are modest.** Zodiac runs a *small* band — provisional weak **×1.30** / neutral ×1.0
  / resist **×0.70** — deliberately *smaller* than Faith's two-application effect, so a single matchup
  never dominates. Shell is a provisional **×0.50** (strong, but it costs a buff turn and is temporary).
- **Big elemental swings are rare designed properties, not the everyday band.** Vanilla-style ×2
  weakness or *absorb* (a fireball that heals its target) are wonderful as a **known, built-around**
  exception — a specific monster, a cursed item — but degenerate as the default sign-compatibility
  number: at ×2 the worst corner jumps from ~2.2× to ~3.4× (toward a one-shot), and absorb is a
  coin-flip swing if it is common. The everyday wheel stays modest; the extremes are content.
- **All-multiplicative, therefore commutative — there is no "stacking order."** Every term is an
  independent fraction (`faith_c × faith_t × zodiac × shell × …`), so order never changes the result and
  the player can read each effect on its own (legibility, `12` P5). The *only* non-multiplicative term
  is a **soft cap on the product, held in reserve** — dormant under the modest bands (the worst realistic
  corner ~2.2× sits under a ~2.5× cap) and earning its keep only if calibration later widens a band.
- **Defense mirrors offense.** Because both sides are multiplicative, every offensive multiplier has an
  inverse counter: a high-Faith caster is answered by a low-Faith *or* shelled target; an elemental
  weakness is answered by the matching resist or by Shell. Stacked defense (atheist + resist + Shell)
  pulls a burst down to ~0.24× — a **hard turtle, but never immunity** (a multiplicative resist never
  chip-zeros), and the attacker still has outs: switch to the target's weak element, cast spiritual
  (Holy/Dark ignore Zodiac), or wait Shell out. The defensive corner is *conditional*, not an auto-win.

So the magic stack is **self-correcting**: offense and defense both stack bounded fractions, the
compounding corner stays ~2.2×, and the magnitudes (the exact Zodiac/Shell bands, the reserve cap) are
calibration — the *structure* stands here.

### Healing — same spine, two-sided Faith, no resist

Healing is the third numeric category and runs the **same spine minus the resist term**:
`heal = base(MA) × heal_power × faith_caster × faith_target × G_m` (no element, no Shell — you do not
*resist* a heal). Crucially the **target's Faith scales healing received**, exactly as it scales damage
taken — *one* Faith rule for all magic: **the more devout a unit, the more magic flows through it, for
good and ill**. Confidence **Strong** (`sim_magic_heal`).

This keeps Faith a clean two-sided build axis with no free lunch:

- A **low-Faith** unit resists enemy nukes (×0.70) **and** is healed less (×0.70) — a **wash** on the
  magic axis; the atheist tank leans on physical HP/DR, not on being topped to full (a Cura is still a
  meaningful ~25–30 % top-up on a big HP pool — heal-*inefficient*, not un-healable).
- A **high-Faith** unit is hit harder (×1.30) **and** healed more (×1.30) — a glass cannon on both ends.
- At **equal Faith investment** the heal corner equals the nuke corner (both ×1.69 at zealot/zealot), so
  Faith is no healing-specific exploit: a dedicated heal out-paces a *single* nuker (a 2-unit hold vs 1),
  but the ×1.30-everything target collapses to focus-fire or a CC'd healer. High-variance, not unkillable.

The band is the **same [0.70, 1.30]** as damage for one-rule legibility (`12` P5); a gentler heal-only
band (e.g. [0.80, 1.20]) is held **in reserve** if playtest finds healers abandoning low-Faith
frontliners. Healing keeps its FFT-faithful special case — **undead invert it** (healing harms undead) —
as a designed property, the heal analogue of elemental *absorb* (`09`).

## Magic Evade — spells are resistible, per target

The one crossover from the physical layer: offensive magic is **resistible**. Each *target* rolls
**Magic Evade** to shrug the spell off — and because almost all FFT magic is **area-of-effect**, this is
a **per-target** roll that applies **inside an AoE too**: every unit caught in the blast rolls its own
Magic Evade independently (a single-target-only "dodge" would be useless when most spells are areas). The
single-target weapon bolt (`14`) and any bolt spells roll it the same way. This homes the Magic Evade
attribute (`01`); it is a **binary** evade (evade = take nothing), so it sits exactly where the
**"randomness only in landing"** rule allows it — the damage stays deterministic, only *whether it
lands* is rolled.

- **Source: built, not innate.** Unlike the physical **Dodge floor** (which everyone has a little of,
  `04`), Magic Evade has **no universal floor** — it comes **mainly from equipment** (robes, shields,
  accessories) and from **jobs naturally strong against magic**. Most un-invested units have little or
  none, so magic **reliably lands** on them; you *build* magic resistance. This is what keeps magic the
  dependable **answer to physical evasion and armour** — you cannot innately duck a fireball.
- **Off the Speed axis** (consistent with the B1 Dodge/Speed decoupling, `01`).
- **Capped below 100%** — stacked magic resistance is strong but **never full immunity** (the evade-roll
  analogue of the multiplicative resists that never chip-zero, Q3/Q4): magic is never fully walled.
- There is **no magic Parry/Block** (no depleting magic defence); Magic Evade is the whole
  magic-avoidance story. Magical **status** uses its own 3d6 resist instead (`13`); **healing** is not
  evaded.

## Charged spells are not interrupted by damage

Charge times are FFT-native. A charging spell is **not** interrupted by taking damage (vanilla
behaviour) — it still resolves unless the caster is **incapacitated** (KO'd, or hit by a status that
stops them) or struck by a **specific interrupt skill**. Interruption is therefore *ability-driven*
and legible, not an automatic consequence of any hit; **Brave (composure)** resists interrupt skills
(`07`). This keeps charged magic reliable to plan around, while still allowing dedicated anti-caster
builds via an explicit interrupt tool.

## The magic economy — MP budget and the weapon-bolt floor

Magic is gated by two independent costs: **time** (charge, above) and a **resource** (MP). MP is a
**per-battle budget**, not a slowly-refilling pool — a small trickle, so the real decision is *which
bursts to spend this fight*, and a committed mage (higher MA / job investment) carries a larger budget
than a dabbler. This is the **battle-scale** cousin of the turn-scale exhaustible guard (`04`): both are
exhaustible resources that reward managing a budget, at different timescales.

But FFT is heroic — a mage must be a **mage all battle**, never reduced to poking with a staff. So the
budget gates only the **big spells**; underneath sits the **caster weapon's basic Attack** itself: a
free (0 MP), always-available, **range-3 MA-scaled elemental bolt** — the magic-gun pattern (`14`), with
the element set by the equipped Rod/Staff SKU. This is the mage's true basic attack and the *offensive*
analogue of the physical **Dodge floor** (`04`) — an always-on baseline that never depletes. It is *not*
a new spell or ability: it is the `Attack` command re-tuned to a magic formula, so it scales with
MA/Faith and **ignores DR** like all magic — which makes the depleted mage the **anti-armour chipper**
(flat damage where a fighter's sword collapses against plate). Confidence **Strong**
(`sim_magic_economy`).

The resulting resource ladder gives two legible sustain styles, both of which keep the mage casting
*magic* every turn (the only zero-output turns are *charge* turns):

- **Burst then floor:** spend MP on bursts (Firaga), then fall to the weapon-bolt tail — front-loaded,
  kill key targets first.
- **Cheap-spell sustain:** stretch the budget with the cheap tier (Fire) most turns — smoother, saves a
  burst for a clutch moment.

So the budget shapes the *style*, never an on/off. A mage's per-battle total lands near a fighter's
against soft targets — trading single-target damage for front-loaded burst, AoE, and anti-armour — and
well above a fighter against plate.

**Strong healing has no free floor.** The free floor is a **damage bolt** — including a *Staff's* bolt,
so a healer with nothing to heal still has a useful ranged action (`14`). A Staff's *heal-on-attack* SKU
is **floor-tier** only; real restoration (Cure/Raise) stays MP-gated, so a healer's sustain is a
spendable budget rather than infinite at-will healing.

Pool size, trickle, and the weapon bolt's power are calibration; the **shape — a per-battle MP budget
over an always-on weapon-bolt floor — stands here**.

## Area spells are a positional game — facing stays physical

Area-of-effect magic plays a **positional** game, not a facing one: clustering is punished by AoE, and
spacing out trades that for vulnerability to being picked apart physically. This is a clean division of
the spatial game — **magic owns the *position* axis, physical owns the *facing* axis** (`05`: back/side
strikes, active-defence reduction). Facing does **not** modify an area spell (a blast has no "behind",
and per-unit facing inside a cluster would cost legibility for little gain — `P5`); **Magic Evade is
likewise facing-independent** (a per-target resist, not a directional dodge). The spatial richness of
magic comes from the **spell's shape** (burst / line / cross) crossed with where the enemy stands, not
from where units face.

## The trait stack on magic

Magic sits at the intersection of two of the three permanent traits:

- **Faith** (`08`) scales magic output and magic vulnerability (two-sided), entering the damage
  equation **twice** (caster output × target vulnerability) on a bounded centered band — see *Faith
  enters twice* above. This — *not* Brave — is the magic offense axis, which is what keeps the Brave
  slider clean (`07`).
- **Zodiac** (`09`) gives the target an elemental resist/weakness that applies to elemental spells — a
  *modest bounded band* (see *Zodiac, Shell, and how the bands stack* above).
- **Brave does not touch magic at all.**

A spell that is both elemental and Faith-scaled stacks both interactions (Faith × Zodiac × Shell), all
**multiplicative and commutative** — there is no stacking *order* to tune, only the band magnitudes and
the reserve cap (`12`).

## Holy and Dark

Sacred (Holy) and Dark are **spiritual**, not elemental: they scale with and are resisted by
**Faith** (`08`), and are *outside* the Zodiac elemental wheel (`09`). Lightning is a neutral
element with no zodiac sign attached.

## Open items

The magic damage **shape is resolved** (above): multiplicative, spell-centric, `base(MA) ×
spell_power × faith × element × G_m`, with **Faith resolved as two bounded centered applications**
(*Faith enters twice*) and the **Zodiac/Shell stacking resolved** (all-multiplicative and commutative,
modest bounded bands, big swings as designed exceptions, soft-cap in reserve — *Zodiac, Shell, and how
the bands stack*), the **economy resolved** (a per-battle **MP budget** over an always-on
**weapon-bolt floor** — the caster weapon's range-3 elemental Attack, `14` — *The magic economy* above),
**healing resolved** (*Healing — same spine, two-sided Faith, no resist*), and **Magic Evade resolved**
(per-target, AoE included, built from equipment + anti-magic jobs, capped — *Magic Evade* above). Still
open: the **Magic Evade % values**; **AoE × facing** rules; and all magnitudes (`G_m` —
`sim_magic_economy` is evidence toward ~3 — spell tiers incl. the weapon bolt's, the Faith/Zodiac/Shell
band widths, the reserve-cap value, MP pool/trickle, the `base(MA)` curve) — `12-open-questions.md`.
