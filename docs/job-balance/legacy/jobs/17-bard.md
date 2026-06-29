# Bard

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Bard layers — `26` (v1 proposal, Bard rows) and `46` (concrete-v0, Bard
rows) — folded into this single decision doc.

> **No rediscussion yet.** Bard never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; performance damage/heal rides the **unwritten
> DCL magic equation** (DCL `11`), and the **sustained-area model** (tick/target-count) is v0.2 schema.
> See *DCL rebase notes*.

## Identity / compass

Bard is the **support performer**: songs that heal, lift morale, build party momentum, and grant slow
global buildup. It should feel like an FFT performer, not a caster with strange animations. Its power
is real because it touches many allies over time — and it is **paid for** by performer fragility, the
vulnerable `Performing` state, slow tick timing, interruption risk, low per-target values, and
target-count normalization. The player thinks in tempo and formation: *can I protect this performer
long enough for the song to matter, and will slow global support beat a direct action right now?*

It is punished by `Performing` vulnerability, focus fire, Silence/Stop/Sleep/displacement,
anti-performance reactions, and short fights that end before songs compound.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `performer` |
| Secondary tags | `support`, `instrument` |
| Growth profile | hybrid |
| Armor class | `cloth` |
| Weapon families | instrument, bag, fists (missile / crush) |
| Role reason | Performance support job; action identity differs from Dancer, but reaction/support/move must match Dancer **exactly**. |

**Good at:** slow global recovery/morale/magic support, long-fight value, ranged instrument chip while
waiting for a window.
**Bad at / countered by:** cloth fragility, `Performing` interruption, low immediate payoff, poor
direct burst, short fights.

### Gender + parity rule (load-bearing)

Bard is **male-only**; Dancer is **female-only** — the only accepted generic gender restriction. By
**mandatory rule**, Bard and Dancer share **identical** reaction/support/movement records (asserted by
machine-diffable data-record equality), so no gender is locked out of global build pieces. Action
abilities differ; global build pieces may not. See `jobs/18-dancer.md`.

## Action skills (Bardsong)

Sustained per-tick global effects; `Performing` commits the performer (no other actions, evasion
suppressed, interruption cancels future ticks). *(v0.2: tick schedule 4/12/20/28; default target cap
4, strong-effect cap 3; HP ticks min of percent-of-max-HP, a flat cap, and a `K·MA·0.60` performer
cap so high-HP targets can't scale them freely.)*

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Seraph Song** | Gentle global Regen-like recovery. | Living allies only; below direct healing; must not replace White Mage/Chemist. *(v0.2: 5% max HP/tick, K 3.0 MA cap, per-tick cap 22, max 4, 4 ticks.)* |
| **Life's Anthem** | Stronger recovery song. | No safe mapwide Arise loop; caps/interruption stay real (HP 624 cap-3 total ≈ 384, near Bahamut — watch). *(v0.2: 7% max HP/tick, K 4.0 MA cap, per-tick cap 32, max 3, 4 ticks.)* |
| **Rousing Melody** | Slow global tempo/morale support. | **No** same-tick action grant, **no** Quick-class recursion. *(v0.2: +6 CT per completed tick, max 3, 4 ticks.)* |
| **Battle Chant** | Battle-scoped Brave/morale build. | **No** permanent Brave economy; Orator remains the targeted morale controller. *(v0.2: +2 Brave/tick, +8 battle cap, max 4.)* |
| **Magickal Refrain** | Allied magic support. | **Does not stack with Mystic `Belief`** (use stronger layer); not a broad caster-tax. *(v0.2: +0.015 outgoing-magic layer/tick, cap 1.06, max 4.)* |
| **Nameless Song** | Bounded random ally buff (performer flavor). | Bounded table {Protect, Shell, Float, Regen}; **excludes** Reraise/Haste/Invisible/immunity/instant action. *(v0.2: one random ally/tick.)* |
| **Finale** | Setup capstone: ends the song for a large nonlethal pulse + light cleanse. | Requires 2 completed ticks; **no** revive/instant-KO/action grant; clears one of Poison/Blind/Silence/Slow/Immobilize/Oil. *(v0.2: 12% max HP pulse, K 8.0 MA cap, pulse cap 60, max 4.)* Ordinary mapwide instant KO is **rejected**. |

## Reaction / Support / Movement — SHARED with Dancer (identical records)

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
- **Parity assertion (`J-PERF-PARITY`):** Bard and Dancer RSM records must be field-by-field equal —
  enforce mechanically, not by prose.
- Sustained-throughput composition (target-count × tick-count); `Performing` vulnerability/interruption;
  `T3xT5xT11` for Seraph/Life's Anthem; `T11xT5` for morale/magic layers; `T4`/`T8` for Nameless Song;
  `F4`/`F5`.
- Watch: every party wanting a song running by default; Bardsong replacing White Mage/Chemist;
  Battle Chant/Magickal Refrain as mandatory upkeep; `Magickal Refrain` × `Belief` feeding the
  Belief × Oil × fire-weak compound; Finale as global instant-win; shared RSM as a gender-locked
  advantage.

## DCL rebase notes

- **Performance damage/heal rides the unwritten DCL magic pipeline (`11`)** (and the performer-stat
  caps); every `K·MA·0.60` value re-derives once the DCL magic equation exists.
- **The sustained-area model is a v0.2 schema** (tick schedule + target-count normalization). The DCL
  needs its own AoE/over-time treatment; the *discipline* (`per_tick × expected_targets × tick_count`)
  is engine-independent and should carry over, as should `Performing` as the cost for global value.
- **Brave/morale and magic layers** (`Battle Chant`, `Magickal Refrain`) re-express on the DCL's
  **two-sided Brave/Faith** model (shared with Orator's morale tools and Mystic's `Belief`); the
  no-stack-with-`Belief` rule prevents double-dipping the same Faith axis.
- **CT pulses** (`Rousing Melody`) are engine-neutral — Speed/CT are native (turn frequency) in the
  DCL; the no-Quick-recursion rule carries over.
- **Status/random tables** (`Nameless Song`) resolve through the DCL **3d6 contest** (`13`) where a
  status is rolled; the bounded table carries over.
- **`Earplugs`/`Encore`** classify in the DCL reaction taxonomy (`13`) as **neutral** reactions (not
  Brave-scaled); `Earplugs` doubles as native counterplay to performer/speech pressure.
- **Weapon fallback:** instruments are `missile`, bags `crush`; under the DCL weapon-side damage uses
  **subtractive DR by type**, so the v0.2 multiplicative routines re-derive — but Bard's identity is
  the performance, not the weapon.
