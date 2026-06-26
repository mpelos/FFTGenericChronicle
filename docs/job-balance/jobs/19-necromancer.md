# Necromancer

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Necromancer layers — `27` (v1 proposal) and `45` (concrete-v0) — folded into
this single decision doc. Necromancer **replaces the vanilla Arithmetician/Calculator slot**.

> **No rediscussion yet.** Necromancer never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; dark/drain damage and Faith interactions ride
> the **unwritten DCL magic equation** (DCL `11`) and the DCL's **inverse-Faith** model, so every
> `K·MA·Faith` number is a placeholder pending the DCL magic pipeline. The v0.2 stat anchor itself was
> borrowed from Mystic (no real Necromancer job row existed). See *DCL rebase notes*.

## Identity / compass

Necromancer is the **late dark-state controller** that replaces Calculator without inheriting its sin:
it never solves the map through abstract global rules. Its power comes from **battle state that has
already happened** — wounded, poisoned, and doomed targets, KO bodies, undead units, MP-starved
casters, and risky drain windows. The player should feel they are *using* the fight (attrition, rot,
delayed lethality, drain, undead conversion, conditional finishers), not ignoring it.

Hard rejection (the anti-Calculator rule): **no** arithmeticks selectors, **no** level/CT/height/
prime/multiple global routing, **no** free whole-map targeting, **no** action that bypasses range,
line, CT, MP, Faith, hit chance, immunity, target state, and positioning all at once.

It is punished by status immunity / boss exclusions, low immediate burst, fast kills that prevent
setup, undead-specific reversals, low-value targets, Silence/MP pressure, and cloth fragility.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `late-reward` |
| Secondary tags | `dark-magic`, `undead` |
| Growth profile | magical |
| Armor class | `cloth` |
| Weapon families | book, pole, fists (crush / magic / drain) |
| Role reason | Late dark caster/debuffer; wins through dark-state setup, not raw damage or abstract global targeting. |

**Good at:** attrition/rot over long fights, delayed lethal pressure, HP/MP drain, undead/healing
inversion, corpse-adjacent zone play, setup-gated finishing.
**Bad at / countered by:** immune/boss/undead-safe targets, immediate burst races, MP/CT pressure,
poor value before the fight has wounds/bodies/states, fast kills that deny setup.

### The Mystic moat (strict subset)

Necromancer shares the **pole `ma_wp` crush** surface with Mystic and is intentionally **narrower**:
Mystic remains the cleaner broad spiritual/status controller; Necromancer only wins when **dark-state,
undead, drain, Doom, or corpse** conditions are actually relevant. The intended moat is kit
composition — Necromancer brings `Death Mark`/`Dark Harvest`/`Gravebind`/corpse; Mystic brings Faith
windows, broad control, and `Harmony`. The `Drain`/`Syphon` vs `Invigoration`/`Empowerment` overlap is
the thinnest coexistence point (`T2.1` watch). If a later equipment pass gives Mystic books, this must
be revisited.

## Action skills

| Skill | Effect | Intent | Guardrail |
|-------|--------|--------|-----------|
| **Rot** (Poison/Rot) | attrition status | HP attrition that pressures durable targets over turns. | Battle-scoped; not an invisible unavoidable tax; immune/boss rows. *(v0.2: 8% max HP/tick, cap 45, 4 ticks, 70 base.)* |
| **Death Mark** (Doom) | delayed conditional lethal | A visible countdown that threatens an already-weakened target. | **No cold instant kill** — lethal only if the mark persists to expiry **and** target ≤ 50% max HP, else nonlethal fallback; cleanseable; immunity matters. *(v0.2: 36-tick mark, 45 base; fallback 20% max HP, cap 120.)* |
| **Drain** | HP drain | Bounded dark damage + risky self-sustain. | No infinite sustain loop; not top burst. *(v0.2: K 12, 50% heal to caster.)* |
| **Syphon** | MP drain | Caster-resource pressure / dark fuel. | Target-resource limited; **no** MP loop; cannot delete caster jobs. *(v0.2: K 6 MP, 50% recovery — allowed above Mystic `Empowerment` because Necromancer is late + narrower.)* |
| **Undead Mark** (Zombie) | undead-state window | Invert a target's healing/revive profile for a window. | Battle-scoped; **state inversion, not damage amplification**; cleanseable; no permanent campaign state. *(v0.2: 24 ticks, 55 base.)* |
| **Corpse Puppet** | KO-body object | Consume a KO body into a non-acting targetable obstacle/decoy/zone-anchor. | **Non-acting by default** — no turns, skills, reactions, inherited gear, or extra unit; same-tick death-clock unsafe; immunity respected. *(v0.2: consumes body, 24-tick object.)* |
| **Command Undead** | restricted control | Narrow control of an undead/undead-marked body. | Undead/marked only; low accuracy; **not** a broad Charm replacement; no monster-scope dependency. *(v0.2: 12 ticks, 40 base.)* |
| **Gravebind** | corpse-anchor area attrition | Local percent-HP attrition zone around a body/marked target. | Local only (not mapwide); **non-elemental** percent max-HP, so `Belief`/`Oil` do **not** amplify it. *(v0.2: 6% max HP/tick, cap 35, 3 ticks, max 3.)* |
| **Dark Harvest** | conditional finisher | Finish a target that prior setup (mark/rot/undead) or low HP has already weakened. | **Requires precondition** (mark/rot/undead state or HP ≤ 30%); capped; boss/immune exclusions; **not** a random hard KO; not mapwide. *(v0.2: 30% max HP, cap 140, 70 base.)* |

