# Dancer

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Dancer layers — `26` (v1 proposal, Dancer rows) and `46` (concrete-v0, Dancer
rows) — folded into this single decision doc.

> **No rediscussion yet.** Dancer never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; performance damage rides the **unwritten DCL
> magic equation** (DCL `11`), and the **sustained-area model** (tick/target-count) is v0.2 schema. See
> *DCL rebase notes*.

## Identity / compass

Dancer is the **pressure performer**: dances that drain HP/MP, lower Speed and offensive stats, roll
bounded random debuffs, and apply slow global disruption. Like Bard, it should feel like an FFT
performer, and its global power is **paid for** by performer fragility, the vulnerable `Performing`
state, slow tick timing, interruption risk, low per-target values, and target-count normalization.
The player asks: *can I keep the Dancer safe long enough for pressure to compound, and is slow global
debuff better than a targeted Mystic / Time Mage / Black Mage action?*

It is punished by `Performing` vulnerability, focus fire, Silence/Stop/Sleep/displacement,
anti-performance reactions, status resistance, and short fights that end before attrition matters.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `performer` |
| Secondary tags | `debuff`, `cloth_weapon` |
| Growth profile | hybrid |
| Armor class | `cloth` |
| Weapon families | cloth_weapon, bag, knife, fists (swing / crush / thrust) |
| Role reason | Performance pressure job; action identity differs from Bard, but reaction/support/move must match Bard **exactly**. |

**Good at:** slow global HP/MP/stat attrition, long-fight pressure, performing from safe angles, light
weapon fallback when performance is unsafe.
**Bad at / countered by:** cloth fragility, `Performing` interruption, low immediate payoff, poor
direct burst, status-resistant/boss targets, short fights.

### Gender + parity rule (load-bearing)

Dancer is **female-only**; Bard is **male-only** — the only accepted generic gender restriction. By
**mandatory rule**, Dancer and Bard share **identical** reaction/support/movement records (asserted by
machine-diffable data-record equality), so no gender is locked out of global build pieces. Action
abilities differ; global build pieces may not. See `jobs/17-bard.md`.

## Action skills (Dance)

Sustained per-tick global effects; `Performing` commits the performer (no other actions, evasion
suppressed, interruption cancels future ticks). *(v0.2: tick schedule 4/12/20/28; default target cap
4, strong-effect cap 3; HP ticks min of percent-of-max-HP, a flat cap, and a `K·PA·0.60` performer
cap so high-HP targets can't scale them freely.)*

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Witch Hunt** | Slow mapwide MP/resource pressure vs casters. | Battle-scoped; **no** hidden campaign drain or caster deletion. *(v0.2: MP/tick = min(current MP, 12, 1.90·PA·0.60), max 4 enemies, 4 ticks.)* |
| **Mincing Minuet** | Low global HP attrition for long fights. | Must not replace Black Mage/Summoner; can KO. *(v0.2: 4% max HP/tick, K 3.0 PA cap, per-tick cap 20, max 4, 4 ticks.)* |
| **Slow Dance** | Slow global tempo debuff. | **No** same-tick hard denial; **not** `Stop`; Time Mage remains the targeted tempo specialist. *(v0.2: −6 CT per completed tick, max 3, 4 ticks.)* |
| **Polka** | Battle-scoped physical-output pressure. | **No** permanent stat damage. *(v0.2: −0.03 outgoing-physical layer/tick, floor 0.88, max 4.)* |
| **Heathen Frolic** | Battle-scoped magical-output pressure. | Mystic remains the targeted Faith/status controller. *(v0.2: −0.03 outgoing-magic layer/tick, floor 0.88, max 4.)* |
| **Forbidden Dance** | Bounded random enemy debuff (performer flavor). | Table {Blind, Silence, Slow, Immobilize, Poison}; **excludes** the hard-control lottery (Stop/Sleep/Stone/Charm/Death/Toad/Vampire/Traitor). *(v0.2: one random enemy/tick, 35% base / ~21% effective.)* |
| **Last Waltz** | Setup capstone: ends the dance for a large nonlethal pulse. | Requires 2 completed ticks; **cannot KO**; no Stop/action denial/CT-set hard lock. *(v0.2: 12% max HP nonlethal pulse, K 8.0 PA cap, pulse cap 60, max 4.)* Ordinary mapwide instant KO is **rejected**. |

## Reaction / Support / Movement — SHARED with Bard (identical records)

*(RSM values deferred in v0.2 pending build-incidence; the parity set is locked.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Earplugs** | Narrow anti-performance/speech/morale defense (the safer default). | Narrow scope; must not become broad status immunity. |
| Reaction | **Encore** | Performance-resilience candidate. | High risk; optional; must **not** erase `Performing` vulnerability. |
| Support | **Performance Mastery** | Performance tick reliability/duration/interruption-resistance only. | No broad magic/physical/stat boost. |
| Support | **Stagecraft** | Narrow performer setup / self-risk reduction. | Optional; must not compress mobility+defense+output. |
| Movement | **Performance Step** | Narrow performer positioning (the default). | Must not become generic mobility. |
| Movement | **Fly** | Dramatic-movement promotion candidate. | High risk; promote only if performers truly can't hold channel positions; must not become a non-performer export. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- **Parity assertion (`J-PERF-PARITY`):** Dancer and Bard RSM records must be field-by-field equal —
  enforce mechanically, not by prose.
- Sustained-throughput composition (target-count × tick-count); `Performing` vulnerability/interruption;
  `T3xT5xT11` for Mincing Minuet; `T9` for Witch Hunt; `T7` for Polka; `T11xT5` for the layers;
  `T4`/`T8` for Forbidden Dance; `F4`/`F5`.
- Watch: every party wanting a dance running by default; global damage replacing normal offense;
  random dance as a mapwide hard-control slot machine; Last Waltz as global instant-win; shared RSM as
  a gender-locked advantage.

## DCL rebase notes

- **Performance damage rides the unwritten DCL magic pipeline (`11`)** (and the performer-stat caps);
  every `K·PA·0.60` value re-derives once the DCL magic equation exists.
- **The sustained-area model is a v0.2 schema** (tick schedule + target-count normalization). The DCL
  needs its own AoE/over-time treatment; the *discipline* (`per_tick × expected_targets × tick_count`)
  is engine-independent and should carry over, as should `Performing` as the cost for global value.
- **Stat-pressure layers** (`Polka` physical, `Heathen Frolic` magic) re-express as DCL output
  debuffs; the magic layer rides the **two-sided Faith** model (kept off the same axis as Mystic
  `Belief`/Bard `Magickal Refrain` to avoid stacking).
- **CT pulses** (`Slow Dance`) are engine-neutral — Speed/CT are native (turn frequency) in the DCL;
  the not-`Stop` rule carries over.
- **MP drain** (`Witch Hunt`) is engine-neutral in shape; the target-resource cap carries over.
- **Status/random tables** (`Forbidden Dance`) resolve through the DCL **3d6 contest** (`13`); the
  bounded table and hard-control exclusions carry over.
- **`Earplugs`/`Encore`** classify in the DCL reaction taxonomy (`13`) as **neutral** reactions
  (identical records to Bard's).
- **Weapon fallback:** cloth_weapon is `swing`, knife `thrust` (Speed/PA — but must not replace
  Thief), bag `crush`; under the DCL weapon-side damage uses **subtractive DR by type**, so the v0.2
  multiplicative routines re-derive — Dancer's identity is the performance, not the weapon.
