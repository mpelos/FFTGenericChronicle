# 13 — Orator · "The Demagogue"

The social specialist, rebuilt as a **gunslinger demagogue** — a two-pillar job that wins fights with
**lead and voice**. The **gun** is a real damage pillar (the job is the roster's firearms expert): reliable,
long-range, **stat-independent** (skill-scaled per the DCL gun model, `docs/deep-combat-layer/10`) and
**armour-defeating**. The **voice** is the game's **only editor of the real Brave/Faith stat** (permanent on
the vanilla 4:1 rule, any target), plus the social-control suite (Charm, Berserk, the taunt) and **monster
recruitment**. It is no longer the vanilla "useless talker": the gun makes it self-sufficient, and costing
the voice makes it *reliable*.

This doc records the design decision for the Orator on the Deep Combat Layer (DCL). Mechanics it leans on are
owned by the DCL docs and cross-referenced inline; numbers are calibration (`docs/deep-combat-layer/12`).
Method: `docs/job-balance/job-design-process.md`.

## Tier & tree position

- **Tier B.** Tier is **acquisition position**, not power (`docs/deep-combat-layer/15`, *Tiers*): a **mid**
  unlock past a first-rank job (`docs/job-balance/00-job-tree.md`). It earns its keep both as a primary (the
  gun + the voice) and as a donor (the **Master Gunner** weapon export; rationed — see *R / S / M*).

## The vanilla problems it solves

Vanilla Mediator (`docs/job-balance/vanilla/13-orator.md`) is **"combat-null, unreliable, never carries a
fight."** Four concrete problems, each mapped to a design move:

1. **The whole kit is unreliable.** Vanilla Talk is **instant and free**, so the devs taxed its hit-rates to
   near-uselessness. **Fix:** the voice **costs MP** — and in exchange it is **reliable** (a DCL 3d6 contest,
   not a coin-flip). Reliability is bought, not free.
2. **No combat presence (the "useless" feel).** Vanilla Mediator can't carry a turn. **Fix:** the **gun is a
   damage pillar** — the roster's firearms expert (it is the only job that owns generic guns; the Chemist has
   none — `docs/job-balance/jobs/02-chemist.md` — and Mustadio is a *unique character*, not the mechanic's
   owner). It alone clears a low-level pack (J6, SIM 1) and gives the job a real offensive identity (SIM 9).
