# Geomancer (v2, melee) — battle-simulation gate record

Job redesigned (Marcelo-directed) from a ranged weakness-battery into "The Elemental Reaver": a melee
Axe-A bruiser whose tactical options come from the terrain. Script: `*-sim-geomancer.py`. GPT peer
(gpt-5.5, thread 019f1030) consulted across 3 brainstorm rounds (10 ideas -> convergence -> Marcelo's
corrections -> locked structure), 100% on the structure.

## Reads (all clean)
- **Design law (SIM 1):** Geomancy stays BELOW a normal melee hit even on-weakness (63 vs 93) -> never a
  "better normal attack" / never melee spam. Its value = reach/element/status. Holds structurally.
- **J1 (SIM 2):** melee bruiser first (normal Axe A); Geomancy = the tactic (reach 42 when it can't close;
  weakness 63; vs Heavy the DR-subject element craters to 15 -> use the axe / a Black instead).
- **Status (SIM 3):** tactical + reliable-by-position — Oil combo (fire follow-up 63->94), Blind halves an
  attacker (42->21). Reliable = must stand on the terrain + high base rate, still resisted/immune.
- **Survival (SIM 4):** two-sided — shrugs magic (low Faith, TTK ~5.7), folds to a focused physical dive
  (TTK ~2.3). Frontliner that fights back (normal attack + Nature's Wrath), not a wall.
- **Portability (SIM 5):** Landreader-gated element; Axe A exports like Bow A (welcome splash, host pays
  slots + own body). Axe-A magnitude flagged for the grade-budget reconciliation.
- **Lane check (SIM 6):** distinct from Knight/Monk/Samurai/Dragoon/Black; enemy-use counters all work.

## Calibration watch (NOT a design break, doc 12)
- **HP / durability:** at the placeholder HP 130, a dedicated high-Brave bruiser dives it in ~2 turns. As
  a melee FRONTLINER (it lives in melee) it likely wants a bit more HP than 130 so it is not 2-shot — its
  identity is magic-resist + fights-back, not glass. Tune HP/Block so the frontliner survives a normal
  exchange while still folding to COORDINATED focus-fire.
- Standard placeholders (Geomancy base/element mults, status rates, Axe-A export magnitude) -> doc 12.

Registered: `docs/job-balance/jobs/13-geomancer.md` (Tier B).