## Reaction / Support / Movement

*(All deferred in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Soulbind** | Narrow dark backlash / counter-drain or curse on incoming pressure. | No broad damage reflection or immunity. |
| Reaction | **Death's Door** | Critical-state reaction buying a last dark action or curse. | `T10` if action-granting; otherwise no loops. |
| Support | **Dark Lore** | Necromancy specialization (drain caps, undead/corpse reliability). | No broad magic boost; worthless outside Necromancy by design. |
| Support | **Deathcraft** | Corpse/undead specialization (if that sub-kit survives). | Optional; Necromancy-only. |
| Movement | **Grave Step** | Reposition around KO bodies / marked targets / undead states. | Narrow; **not** Teleport/Fly/Move +3. |
| Movement | **Shadow Step** | Dark positioning fallback if corpse positioning is unimplementable. | Limited; preserves caster fragility. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- Real Necromancer stat/multiplier row (v0.2 borrowed Mystic's anchor); `T3`/`T5` Rot/attrition;
  `T4`/`T5`/`T8` Death Mark + Dark Harvest eligibility; `T9` Drain/Syphon resource loops;
  `T3xT5xT8` (KO/corpse/undead-state composition) for Corpse Puppet/Command Undead/Undead Mark;
  `T11`/`T3xT5xT11` Gravebind area-over-time; `F4`/`F5` book/pole MA-crush coexistence with Mystic.
- **Separate F5 watch vectors (do NOT fold into the Belief/Oil weak-element compound):** `Gravebind`
  is its own non-elemental percent-HP area-over-time vector; `Undead Mark` healing inversion is
  tracked as state inversion, not damage amplification.
- Watch: Necromancy as the default late caster secondary; Death/Doom solving bosses more safely than
  damage; drain replacing healing/resource economy; corpse actions creating extra actions/permanent
  units; Necromancer becoming Calculator again via any abstract/global targeting.

## DCL rebase notes

- **Dark damage rides the unwritten DCL magic pipeline (`11`)** and the inverse-Faith model; `Drain`
  and any Faith-linked dark damage re-derive there. The v0.2 Mystic-borrowed stat anchor must be
  replaced by a real Necromancer row.
- **Undead / corpse / drain interacts with the DCL KO/corpse/undead state rules** — `Corpse Puppet`,
  `Command Undead`, and `Undead Mark` healing inversion plug directly into the DCL death-state model;
  the non-acting-body and battle-scoped constraints carry over. Coordinate with **Mystic's
  `Corruption`** (Mystic must not preempt Necromancer's deeper undead/dark identity — Corruption is a
  shallow mark, Necromancer owns the full corpse/raise/inversion sub-kit).
- **Status infliction** (`Rot`, `Death Mark`, `Undead Mark`, `Command Undead`, `Dark Harvest`
  eligibility roll) runs the DCL **3d6 contest** (`13`); the base-hit % re-express as 3d6 resist
  numbers, dark/magic-source statuses resisting on the relevant DCL pool.
- **Percent-HP / drain effects** (`Rot`, `Gravebind`, `Death Mark` fallback, `Dark Harvest`, `Drain`/
  `Syphon` recovery, `Undead Mark` inversion) are **engine-neutral in shape** (cap + no-MA-scaling
  style, like Summoner's `Lich` and Time Mage's `Gravity`); only the magnitudes re-anchor on the DCL
  scale.
- **Reactions** (`Soulbind`/`Death's Door`) map to the DCL reaction taxonomy (`13`) — a wounded-state
  **caution/neutral** dark reaction rather than Brave-scaled.
- **Judgment call:** the anti-Calculator rule (local/targeted/state-bounded, never global-abstract) is
  engine-independent and remains the load-bearing identity constraint under the DCL.
