# Mystic

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Mystic layers — `20` (v1 proposal) and `44` (concrete-v0) — folded into this
single decision doc.

> **No rediscussion yet.** Mystic never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; Faith-window and drain math depend on the
> **unwritten DCL magic equation** (DCL `11`) and the DCL's **inverse-Faith** model. See *DCL rebase
> notes*.

## Identity / compass

Mystic is the **spiritual / status controller** with unusually broad MA-crush weapon access. It
controls spiritual state — Faith windows, Brave/morale, silence/disable-style pressure, drain, and
undead-adjacent hooks — and wins through *matchup preparation*, not raw damage. Each status must have
a distinct reason to use it and a distinct reason not to. Hard control is deliberately low-accuracy
and short — Mystic's reason to exist is its **soft** value (Faith windows, drains, anti-caster
pressure, cleanup), not hard shutdown.

It is punished by status immunity, anti-magic/low-Faith targets, fast physical rushdown, Silence/MP
denial, undead inversions, and cloth fragility.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `controller` |
| Secondary tags | `Faith`, `crush` |
| Growth profile | magical |
| Armor class | `cloth` |
| Weapon families | pole, book, staff, rod, fists (crush / magic) |
| Role reason | Spiritual/status controller with unusually broad MA-crush access; must not eclipse dedicated casters or crush specialists. |

**Good at:** Faith setup windows, anti-caster status/MP pressure, bounded drains, spiritual cleanup,
soft debuffs.
**Bad at / countered by:** status immunity, boss resistance, low direct damage, accuracy/Faith/CT
limits, MP pressure, fast rushdown.

### The Monk moat (do not eclipse)

Mystic's broad MA-crush access (rod/pole/book/staff) is **`ma_wp` / `pampa_wp` crush**; Monk fists are
**`br_pa_pa` Brave/PA crush**. The lanes are separated by **stat axis**, not weapon — Mystic must not
become a better unarmed-crush job than Monk, nor a better terrain hybrid than Geomancer.

## Action skills (Mystic Arts)

| Skill | Status / effect | Intent | Guardrail |
|-------|-----------------|--------|-----------|
| **Chant** | next Mystic Art +10 base hit | Spend an action to make one status land. | No stacking; Mystic statuses only. *(v0.2: +10; MP 0.)* |
| **Umbra** | `Blind` | Accuracy disruption. | `T4` rows; not broad defense. *(v0.2: 75 base, 24t.)* |
| **Empowerment** | MP drain | Anti-caster resource pressure + fuel. | Target-resource limited; **no** infinite MP loop. *(v0.2: K 5 MP, 50% drain.)* |
| **Invigoration** | HP drain | Spiritual life-drain (damage + recovery). | Below dedicated heal/damage; cannot replace White/Black Mage. *(v0.2: K 10, 50% drain.)* |
| **Belief** | `Faith` window (up) | Empower a planned magic window. | Battle-scoped, deliberately small (affects every caster); `F4`/`F5`. *(v0.2: ×1.15 layer, 24t.)* |
| **Disbelief** | `Faith` window (down) | Weaken enemy magic / protect an ally. | Battle-scoped; cannot invalidate magic. *(v0.2: ×0.80 layer, 24t.)* |
| **Corruption** | undead mark | Undead-adjacent interaction setup. | Must **not** preempt Necromancer; undead rows required. *(v0.2: 65 base, 24t.)* |
| **Quiescence** | `Silence` | Anti-caster shutdown. | `T4`/`T5` immunity/duration. *(v0.2: 75 base, 24t.)* |
| **Fervor** | `Berserk` | Force risky aggression. | Not broad AI control; `T8`-sensitive. *(v0.2: 60 base, 18t.)* |
| **Trepidation** | Brave window (down) | Reduce physical confidence/reaction reliability. | Battle-scoped; **no** permanent Brave grief. *(v0.2: −15 Brave, 24t, 80 base.)* |
| **Delirium** | `Confuse` | Risky misplay pressure. | `T8`-sensitive; not reliable hard control. *(v0.2: 55 base, 12t.)* |
| **Harmony** | spiritual cleanup | Clear the Mystic spiritual/control set. | Does not replace Esuna as general cleanup. *(v0.2: MP 16.)* |
| **Hesitation** | `Disable` | Constrain selected actions. | `T4`/`T5` immunity/duration. *(v0.2: 60 base, 12t.)* |
| **Repose** | `Sleep` | Temporarily remove a target. | Damage breaks it; no boss/default answer. *(v0.2: 60 base, 12t.)* |
| **Induration** | `Stone` | Rare high-risk hard seal. | Very strong immunity/accuracy/CT limits. *(v0.2: 35 base, 8t.)* |

