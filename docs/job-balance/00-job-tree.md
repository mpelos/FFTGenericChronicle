# Job Tree

Engine: **Deep Combat Layer** (`../deep-combat-layer/`).

This document defines the **shape and intent** of the generic job tree — which jobs exist, which
vanilla slots are replaced, and which are re-tiered — together with the feel the tree reaches for and
the trade-offs each move carries.

It deliberately **defines no individual job**: no kit, stats, skills, reaction/support/movement, JP
costs, equipment, or exact prerequisites. Several jobs receive large redesigns, so the precise wiring
is left open on purpose. What is fixed here is the *intent* and the *confirmed structural moves*;
per-job design and the exact prerequisite graph come later.

The baseline for every swap below is the vanilla/IVC roster analysed in `vanilla/` (20 generics +
Ramza). The job **count is unchanged** — every move is a replacement or a re-tiering, never an
addition or a removal.

## The feel the tree reaches for

The tree is a promise about reward. A few intertwined intents:

- **Depth must pay off.** A job deep in the tree costs a long grind to reach, so reaching it has to
  *feel* like arriving somewhere great. The deepest slots are reserved for genuinely exciting,
  top-tier jobs — never a trap, a chore, or a hollow gimmick. The further a player walks, the better
  the payoff feels.
- **No dead ends and no parasites.** Every job earns its existence somewhere. There is no
  win-button-with-homework, no copy-only job with no agency of its own, and no fragile non-combatant
  that contributes only from a safe corner.
- **Accessible jobs earn their keep by being useful and exportable**, not by being destinations. A
  shallow job is worth visiting for what it teaches and what it lends to other builds.
- **Legibility and the pleasure of planning a route.** A player should be able to read the tree, feel
  the fantasy of a path, and plan toward it.

These intents are *why* the two worst payoff curves in the vanilla tree are torn out, and why the
performers are pulled forward — see the swaps below.

## Organizing principle: role and fantasy first

The tree is organized by **role and fantasy**, not by any mechanical backbone.

Armor-class coherence is a **downstream sanity check**, not the organizing law: a route should not
lurch from a light, agile lineage straight into full plate without a reason. Where role/fantasy
routing is clean, armor coherence tends to fall out on its own. Where a job's fantasy deliberately
crosses armor classes — a mail terrain-hybrid, a plate spellblade — that is an accepted, declared
exception rather than a violation.

Natural groupings (martial, agile, caster, hybrid) emerge from fantasy, but their exact membership
and wiring are **not fixed here** — redesigns may reshape which job flows from which.

## The swaps (relative to vanilla / IVC)

Two slots are replaced outright; two jobs are re-tiered.

### Replaced slots

- **Arithmetician / Calculator → Necromancer.** The Calculator's identity *is* the bypass — instant,
  free, map-wide, ignoring range, charge time, Faith, hit chance, immunity, and position all at once.
  It cannot be rebalanced without destroying it, and it sits at the deepest tier, so the longest grind
  in the game buys the most degenerate and tedious payoff. The slot becomes a late dark/state
  controller whose power comes from battle state that has already happened — wounds, corpses, Doom,
  drain, undead — rather than from abstract global rules.

- **Mime → magic knight** (spellblade; final name open). The Mime is a parasite with no agency — it
  cannot equip, cannot set abilities, and cannot act on its own — oscillating between dead weight and
  stack-cheese while exporting nothing. That is a hollow reward for one of the deepest slots. The slot
  becomes a deep martial **spellblade**: weapon strikes that carry magic, the identity that in vanilla
  only unique characters (Templar, Holy/Divine Knight) hold. Handing that identity to a generic
  attacks the very reason generics get benched at endgame (see `vanilla/00-endgame-meta.md`). *This
  supersedes the legacy "Vanguard" concept, which was a physical protection knight; the slot is a
  magic knight, not a physical vanguard.*

### Re-tiered jobs

- **Performers (Bard & Dancer): pulled forward, and made to fight.** A deep-grind slot that delivers a
  fragile, corner-parking non-combatant is the worst payoff curve in the tree. The performers move to
  an **accessible** tier and are reimagined as **battle-active** jobs — they go into the fight rather
  than singing or dancing map-wide effects from safety. They move as a **pair**: their cross-job
  reaction/support/movement access stays equal, and only their active commands differ. *(Whether the
  gender lock survives an active redesign is left open.)*

- **Dragoon (Lancer): pushed deeper, into a capstone.** In vanilla it is a mid-tier job whose
  reputation outruns its performance. There is room to make it a deep, exciting heavy capstone, and
  pushing it deep also removes the jarring light-lineage-into-full-plate jump that the old routing
  produced.

## Tiers, not a locked wiring

The tree distinguishes **accessible** jobs (early, useful, export-rich) from **capstones** (deep,
grind-gated, and obligated to feel like a great reward). That distinction is fixed. The **exact
prerequisite graph, bands, and per-job placement are not** — because several jobs are redesigned
heavily enough to change their natural routes. This document fixes intent and the confirmed moves; it
does not lock the graph.

## Open tensions

- **Deep-end balance.** The confirmed moves leave the capstone tier martial-heavy — Samurai, Dragoon,
  the magic knight, and Ninja — against a single caster capstone, the Necromancer. This is an
  imbalance of *aspiration*, not of power (mid-tier casters are already strong). Whether to give the
  caster line a second capstone or to accept the asymmetry is open.
- **Performer gender lock.** Turning the performers into active combatants sharpens the question of
  whether a gender restriction on them still makes sense.

## Out of scope (deferred)

- Every individual job's kit, stats, skills, reaction/support/movement, JP costs, and equipment.
- The exact prerequisite graph, band assignments, and per-job tree placement.
