# Ramza

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Ramza layers — `57` (concrete-v0, Ramza chapter rows) — folded into this
single decision doc. Ramza had no separate v1 proposal; the concrete pass is the primary source.

> **No rediscussion yet.** Ramza never received the bolder "good-job rediscussion" pass; this
> consolidates the v0.2 concrete chapter table. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; Ramza's physical strikes re-anchor onto DCL
> subtractive-DR weapon math, while his hybrid/magic actions (`Spellblade`, `Arc Blade`, `Ultima`)
> ride the **unwritten DCL magic equation** (DCL `11`). See *DCL rebase notes*.

## Identity / compass

Ramza is the **unique protagonist job**: always present, he **evolves by chapter** and reaches
top-tier *broad* value by Chapter 4 through **flexibility, not specialist dominance**. He should feel
useful in every chapter, but never the best Squire, best Knight, best caster, best controller, or best
late vanguard in the same row — his power is breadth (weapons + leadership/morale + a hybrid
sword/magic bridge), bought at the price of never owning a protected specialist lane.

Chapter skills are **story-unlocked at JP 0** — chapter evolution is guaranteed without turning Ramza
into a hidden grind path.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `protagonist` |
| Secondary tags | `hybrid`, `leadership` |
| Growth profile | hybrid |
| Armor class | chapter-dependent (TBD) |
| Weapon families | knight/mage hybrid access (TBD); swing / crush / thrust / magic |
| Role reason | Evolves by chapter; reaches top-tier value by Chapter 4 through flexibility, not specialist dominance. |

**Good at:** being relevant in every band, self-tempo + morale support, a knight/mage hybrid bridge,
broad late-game flexibility.
**Bad at / countered by:** anything a dedicated specialist does in its protected lane — no Rend, no bow
identity, no item breadth, no White revive, no Black burst, no Time control, no Mystic status, no
terrain, no Jump, no speech/gun, no Iaido, no two-hit burst, no summon area, no Vanguard protection.

Ramza does **not** receive native staff/rod/pole MA-crush access — his magical scaling appears only
through chapter actions, not an unbounded staff-melee anti-plate route.

## Action skills (chapter-progressing)

*(v0.2 chapter stat bands: Ch1 HP150/PA4/MA4/Spd6 → Ch2 280/8/7/7 → Ch3 308/9/8/7 → Ch4 473/12/12/8.)*

| Chapter | Skill | Intent | Guardrail |
|---------|-------|--------|-----------|
| 1 | **Squire fundamentals** | Same early damage floor as Squire — parity, not a better starter. | Inherits Squire values (`jobs/01-squire.md`); no Chemist item-heal, no Squire Rally. |
| 1 | **Tailwind** | Self-tempo nudge. | Self only; once/round; **no** Speed stat, **no** ally-Rally replacement. *(v0.2: self CT +8.)* |
| 2 | **Steel** | Battle-scoped morale buff (weaker than Orator `Praise`). | Self/one ally; **no** permanent Brave. *(v0.2: Brave +6, cap 80.)* |
| 2 | **Chant** | Small at-a-cost ally heal. | **No** revive, status clear, or Faith scaling. *(v0.2: ally +35 HP, Ramza −15 HP.)* |
| 3 | **Spellblade** | The hybrid sword bridge appears. | Single target; `floor((PA+MA)/2)` sword pressure, swing response. *(v0.2: hybrid sword ×0.85.)* |
| 3 | **Ward** | Light single-hit magic guard. | Self/one ally; one hit; **no** Shell replacement. *(v0.2: next incoming magic hit ×0.85.)* |
| 4 | **Shout** | Combined self-tempo + morale. | Self only; once/round; **no** PA/MA stat stack. *(v0.2: self CT +12, Brave +6 cap 80.)* |
| 4 | **Arc Blade** | Final single-target hybrid strike. | Single target; **no** line/AoE Holy Sword clone. *(v0.2: hybrid sword ×1.00.)* |
| 4 | **Ultima** | A real but non-dominant burst capstone. | **Below** dedicated Black Mage high spell; Shell/Faith/Reflect policy applies. *(v0.2: K22 MA/Faith small area; MP40/CT4; ≈158 vs Black Mage late ≈234.)* |

`Steel`/`Shout` use the same **battle-scoped** morale doctrine as Orator — no permanent Brave.

## Reaction / Support / Movement

*(Deferred by chapter; any RSM must preserve the per-band specialist-protection checks below.)*

## Per-band specialist-protection rule

Ramza bridges each band but never wins its protected lane:

| Band | Ramza has | Ramza lacks (stays specialist-owned) |
|------|-----------|--------------------------------------|
| 0/A | Squire sword floor + self CT | Chemist item-heal, Squire Rally |
| B | Brave/heal utility | Rend, bow, item breadth, White revive/protection, Black burst |
| C | Spellblade/Ward hybrid bridge | Time control, Mystic status, terrain, Jump, speech, gun |
| D | broad coverage | Iaido, two-hit burst, summon area, global performance |
| E | top-tier breadth + `Ultima` + `Arc Blade` | Black burst ceiling, Vanguard protection |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- `P0`/`F5` Squire-floor parity; `T5`/`T10` self-CT (Tailwind/Shout, no recursion); `F4`/`F5` hybrid
  strikes + `Ultima` (below Black burst, per-band rule holds); `T6xPS` Ward; final chapter equipment.
- Watch: Ramza outscaling any specialist in its protected lane; `Ultima` creeping toward Black Mage
  burst; permanent Brave leaking in via Steel/Shout; RSM choices breaking the per-band checks.

## DCL rebase notes

- **Physical strikes** (Ramza sword across chapters, the `Decisive`-less weapon rows) re-anchor onto
  the DCL's **subtractive DR by damage type**; magnitudes re-derive on the DCL scale, parity-with-
  Squire intact.
- **Hybrid / magic** (`Spellblade`, `Arc Blade`, `Ultima`) ride the **unwritten DCL magic equation**
  (`11`) and the inverse-Faith model — these are **placeholders** until that pipeline exists; the
  "Ultima below Black burst" relationship must be re-proved there.
- **Leadership / morale** (`Steel`, `Shout`, Brave deltas) plug into the DCL **two-sided Brave**
  ecology (shared with Orator) — battle-scoped, no permanent Brave.
- **Ward** (single-hit magic guard) attaches to whatever the DCL magic-defense primitive becomes
  (pending `11`).
- **Tailwind / Shout** self-CT are turn-economy effects independent of the damage engine; the
  once/round, no-recursion rules carry over.
- **Reactions** (deferred) classify per the DCL reaction taxonomy (`13`); a protagonist survival/
  support reaction is most naturally **neutral/caution**, not Brave-scaled.
