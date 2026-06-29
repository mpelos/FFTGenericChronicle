# Geomancer

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Geomancer layers — `21` (v1 proposal) and `43` (concrete-v0) — folded into
this single decision doc.

> **No rediscussion yet.** Geomancer never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; Geomancy used the v0.2 `pampa_wp` hybrid
> routine + multiplicative armor-response table, both of which re-derive under the DCL. See *DCL rebase
> notes*.

## Identity / compass

Geomancer is the **PA/MA terrain hybrid**: moderate pressure, terrain-flavored damage and status,
mail durability, and weapon-backed map adaptation. Terrain should matter **without** forcing the
player to memorize hidden tables — the question is "what am I standing on / targeting through, and is
this a damage, status, or control opportunity?" It connects physical weapon identity to map state
**without** becoming generic melee or a free-ranged-magic fighter.

It is punished by poor-terrain maps, flying/terrain-immune targets, status immunity, ranged/magic
pressure into mail, and enemies who reposition out of terrain traps.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `hybrid` |
| Secondary tags | `terrain`, `mail` |
| Growth profile | hybrid |
| Armor class | `mail` |
| Weapon families | axe, sword, fists (crush / swing) |
| Role reason | PA/MA terrain hybrid; connects physical weapon identity to map state without becoming generic melee. |

**Good at:** terrain-rich maps, flexible damage+status pressure, mail survivability over cloth casters,
weapon fallback on dead maps.
**Bad at / countered by:** map dependence, lower ceiling than dedicated specialists, mail vs ranged/
magic, status immunity, terrain mismatch.

## Geomancy design rules

- **No MP and no CT** on Geomancy — its cost is **terrain availability** and modest damage.
- Damage is deliberately **below Black Mage tier I** (highest stress row 134 < BM tier I 151): free,
  reliable, mail-backed pressure cannot also win a raw-damage contest.
- Terrain identity comes from **terrain access + damage type + element + status rider**, *not* a new
  arithmetic routine — it reused the validated `pampa_wp` hybrid (`floor((PA+MA)/2)·terrain_wp`).
- Must not eclipse Monk anti-plate (`Tremor`), Archer missile (`Wind Slash`), or Black Mage elements
  (`Will-o'-the-Wisp`).

## Action skills (Geomancy)

Terrain expressions, not twelve indistinguishable buttons; final data may consolidate if terrain
availability can't keep all distinct. *(v0.2: all JP 150; terrain WP 7–9; status rates 20–35%.)*

| Skill | Type / element | Status rider | Note |
|-------|----------------|--------------|------|
| **Sinkhole** | crush / earth | Immobilize | ground pressure + positional |
| **Torrent** | crush / water | Slow | water-map pressure |
| **Tanglevine** | swing / plant | Immobilize | vegetation soft control |
| **Contortion** | crush / none | Confuse | rough-terrain disruption |
| **Tremor** | crush / earth | Disable | impact into plate (must not eclipse Monk) |
| **Wind Slash** | swing / wind | Blind | line chip (must not replace Archer) |
| **Will-o'-the-Wisp** | swing / fire-spirit | Silence | magic-adjacent (must not replace BM elements) |
| **Quicksand** | crush / earth | Slow | movement trap |
| **Sandstorm** | swing / earth-wind | Blind | vision/accuracy pressure |
| **Snowstorm** | swing / ice | Slow | cold-terrain status |
| **Wind Blast** | swing / wind | Immobilize | spacing/chip |
| **Magma Surge** | crush / fire-earth | **Oil** | lava-map high-risk; **feeds the fire-summon weak-element compound watch** |

## Reaction / Support / Movement

*(All deferred in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Nature's Wrath** | Terrain retaliation. | Terrain-dependent, not broad counter damage. |
| Support | **Terrain Lore** | Terrain reliability / wider terrain use. | Helps Geomancy only, not all magic/weapon damage. |
| Support | **Attack Boost** | (deferred) Protected v0.2 stress engine; ownership undecided. | **Not** accepted as a Geomancer reward here; placed by the global support/progression pass; not Geomancer's only reason to exist. |
| Movement | **Ignore Terrain / Ignore Weather** | Terrain-specialist movement. | Must not become the default movement for all jobs. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- Terrain-availability model (dead-map vs universal-map); `T4`/`T5`/`T8` status riders; hybrid-routine
  + armor-response re-derivation; `F4`/`F5` (MA-PA lanes vs Monk/BM/Archer); `Attack Boost` placement.
- **Cross-job compound watch (F5):** `Magma Surge → Oil` is the constructible setup for the fire-summon
  weak-element over-ceiling vector (see `jobs/12-summoner.md`, `jobs/11-mystic.md`).
- Watch: Geomancy as the best secondary (always *some* ranged damage/status); `Attack Boost` making
  Geomancer a mandatory support detour; `Ignore Terrain` as universal movement; terrain pressure
  making Archer/Monk/BM/Mystic unnecessary; Geomancer useless on too many maps (terrain too narrow).

## DCL rebase notes

- **Geomancer is more DCL-tractable than the pure casters** — it is mail-armored and weapon-backed.
  The `pampa_wp` hybrid (`floor((PA+MA)/2)·terrain_wp`) and the multiplicative `armor_response` table
  are v0.2 constructs: under the DCL, weapon-side damage uses **subtractive DR by damage type**, so
  Geomancy's crush/swing rows re-anchor onto the DCL damage-type math (crush into plate, etc.).
- **Status riders** (Immobilize/Slow/Blind/Confuse/Disable/Silence/Oil) run the DCL **3d6 contest**
  (`13`); the rates re-express there.
- **`Oil`** stays a fire-combo setup; the `Magma Surge → Oil → fire summon` compound is a DCL-side
  watch too (the DCL element/affinity system carries `Oil`'s weakness amplification).
- **Terrain availability** is a map/geometry concept independent of the damage engine; it carries over
  as the cost that keeps Geomancy's free (no-MP/no-CT) pressure bounded.
- **Nature's Wrath** maps to the DCL reaction taxonomy (`13`) — a terrain-gated **neutral** retaliation.
