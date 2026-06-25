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

## Targeted spells can be dodged (magic dodge)

The one crossover from the physical layer: **targeted / bolt spells can be evaded**, using **Magic
Evade** as a "magic dodge". This homes the Magic Evade attribute (`01`) and gives fast/evasive units
a way to duck a single-target spell, mirroring the physical Dodge floor (`04`).

- **Single-target / bolt spells:** the target rolls magic-dodge to evade, analogous to the physical
  defense roll.
- Magic dodge is the magic-axis analogue of Dodge — an always-available floor, not a depleting
  defense (there is no magic Parry/Block).

## Charged spells are not interrupted by damage

Charge times are FFT-native. A charging spell is **not** interrupted by taking damage (vanilla
behaviour) — it still resolves unless the caster is **incapacitated** (KO'd, or hit by a status that
stops them) or struck by a **specific interrupt skill**. Interruption is therefore *ability-driven*
and legible, not an automatic consequence of any hit; **Brave (composure)** resists interrupt skills
(`07`). This keeps charged magic reliable to plan around, while still allowing dedicated anti-caster
builds via an explicit interrupt tool.

## Area spells interact with facing and position

**Area-of-effect spells interact with facing and position** (`05`): where a unit stands and which way
it faces matters for area magic, tying magic into the same spatial game as physical combat. Standing
clustered is punished by AoE; spacing out trades that for vulnerability to being picked apart
physically. (Exact facing interaction for AoE is open — `12`.)

## The trait stack on magic

Magic sits at the intersection of two of the three permanent traits:

- **Faith** (`08`) scales magic output and magic vulnerability (two-sided). This — *not* Brave — is
  the magic offense axis, which is what keeps the Brave slider clean (`07`).
- **Zodiac** (`09`) gives the target an elemental resist/weakness that applies to elemental spells.
- **Brave does not touch magic at all.**

A spell that is both elemental and Faith-scaled stacks both interactions (Faith multiplier × Zodiac
elemental multiplier); the exact stacking order is a tuning item (`12`).

## Holy and Dark

Sacred (Holy) and Dark are **spiritual**, not elemental: they scale with and are resisted by
**Faith** (`08`), and are *outside* the Zodiac elemental wheel (`09`). Lightning is a neutral
element with no zodiac sign attached.

## Open items

The exact magic damage formula (how MA/MP/Faith combine), magic-dodge values, AoE × facing rules,
the Faith×Zodiac stacking order, and MP economics are deferred — `12-open-questions.md`.