Faith and Brave changes are **battle-scoped** unless a later progression doc deliberately accepts
permanent morale/religion manipulation (permanent stat grief fights the growth policy).

## Reaction / Support / Movement

*(All deferred in v0.2 pending build-incidence; design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Absorb MP** | Anti-caster resource reaction (narrower identity). | `T9`; no infinite MP loop. |
| Reaction | **Mana Shield** | Spend MP to soften a hit. | `T9`/mitigation gate; real visible MP cost; **no** broad immunity. |
| Support | **Halve MP** | Caster endurance specialization. | `T9`/`T2.1`; must not be a mandatory caster tax. |
| Support | **Magick Defense Boost** | Anti-magic posture. | Mitigation gate/`F4`; must not stack into magic immunity. |
| Support | **Mystic Focus** | Status/spiritual reliability only. | Must not boost every status + magic action. |
| Movement | **Manafont** | Small MP recovery via movement/terrain. | `T9`; must not erase MP attrition. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- `T4`/`T5` status accuracy + duration; `T8` for Fervor/Delirium/Corruption; `T9` drain/MP economy;
  `T3`/`T3xT5` for Invigoration; `F4`/`F5` Faith manipulation + MA-crush coexistence; undead rows.
- **Named cross-job compound watch (F5):** `Belief × Geomancer Magma Surge/Oil × Summoner Ifrit/
  Salamander` — Belief turns a weak-element fire-summon cluster into a ~2.30× Faith-linked area vector
  (worst-case 3-target totals: Ifrit 558, Salamander 681). Must be tested as one compound case.
- Watch: every caster wanting Halve MP; magic parties requiring Belief/Disbelief; Mana Shield + MP
  economy → broad immunity; Induration/Repose/Delirium as the safest hard-enemy answer; MA-crush
  breadth making Monk/Geomancer/caster weapon choices irrelevant; Mystic-Arts secondary always having
  *some* relevant status into every matchup.

## DCL rebase notes

- **Faith is inverse and two-sided in the DCL.** `Belief`/`Disbelief` re-express as temporary
  Faith/anti-Faith deltas on the DCL Faith model (shared with Orator's `Faith`/`Atheist` windows and
  the casters); the ×1.15/×0.80 layers re-derive once the DCL magic equation (`11`) exists.
- **All the statuses** run the DCL **3d6 contest** (`13`); base-hit % re-express as 3d6 resist
  numbers. `Trepidation` (Brave down) and `Fervor` (Berserk) plug into the DCL's two-sided Brave
  ecology (cf. Orator).
- **Drains** (`Empowerment` MP, `Invigoration` HP) are engine-neutral in shape; only the magnitude
  re-derives on the DCL scale.
- **The Monk moat is a DCL concept too** — DCL `14` already separates unarmed `br_pa_pa` crush from
  MA-based weapons; Mystic's MA-crush lane stays distinct by stat axis there.
- **Reactions** (`Absorb MP`/`Mana Shield`) map to the DCL reaction taxonomy (`13`) — anti-caster
  **neutral** reactions with real resource cost.
- **Corruption / undead** interacts with the DCL KO/corpse/undead state rules; keep it from preempting
  the Necromancer identity.
