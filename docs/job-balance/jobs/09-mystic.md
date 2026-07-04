# 09 — Mystic · "The Spiritbreaker"

The status caster, rebuilt as the **conduit-tuner**. It does not out-damage or out-heal anyone; it
**attacks the enemy's spirit** — bending Faith and Brave (the two-sided conduits no other job touches),
then landing the afflictions those bends open. Its loop is *degrade a resistance, then exploit it*:
raise a target's Faith and it now succumbs to magical control **and** to the team's nukes; lower its
Brave and it opens to the mental statuses. Vanilla Oracle was "great kit, terrible hit rates, zero
damage, never fielded" — the DCL turns each of those into a strength: the tune-then-strike loop makes
control **reliable**, a reliable HP-drain makes it **self-sufficient**, and every debuff now **chips**,
so no turn is ever fully wasted.

This doc records the design decision for the Mystic on the Deep Combat Layer (DCL). Mechanics it leans
on are owned by the DCL docs and cross-referenced inline; numbers are calibration
(`docs/deep-combat-layer/12`). Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier B.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a **mid**
  unlock reached past a first-rank job (`docs/job-balance/00-job-tree.md`). It keeps its vanilla tree slot;
  the DCL changes its *mechanics*, not its position.

## The vanilla problems it solves

Vanilla Mystic/Oracle (`docs/job-balance/vanilla/09-mystic.md`) is the classic "great kit, bad hit rates"
job — broad control that **whiffs**, no damage, and shut off entirely by status-immune bosses. Four
problems, each mapped to a design move:

1. **Multi-gated low hit rates (the whiff bot).** Vanilla infliction is MA/Faith/zodiac-taxed and misses
   constantly. **Fix:** the DCL 3d6 contest (`docs/deep-combat-layer/13`) plus the **tune-then-strike
   loop** — Belief/Trepidation first *lower the target's resistance*, so the follow-up status lands
   reliably (SIM 1). Setup converts a coin-flip into a high-odds play.
2. **Zero damage / un-fieldable (the support trap).** Vanilla Oracle can't pressure or protect itself.
   **Fix:** **Invigoration is a real, reliable self-sufficiency engine** (formula damage + capped
   self-heal, MP-costed) — a lone Mystic solo-clears a low-level pack, even surrounded (SIM 6, the J6
   floor) — and every contested debuff carries a **damage rider** so a resisted status is never a dead
   turn (SIM 9).
3. **Status-immune bosses shut it off.** **Fix:** conduit **tuning is a battle-scoped trait window**, not
   an all-or-nothing control flag — a boss can take *reduced* tuning but is rarely flat-immune to it, so
   the Mystic keeps a **meaningful but non-dominant** boss role (Disbelief the enemy mages, Belief a kill
   window, drain to sustain — SIM 4).
4. **Thin exports / mine-don't-field.** **Fix:** the innate **Astral Resilience** is a real, wanted export
   (anti-control protection), not a crutch (`docs/deep-combat-layer/15`, *No crutch exports*).

## Fantasy

The spiritbreaker reads the soul of an enemy and turns it against them. It inflames a doubter's faith
until the heavens themselves can strike him; it drains the courage from a champion until his own mind
betrays him; it blinds, silences, and curses; and it siphons the life out of a body to mend its own. It
fights with **belief and fear**, not fire — and it sees through any spirit aimed back at it.

## Chassis

- **High MA** (the status-contest *offense* stat — `docs/deep-combat-layer/13`; below the Black Mage's
  damage-MA) · **high MP** · **HP ~75** (low → also low physical-status resistance) · **low PA** · Speed
  **neutral** · **Move/Jump 3** · **NEUTRAL Faith** · **low Brave**.
- **Armour: Robes** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the mod draws no new art).
  ~No physical DR — it folds to a dive and lives behind the line.
- **Neutral Faith — the distinction from the damage casters.** Its offense (the status contest) runs on
  **MA, not its own Faith**, so it does not need to be a high-Faith glass cannon. **Low Faith was rejected
  as a near-free lunch** (magic-status resistance at no real cost, since a controller never wanted magic
  damage); **neutral** keeps an honest two-sided bolt/drain floor and leaves it normally disruptable —
  its self-defense is its *kit*, not a free stat. (Same neutral-Faith logic as the Time Mage; the two
  controllers share the chassis axis but nothing of the command.)
