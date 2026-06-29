# Summoner

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Summoner layers — `21` (v1 proposal) and `43` (concrete-v0) — folded into
this single decision doc.

> **No rediscussion yet.** Summoner never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; summon damage rides the **unwritten DCL magic
> equation** (DCL `11`), and the **area model** itself (target-count, geometry) is v0.2 schema. See
> *DCL rebase notes*.

## Identity / compass

Summoner is the **high-commitment delayed area caster**: large effects, huge MP/CT cost, visible
payoff, and meaningful whiff risk. It wins **through area total** when targets cluster and timing
justifies the commitment — **not** by being "Black Mage but bigger" per target. The core decision is
commitment: is this cluster worth the CT/MP, and can the caster be protected until resolution?

It is punished by spread formations, fast rushdown before resolution, Silence/MP denial, Shell/
Reflect/element resistance, forced movement breaking clusters, and cloth fragility.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `caster-offense` |
| Secondary tags | `AoE`, `MP` |
| Growth profile | magical |
| Armor class | `cloth` |
| Weapon families | staff, rod, fists (crush / magic) |
| Role reason | Delayed area caster; trades CT/MP and fragility for powerful, readable large-scale effects. |

**Good at:** clustered-target area payoff, defensive/utility summons, premium burst on big clusters.
**Bad at / countered by:** spread enemies, MP attrition, CT/movement, Silence/Shell/Reflect, overkill
on poor clusters, cloth durability.

### Lane vs Black Mage / Meteor

Summoner beats Black Mage **only** when cluster + timing + MP justify area commitment; Black Mage wins
for faster/cheaper/smaller/flexible damage. `Bahamut` (Summoner's on-role reliable clean area payoff)
and Time Mage `Meteor` (off-role, longer telegraph, worse prediction) must **not** collapse into the
same premium non-elemental button. **Area output is normalized by expected target count** in
coexistence checks — a summon cannot pass on per-target damage alone.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Moogle / Faerie** | Area recovery for grouped allies that can wait for CT. | Below White Mage focused healing; must not replace White Mage/Chemist; delayed-heal risk stays. *(v0.2: K 9 / K 13, max 3 allies.)* |
| **Shiva / Ramuh / Ifrit / Titan** | The elemental area damage lanes (ice/lightning/fire/earth). | Affinity + cluster setup; no universal best element; Float/terrain counters Titan. *(v0.2: K 9–10, max 3, MP 18–22, CT 4–5.)* |
| **Golem** | Party-facing physical protection summon. | **Same** physical layer as `Protect`, does **not** stack with it; not mandatory upkeep. *(v0.2: 0.667 layer, max 3 allies.)* |
| **Carbuncle** | Group magic-routing setup with backfire risk. | Routing, **not** pure immunity; one-reflection/fizzle/targetability rules. *(v0.2: Reflect status, max 3.)* |
| **Bahamut / Odin / Cyclops** | Premium non-elemental area capstones for major clusters. | High CT/MP/JP; must not replace the element list; `Bahamut` is the tightest F5 ceiling row. *(v0.2: K 15/14/14; MP 60/54/58; CT 7/6/6.)* |
| **Leviathan / Salamander** | Premium water/fire area when affinity/terrain supports it. | Target-profile dependent; fire summons feed the weak-element compound watch. *(v0.2: K 11, MP 50, CT 6.)* |
| **Sylph** | Lighter wind/spirit area pressure. | Must hold a distinct niche or be consolidated. *(v0.2: K 8.)* |
| **Lich** | Current-HP drain pressure on high-HP/undead targets. | Percent-of-current-HP + cap, **does not scale with MA/Faith/gear**; undead rows. *(v0.2: 25% current HP, cap 120, 50% drain, max 2.)* |
| **Zodiark** | Hidden ultimate reward. | **Outside ordinary balance**; separate hidden/boss proof; must not raise the normal ceiling. |

## Reaction / Support / Movement

*(All deferred in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Summon Ward** | Narrow channeling defense to survive slow casts. | Must not become broad caster immunity. |
| Support | **Summon Focus** | Summon reliability / MP efficiency / area discipline only. | Not a broad magic booster. |
| Support | **Grand Invocation** | Late "true summoner" focus. | Must not compress MP-discount + damage + CT-fix into one mandatory slot. |
| Movement | **Ritual Step** | Set up a cast line / hold safe formation. | Must not erase cloth fragility or CT commitment. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- Area-model (target-count/geometry/ally-safe); `T5` delayed resolution; `T9` MP economy; `T3`/`T3xT5`
  Moogle/Faerie/Lich; mitigation-stack for Golem; spell-routing for Carbuncle; `F4`/`F5`.
- **Named cross-job compound watch (F5):** constructible `Geomancer Magma Surge → Oil → Ifrit/
  Salamander` weak-element 3-target area (worst-case Salamander 594 / Ifrit 486 raw), and the same
  vector amplified by `Mystic Belief` (→ Salamander 681 / Ifrit 558). Test as one compound case.
  `Bahamut` neutral 3-target (405/415) is the tightest ceiling row.
- Watch: Summon as the best secondary for all casters; one premium replacing the element list;
  ally-safe area removing positioning tradeoffs; Golem/Carbuncle as mandatory prebuff; MP supports
  making premiums routine.

## DCL rebase notes

- **Summon damage rides the unwritten DCL magic pipeline (`11`)** and the inverse-Faith model; every
  `K·MA·Faith` value re-derives there. The Bahamut/Meteor ceiling watch carries forward.
- **The area model is a v0.2 schema** (T11A geometry + target-count normalization). The DCL needs its
  own area/AoE treatment; the *discipline* (normalize by expected target count) is engine-independent
  and should carry over.
- **Lich** is percent-of-current-HP — engine-neutral; the cap + no-MA-scaling rule carries over;
  undead interacts with DCL KO/corpse/undead rules.
- **Golem / Protect** share the DCL physical-defense layer (DCL uses subtractive DR by type); `Golem`
  stays non-stacking with `Protect`. **Carbuncle/Reflect** attaches to the DCL magic-redirection
  primitive (pending `11`).
- **Summon Ward** maps to the DCL reaction taxonomy (`13`) — a narrow **neutral** survival reaction.
