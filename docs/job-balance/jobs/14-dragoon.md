# Dragoon

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Dragoon layers — `24` (v1 proposal) and `55` (concrete-v0, Dragoon rows) —
folded into this single decision doc.

> **No rediscussion yet.** Dragoon never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Dragoon's spear/thrust identity is
> **amplified natively by the DCL** (thrust ×2 / Perfuração, native reach), and Jump is engine-neutral
> tempo. See *DCL rebase notes*.

## Identity / compass

Dragoon is the **reach/jump physical specialist** and the clean spear/thrust job: excellent into mail,
good at elevation and timing, and uniquely able to leave the board through Jump at real CT and
prediction cost. Its strength has shape — it is strongest when reach, height, timing, and mail
targets matter. The player asks: is the target likely to stay vulnerable until landing? Is it worth
becoming briefly untargetable? Does this map reward vertical/horizontal reach?

It must **not** become "a Knight with spears," a safe off-board loop, a universal answer to every
armor, or a range/elevation grind ladder. **Plate resists thrust**, so Dragoon is deliberately not a
protected anti-plate answer.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `melee-physical` |
| Secondary tags | `thrust`, `plate` |
| Growth profile | physical |
| Armor class | `plate` |
| Weapon families | spear, fists (thrust / crush) |
| Role reason | Reach/jump physical specialist; the clean spear/thrust job and a natural anti-mail route. |

**Good at:** anti-mail thrust pressure, height/reach control, delayed-commitment Jump timing, plate
durability.
**Bad at / countered by:** plate targets (thrust resisted), Jump CT/whiff/landing risk, magic/status/
speed control, fast/mobile targets that waste delayed attacks.

### Dragoon vs Thief (thrust split)

Both share thrust/anti-mail, but on **different axes**: Dragoon is **PA**-scaled reach + Jump +
elevation + plate durability; Thief is **Speed**-scaled knife precision + stealing + disruption +
leather fragility. The fists fallback must not compete with Monk's protected crush.

## Action skills

Jump should buy **better ways to Jump**, not a stack of numeric damage upgrades. Final data may
collapse the unlock ladder into reach bands (reliable short / long horizontal / high vertical / late
master Jump) — the rule is that mastery expands Jump *target choice*, not damage.

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Jump** | Core delayed spear strike: leave the board, avoid targeting briefly, land a thrust hit. | Spear/thrust only; whiffs if the target/panel is invalid at resolution; same-tick resolution is unsafe; enemy regains agency at landing; non-evadable **only** while CT/whiff/airborne costs hold. *(v0.2: spear ×1.25; CT `ceil(50/Speed)`.)* |
| **Horizontal Jump +1…+7** | Reach unlocks — usability on ordinary maps up to specialist capstone reach. | **No damage bonus**; higher reach must not become safe ranged melee or replace Archer range. *(v0.2: max reach 3 → 8.)* |
| **Vertical Jump ±2…±8** | Elevation unlocks — height usability up to master cliff answer. | Map-dependent; no generic damage; capstone competes with `Ignore Elevation`. *(v0.2: vertical tolerance 2 → 8.)* |

## Reaction / Support / Movement

*(All deferred in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Dragonheart** | Iconic reraise/revive for a frontline jumper who risks landing in danger. | Bounded; **no** broad practical immortality / auto-reraise loop. |
| Reaction | **Brace Landing** | Optional narrow post-landing reaction (fallback if Dragonheart is too broad). | Should not reduce all incoming damage. |
| Support | **Equip Polearms** | Lets non-Dragoon jobs deliberately build anti-mail thrust. | Meaningful support cost; must **not** make Dragoon irrelevant as the spear home or let every physical shell leak spear. |
| Support | **Jump Training** | Jump speed/range/landing reliability only. | Must not become broad melee accuracy/mobility/damage; **no CT reduction accepted** without re-opening Jump timing gates. |
| Movement | **Jump +1/+2/+3** | Vertical map mobility (distinct from active Jump). | Improves *where the unit can move*, not the attack; map-dependent; competes with Teleport/Move +2. |
| Movement | **Ignore Elevation** | Terrain capstone. | High incidence risk; must not become the default late movement. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- `T4`/`T5` Jump hit/timing/whiff; **`T5xT8`** airborne untargetability (must not create an off-board
  loop removing enemy agency); `T6` plate-resist proof; `T2.1` for `Dragonheart`/`Equip Polearms`/
  `Jump +3`/`Ignore Elevation` incidence; `F5`.
- Watch: every physical job using spears (thrust/reach too efficient); Jump as the safest damage loop
  (off-board too often); `Dragonheart` as the default survival reaction; `Ignore Elevation`/`Jump +3`
  as default late movement.

## DCL rebase notes

- **Spear thrust is amplified natively by the DCL.** Spear = `pa_wp` thrust (penetration 0.10) in
  v0.2; in the DCL, **thrust mode = ×2 (Perfuração)** with native anti-mail behavior (`14`). The
  anti-mail identity and the `Jump ×1.25` re-anchor onto the DCL thrust pipeline; damage uses
  **subtractive DR by type** (so the v0.2 multiplicative armor-response rows re-derive). Plate's high
  thrust-DR keeps Dragoon honestly weak into plate.
- **Reach is native in the DCL** — the engine already models weapon reach, so Dragoon's reach identity
  is structural, not bolted on.
- **Jump timing** (CT `ceil(50/Speed)`, airborne window, landing tick) is **engine-neutral**: CT and
  charged/telegraphed actions exist in the DCL, and charges are not damage-interrupted. The T5xT8
  airborne-untargetability discipline carries over.
- **Dragonheart / Brace Landing** map to the DCL reaction taxonomy (`13`). Dragonheart (revive under
  risk) is most naturally a **caution/neutral** reaction; it is **not** a Brave lever.
