# Trait: Faith — Magic/Spiritual Temperament

Status: Draft (shape locked; tuning open)
Date: 2026-06-25
Depends on: 01-attribute-map, 07-trait-brave, 11-magic.
Review: Pending.

## Role

Faith is the **magic/spiritual** member of the permanent-trait trio (Brave = body, Faith = spirit,
Zodiac = element). It stays **FFT-native** in shape — Faith is already a two-sided slider in vanilla
FFT and the DCL keeps that, because it is exactly the kind of permanent, transparent, two-gumes axis
the DCL wants.

## Two-sided by nature

Faith scales **both** sides of magic:

- **+ Magic output** — your spells (and your friendly magic's effectiveness) scale up with Faith.
- **− Magic vulnerability** — but high Faith also makes you take *more* magic damage **and resist
  magical *statuses* worse** (sleep, petrify, stop… resist on **inverse Faith**: low Faith resists,
  high Faith succumbs; the contest's offense is the caster's MA — `13`, validation A2). The devout are
  open conduits in both directions, statuses included.

So, symmetric to Brave: high Faith is a magic glass-cannon stance, low Faith is magic-resistant but
magically inert.

### Floor 0.60

Faith's effective multiplier is **floored at 0.60** (carried over from the validated v0.2 policy's
Faith handling — one of the few places the two tracks happen to agree on a number). The floor stops
very-low-Faith units from being completely immune to (and useless at) magic; even a faithless unit
is a *somewhat* valid magic target and can be *somewhat* affected by buffs/heals. It keeps magic
relevant against everyone.

## Relationship to Brave (the clean split)

Faith and Brave partition the offense space cleanly and **this partition is what makes both sliders
work** (see `07-trait-brave.md`):

| | Physical | Magic |
|--|----------|-------|
| **Offense scaled by** | Brave | Faith |
| **Defensive cost of going high** | −active defense (get hit more) | −magic vulnerability (take more magic) |

Because **magic lives only on Faith and never on Brave**, a mage has a real reason to want *low*
Brave and *high* Faith, and a physical bruiser the reverse. Neither slider is universally good. This
decoupling is the core of the trait design.

## Holy and Dark belong here

Sacred (Holy) and Dark damage are **spiritual** damage and sit on the Faith axis, not the elemental
Zodiac axis (`09`). They scale with and are resisted by Faith like other magic, fitting their
flavor as faith-aligned forces rather than elements.

## Open items

Exact Faith curve, the magic-vulnerability slope, how Faith interacts with the Zodiac elemental
multiplier on a spell that is both elemental *and* faith-scaled, and final tuning are in
`12-open-questions.md` and `11-magic.md`.
