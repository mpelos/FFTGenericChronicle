# Ninja

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Ninja layers — `25` (v1 proposal), `56` (concrete-v0, Ninja rows), and the
Ninja cross-job rows in `58` (physical-foundation RSM) — folded into this single decision doc.

> **No rediscussion yet.** Ninja never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Ninja is one of the **highest-risk
> convergence jobs**: `Dual Wield` (Two Swords) is a protected multi-hit stress engine and must stay
> incidence-gated and hit-count-normalized. See *DCL rebase notes*.

## Identity / compass

Ninja is the **fast multi-hit skirmisher and thrown-weapon specialist**: speed, dual-wield burst,
ninja-blade/knife pressure, single-target assassination, finite tactical Throw reach, and evasive,
fragile, positional combat. Its iconography is exactly where late-game physical convergence happens
if the job is not bounded.

Ninja must **not** be the best shell for every physical build, the best user of every weapon family
through Throw, the universal anti-armor answer, the universal owner of the support slot via
`Dual Wield`, or functionally untargetable through Vanish/evasion loops.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `melee-physical` |
| Secondary tags | `fast`, `dual-wield` |
| Growth profile | physical |
| Armor class | `leather` |
| Weapon families | ninja_blade, knife, fists (swing / thrust / crush) — **flail rejected for this pass** |
| Role reason | Fast physical pressure and multi-hit stress job; must stay iconic without making all physical optimization converge on it. |

**Good at:** fast two-hit single-target burst, knife/thrust anti-mail spikes, isolating fragile
targets, map skirmishing.
**Bad at / countered by:** leather durability/low sustain, area magic + status bypassing single-target
evasion, durable plate without a crush plan, anti-evasion/accuracy pressure, Throw resource limits.

### Weapon-mode discipline

- **ninja_blade** = `spd_pa_wp` swing; **knife** = `spd_pa_wp` thrust (anti-mail access). Both on the
  **Speed/PA axis** — Ninja naturally cares about Speed + PA.
- **flail** (`rdm_pa_wp` crush) is **rejected for the concrete pass**: dual-flail variance reduction
  would make Ninja a sneaky reliable anti-plate unit. The equipment pass must remove flail from Ninja
  or make it a weak/volatile side route — Ninja is **not** a protected anti-plate job (Monk/Geomancer
  own that).
- **fists** crush exists but must not compete with Monk's protected unarmed lane.

## Throw design rules

Throw is **tactical reach, not a second equipment system**. V1 routing principle:
`Throw = Speed-routed thrown routine using the category's own capped value; damage type = missile`.
A thrown sword does **not** use sword's swing routine; a thrown katana does **not** carry Brave/Iaido;
a thrown spear does **not** become Dragoon thrust; a thrown flail is **not** ranged crush. Categories
vary by thrown value/range/JP/rarity/resource friction, single-target by default. Inventory/gil
economy is deferred campaign policy — Throw must be balanced as **battle behavior first**.

## Action skills (Throw)

*(v0.2: `throw_pressure = floor((PA+Speed)/2)·(throw_value·phase_scalar)`, missile, penetration 0.20.
Throw values below; all single-target, JP-gated.)*

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Throw Shuriken** | Baseline low-cost ranged chip. | Below melee; finite/resource policy. *(v0.2: value 7.)* |
| **Throw Daggers** | Light precision throw. | Does not inherit knife thrust; must not replace Thief/Dragoon reach. *(v0.2: 8.)* |
| **Throw Ninja Blades** | Signature thrown burst for exposed targets. | High resource/cost; not repeated default damage. *(v0.2: 12.)* |
| **Throw Swords / Katana / Polearms / Poles / Books** | Generic/odd weapon throws (preserve if distinct). | Do **not** inherit the family routine/type — no borrowed Knight/Samurai/Dragoon identity; consolidate duplicates. *(v0.2: 9–10.)* |
| **Throw Axes** | Heavy volatile throw. | Speed-routed missile; no universal finisher. *(v0.2: 11.)* |
| **Throw Flails** | Heavy thrown option (if flail access survives). | Missile only, **no ranged crush**. *(v0.2: 10.)* |
| **Throw Knight's Swords** | Premium very-late thrown burst. | Campaign/economy + F5 risk; not ordinary baseline. *(v0.2: 13.)* |
| **Throw Bombs** | Consumable special/elemental throw. | `T9`/campaign policy; no Black Mage/Summoner replacement; element/status deferred. *(v0.2: 10, missile.)* |

