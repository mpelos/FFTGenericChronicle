# Time Mage

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Time Mage layers — `20` (v1 proposal) and `44` (concrete-v0) — folded into
this single decision doc.

> **No rediscussion yet.** Time Mage never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; the magic-damage parts (`Meteor`) depend on
> the **unwritten DCL magic equation** (DCL `11`). See *DCL rebase notes*.

## Identity / compass

Time Mage is the **tempo controller**: it wins by changing turn windows — CT, Speed states, delayed
action risk, Reflect routing, proportional pressure, and special movement — **not** by being another
damage caster. The central question is always "is it worth a turn now to create (or deny) turns
later?" Control is more dangerous than damage because it removes enemy decisions, so every tool is
bounded by accuracy, duration, and immunity.

It wins by exploiting timing windows; it is punished by fast pressure before setup resolves,
Silence/MP denial, status immunity, Reflect backfire, spread enemies, and cloth fragility.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `controller` |
| Secondary tags | `CT`, `staff` |
| Growth profile | magical |
| Armor class | `cloth` |
| Weapon families | staff, fists (crush / magic) |
| Role reason | Time/tempo controller; value comes from CT, speed, delay, and timing instead of raw spell damage. |

**Good at:** creating/denying turn windows, setting up allied burst (Archer/Dragoon/Summon/Meteor
timing), Reflect routing, proportional softening of high-HP targets.
**Bad at / countered by:** cloth durability, low direct damage outside Gravity/Meteor, MP/CT pressure,
status immunity, Silence, units that act effectively while Immobilized.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Haste / Hasteja** | Focused (and committed-area) tempo advantage. | Short, visible window; **never** permanent upkeep; `Hasteja` must not make Haste mandatory every fight (first cut: 3→2 ally cap). *(v0.2: ×1.50 Speed, 24 ticks; area max 3 allies.)* |
| **Slow / Slowja** | Focused (and risky-area) tempo denial. | Accuracy/duration/immunity-bounded; area must not hard-lock encounters. *(v0.2: ×0.67 Speed, 24 ticks; single 80 / area 65 base hit.)* |
| **Stop** | High-impact hard tempo stop. | Strong accuracy/immunity/duration limits; **no** boss/default answer. *(v0.2: 12 ticks, 45 base hit.)* |
| **Immobilize** | Position lock that still allows actions — enables ranged/terrain play. | Counters melee movement, not all threat. *(v0.2: 24 ticks, 85 base hit.)* |
| **Float** | Narrow terrain/earth-hazard utility. | Not a movement-skill replacement. *(v0.2: 36 ticks, ally utility.)* |
| **Reflect** | Magic routing as both protection and risk. | Can backfire; not pure immunity; bound by the spell-routing composition gate. *(v0.2: 24 ticks, 100 ally / 60 enemy base hit.)* |
| **Quick** | Action-window grant for a decisive setup/rescue. | **One-for-one, recursion blocked** (net action delta 0); high MP/JP/CT. *(v0.2: party + per-target grant cap 1; MP 42; CT 4.)* |
| **Gravity / Graviga** | Proportional pressure on high-HP targets — setup, not a finisher. | Percent-of-current-HP, **nonlethal**, capped; immunity/boss rules; must not outclass Black Mage. *(v0.2: Gravity 25% cap 120; Graviga 20% cap 90, max 3.)* |
| **Meteor** | Slow, telegraphed area capstone when timing/space are controlled. | Very slow/expensive/predictable; sits just below `Bahamut` on max total — joint F5 ceiling watch. *(v0.2: K 14, max 3, expected 1.8; MP 58; CT 10.)* |

## Reaction / Support / Movement

*(All deferred in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Critical: Quick** | Emergency tempo reaction for a wounded Time Mage. | Critical-only; **no loops**; cut if it causes recursion/universal adoption/opaque swings. |
| Support | **Swiftspell** | Deliberate fast-caster build (the dangerous global piece). | Priced as a major support choice, **not** a repair patch for every delayed spell; high incidence risk. |
| Support | **Temporal Focus** | Time Magicks timing/duration reliability only. | Must not accelerate every spell school. |
| Movement | **Teleport** | Iconic map-bending movement for committed builds. | Failure/range/cost limits; must **not** become the universal late movement default. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- `T5` speed/CT/duration; `T10` Quick + anti-recursion; `T4` status accuracy; spell-routing
  composition for Reflect; `T9` MP; `F4`/`F5` (Meteor vs Bahamut ceiling; Hasteja incidence).
- Watch: Haste as prebuff upkeep; Quick loops/Speed invalidation; Stop/Slow as the safest hard-enemy
  answer; Reflect turning magic matchups into non-decisions; Teleport/Swiftspell as universal defaults.

## DCL rebase notes

- **CT / Speed / charged actions all exist in the DCL** (turn frequency is native), so Haste/Slow/Stop/
  Immobilize and the `Meteor` charge are engine-neutral in shape; only the tick/Speed math re-anchors.
- **Status infliction** (Slow/Stop/Immobilize landing) runs the DCL **3d6 contest** (`13`); the base-hit
  % re-express as 3d6 numbers, magical-source statuses resisting on the relevant DCL pool.
- **Meteor damage** rides the **unwritten DCL magic pipeline** (`11`) and re-derives there; the
  Meteor/Bahamut ceiling watch carries forward.
- **Gravity/Graviga** are percent-of-current-HP — engine-neutral; the nonlethal cap carries over.
- **Reflect** attaches to whatever the DCL magic-redirection primitive becomes (pending `11`).
- **Quick** (action grant) is a turn-economy effect independent of the damage engine; the one-for-one,
  no-recursion rule carries over.
- **Critical: Quick** maps to the DCL reaction taxonomy (`13`) — a **caution/neutral** survival
  reaction, not Brave-scaled.
