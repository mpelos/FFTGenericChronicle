# 02 — Chemist · "The Field Alchemist"

The Faith-free item master, and the other half of the generic starting pair (with the Squire). Its
signature is the classic FFT **Items** kit — Potion, Phoenix Down, the status cures, Ether — kept by
name. What the DCL adds is a **reason to field it, not just mine it**: a Faith-proof offensive lane
(alchemy) that is innate to the Chemist primary, plus a JP layout that stops punishing the player with
grind for trivial cures.

This doc records the design decision for the Chemist on the Deep Combat Layer (DCL). Mechanics it leans
on are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`).

## Tier & tree position

- **Tier D.** Tier here is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*):
  D = the most accessible jobs. The Chemist is a **starting job** alongside the Squire
  (`docs/job-balance/00-job-tree.md`) — no prerequisites — so it sits in the most-reachable tier and is
  balanced *within* it.

## The vanilla problems it solves

Vanilla Chemist (`docs/job-balance/vanilla/02-chemist.md`) has a split reputation: the *Items* command
and Auto-Potion are best-in-class **reliable** tools (immune to Silence / low-Faith / MP-burn; cheapest
revive), but the **job** is "squishy and offenceless, items don't scale, purely reactive, low standalone
value" — prized as a splash, rarely fielded as a primary. On top of that it carries a **pacing wart**:
you sink large JP to unlock each trivial single-status cure, so a lot of invested time buys weak
abilities.

The DCL Chemist fixes both:

1. **A reason to field it** — a Faith-proof **offensive alchemy** lane (thrown flasks) that is **innate
   to the Chemist primary** (non-portable), so the job has its own offence instead of being only a
   support graft. (The specific offensive abilities are reserved — see below.)
2. **The JP wart** — the function is **never gated behind grind**, and the pile of single-status cures
   **collapses into one learnable ability** (detailed in the ability list).

## Fantasy

The field technician/alchemist: Items that work no matter how godless the squad, and reagents thrown to
burn or break what stands in the way. The reliable backline answer — the one who keeps working when the
magic stops.

## Chassis

- Modest stats, **Brave low** (backline), Speed moderate, Move 3.
- **Faith irrelevant** — Items *and* alchemy bypass it; this is the defining edge vs every caster.
- **Armour: Clothes & Suits** (`docs/deep-combat-layer/14`). Defence = Dodge + distance + Auto-Potion.
- **No shield.** The **Field Lab** (the reagent satchel) occupies the off-hand — see Innate.
- **No gun.** Firearm specialism is a unique-character identity (Mustadio), not the generic Chemist's.
- Movement: Ignore-Terrain-lite.

## Innate — Field Lab (free, and exported)

The reagent satchel — the access that lets a unit throw **offensive alchemy** with the Items command. The
Chemist has it **free** (innate). Per the portability rules (`docs/deep-combat-layer/15`), Field Lab is
**also a learnable Support** ("Reagent Kit"): another job can run Items-secondary **+** Field Lab-support
to throw flasks too — paying a heavy price (two slots spent, and the **satchel occupies the off-hand → no
shield**). The Chemist's moat is getting Field Lab **free** (slots open) on a **Faith-proof backline
chassis** with the full cure spine native — not exclusivity. The alchemy stays bounded (no-strictly-better
vs Black, below), so a sturdier body throwing flasks is a costly, welcome splash, never a strictly-better
home (`docs/job-balance/job-design-process.md`).

## The Items command — what the Chemist learns

Every entry below keeps its **canonical FFT name**. The design intent is the **JP layout**: the basic
function is cheap and available early; JP buys *bigger numbers and breadth*, never the basic ability to
function.

**HP restore — three separate abilities (the tiered spine):**

- **Potion** — small HP, cheap, early.
- **Hi-Potion** — medium HP.
- **X-Potion** — large HP, the late workhorse, expensive.

Each tier is its own learnable ability (the player upgrades the spine deliberately, as in vanilla).

**Revive — one ability:**

- **Phoenix Down** — revive (random HP; damages undead). The signature, cheap — the reason Items is on
  so many bars.

**Status cure — the pacing fix:**

- **One single ability unlocks the entire single-status cure set** — Antidote, Eye Drops, Echo Grass,
  Maiden's Kiss, Soft, Holy Water — instead of one JP purchase per item. Learn it once and the Chemist
  can apply *any* of those targeted cures (one ailment per use). This is the direct answer to the vanilla
  wart (huge JP for trivial cures).
- **Remedy — one ability.** Remedy is the **generalized cleanse item** (clears several ailments from a
  target in a single action), so it is its own, later ability — the efficient multi-status sweep, above
  the one-ailment-at-a-time cures.

**MP restore:**

- **Ether** and **Hi-Ether** — restore MP. Keep casters going; a deliberate convenience tier, priced
  accordingly.

## Offensive alchemy & status-infliction — reserved (not decided here)

The Chemist's offensive identity is the **Faith-proof alchemy lane** — the deliberate third damage
shape on the roster:

- **Caster** = MA/Faith magic (scales on Faith; resisted by Magic-Evade).
- **Chemist** = **Faith-proof** thrown alchemy — works under Silence / anti-magic / low Faith, and
  cannot be Faith-floored.

The **framework is decided**, the **specific abilities are not** (and neither are any status-inflicting
throws — that space is intentionally left open). What is locked:

- **Field-Lab-gated** (free innate on the Chemist; portable to others as the Reagent Kit support at a
  two-slot + no-shield cost — Confidence **Strong**). The free innate + Faith-proof chassis is the reason
  to field; the export is welcome, not strictly better.
- **Faith-proof**, available **from early game** (so the Chemist is functional and fun in the opening
  band, with no dependence on a late-acquired weapon).
- **No-strictly-better vs Black Mage** (`docs/deep-combat-layer/11`, calibration `12`): magnitude below
  same-tier magic · finite (reagent stock / per-battle cost) · small splash with falloff · no Faith/MA
  scaling · no crits · range / LoS / height limits like a thrown object. Its win condition is the
  *corner* (armoured / low-Faith / Silenced / anti-magic targets), not raw ceiling.

Confidence on the specific offensive/status abilities: **Hypothesis** — to be designed in a later pass.

## R / S / M

- **Reaction — Auto-Potion**, with guardrails against the vanilla auto-immortality: post-damage only ·
  only if the unit survives the hit · once per own-turn-cycle · **Potion-line only** (no Elixir /
  Phoenix Down / Remedy) · no Item-Lore-style multiplier · X-Potion eligibility is a calibration gate
  (`12`) with Hi-Potion fallback if it tests as practical immortality.
- **Support — Throw Items** (export): use **support items at range**, single-target only. Wanted by every
  backline / rescue-oriented Items build; frontline self-sustain builds can skip it.
- **Support — Field Lab / Reagent Kit** (export): the innate, learnable — grants offensive-alchemy access
  (with Items) at the satchel cost (no shield). See *Innate*.
- **Support — Field Arms** (weapon-proficiency export, `15`): grants the Chemist's **Crossbow C + Knife D**
  to whatever job equips it (the Chemist is otherwise off the weapon axis; both grades export at source).
- **Movement — Ignore-Terrain-lite.**

*(R/S/M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot.)*

## Portable vs fielded — the key split

- **Light splash** (Items as a secondary command only): the **support kit** — Potion line, Phoenix Down,
  the cures, Remedy, Ether. The best utility splash in the game; carries **no offence**.
- **Heavy splash** (Items secondary **+** Field Lab / Reagent Kit support): also the Faith-proof offensive
  alchemy — at a real cost (two slots, **no shield**, and the holder's own chassis). A welcome option,
  never strictly better.
- **Fielded** (Chemist primary): Field Lab **free** (slots open) on the Faith-proof backline chassis with
  the full cure spine native, plus Throw Items. The free innate + chassis is what makes fielding it worth
  it — not exclusivity.

## Equipment & weapon aptitude

The Chemist is **off the weapon-grade axis** — its offence is *items*, the way casters are off-axis with
magic (`docs/deep-combat-layer/15`, *Weapon aptitude*). Weapons are a minor fallback.

| Slot | Grant |
|------|-------|
| Armour | **Clothes & Suits** |
| Off-hand | — (Field Lab occupies it; no shield) |
| Knife | **D** — melee poke |
| Crossbow | **C** — a light *mechanical* ranged fallback (not a firearm) |
| Gun | — (cut: a unique-character identity) |

## Early / mid / late

- **Early.** Already functional and characterful: cheap Potion / Phoenix Down / one status-cure ability,
  plus early Faith-proof alchemy for offence. No JP-starved dead band, no waiting on a late weapon.
- **Mid.** Comes into its own as the **Faith-proof toolbox** — triage, cleanse, and alchemy that work
  where casters get shut down; the export (support Items + Throw Items) feeds the whole squad.
- **Late.** Never a magnitude job — its value is **reliability** (flat, Faith-proof triage and revive)
  and the **anti-magic / armoured / low-Faith corner** its alchemy owns. A toolbox you bring for the
  matchup.

## Battle dynamics

**What the player does with it.** Field the Chemist as the **Faith-proof toolbox** for the matchup where
magic gets shut down or the squad is godless: triage and revive that no Silence/low-Faith can blank, and
thrown alchemy that **owns the corner** (low-Faith / Silenced / anti-magic targets) where the Black Mage
craters to its floor. It plays from the backline behind a screen (light, no shield), chipping with
Faith-proof flasks and topping the line with flat Potions; Auto-Potion buys it a few extra turns under
chip. As a donor it hands out the support Items spine (Throw Items), and — at a heavy cost — the Reagent
Kit (offensive alchemy) and Crossbow C. *(Sim: alchemy 30 sits between Black's 45 ceiling and 27 floor —
Black out-nukes the faithful, the Chemist owns the blanked corner.)*

**How an enemy version harms the player.** An enemy Chemist is a **reliability engine**: it Auto-Potions
through your chip, **revives with Phoenix Down**, and throws Faith-proof flasks that **Silence won't
stop**. Counterplay: **burst through** Auto-Potion (it is once-per-cycle, Potion-line only), focus the
revived target down again before it stabilises, or **dive** the shieldless light body (Move 3, folds to
melee pressure). Don't try to Silence it out of the fight.

## J1 — the pick / wrong-pick

- **The pick:** where magic gets shut down (Silence / anti-magic / Faith-hostile maps), vs **armoured /
  low-Faith** targets (alchemy is Faith-proof and bypasses DR), and to keep a caster-light squad alive
  Faith-free — all available **from the early game**.
- **Wrong pick (two-sided):** dive pressure (a light, shieldless backline body folds if reached); a raw
  DPS race into soft targets (alchemy is modest and finite — Black Mage deletes better); a healing-by-
  *volume* sustain race (the lane is triage, not throughput); and terrain / height that denies the
  thrown line. Its value is **reliability and the corner**, not power.