**Active Ninja innate dual-wield:** two eligible weapon hits (`ninja_blade`/`knife` only), second-hit
×1.00; does **not** apply to Throw/Iaido/spells/reactions/fists/flails.

## Reaction / Support / Movement

*(All deferred / boundary-bound in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Vanish** | Brief escape from targeting after taking risk. | `T5xT8`/`T4`; short/breakable/visible, ends on action or damage; **no** permanent untargetable loop; T4 must decide stealth-strike bypass and keep it narrow. |
| Reaction | **Reflexes** | Light evasion fallback if Vanish can't be bounded. | Must not stack into practical evasion immunity. |
| Support | **Dual Wield** | The iconic late multi-hit engine. | Protected Two Swords `2`-hit; **no** fists/Throw/Iaido/spell/reaction/non-weapon use; **no** `Doublehand` stacking; **no** double-Throw; incidence-gated — must not become the universal physical default. |
| Support | **Throw Mastery** | Throw range/resource/category only. | No compression with `Dual Wield` into safe ranged dominance. |
| Movement | **Move +3** | Elite skirmisher mobility. | Asymmetric **donor-pull** risk (other jobs dip Ninja just for movement); must compete with Teleport/Move +2/Jump/terrain; measure on-job vs off-job value separately. |
| Movement | **Ignore Terrain** | Stealth-route alternative if Move +3 is too universal. | Map-dependent; not a default mobility patch. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- `T2.1`/`F5` active + learned `Dual Wield` incidence (vs Doublehand/Attack Boost/Brawler); `T4`/`T6`
  Throw routing + flail/crush side routes; `T5xT8`/`T4` Vanish (untargetable loop + stealth-strike
  bypass); `T9`/deferred policy Throw resource pacing; `T2.1` Move +3 donor-pull.
- Watch: most physical builds taking `Dual Wield` by default; active Ninja as the best shell for every
  weapon plan; Throw giving Ninja the best ranged version of every family; Vanish/Reflexes →
  practical untargetability/evasion immunity; Move +3 as the default late movement.

## DCL rebase notes

- **`Dual Wield` (Two Swords) is a multi-hit stress engine** in both v0.2 and the DCL. The discipline
  that matters carries over: a two-hit total must be compared **with the dual-wield lens** (hit-count
  normalized), not as if it were a single-hit family, and it stays incidence-gated. The `×2` hit shape
  is engine-neutral; only the per-hit damage re-derives.
- **Speed is native to the DCL** (turn frequency + contest input), so `spd_pa_wp` ninja_blade/knife and
  the `fast` identity carry over directly. Damage uses **subtractive DR by type** — the v0.2
  multiplicative `armor_response` rows re-derive; knife thrust keeps its anti-mail spike, fists/flail
  crush stays bounded so Ninja never becomes protected anti-plate.
- **Throw** is a missile expression; under the DCL, missiles halve DR (the divisor primitive), so the
  Speed-routed `missile` throw re-anchors there — still **without** inheriting the thrown family's
  routine/type (the anti-family-theft rule is engine-independent).
- **Vanish** (timed self-untargetability) maps to the DCL via the same timed-exclusion logic as
  Dragoon's airborne window; the no-loop / narrow-stealth-strike rules carry over. **Vanish/Reflexes**
  classify as **neutral** reactions (not Brave-scaled).
- Statuses delivered by Ninja actions run the DCL **3d6 contest** (`13`).