- **Low Brave** fits the backline (low physical offense, *Caution* reactions — `docs/deep-combat-layer/13`).
  Note the deliberate openness: low Brave is **mentally vulnerable** and low HP is **physically
  vulnerable** — the spiritbreaker is hard to attack *spiritually* (its innate) but easy to attack with
  **steel** (the intended counterplay).
- **Off the weapon axis:** low PA — output is magic and control, not melee. The caster weapon is the
  **Rod/Staff** — a free, range-3, MA-scaled bolt (`docs/deep-combat-layer/11`, the weapon-bolt floor).

## Innate — Astral Resilience (free, and exported)

The seer **sees through spiritual interference**: native Mystic carries improved resistance against
**magical statuses, mental statuses, and Faith/Brave tuning** (a bonus to the target side of those 3d6
contests — `docs/deep-combat-layer/13`). It is the signature learnable **Support**, the reason other
builds visit the job (the White-Liturgy / Black-Rod-Attunement pattern: one quality that is both the
innate moat and the export). Constraints that keep it clean:

- **Defense only — it touches no offense.** It does **not** improve any status the holder *inflicts*, and
  it is **export-safe**: slotted on another unit it cannot turbo-charge that unit's Stop / Death / Charm
  or any status rider (SIM 5). This is the lesson of the rejected first innate "Conduit Focus" — a
  reliability buff for the Mystic's *own* skills was a crutch that made the secondary feel broken without
  it; reliability was moved into the command (the tuning loop), and the innate became a perk wanted **for
  its own sake** (`docs/deep-combat-layer/15`, *No crutch exports* / *Desirability = base stats + innate*).
- **It is resistance, not immunity** (a meaningful bump in the contest, never a wall — calibration,
  `docs/deep-combat-layer/12`, **Hypothesis**).
- **It does not reduce magic *damage*** — only status/conduit attacks. The Mystic and its export holder
  still take full magic damage; the spirit-master resists being *controlled*, not being *burned*.

J2 (`docs/deep-combat-layer/15`): the export is **wanted** by a frontliner who fears enemy
Charm/Sleep/Stop and **skipped** vs a control-light enemy (a dead slot there) — matchup-dependent, never
an auto-include (SIM 7).

## Command — Mystic Arts

**The always-on action** is the **free Rod/Staff bolt** (range 3, MA-scaled, `docs/deep-combat-layer/11`).
Above it sit the tuning levers, the suppression, and the drain — so a Mystic turn is never dead (SIM 6/9).

**Core:**

> **The three tuning levers are magical STATUSES, not stat edits.** Each imposes a temporary, curable
> effect that makes the target *behave* as if its Faith/Brave were shifted (for the magic and affliction
> math) — it does **not** touch the unit's real Brave/Faith trait. **Permanent, direct change to the real
> Brave/Faith stat is the Orator's lane** (`docs/job-balance/jobs/13`, on the vanilla 4:1 rule); the two
> jobs touch the same dials by deliberately different mechanisms (temporary status vs. real stat). Like any
> targeted ability these hit **any unit** — an ally by friendly fire exactly as readily as an enemy.

- **Belief** (raise a target's Faith — a **finite, non-stacking, refresh-only** status window, *not* a
  rest-of-fight mark): opens the target to magical statuses **and** makes the team's magic hit it harder
  (×1.30 vs ×1.0 — `docs/deep-combat-layer/08`/`11`). On an enemy it is an **offensive setup**; the bounded
  window means a stacked-caster focus-fire is a **planned burst window, not a permanent magic tax** (SIM 3).
  Two-sided: Belief on an enemy caster also briefly raises *its* output until you exploit/kill it.
