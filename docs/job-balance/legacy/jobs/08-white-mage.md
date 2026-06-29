# White Mage

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered White Mage layers — `19` (v1 proposal) and `42` (concrete-v0) — folded into
this single decision doc.

> **No rediscussion yet.** Unlike Squire–Orator, White Mage never received the bolder
> "good-job rediscussion" pass; this consolidates the most recent material that exists (v1 intent +
> v0.2 concrete numbers). The identity is solid; the kit should still get a rediscussion pass on this
> clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**, and casters are the **least settled under the
> DCL**: the engine's **magic-damage equation is not yet written** (DCL `11`) and the DCL resolves
> magic via **inverse-Faith** resistance — so every `K·MA·Faith` number below is a placeholder pending
> the DCL magic pipeline. See *DCL rebase notes*.

## Identity / compass

White Mage is the **delayed, Faith-linked recovery and protection caster**: the dedicated home of
healing throughput, revive, status cleanup, and the Protect/Shell/Wall mitigation package, with a
single high-investment offensive outlet (`Holy`). It should feel stronger and more expressive than
vanilla, but **without** erasing Chemist certainty, Monk frontline sustain, or physical durability.

It wins when delayed, Faith-linked healing/protection has time to resolve; it is punished by burst
that beats the heal, Silence/MP pressure, Reflect/Shell, forced movement, and cloth fragility.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `caster-support` |
| Secondary tags | `Faith`, `staff` |
| Growth profile | magical |
| Armor class | `cloth` |
| Weapon families | staff, fists (crush / magic) |
| Role reason | Healing/protection caster; staff gives a minor MA/crush fallback, but support magic is the identity. |

**Good at:** delayed recovery throughput, revive, status cleanup, tactical mitigation windows, `Holy`
as a committed nuke.
**Bad at / countered by:** cloth durability, CT/interrupt windows, MP attrition, Silence/Reflect/Shell,
weaker direct offense, lower reliability than items in immediate lethal windows.

## Three-way recovery ecosystem

Revive/heal lanes stay legible across jobs: **Phoenix Down** (Chemist) is cheap/certain/item-based,
**Monk Revive** is risky/frontline/adjacent, **White Mage Raise/Arise** are delayed, Faith-linked, and
higher-throughput. The shared revive-race validation is `T3xT5`.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Cure line** (`Cure`/`Cura`/`Curaga`/`Curaja`) | Primary delayed HP lane; beats low-tier items when timing allows. | CT/MP/Faith/range/overheal/interrupt cost; same-tick heals are unsafe. *(v0.2: K 14/20/26/32; MP 5/12/22/34; CT 2/3/4/5.)* |
| **Raise** | Basic delayed revive (reachable before Arise). | Must resolve before the death clock; same-tick fails; must not erase Phoenix Down or Arise. *(v0.2: 25% max HP; MP 12; CT 4.)* |
| **Arise** | Premium revive for committed builds. | High MP/JP/CT; must not trivialize death-clock races. *(v0.2: 70% max HP; MP 30; CT 5.)* |
| **Reraise** | Preemptive safety on a key unit. | Expensive, narrow, timing-sensitive; never permanent upkeep. *(v0.2: one auto-revive; MP 36; CT 5.)* |
| **Regen** | Attrition prevention, not burst. | Competes with (not replaces) direct healing. *(v0.2: 10% max HP/tick × 4; MP 10; CT 2.)* |
| **Protect** | Pre-emptive physical mitigation. | Visible, duration-bounded, weaker than immunity; a separate ordinary layer. *(v0.2: 0.667 physical layer; `Protectja` = area.)* |
| **Shell** | Magical mitigation; keeps magic/Faith/status answerable. | Protected stress engine; bound by mitigation-stack + magic-coexistence gates. *(v0.2: 0.667 magic layer; `Shellja` = area.)* |
| **Wall** | Combined Protect+Shell package on one target. | Must not become cheap universal upkeep (MP must bite). *(v0.2: both layers; MP 24; CT 4.)* |
| **Esuna** | Defined-set status cleanup. | Reactive cure, not prevention/immunity; dedicated status jobs still matter. *(v0.2: MP 12; CT 2.)* |
| **Holy** | The one high-investment offensive outlet. | Single-target/narrow; must **not** outclass Black Mage's general damage plan. *(v0.2: K 32; MP 42; CT 5.)* |

## Reaction / Support / Movement

*(RSM values were deferred in v0.2 pending build-incidence; these stay design placeholders.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Divine Grace** | Narrow emergency survival/protection reaction. | Low-incidence; **not** the default caster reaction; recovery reactions pass `T3/T3xT5`. |
| Support | **Arcane Ward** | Healing/protection reliability or efficiency. | Must not boost all magic damage; must not compress White+Black support into one slot. |
| Support | **Faithful Casting** | Rewards committed Faith-linked support builds. | Helps White Magicks specifically, not every caster action. |
| Movement | **Sanctuary Step** | Reach safe support positions / hold formation. | No broad teleport/flying; must not erase cloth vulnerability. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** (bolder/readable premises) on top of this clean
  consolidation.
- `T3`/`T3xT5` healing + revive races; `T5` delayed timing; `T4` status delivery/accuracy;
  mitigation-stack composition for Protect/Shell/Wall; `T9` MP economy; `F4`/`F5` magic-physical
  coexistence.
- Watch: White Magicks preferred over Items for *all* recovery; every caster equipping the same
  support; Protect/Shell/Wall as mandatory prebuff upkeep; `Holy` as the best generic single-target
  magic damage.

## DCL rebase notes

- **The DCL magic pipeline is unwritten (`11`).** Every `K·MA·Faith` value and the `0.60` Faith floor
  are v0.2 artifacts; they must be re-derived once the DCL magic-damage equation exists. This is the
  single largest rebase gap for casters.
- **Faith is inverse in the DCL.** The DCL resolves spell resistance via **inverse-Faith** (and Faith
  is two-sided), so the `casterFaith·targetFaith` product re-expresses on the DCL's own Faith model —
  Orator's `Faith`/`Atheist` windows (`jobs/07-orator.md`) plug into the same system.
- **Protect / Shell** become the DCL's defensive layers. The DCL uses **subtractive DR by damage type**
  for physical; `Protect` re-expresses there, while `Shell` attaches to whatever the DCL magic-defense
  primitive turns out to be (pending `11`). The `[0.25, 2.50]` clamp is a v0.2 construct.
- **Healing / revive / Regen / Esuna** are effects with engine-neutral *shape*; only the HP/MP scaling
  re-derives onto the DCL scale. The three-way revive ecosystem (Phoenix Down / Monk Revive / Raise)
  is preserved across engines.
- **Reactions** (`Divine Grace`) map to the DCL reaction taxonomy (`13`) — a White Mage emergency
  reaction is most naturally **neutral** (not Brave-scaled).
