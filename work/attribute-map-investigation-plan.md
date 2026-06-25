# Battle-unit struct — full attribute mapping investigation (plan)

Status: STARTED 2026-06-24 (autonomous /loop). Objective set by user: investigate every
attribute in the existing `[DUMP]` captures, build a map of everything mappable **with an
explicit confidence level**, and clearly flag anything that cannot reach confidence so we
can investigate it better later.

## Objective

For the GC "complete battle-mechanics rewrite", the runtime formula must read all relevant
caster/target attributes. This task produces a **confidence-rated offset map** of the battle
unit struct from the dumps we already have — zero new in-game captures.

## Hard constraints / data hygiene (learned this session)

1. **Level-matching is mandatory.** Dumps span multiple days; several units leveled up
   mid-testing. Stale dumps carry stale stats. We restrict each unit to dumps whose Level
   byte (`+0x29`) matches its CURRENT level. Detected current levels (pending user confirm):
   Ramza 75, Agrias 69, Beowulf 68, Cloud 67, Ninja 71.
2. **The 06-23 19:57 session is a low-level outlier** (Agrias/Ninja L50, Cloud L49/52,
   Beowulf L50) — a different save state; excluded by the level filter.
3. **`id0x82` is a reused id** (different monster each battle, levels 47–75) — unusable for
   per-unit attributes; dropped.
4. **`id0x03` (Orlandeau?) and `id0x81` (archer?)** appear only in the old low-level session
   → usable only for IMMUTABLE attributes (zodiac/gender) and only if the user identifies
   them; never for level-dependent stats.
5. **No job/equipment change since 06-24** (user-confirmed) → class, equipment, zodiac,
   gender are stable across the current-level captures. **Exception: the Support ability
   slot may have varied** between captures → treat that slot as volatile / lower confidence.
6. **The "gem" with all character attributes did NOT arrive** in-thread. Without per-unit
   ground-truth values, job/zodiac/gender/ability *identities* cannot be cravados at high
   confidence — they will be produced as CANDIDATES to confirm once the gem is provided.

## Primary data set

The 5 named party units at current level (plenty of level-matched captures each: 35/43/29/
34/15 in-window). Source: `work/live-captures/*.txt`, filtered by level + mtime.

## Method (3 layers)

1. **Within-unit volatility split.** For each offset, across all of a unit's level-matched
   captures, is the byte CONSTANT (static attribute) or does it VARY (runtime state)? A byte
   that varies within a single unit at fixed level is runtime state (CT, action, current
   status, charge, …), NOT a static attribute.
2. **Cross-unit variation split.** Among STATIC offsets, compare across the 5 units:
   - same value for everyone → CONST (global constant / type marker / padding);
   - differs across units → CANDIDATE per-unit attribute (the interesting ones).
3. **Identification + confidence.** Assign each offset using: known doc-05 mapping, value
   range, byte/word/nibble/bit shape, adjacency, and FFT-layout knowledge. Bit/nibble
   decomposition for packed fields (gender/zodiac are often packed).

## Confidence tiers

- **CONFIRMED** — proven by ground-truth match (equipment), live diff (death `+0x61`), or a
  prior live test (CT `+0x41`, Speed `+0x40`). Safe to expose in formulas now.
- **HIGH** — structurally certain from the dumps without per-unit values (e.g. HP==MaxHP in
  full-HP captures; value range + adjacency uniquely pin it; matches doc-05 analytic map).
- **MEDIUM** — a static per-unit byte whose range strongly suggests one attribute (e.g. 0–11
  → zodiac) but which a sibling field could be confused with; needs gem to disambiguate.
- **LOW** — clearly a per-unit attribute byte, but no strong signal which one; needs gem.
- **DEFER** — not mappable from these static, full-HP, no-status captures. Requires targeted
  captures later: full status bitfield (need status-varied diffs), elemental affinity (may be
  derived, not stored), battlefield geometry (facing/height/position), raw vs modified stats.

## Attribute checklist (target coverage)

Numeric core (expect CONFIRMED/HIGH from doc-05 + structure): HP/MaxHP, MP/MaxMP, PA, MA,
Speed, Move, Jump, Level, EXP, Brave/MaxBrave, Faith/MaxFaith, CT.
Identity (expect CANDIDATE/MEDIUM until gem): Job/class, Zodiac, Gender, monster family.
Equipment (CONFIRMED this session): head/body/acc/R-hand/L-hand `+0x1A..+0x26`.
Equipped abilities (expect CANDIDATE; Support volatile): Reaction/Support/Movement/Secondary.
Defensive/evasion (expect LOW/DEFER): C-Ev/P-Ev/M-Ev/weapon guard — may be derived.
Status (DEFER mostly; KO bit `+0x61 0x20` CONFIRMED): current statuses, immunities.
Elemental affinity (DEFER): weak/half/absorb/null/strengthen — likely derived.
Geometry (DEFER): position X/Y, height, facing.

## Deliverables

1. `work/battle-unit-struct-attribute-map.md` — the confidence-rated map (every offset
   0x00..~0xC0 classified), with per-unit value evidence and "what's needed to confirm".
2. `work/attribute-map.result.json` — machine-readable result for later formula wiring.
3. Updates to `docs/modding/05-battle-data-map.md` (confirmed additions; candidates noted).
4. Commits at milestones; a morning summary.

## Tools

- `tools/profile_struct.py` (NEW) — unsupervised per-offset volatility/variation classifier.
- `tools/map_attributes.py` — ground-truth offset finder (byte/word/nibble/bit), level-aware
  (self-tested against known equipment → PASS).
- `tools/dump_levels.py` — per-unit level/recency diagnostic for the level filter.

## Next when the gem arrives

Feed per-unit job/zodiac/gender/ability ids into `map_attributes.py` to upgrade every
CANDIDATE/MEDIUM/LOW to CONFIRMED (or correct it), then expose the confirmed set in the
formula context (`attacker.*` / `target.*`).
