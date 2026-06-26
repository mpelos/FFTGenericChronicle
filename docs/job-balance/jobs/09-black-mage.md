# Black Mage

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Black Mage layers — `19` (v1 proposal) and `42` (concrete-v0) — folded into
this single decision doc.

> **No rediscussion yet.** Black Mage never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**, and casters are the **least settled under the
> DCL**: the engine's **magic-damage equation is not yet written** (DCL `11`) and the DCL resolves
> magic via **inverse-Faith** resistance — so every `K·MA·Faith` number below is a placeholder pending
> the DCL magic pipeline. See *DCL rebase notes*.

## Identity / compass

Black Mage is the **delayed, Faith-linked elemental and high-output damage caster** — the early/midgame
home of direct spell pressure. Its core decision is **spell selection as target selection**: element
vs. affinity, status vs. damage, big-MP capstone vs. efficient tier. It must coexist with the physical
stress engines, not replace weapon planning.

It wins through elemental affinity, Shell/Faith-aware burst, and the `Flare` capstone; it is punished
by anti-magic/Shell, Reflect, Silence/MP denial, fast CT exploitation, resistant targets, and cloth
fragility.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `caster-offense` |
| Secondary tags | `Faith`, `rod` |
| Growth profile | magical |
| Armor class | `cloth` |
| Weapon families | rod, fists (crush / magic) |
| Role reason | Main magical damage job; preserves spell pressure and interacts cleanly with Faith/Shell/element. |

**Good at:** elemental burst into weak/exposed targets, flexible target answers (status, KO, capstone),
backline damage pressure.
**Bad at / countered by:** cloth durability, CT/movement, MP attrition, Silence/Shell/Reflect, element
resistance, status/KO immunity, spread/forced-movement maps.

## Element identity engine

The primary element-identity lever is **affinity** (weak / resist / absorb / halve / Oil-style setup) —
existing FFT vocabulary, no new data feature. Elemental tiers may *also* vary by CT/area/MP/range, but
affinity is what makes element selection a real decision; `Flare` exists as the non-elemental answer
when routing is wrong, **not** as a universal best spell.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Fire / Thunder / Blizzard lines** | The three elemental damage lanes; cheap tiers stay useful via speed/MP-efficiency/overkill control. | Each must be a real affinity choice, not "Fire with another name"; CT/MP/Faith/Shell/Reflect counters apply; no universal best element. *(v0.2 tier K: 14 → 18 → 21 → 22; MP 5/12/22/34; CT 2/3/4/5. Lower tiers deliberately below `Flare` neutral.)* |
| **Poison** | Attrition status against durable/anti-tank targets. | Status accuracy + immunity + undead + duration checks; respects immunity. *(v0.2: 10% max HP/tick × 4; 70% base; MP 8; CT 2.)* |
| **Toad** | Rare hard transformation control. | Strong accuracy/immunity/CT/undead limits; **not** a boss answer or broad shutdown. *(v0.2: 35% base; MP 24; CT 4.)* |
| **Death** | High-risk instant-KO fantasy. | Accuracy/immunity/undead/CT/Faith keep it situational; immunity-heavy. *(v0.2: 25% base KO; MP 36; CT 5.)* |
| **Flare** | Non-elemental capstone when elements are wrong/resisted. | High MP/CT/JP; slower than elements; must **not** become the default answer; stays below the strongest physical stress reference. *(v0.2: K 30; MP 46; CT 6.)* |

## Reaction / Support / Movement

*(RSM values were deferred in v0.2 pending build-incidence; these stay design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Arcane Backlash** | Risky, narrow caster retaliation. | Must **not** answer all physical pressure; caster fragility stays real; reaction identity is optional. |
| Support | **Elemental Focus** | Black-magic specialization (safer V1 direction). | Improves elemental planning only, not every magical action; not the default caster support. |
| Support | **Arcane Strength** | (deferred) Possible late broad magic-damage booster. | High `T2`/`F4` risk; only if it survives incidence + coexistence proof; else stays cut. |
| Movement | **Ley Step** | Spell line/range setup. | No mobile-skirmisher identity; must not erase CT/range/cloth counters. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** (bolder/readable premises) on top of this clean
  consolidation.
- `F4`/`F5` magic-physical coexistence (with **Shell on**, not only unmitigated magic); `T5` delayed
  timing; `T4` status delivery; `T8` for `Death`; undead rows for `Death`/`Poison`/`Toad`/elements;
  `T9` MP economy.
- Watch: Black Magicks as the best secondary for most casters; `Flare` replacing elemental choice;
  `Death`/`Toad` as a universal hard-enemy answer; magic making physical weapon planning feel optional.

## DCL rebase notes

- **The DCL magic pipeline is unwritten (`11`).** Every elemental `K`, the `Flare` constant, and the
  `0.60` Faith floor are v0.2 artifacts pending the DCL magic-damage equation. The whole damage column
  re-derives there.
- **Faith is inverse in the DCL** (two-sided; spell resistance via inverse-Faith); the
  `casterFaith·targetFaith` product re-expresses on the DCL Faith model. Orator's `Faith`/`Atheist`
  windows feed the same system.
- **Affinity (weak/resist/absorb/halve) is engine-neutral** — it is existing FFT status/element
  vocabulary and carries into the DCL directly; it stays the primary element-identity engine.
- **Status spells** (`Poison`/`Toad`/`Death`) run the DCL **3d6 status contest** (`13`); the % targets
  re-express as 3d6 resist numbers, magic-source statuses resisting on the relevant DCL pool.
- **Shell / Reflect** attach to whatever the DCL magic-defense and redirection primitives become; the
  v0.2 `0.667` Shell layer is provisional.
- **`Arcane Backlash`** is an optional, narrow caster reaction → DCL reaction taxonomy (`13`), most
  naturally a **caution/neutral** reaction rather than Brave-scaled.
