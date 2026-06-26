# Monk

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Monk layers — `04`, `18`, `54` (Monk rows), `58`, and `72` — folded into this
single decision doc.

> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Monk is **strongly native to the DCL**: the
> engine's `14-equipment.md` already models the Monk's body as a weapon (Martial Arts, `MA_wmod`
> scaling with job level, crush ×1, depleting MA-parry, Counter/Hamedo). See *DCL rebase notes* — and
> note a real Brave tension there.

## Identity / compass

Monk is the **protected unarmed impact job**: body discipline, fists, crush pressure, nearby
recovery, and counter-fighting.

It is excellent when it can commit to close positioning, exploit plate with crush, protect a tight
formation with Chakra, and punish enemies who engage it carelessly. It is weaker into mail, ranged
pressure, magic/status pressure, bad maps, and situations where cloth-melee exposure is too
dangerous.

Monk is **not** allowed to be the best damage job, best healer, best reviver, best reaction donor,
**and** best weapon-independent shell all at once.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `melee-physical` |
| Secondary tags | `crush`, `Brave` |
| Growth profile | physical |
| Armor class | `cloth` |
| Weapon families | fists / Martial Arts (crush) |
| Role reason | The unarmed impact specialist; a real anti-plate / body-discipline route that needs no weapons. |

**Good at:** close commitment, anti-plate crush, formation recovery (Chakra), punishing engagers.
**Bad at / countered by:** mail, ranged, magic/status, bad maps, cloth-melee exposure.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Pummel** | Reliable adjacent strike (a real learned body-tech, not a sub-attack). | Fists only; adjacent; no status/sustain rider. *(v0.2: fists ×1.10, hit +0.10.)* |
| **Cyclone** | Exposed close AoE — rewards body placement in a cluster. | The Monk must stand in the cluster; no range safety. *(v0.2: fists ×0.80, small self-centered AoE.)* |
| **Aurablast** | Limited ranged ki — act on bad maps without erasing the close-range weakness. | No AoE; no replacement for Archer/Dragoon/spell range. *(v0.2: fists ×0.85, range ~3.)* |
| **Shockwave** | Grounded line pressure — rewards lanes/terrain reads. | Grounded/path restricted; Float/height counters it; no universal ranged pressure. *(v0.2: fists ×0.95.)* |
| **Doom Fist** | Pressure-point status: a visible delayed KO. | Visible `Doom`, countdown 3, adjacent; respects immunity; `Purification` does not clear it. *(v0.2: fists ×0.60; Doom 45%.)* |
| **Purification** | Narrow body-discipline cleanup (incl. Oil — a fire-combo counter). | Self or adjacent ally; clears Poison/Blind/Silence/Immobilize/Oil only; no HP heal; no Doom. |
| **Chakra** | Headline close-formation pulse: HP (and iconic MP) to self + adjacent allies. | No revive; no range safety; HP below dedicated healers; **MP tightly capped (top sim-watch)**. *(v0.2: HP = 20+⌊L/3⌋+2·PA, cap 18% max; MP = 6+⌊L/8⌋+⌊PA/2⌋, cap 10% max.)* |
| **Revive** | Risky emergency revive at fragile HP. | Adjacent; guaranteed; no Reraise/mass-revive/bonus-heal. *(v0.2: max(20, 20% maxHP).)* |

## Reaction / Support / Movement

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Counter** | Core Monk reaction: make melee engagement costly. | Post-hit, adjacent melee only; once/round; **rediscussion intent: non-Brave** (see DCL note); no ranged/magic/recursion. *(v0.2: 70% trigger; fists ×0.75.)* |
| Reaction | **First Strike** | Late, narrow, **non-negating** preemptive strike. | Does not cancel the incoming attack; adjacent melee only; **non-Brave** (see DCL note). *(v0.2: 45% trigger; fists ×0.70.)* If only one reaction survives, `Counter` is priority. |
| Support | **Brawler** | Monk's portable build hook. | Unarmed/fist only; no weapons; does not improve Chakra/Revive/Purification; no stacking with premium weapon engines. *(v0.2: ×1.20.)* |
| Support | **Martial Discipline** | (deferred) | Reopen only if Monk secondary later needs a support hook; risks making Martial-Arts secondary too complete. |
| Movement | **Lifefont** | Scaling movement-heal hook. | No MP/stock/revive; once per turn; not on forced movement. *(v0.2: 8+⌊L/6⌋+PA, cap 8% maxHP.)* |

## Open items / validation hooks

- Watch: Chakra/Revive/Brawler making Martial Arts the default secondary; Knight-body + Monk
  secondary compressing damage+sustain+revive+durability; `Lifefont`+`Chakra` erasing attrition;
  `First Strike` feeling like Hamedo; Chakra-MP fueling caster loops; `Doom Fist` oppressive in
  ordinary encounters.
- `T3 healing/attrition`, `T3xT5 revive timing`, `T4 accuracy`, `T6 armor response` (anti-plate
  without erasing mail/weapon families), `T9 MP economy` (Chakra MP), `F5 real-roster`.

## DCL rebase notes

- **Monk is native to the DCL.** `14-equipment.md` models unarmed as `base(PA) + MA_wmod`, crush ×1,
  with `MA_wmod` scaling by Martial-Arts (Monk job) level, plus a depleting **MA-parry** and
  Counter/Hamedo reactions. The fists multipliers (×1.10, etc.) re-anchor onto that unarmed pipeline;
  the **anti-plate crush identity** (crush vs plate's low crush-DR) is the DCL's own design.
- **Brave tension (resolve in rebase).** The rediscussion deliberately made Counter/First Strike
  **non-Brave**. But in the DCL, Monk **offense already scales with Brave** (`(base(PA)+MA_wmod) ×
  Brave-offense`), and the DCL reaction taxonomy (`13`) makes **Counter a *courage* reaction (high
  Brave)** by default. So under the DCL, Brave is naturally central to Monk — Counter can stay a
  courage reaction (Brave-scaled) rather than flat. Decide per the DCL taxonomy at rebase; the
  rediscussion's "no reaction should be a universal Brave problem" concern is satisfied DCL-side by
  the courage/caution/neutral split, not by forcing Monk off Brave.
- **Doom Fist** → `Doom` runs the DCL 3d6 status contest; the % re-expresses there.
- **Chakra / Revive / Lifefont / Purification** are recovery effects — engine-neutral in shape; only
  the HP/MP scaling re-derives on the DCL scale.
- **Defensive identity** = MA-parry (depleting) + high HP + Dodge + Counter, exactly as `14` frames
  it; cloth armor keeps Monk fragile to ranged/magic.
