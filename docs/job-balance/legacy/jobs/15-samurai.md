# Samurai

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Samurai layers — `24` (v1 proposal), `56` (concrete-v0, Samurai rows), and
the Samurai cross-job rows in `58` (physical-foundation RSM) — folded into this single decision doc.

> **No rediscussion yet.** Samurai never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Samurai's `Brave` tag is **central to the
> DCL**: the engine makes Brave two-sided and its katana offense already scales Brave natively. See
> *DCL rebase notes*.

## Identity / compass

Samurai is the **disciplined Brave/katana physical job**: Brave-linked swing pressure, Iaido spirit
techniques, and high-value support/reaction pieces (`Shirahadori`, `Doublehand`) that reward late
investment. It should feel strong and stylish — strongest when Brave, katana access, deliberate
single-weapon commitment, and measured area/support draws matter — **without** becoming another
universal sword answer.

**Plate resists swing**, so Samurai must **not** solve plate by default unless a specific Iaido/support
investment earns a bounded alternate route. Damage Iaido is a **Brave/PA physical katana-spirit**
expression — explicitly **not** Faith-independent reliable magic on a plate body.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `melee-physical` |
| Secondary tags | `katana`, `Brave` |
| Growth profile | physical |
| Armor class | `plate` |
| Weapon families | katana, fists (swing / crush) |
| Role reason | Disciplined Brave/katana job; strong and stylish without becoming another universal sword answer. |

**Good at:** Brave-scaled katana pressure, single-target discipline, small-area/support Iaido draws,
weapon-defense reads (`Shirahadori`), single-weapon commitment (`Doublehand`).
**Bad at / countered by:** plate targets (swing resisted), lower reach than Dragoon / range than
Archer, magic/status bypassing weapon defense, low Brave or Brave pressure.

## Iaido design rules

- **Damage Iaido** = `br_pa_wp katana pressure × skill_multiplier`, swing type, **no Faith**, **not
  balanced by katana break / inventory**. Ordinary katana attack stays the direct single-target line;
  Iaido spends damage for **shape, support, status, or premium identity**.
- **Support draws are routed independently** from damage draws and must stay small-area, non-looping,
  not replacements for White Mage / Time Mage / Chemist / Summoner.
- Iaido must not be a pure damage ladder — consolidate duplicates rather than keep twin buttons.

## Action skills (Iaido / Draw Out)

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Ashura** | Early draw teaching Iaido range/target selection. | Low ceiling; should not beat direct katana attacks. *(v0.2: katana-spirit ×0.60.)* |
| **Kotetsu** | Reliable ordinary draw. | Range/area limits; below direct katana attack; no universal best button. *(v0.2: ×0.75.)* |
| **Bizen Osafune** | Focused single/narrow-area pressure. | Must not erase basic katana or Black Mage damage. *(v0.2: ×0.90.)* |
| **Murasame** | Classic restorative draw — small area ally heal. | **Support route**; no revive, no status clear, no Faith scaling; below White Mage/Chemist. *(v0.2: 60 HP.)* |
| **Ame-no-Murakumo** | Area/formation draw — target count is the value. | `T11` target-count normalization; not generic AoE superiority. *(v0.2: ×0.85.)* |
| **Kiyomori** | Protective draw — small-area ally guard. | **Support route**; one physical/magical hit; **non-stacking** with Protect/Shell-like mitigation. *(v0.2: next incoming hit ×0.85.)* |
| **Muramasa** | Curse draw — status is the value, not damage. | `T4`/`T5` accuracy/immunity; uses Slow (Doom/Confuse avoided — Doom already on Monk/Orator). *(v0.2: ×0.50 + Slow 30%.)* |
| **Kiku-ichimonji** | Line/reach draw rewarding lane reading. | Line geometry must prove target-count safety; not Archer/Summoner replacement. *(v0.2: ×0.90 line.)* |
| **Masamune** | Decisive support draw — small-area ally momentum. | **Support route**; **no** Haste/Quick/Reraise/action-grant this pass; `T10` if any grant added. *(v0.2: Regen + CT +8.)* |
| **Chirijiraden** | Premium capstone finisher. | High cost/risk; narrow target shape; may exceed ordinary attack only as a late, narrow, expensive payoff. *(v0.2: ×1.10.)* |

## Reaction / Support / Movement

*(All deferred / boundary-bound in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Shirahadori** | Disciplined read/avoid of a narrow class of weapon attacks. | **Hard block-chance ceiling regardless of Brave** (Brave 97 cannot reach practical immunity — a design invariant); weapon-family/facing/range limits; **no** coverage of magic/status/area/guns/special. |
| Reaction | **Bonecrusher** | Wounded-swordsman critical retaliation. | Should not become the best general counter over Monk/Thief/Knight. |
| Support | **Equip Katana** | Non-Samurai Brave-linked katana swing unlock. | Must not make Samurai only a support stop. |
| Support | **Doublehand** | Protected single-weapon power engine. | Protected Two Hands `×1.80`; **no stacking with Dual Wield**; competes with Dual Wield/Attack Boost/Brawler/equipment; fails only if most physical builds take it regardless of context. |
| Support | **Iaido Focus** | Iaido reliability/radius/resource only. | No broad magic/damage/defense compression. |
| Movement | **Waterwalking** | Terrain-flavored movement. | Map-dependent; not broad mobility. |
| Movement | **Blade Step** | Optional non-water stance/position movement. | Must not become a generic Move +N / Teleport replacement. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- `F4`/`F5` damage Iaido vs Black Mage/Summoner/direct katana; `T11` Iaido area/line; `T3`/`T3xT5`
  support draws; `T6xPS` Kiyomori; `T4`/`T5` Muramasa; `T2.1`/`F5` `Doublehand`/`Shirahadori`
  incidence; high-Brave Samurai stress rows.
- Watch: `Doublehand` as the default physical support; `Shirahadori` + evasion → practical immunity;
  Iaido as "better Black Magic on a plate body"; `Equip Katana` making active Samurai irrelevant;
  premium Iaido replacing Summoner/Black Mage area without CT/MP/Faith/target-count tradeoffs.

## DCL rebase notes

- **Brave is central to Samurai in the DCL.** Katana = `br_pa_wp` swing; the DCL makes **Brave
  two-sided** (Brave-scaling weapon families, courage/caution reactions, Brave-inverted taunt), so
  Samurai's offense and Iaido **scale Brave natively** — the v0.2 "Brave should matter, bounded" goal
  is delivered by the engine, not hand-rolled. The `Orator Brave manipulation visibly affects Samurai`
  cross-job effect stays an F5-visible watch.
- **Damage Iaido is physical katana-spirit**, so it rides the **weapon-side DCL pipeline** (subtractive
  DR by type), *not* the unwritten magic equation — this is exactly the v0.2 "no Faith-independent
  magic on a plate body" decision, and it transfers cleanly. Swing into plate keeps Samurai honest.
- **Iaido status / curse** (Muramasa's Slow, etc.) runs the DCL **3d6 contest** (`13`); the rates
  re-express there.
- **Shirahadori** is a weapon-defense reaction → DCL reaction taxonomy (`13`). The DCL's depleting
  active-defense (Parry/Block) and the **hard Brave ceiling** invariant both fit naturally; keep it
  bounded away from magic/projectile coverage. Classify as a **courage (Brave)** reaction with a cap.
- **Doublehand** (Two Hands ×1.80) is a v0.2 stress engine; under the DCL it re-expresses as a
  single-weapon damage/handling bonus and must stay non-stacking with Dual Wield.
- The multiplicative `armor_response` rows are v0.2 and re-derive under the DCL's subtractive DR.
