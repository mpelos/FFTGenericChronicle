# Orator

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Orator layers — `22`, `55` (Orator rows), `74`, `75` (V1, git history), and
`76` (the accepted V2 redesign) — folded into this single decision doc. V2 was the
GPT/Claude-consensus rewrite (`Call Out` accepted with a visible `Called Out` marker).

> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Orator is **strongly native to the DCL**: the
> engine already makes Brave and Faith **two-sided systems** (Brave-inverted taunt, courage/caution
> reactions, inverse-Faith resist), and Orator's whole kit is "named Brave/Faith pressure" — exactly
> what the DCL rewards. See *DCL rebase notes*.

## Identity / compass

Orator is the **social battlefield controller and recruitment specialist**: it recruits, rallies,
shames, baits, exposes targets to magic, suppresses magic, interrupts pending actions, and falls back
to guns when speech is the wrong turn. The vanilla job fails because most Speechcraft turns are too
small or too disconnected from immediate value; V2 keeps the FFT texture but rebuilds every action
around a real battle window that beats attacking.

Orator should be **better than every other job at social control, recruitment, and morale pressure**.
It must **not** become a Mystic replacement, a Time Mage replacement, a pure gun job, a permanent-stat
grind chore, or a generic hard-disable chain.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `controller` |
| Secondary tags | `recruit`, `gun` |
| Growth profile | hybrid |
| Armor class | `leather` |
| Weapon families | gun, knife, fists (missile / thrust / crush) |
| Role reason | Social manipulator; matters in combat through morale, speech, recruitment, Brave/Faith/status, and gun fallback. |

**Good at:** flipping/recruiting enemies, morale pressure on high-Brave threats, Faith/Atheist setup
windows, tempo interruption, gun fallback on a utility chassis.
**Bad at / countered by:** Silence, status immunity, boss/protected targets, low-value control
targets, fast setup-punishers, ranged pressure into leather durability.

## Shared status & marker vocabulary

| State | Meaning | Orator source |
|-------|---------|---------------|
| `Traitor` | target allegiance-flipped for the battle; damage does not break it; recruit if it survives | `Entice` |
| `Chicken` | morale collapse (cleared by `Praise`, pressured by `Intimidate`) | `Praise` / `Intimidate` |
| `Faith` | visible receptivity spike — stronger magic dealt **and** received (double-edged) | `Preach` |
| `Atheist` | visible narrow anti-magic state | `Enlighten` |
| `Called Out` | visible target-pressure marker naming the declared challenger/protected ally | `Call Out` |
| `Doom` | visible delayed KO (countdown 4) | `Condemn` |
| `Berserk` | forced reckless offense | `Insult` |
| `Sleep` | misses one action / wakes on damage | `Mimic Darlavon` |

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Entice** | Heavy social-charm + recruitment payoff: flip an eligible enemy, recruit if it survives. | Eligible human, non-boss, non-protected only; `Traitor` is a payoff to prior pressure, **not** a turn-one conversion lottery; hard cap + active-flip cap required; survival-gated recruitment. *(v0.2: low/moderate base; bonus vs low-HP / low-Brave / recently `Intimidate`d or `Called Out`.)* |
| **Stall** | Direct tempo answer; cancels a pending charge/performance. | No `Slow`, no Speed change, no ongoing lock; Silence/immunity apply. *(v0.2: CT −50; interrupts `Charging`/`Performing`.)* |
| **Praise** | Ally morale rally + repair; clears `Chicken`. | Permanent positive Brave drift uses the game's existing rule; this pass adds no new Brave cap/rate. *(v0.2: Brave +20, capped below extreme optimization.)* |
| **Intimidate** | Enemy morale pressure and `Entice` setup. | **Battle-only**; no permanent Brave loss; no invisible fear status; normal-Brave enemies are pressured, not disabled. *(v0.2: Brave −20.)* |
| **Preach** | Visible `Faith` setup window for a high-value spell/heal. | Double-edged (target deals **and** receives stronger magic); single target; no broad Faith engine; Mystic stays the deeper spiritual controller. Permanent positive Faith drift uses the existing rule. |
| **Enlighten** | Visible `Atheist` anti-magic window (shield an ally / shut a caster). | Narrow duration; non-stacking; cleansable; **no** permanent Faith loss; must not invalidate the magic ecosystem. |
| **Condemn** | Ranged delayed lethal pressure on durable targets. | Visible `Doom`, countdown 4; no instant KO; no damage rider; immunity/cure policy respected. *(v0.2: low-to-mid hard-status success.)* |
| **Call Out** | **Signature** non-vanilla control: Brave-band public pressure via the challenge/taunt model. | Visible `Called Out` marker naming the pressure target; **not** Confuse/Berserk/raw-Brave-reduction; no permanent effect; T8 constraints (lethal/self-preservation/objectives/immunity) hold. *(v0.2 bands: low → resists, minor CT shame; normal → soft challenge; high → strong overcommitment; extreme → hard-overcommit stress case.)* |
| **Insult** | Forced-offense disruption when the enemy's non-attack options are the real threat. | Visible `Berserk`; Brave-band-sensitive (bold targets easier); shortest readable duration; can backfire; no target-selection ownership (keeps it distinct from `Call Out`); no Brave-stat reduction (distinct from `Intimidate`). |
| **Mimic Darlavon** | Clean single-target pause. | Visible `Sleep`; damage break; immunity respected; no default multi-turn lock. |

