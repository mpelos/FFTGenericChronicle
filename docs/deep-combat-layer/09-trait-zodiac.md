# Trait: Zodiac — Elemental Temperament

Status: Draft (approach approved; 12→4 mapping proposed; tuning open)
Date: 2026-06-25
Depends on: 01-attribute-map, 11-magic.
Review: Pending.

## Decision: repurpose Zodiac (option b, approved)

Vanilla FFT uses the zodiac sign only as a **hidden compatibility multiplier** between attacker and
target — an opaque good/bad/neutral modifier the player can't see and can't plan around. The DCL
**replaces** it (satisfying the use-or-replace constraint, `01`) with a **transparent elemental
temperament**: an innate affinity to one element and a weakness to its opposite.

This completes the permanent-trait trio:

- **Brave = physical temperament** (`07`)
- **Faith = magic temperament** (`08`)
- **Zodiac = elemental temperament** (this doc)

Three permanent, transparent, two-sided personal axes — the same "should I lean into this?" question
asked across body, spirit, and element.

## The two-sided effect

A unit's sign grants:

- **+ Resistance** to its affinity element (takes less of that element; arguably deals more of it).
- **− Weakness** to the opposite element (takes more of that element).

Small, transparent, always visible. It is a flavor-rich nudge, not a dominant multiplier — the
intent is readability and identity, not a hidden swing that decides fights.

## 12 signs → 4 elements

Marcelo's concern: there are **12 signs** but few magic elements (~4 classical). The mapping uses
the standard astrological element grouping, collapsing 12 signs onto 4 elements (3 signs each):

| Element | Signs | Opposite element |
|---------|-------|------------------|
| **Fire** | Aries, Leo, Sagittarius | **Water / Ice** |
| **Earth** | Taurus, Virgo, Capricorn | **Wind / Air** |
| **Wind / Air** | Gemini, Libra, Aquarius | **Earth** |
| **Water / Ice** | Cancer, Scorpio, Pisces | **Fire** |

Opposition pairs: **Fire ↔ Water**, **Earth ↔ Wind**. This gives two clean opposed axes, each
sign resisting one element and weak to the one across from it.

### Why this works with few elements

- Three signs share an element, so any given element shows up on ~1/4 of units — common enough to
  matter, rare enough to feel like an identity.
- The four classical elements map directly onto FFT's existing element system (Fire, Ice/Water,
  Wind, Earth), so **no new element infrastructure is needed** — it reuses what's there.
- Lightning, Holy, and Dark sit *outside* the zodiac wheel: Holy/Dark are spiritual and live on the
  **Faith** axis (`08`); Lightning is a non-zodiac element (no sign resists/weakens it) — a neutral
  element by design, which is fine and keeps the wheel to the clean classical four.

## Reuses the element system

Because the effect is "resist element X / weak to element Y", it runs entirely on the existing
elemental damage multipliers. A unit's sign simply tags it with one elemental resistance and one
elemental weakness. Transparent in the menu, computed with the same math as any other elemental
interaction (`11-magic.md`).

## Open items

The magnitude of the resist/weak (how big a multiplier), whether the affinity also boosts *dealing*
that element or only resisting it, the exact handling of Lightning's neutrality, and whether
physical elemental weapons interact with Zodiac are in `12-open-questions.md`.