- **Disbelief** (lower a target's Faith — finite window): neuters an enemy caster's magic output, or
  **wards an ally** against an enemy mage (at the cost of the ally's own magic — two-sided).
- **Trepidation** (lower a target's Brave — finite window): cuts the enemy's physical offense and Courage
  reactions, **and** opens it to the mental statuses (Confuse). One prep opens **one** axis — Belief opens
  magical, Trepidation opens mental, never both at once; double-opening a unit costs two setup turns
  (SIM 2, the anti-omnicapability guard).
- **Blind** (magical-source soft status, resists on inverse Faith — `docs/deep-combat-layer/13`): cuts the
  target's hit chance (anti-physical-attacker), **plus a low damage rider** (spell-power below the bolt).
- **Silence** (magical-source soft status): locks an enemy caster, **plus the damage rider** (slightly
  higher MP than Blind so it is a choice, not the auto-button into every mage). Short duration — a landed
  Silence buys a window, not the whole fight.
- **Invigoration** (HP-drain — formula magic damage that **heals the Mystic for ~100% of damage dealt,
  capped by the Mystic's missing HP**, real MP cost): **the self-sufficiency engine.** Reliable (not a
  contest), range 3, single-target. It is the deterministic floor that lets a lone Mystic clear a
  low-level pack and survive (SIM 6); the MP budget (a handful of casts) means it **cannot drain-tank a
  boss forever**. Damage stays below Comet and far below the Black Mage's ladder — its value is
  *survival*, not burst.

**Tier-2 (costed):**

- **Sleep** (magical, inverse Faith — **pure**, no damage rider: damage would wake the target): a soft
  removal — the target sleeps until struck, so it is self-limiting.
- **Confuse** (mental, Brave — **pure**, damage would cure it): chaos; reliable after **Trepidation**, and
  the controller's group answer (a Confused low-level turns on its allies).
- **Frog** (magical hard-transform, capstone — **pure, no damage rider**): the hard removal that came from
  the Black Mage's old Toad. It stays pure on purpose: it is the highest-control button, so the **miss is
  its cost** — a damage rider would make risk-free hard control (GPT consensus). *(Open flavor swap: a
  player who prefers the Oracle's classic "turn to stone" can run **Petrify instead of Frog** — never both;
  `docs/deep-combat-layer/12`.)*
- **Empowerment** (MP-drain — anti-caster resource denial; a separate skill from Invigoration, since one
  action drains HP **or** MP, not both — `docs/deep-combat-layer/15`, engine-feasibility).
- **Harmony** (cleanse — **scoped** to spiritual/control statuses only; it is **not** a White Esuna or
  Chemist Purge replacement — `docs/job-balance/jobs/05-white-mage.md`).

*Not in the kit:* **Charm** and **Berserk** (Orator's social-conversion lane), **Slow/Stop** (Time's
clock), **Poison/Disease/Doom** (Necromancer's DoT/attrition) — the control-status ration keeps each door
rare (`docs/deep-combat-layer/13`/`15`).

## R / S / M

- **Reaction — Hex Ward** (*Caution* category, fires ∝ inverse Brave — fits the low-Brave body): when the
  Mystic is struck, a chance to reflexively inflict Blind (or a Brave-down) on the attacker — instinctive
  sabotage. Wanted by a defensive low-Brave build; a high-Brave Courage build takes a Counter instead.
- **Support — Astral Resilience** (the innate, learnable — see *Innate*): exports anti-control protection.
- **Support — Rod/Staff Training** (weapon-proficiency export, `docs/deep-combat-layer/15`): grants the
  caster weapon and its range-3 bolt to whatever job equips it.
- **Movement — Phase Step**: the Mystic may **move through occupied tiles** (units) — a ghostly seer's
  drift to slip out from behind its own frontline to safety or line-of-sight. Wanted by a boxed-in caster;
  skipped on open maps. *(Engine-feasibility, `docs/deep-combat-layer/12`: Tier-1 if a native pass-through
  movement flag exists, else Tier-2.)*

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot.)*

## Equipment & weapon aptitude

Pool from `docs/deep-combat-layer/15` (*Weapon aptitude*; mechanic owned by `docs/deep-combat-layer/10`),
spent **lean** — a caster, not a weapon user:

| Slot | Grant |
|------|-------|
| Armour | **Robes** (sprite-mandated — ~no physical DR) |
| Off-hand | **none** — no shield; a backline caster |
| Rod/Staff | **A** — the caster weapon: the free range-3 MA-scaled bolt under its real offense |

## Early / mid / late

- **Early.** Already self-sufficient and active: Invigoration (the drain engine), the bolt, Blind/Silence
  (debuff + chip), and the tuning levers — it clears low-level packs and never has a dead turn, with no
  dependence on a late unlock.
- **Mid.** The spiritbreaker proper: **Belief** an enemy open for the team's nukes, **Trepidation →
  Confuse** a champion, **Silence** the enemy healer, **Disbelief** the enemy mage, drain to stay alive.
- **Late.** The hard doors (**Sleep**, **Frog**), **Empowerment** to starve enemy casters, and the export
  hub (Astral Resilience) hardening the squad against enemy control. Never a damage caster — the master of
  the enemy's **spirit**.

## Battle dynamics

**What the player does with it.** **Tune, then break.** Read the enemy's Faith/Brave: **Belief** a durable
target so the team's nukes (and your own control) land harder, **Trepidation** a physical threat to soften
it and open Confuse, **Silence/Disbelief** the enemy casters, then land **Sleep/Frog** on whoever you have
opened. When nothing needs controlling, **Invigoration** is a reliable, self-healing attack that clears
chaff and keeps the Mystic alive (it solo-clears a low-level pack — SIM 6). As a donor it lends **Astral
Resilience** (anti-control) and Rod proficiency. It pairs with a **frontline screen** (it is fragile) and
with **damage casters** (it opens their targets).

**How an enemy version harms the player.** An enemy Mystic is **legible and counterable** — it *prepares*
before it disables, so you see the disable coming. It can Silence your healer, Sleep your carry, or Confuse
your line. Counterplay: **rush it** (robes, low HP — one diver folds it); field **faithless, high-Brave,
high-HP bruisers** (they resist *all three* of its axes — magical via low Faith, mental via high Brave,
physical-status via high HP — its whole kit bounces); and remember that **physical disables (Stun/Knockdown,
base-HP-resisted) and raw damage bypass its Astral Resilience entirely** — you beat the spirit-master with
**steel**, not spells.

## Two-sided cost (why it is not strictly-better)

- **Physically fragile** — robes, low HP, low PA: a single diver folds it, and physical Stun/Knockdown
  (base-HP, *not* covered by Astral Resilience) shut it down.
- **Hard-countered by the faithless bruiser** — a **low-Faith + high-Brave + high-HP** body resists its
  magical statuses (inverse Faith), its mental statuses (high Brave), **and** its physical statuses (high
  HP) all at once; even with a full setup turn the afflictions stay low-odds (SIM 1), the damage is
  irrelevant, and the right answer is to bring a Knight/Black instead. This is its clean declared weakness.
- **Setup costs tempo** — opening a neutral target is a two-turn play; it is 1-turn efficient only vs the
  devout/casters it is built to beat. Double-opening a unit (both axes) costs three turns (SIM 2).
- **Modest, single-target offense** — Invigoration is the floor, not a damage plan; the Black Mage
  out-damages it many times over (SIM 4). Its damage rider is consolation (below the free bolt — SIM 9),
  never a nuke.
- **Boss soft-spot** — vs a control-immune boss it is meaningful but **non-dominant** (Disbelief/Belief +
  drain), and a damage job contributes more (SIM 4).

Distinct from **Black Mage** (magnitude / burst / AoE), **White Mage** (heal / ward / revive), **Time
Mage** (the clock — Slow/Stop/Haste/Quick — and Gravity), the future **Orator** (Charm + Brave/Faith
*recruitment*-adjacent social conversion), and the future **Necromancer** (Poison/Disease/Doom/drain-state/
undead). **Mystic owns the spiritual-affliction suite** — Faith/Brave **tuning**, Sleep, Confuse, Frog,
Blind, Silence — and the spirit-defense innate. Three different "controllers" (Mystic / Time / Orator),
three different axes.

## J1 — the pick / wrong-pick

- **The pick:** fights where the enemy is **caster/devout-heavy** (high Faith → its control lands first
  turn; Disbelief neuters their mages; Silence locks their healer), a **single dangerous high-Faith
  threat** to disable, an **ally damage-caster** to set up with Belief, and **attritional** fights where
  its self-healing drain and resource denial grind the enemy down.
- **Wrong pick (two-sided):** a **faithless, high-Brave, high-HP** enemy roster (its whole kit bounces —
  bring damage), a **fast-dive** enemy (it folds before it sets up), a **control-immune boss** (meaningful
  but non-dominant), and a board that **just needs raw damage or healing** (Black/White do that — the
  Mystic only chips and drains).