**Name change:** `Call Out` replaces `Defraud` (Gil economy out of scope). Final localized naming may
pick a better speech-flavor name; the accepted design point is Brave-sensitive public pressure.

## Reaction / Support / Movement

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Fast Talk** | Speech tempo-friction reaction (not evasion/heal/counter). | Triggers when targeted and still able to speak; **non-Brave**; does not prevent damage or counterattack; fails if Silenced; attacker loses CT. No once-per-round clause — the fixed chance / CT bite is the only dial (watch for a focus-fire CT-lock engine). |
| Support | **Equip Guns** | Major Orator export: PA/MA-independent missile pressure for low-stat builds. | No new guns; no gun-formula change; no damage rider; competes with all supports; **high-JP / late-export** posture; top incidence watch. |
| Support | **Silver Tongue** | Speechcraft reliability within category caps. | Speechcraft only; no immunity bypass; does not touch Steal/spells/items/guns/attacks; recruitment + hard-status caps still apply. |
| Support | **Tame** | Protected monster recruitment/breeding route. | Keep with **access promise**; monster trigger/eligibility deferred to the monster pass; do not reuse this record for unrelated effects. |
| Support | **Beast Tongue** | Protected monster-Speechcraft route. | Keep with **access promise**; must stay compatible with later breeding/Poach planning. |
| Movement | **Social Positioning** | Reach the right speech line without becoming a mobility job. | Move +1, Speechcraft range +1 only; no gun/spell/item range; no terrain/elevation bypass. |

## Cross-job ownership note (Thief ↔ Orator)

V2 consolidates the **heavy** social-charm / recruitment identity into Orator's `Entice`. Thief keeps a
lighter `Steal Heart` charm (see `jobs/06-thief.md`); the two must stay deliberately distinct — Orator
owns recruitment and morale, Thief owns the opportunistic in-combat charm. This is a flagged follow-up,
not a silent rewrite of either job.

## Open items / validation hooks

- Watch: `Entice` as a turn-one conversion lottery; `Call Out` as a universal hard taunt; `Enlighten`
  invalidating magic-heavy encounters; `Fast Talk` CT-lock under focus fire; `Equip Guns` as a default
  support on too many builds; `Entice` flip-cap complexity.
- `T4` accuracy/recruitment, `T5` status/duration, `T8` targeting overrides (`Call Out`/`Insult`),
  `F5` real-roster, campaign/recruitment rows (`Entice`, `Tame`, `Beast Tongue`).
- Control-identity sweep: `Stall`/`Condemn`/`Mimic Darlavon` vs Time Mage and Mystic.

## DCL rebase notes

- **Brave is two-sided in the DCL**, which is exactly Orator's wheelhouse. The DCL taunt is
  **Brave-inverted** (high-Brave glass-cannons are baitable), so `Call Out`'s "high Brave → strong
  overcommitment" band is the engine's own taunt behavior; `Call Out` becomes the named Orator entry
  into the DCL challenge/taunt model (`15` + DCL `13`). `Insult`/`Intimidate`/`Praise` are named Brave
  uses the new ecology explicitly preserves.
- **Faith is two-sided in the DCL** (magic damage scales with caster Faith; **inverse-Faith** governs
  resistance). `Preach` (`Faith` window) and `Enlighten` (`Atheist` window) re-express directly as
  temporary Faith/anti-Faith deltas on that pipeline — single visible windows, with Mystic owning the
  deep engine.
- **Status infliction** (`Doom`, `Berserk`, `Sleep`, the `Entice` flip) runs the DCL **3d6 contest**
  (`13`); the mental/social ones resist on **Brave/will**. The % targets re-express as 3d6 numbers.
- **Stall** (CT loss + charge/performance interrupt) is engine-neutral — CT and charged actions exist
  in the DCL and are not damage-interrupted; `Stall` is the explicit speech interrupt.
- **Guns are skill-primary in the DCL** (weapon-skill → penetration); the gun-fallback identity and
  `Equip Guns` export ride that pipeline unchanged.
- **Fast Talk** is non-Brave and post-targeting → the DCL **neutral reaction** category (fixed chance,
  CT bite, no damage prevention).