3. **It competes badly with the Mystic on status.** **Fix:** clean lanes by **mechanism** — the Mystic edits
   *magical* Brave/Faith (a temporary **status** that doesn't touch the stat); the Orator edits the **real
   stat** (permanent, 4:1). Same dial, opposite mechanism (SIM 3, and `docs/job-balance/jobs/09-mystic.md`).
4. **Recruitment is feast-or-famine.** **Fix:** cut human **Invite** (Charm covers the in-battle flip;
   generic human recruits are not worth it); keep **monster recruitment** (Tame) — the Orator is the only job
   that can do it, a protected game system (SIM 8).

## Fantasy

The demagogue rules a battlefield by **persuasion and firepower**. It talks a wavering enemy into turning its
guns on its own line; it goads the bold into charging a trap; it steadies an ally's nerve for the rest of the
campaign or shakes an enemy's faith on the spot; and when words run out it answers with a gun that punches
through plate at any range. Lead *and* voice — never one without the other.

## Chassis

- **Light/cloth armour** (`docs/deep-combat-layer/14`) — **mandated by the sprite** (the gentleman/lady; the
  mod draws no new art). Defence = Dodge + distance + position.
- **HP ~80** (a hair tankier than the robe casters' ~75) · **modest PA/MA** · Speed **moderate** · **Move/Jump
  normal** · **NEUTRAL Brave & Faith** (the manipulator commits to neither pole).
- **The gun is its weapon** — **two-handed** (no shield). Its offence does **not** scale on PA/MA (the gun is
  skill-scaled); MA is only the **contest hook** for the voice (it is *not* magic — no element, no
  Faith-scaling, no Magic-Evade, no Reflect, no Short-Charge; **Silence** blocks the voice because it is
  spoken).

## Innate — Master Gunner (free, and exported)

**Armour-defeating gun mastery.** The Orator's gun shots realise the **full skill→penetration** of the DCL gun
(`docs/deep-combat-layer/14`, the over-cap) — its bullets defeat armour where a borrowed gun would not. This is
the attractive, **every-fight** innate (vs a monster-rapport innate, which is dead on the common map). Per the
portability rules (`docs/deep-combat-layer/15`), Master Gunner is **one collapsed package** that is both the
innate *and* the learnable **weapon-proficiency Support**: equipping it grants **Gun A + the full penetration**
in a single slot (two separate supports could never be equipped together — one slot). Export-clean and
build-defining; it is *not* a crutch (it makes no skill "work as a secondary" — it is raw weapon mastery).

> **Penetration = the gun's full DAMAGE DIVISOR, not DR-ignore.** The shot stays a physical missile —
> **dodgeable, blockable** where ranged Block applies, and **LoS/terrain-blocked**. It does **not** ignore
> DR wholesale (that — plus AoE and magic burst — is the **Black Mage's** lane). The gun is reliable
> anti-armour *single-target* pressure, nothing more.

## Command — "Speechcraft" (words & bullets)

One command, two pillars (a generic has one skillset). The **gun half is pure damage/geometry**; the **voice
half is social/stat/control**; the strong control is **Tier-2 and MP-costed** so the job is wide but **not
omnicapable** (SIM 10).

**🔫 Lead (the gun — damage, no control):**

- **Basic gun shot** — the always-on pillar: reliable, long-range, stat-independent, armour-defeating
  (Master Gunner). Clears a low-level pack alone (J6, SIM 1). It is **dodgeable** — it defeats armour, **not**
  evasion (the ≤1-axis ration, `docs/deep-combat-layer/13`; evasive targets are the **Archer's** Aim job).
- **Barrage** — a **committed** multi-hit burst into one target (a CT charge / high MP), the anti-armour
  damage **spike**. Per-hit is **below** the basic shot — the value is the commitment, not "the basic shot but
  bigger" (SIM 9). Distinct from the Archer's Rapid Shot (immediate soft-target bow tempo).
- **Piercing Shot** — a single shot down a straight **line** (LoS): every unit in the path rolls defence;
  per-target is **below** the basic shot — the **geometry** is the value (SIM 9). **Friendly-fire** like any
  line. Collides with no one (Black/Summoner own *magic* AoE; the Dragoon's Skewer is reach-2 *melee*).

**🗣️ Voice (the MP-costed, reliable social kit):**

- **Praise / Threaten** (raise / lower the target's **real Brave**) and **Preach / Doubt** (raise / lower the
  target's **real Faith**) — the game's **only** permanent Brave/Faith editor. Any target (**friendly fire**
  works). Permanent change follows the **vanilla 4:1 rule** (4 points of in-battle shift = 1 permanent), and is
  **reversible** both ways — a deliberate roster-planning tool (a low-Brave tank, a high-Faith caster), not a
  way to ruin a unit. The 4:1 is **symmetric** (an enemy Orator can permanently shift player stats, vanilla-style).
- **Call Out** — the **Brave-inverted taunt** (`docs/deep-combat-layer/13`): baits **high**-Brave units (their
  B9 overcommit bites) and is refused by **low**-Brave (SIM 5). The **second and final** taunt door (the Knight
  is door #1 — `docs/job-balance/jobs/03-knight.md`); distinct delivery (Knight = melee protective draw; Orator
  = ranged morale bait). No third job gets a taunt.

**Tier-2 (MP-costed — the strong control + recruitment):**

- **Entice / Charm** — flip an enemy to your side **for the battle** (no permanent human recruit — Invite is
  cut). A poor bet fresh, **reliable only after setup** (soften + Call Out), boss/named/protected-immune, and
  **capped at one active Traitor per Orator** so it can't snowball the action economy (SIM 4). A flipped
  Traitor cannot itself Entice (no chain).
- **Insult** — **Berserk** (force a target into mindless attack). Orator-owned by lane.
- **Tame** — recruit a **monster** (monster-only, non-boss, eligibility-gated). The protected monster-recruit
  route (SIM 8).

## R / S / M

- **Reaction — open.** No signature reaction that isn't a crutch; better an honest generic slot than a fake one.
- **Support — Master Gunner** (the innate, learnable — see *Innate*): exports **Gun A + full penetration** in
  one slot. The signature donation; the borrowed kit is **basic shots only** — Barrage / Piercing / Speech do
  **not** travel, so the dedicated Orator stays the expert, and a heavy-armour host pays **2H (no shield) + a
  slot** for a flex ranged pick (SIM 6).
- **Support — Beast Tongue** (the protected monster route): monster rapport / recruit access; what a monster-team
  build wants, skipped otherwise — niche-but-wanted, export-clean (J2). *(A learnable Support, not the innate —
  a monster-only innate would be dead on the common map.)*
- **Movement — open.**

*(R / S / M is a **set**, not one-of-each: one reaction, several supports, one movement — equip one per slot.)*

## Equipment & weapon aptitude

Pool from `docs/deep-combat-layer/15` (*Weapon aptitude*; mechanic owned by `docs/deep-combat-layer/10`):

| Slot | Grant |
|------|-------|
| Armour | **Light/cloth** (sprite-mandated) |
| Off-hand | **none** — the gun is two-handed |
| Gun | **A** — the firearms expert; the damage pillar (Master Gunner gives full penetration) |

*(No martial melee weapons — its output is **gun + voice**. The single A is the gun, per "one A below the
capstone tier", `docs/deep-combat-layer/15`.)*

## Early / mid / late

- **Early.** Already a real combatant: a reliable armour-defeating gun (no dead turns, no late-unlock
  dependency) plus the opening voice plays (Praise/Threaten, Call Out).
- **Mid.** The demagogue proper: **soften with the gun → Charm** a key enemy, **Call Out** the enemy's bold
  striker into a trap, **Insult** a threat into Berserk, **Doubt** an enemy caster or **Preach** your own.
- **Late.** Battlefield persuasion at scale (recruit monsters with **Tame**, shape the roster's Brave/Faith
  across the campaign) and the **Piercing Shot / Barrage** firepower — the export hub for guns (Master Gunner).

## Battle dynamics

**What the player does with it.** **Talk and shoot.** Open with the gun (it punches plate at range); **soften**
a target and **Charm** it for an extra body; **Call Out** the enemy's high-Brave striker to pull it out of
position; **Insult** a bruiser into Berserk; **Doubt** the enemy mage or **Preach** your own caster's Faith;
line up a **Piercing Shot** through a column or **Barrage** a tough target. As a donor it lends **Master Gunner**
(ranged anti-armour to a melee line) and **Beast Tongue** (the monster route) — never its command techniques.

**How an enemy version harms the player.** An enemy Orator **turns your own tools against you** — Charms your
strongest unit, Berserks your bruiser into the open, Calls Out your bold striker, and chips you with an
armour-defeating gun; over the campaign it can even shift your units' Brave/Faith (4:1, symmetric — reversible
with your own Orator). Counterplay: **field low-Brave / disciplined units** (they refuse Call Out and resist
the social contest), **rush it** (light armour, no shield — it folds to a diver), and **kill it before the
setup lands** (a fresh Charm is a poor bet — deny it the soften-then-flip tempo).

## Two-sided cost (why it is not strictly-better)

- **Physically fragile** — light armour, two-handed gun (no shield): a diver folds it. Its safety is range and
  position, which pressure removes.
- **The gun is dodgeable** — it defeats armour, **not** evasion: an evasive target is its weak matchup (and the
  **Archer's** job), and a low-Brave/disciplined enemy refuses its social plays.
- **The voice is MP-gated** — reliability is bought; out of MP it is a lone gunner. Charm is setup-dependent and
  capped (one active Traitor) — never a turn-one lottery or a snowball.
- **No control on the gun** — every aimed disable (immobilize / disarm / Don't-Act) is rationed to other jobs;
  the gun is damage only. The Orator out-shoots the Archer **only** at the flat armoured-target-in-LoS shot —
  the Archer keeps evasion (Aim), soft-target tempo (Rapid), zone (Pin), and vertical (Vantage) (SIM 7).
- **A wide kit, but tiered** — the strong control (Charm/Berserk) and recruitment (Tame) are Tier-2 and
  MP-costed; the gun adds damage *shape*, not a new control axis, so it is not omnicapable (SIM 10).

Distinct from the **Mystic/Oracle** (the **spiritual-affliction** suite and *magical* Brave/Faith **status** —
`docs/job-balance/jobs/09-mystic.md`; the Orator owns the **real-stat** edit and **Charm/Berserk**), the
**Archer** (ranged **precision/control** — Aim, Pin, Rapid, Vantage — `docs/job-balance/jobs/04-archer.md`; the
Orator owns reliable armour-defeating **firepower**), the **Knight** (taunt door #1, melee), and the
**Time Mage** (the clock). The **Orator owns generic GUNS** (the firearms expert) and the **real Brave/Faith
stat** (the only permanent editor) plus **Charm / Berserk / monster recruitment**.

## Open items / calibration

All numbers are frozen DCL placeholders (`docs/deep-combat-layer/12`): the gun's base output and penetration
divisor; Barrage's hit count / per-hit / charge cost; Piercing Shot's per-target falloff and line length;
the voice MP costs and 3d6 contest curves (Charm setup bonus, Call Out Brave-inversion magnitude); the active-
Traitor cap interaction with multi-Orator parties; the Reaction and Movement slots (left open until a non-crutch
candidate appears). The **friendly-fire / 4:1-symmetric** Brave/Faith policy is owned here and cross-referenced
from `docs/job-balance/jobs/09-mystic.md`.
