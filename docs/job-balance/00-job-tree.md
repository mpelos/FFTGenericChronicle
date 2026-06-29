# Job Tree

Engine: **Deep Combat Layer** (`../deep-combat-layer/`).

This document defines the **shape and intent** of the generic job tree — which jobs exist, which
vanilla slots are replaced, and which jobs are rebuilt in place — together with the feel the tree
reaches for and the trade-offs each move carries.

It deliberately **defines no individual job**: no kit, stats, skills, reaction/support/movement, JP
costs, equipment, or exact prerequisites. Several jobs receive large redesigns, so the precise wiring
is left open on purpose. What is fixed here is the *intent* and the *confirmed structural moves*;
per-job design and the exact prerequisite graph come later.

The baseline for every swap below is the vanilla/IVC roster analysed in `vanilla/` (20 generics +
Ramza). The job **count is unchanged**, and **vanilla prerequisites are kept** — every move is a slot
replacement or an in-place redesign, never an addition, a removal, or a re-routing of requirements.

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
performers are rebuilt into real combatants in place — see the swaps below.

## Organizing principle: role and fantasy first

The tree is organized by **role and fantasy**, not by any mechanical backbone.

Armor-class coherence is a **downstream sanity check**, not the organizing law: a route should not
lurch from a light, agile lineage straight into full plate without a reason. Where role/fantasy
routing is clean, armor coherence tends to fall out on its own. Where a job's fantasy deliberately
crosses armor classes — a light-lineage Dragoon that arrives in heavy armour, a plate spellblade —
that is an accepted, declared exception rather than a violation.

Natural groupings (martial, agile, caster, hybrid) emerge from fantasy, but their exact membership
and wiring are **not fixed here** — redesigns may reshape which job flows from which.

## The swaps (relative to vanilla / IVC)

Two slots are replaced outright; the performers are rebuilt in place. Every other job keeps its vanilla
tree position and requirements — the only changes are the two slot swaps and the performers' mechanics.

### Replaced slots

- **Arithmetician / Calculator → Necromancer.** The Calculator's identity *is* the bypass — instant,
  free, map-wide, ignoring range, charge time, Faith, hit chance, immunity, and position all at once.
  It cannot be rebalanced without destroying it, and it sits at the deepest tier, so the longest grind
  in the game buys the most degenerate and tedious payoff. The slot becomes a late dark/state
  controller whose power comes from battle state that has already happened — wounds, corpses, Doom,
  drain, undead — rather than from abstract global rules.

- **Mime → magic knight** (spellblade; final name open). The Mime is a parasite with no agency — it
  cannot equip, cannot set abilities, and cannot act on its own — oscillating between dead weight and
  stack-cheese while exporting nothing. It also sits in the **single deepest slot in the game**: the
  longest grind buys the worst job, the most anti-climactic reward curve on the board. The slot becomes
  the roster's **one Tier-S apex** — a deep martial **spellblade**: weapon strikes that carry magic, the
  identity that in vanilla only unique characters (Templar, Holy/Divine Knight) hold. Handing that
  identity to a generic attacks the very reason generics get benched at endgame (see
  `vanilla/00-endgame-meta.md`). *This supersedes the legacy "Vanguard" concept, which was a physical
  protection knight; the slot is a magic knight, not a physical vanguard.*

### Rebuilt in place (same tree position, new mechanics)

- **Performers (Bard & Dancer): rebuilt into combatants, kept where they are.** A deep slot that
  delivers a fragile, corner-parking non-combatant is one of the worst payoff curves in the tree — but
  the fix is **mechanical, not positional**. The performers **keep their vanilla tree positions and
  requirements** (deep — **Tier A**, one reached on the martial side, one on the caster side) and the
  **gender lock stays** (Bard male-only, Dancer female-only). What changes is the play: they become
  **battle-active** jobs that go *into* the fight instead of singing/dancing map-wide effects from a safe
  corner. Their **active commands differ**, and their **Reaction/Support may differ** where it is
  mechanically sensible and fun — but their **universally-valuable supports and movement are not
  gender-split** (no vanilla Move+3 vs Jump+3 divide), so a unit's gender never decides access to a core
  mobility or build advantage.

*(The **Dragoon stays in its vanilla position** — a mid/deep job reached past the agile line. The
light-lineage-into-heavy-armour jump it produces is accepted as a declared armor-crossing exception
above, not engineered away.)*

## Tiers

Tier is **acquisition position** (how deep in the tree a job is reached), **not power** — `S` is the
hardest to reach, `D` the most accessible (`../deep-combat-layer/15`, *Tiers*). The bands distinguish
**accessible** jobs (early, useful, export-rich) from **capstones** (deep, grind-gated, and obligated
to feel like a great reward). The **band each job sits in is fixed** (below); the **exact prerequisite
wiring within and between bands is not** — several jobs are redesigned heavily enough to change their
natural routes, so this document fixes the bands and the confirmed moves, not the graph.

| Tier | Position | Jobs |
|------|----------|------|
| **D** | base — no prerequisite | Squire · Chemist |
| **C** | first rank — unlocks directly off a base job | Knight · Archer · White Mage · Black Mage |
| **B** | mid — reached past a first-rank job | Monk · Thief · Mystic *(Oracle)* · Time Mage · Geomancer · Orator · Summoner |
| **A** | deep — pre-capstone | Dragoon · Ninja · Samurai · Necromancer · Bard · Dancer |
| **S** | capstone — the deepest slot, the apex reward | Mystic Knight |

The single deepest slot is the **Mystic Knight** (the spellblade that replaces the Mime, vanilla's
deepest slot) — the roster's apex reward. Note *Mystic (Oracle)* in tier B is the caster job (`09`),
distinct from the *Mystic Knight* capstone (`20`).

## Settled shape

- **One capstone by design.** Tier S holds a single job — the **Mystic Knight**. The deep pre-capstone
  tier (A) is intentionally **mixed**: martial (Dragoon, Ninja, Samurai), caster (Necromancer), and the
  two performers (Bard, Dancer). The single apex is a chosen shape, not an oversight — do not "balance"
  it by inventing a second capstone.
- **Performer gender lock — kept.** Bard is male-only, Dancer female-only. The constraint this imposes
  is recorded in the performer entry above and binds their design: a gender-locked job must **not** gate
  a *universal* support/movement advantage behind one gender.

## Out of scope (deferred)

- Every individual job's kit, stats, skills, reaction/support/movement, JP costs, and equipment.
- The exact prerequisite graph and per-job tree wiring (the **tier bands are fixed above**; only the
  exact routes within and between them remain open).
