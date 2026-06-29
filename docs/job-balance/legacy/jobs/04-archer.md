# Archer

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Archer layers — `04`, `06`, `53` (Archer rows), `58`, and `71` — folded into
this single decision doc.

> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. One structural caveat for the rebase: the
> rediscussion writes "bow/crossbow output" generically, but in the DCL **bow and crossbow scale on
> different stats** (bow = PA, crossbow = weapon-skill) — see *DCL rebase notes*.

## Identity / compass

Archer is the **ranged prediction specialist** and the only true bow/crossbow job. It stays
endgame-relevant by choosing the right shot for the map state: fast reliability, delayed payoff,
movement pinning, piercing a prepared target, or covering a tile.

It feels strong when it owns **height, line of fire, target prediction, and missile-favored armor
matchups**. It is weaker when rushed, terrain-blocked, denied line of fire, forced into close melee,
or asked to solve shield/evasion without setup. The vanilla `Aim` ladder (eight numeric variants)
collapses into a small set of readable shot choices.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `ranged-physical` |
| Secondary tags | `missile`, `anti-mail` |
| Growth profile | physical |
| Armor class | `leather` |
| Weapon families | longbow, crossbow, fists (missile / crush) |
| Role reason | The main bow job; stays endgame-relevant through range, targeting, height, and missile identity. |

**Good at:** height/line-of-fire control, target prediction, missile-favored (mail) matchups, tempo
pinning.
**Bad at / countered by:** being rushed into melee, terrain/line denial, shields/evasion without
setup, leather durability.

## Shared status vocabulary

| Status | Meaning | Archer source |
|--------|---------|---------------|
| `Pinned` | target Move −1 (short, non-stacking; not `Immobilize`/`Slow`/`Stop`) | `Pinning Shot` |
| `Pierced Mark` | next bow/crossbow hit on the target gains a piercing benefit, consumed once | `Piercing Shot` |
| `Covered Zone` | a visible delayed tile threat | `Covering Shot` |

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Quick Shot** | Low-commitment reliability: land a shot that matters *now*. | CT 0; no Charging; bow/crossbow only; not the default attack. *(v0.2: ×0.75, hit +0.15.)* |
| **Aimed Shot** | One readable delayed power shot (replaces the `Aim +1…+20` ladder). | CT 2, Charging; can fail/lose value if the target leaves the line; rewards constraining the target. *(v0.2: ×1.35, hit +0.15.)* |
| **Pinning Shot** | Movement + tempo pressure, not hard control. | `Pinned` (Move −1) is visible, non-stacking, short; the CT bite carries most of the weight. *(v0.2: ×0.70; target −12 CT.)* |
| **Piercing Shot** | A prepared armor-defeating setup, consumable by an allied bow/crossbow shot. | Bow/crossbow only (no guns/spells/items); one mark per target; expires if the target acts/becomes illegal. *(v0.2: ×1.05; `Pierced Mark` = next bow/crossbow hit ×1.20 final.)* |
| **Covering Shot** | Force enemies to respect a tile without hard-interrupting them. | Delayed visible `Covered Zone`; one legal enemy hit on resolution; fizzles if the zone is empty; Charging risk. *(v0.2: CT 2; hit ×1.00, +0.10.)* |

**Cut:** `High-Ground Shot` — height folds into the bow/crossbow baseline instead (height advantage
≥ 2 → eligible bow/crossbow hit +0.10; not a CT-0 damage button).

## Reaction / Support / Movement

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Arrow Guard** | Narrow anti-missile defense — strong in ranged duels, near-dead elsewhere. | Missile hits only; no melee/spell/status; **not** Brave-scaled. *(v0.2: 65% trigger; incoming missile hit ×0.50.)* |
| Reaction | **Speed Save** | A short tempo bump on surviving a hit. | Survivor-only; once per round; **no** permanent/battle Speed growth; not Brave-scaled. *(v0.2: 60% trigger; +8 CT.)* |
| Support | **Equip Bow / Equip Crossbows** | The bow/crossbow unlock lane. | No guns; no damage/accuracy rider; active Archer stays the best native shell. |
| Support | **Concentration** | An accuracy floor for bow/crossbow + Archer commands (narrowed from vanilla's universal evade bypass). | Bow/crossbow + Archer commands only; no spells/items/guns/melee; no immunity bypass; no guaranteed-hit stacking. *(v0.2: hit floor 0.75.)* |
| Innate | **Bow Mastery** | Active-Archer native payoff (not a support slot). | Active Archer only; no guns/spells/generic missile. *(v0.2: bow/crossbow ×1.10, hit +0.05.)* |
| Movement | **Jump +1** | Vertical utility supporting the height game. | +1 vertical only; no Move bonus; no terrain bypass. |

## Open items / validation hooks

- Watch: `Quick Shot` + `Concentration` making evasion irrelevant; `Pinned` chaining into
  immobilization; `Pierced Mark` becoming mandatory missile setup; `Covering Shot` locking narrow
  maps; `Arrow Guard` + evasion → practical missile immunity; innate `Bow Mastery` pushing active
  Archer too high.
- `T4 accuracy/evasion`, `T5 CT/delay`, `T6 armor response` (Pierced Mark), `T11 terrain/height`,
  `F5 real-roster` (rushed / shielded / evasive / bad-line / high-ground / mail-heavy).
- Open reviewer question: innate `Bow Mastery` only, vs adding a separate non-stacking portable
  `Marksman Training` support — deferred to incidence review.

## DCL rebase notes

- **Bow / crossbow / gun split is native and richer in the DCL** — and the rediscussion's generic
  "bow/crossbow output" must split: in the DCL **bow scales on PA** (the strength archer, keeping PA
  alive for archer builds), **crossbow scales on weapon-skill** (raw), and **gun scales on
  weapon-skill** (penetration). The per-shot multipliers re-anchor onto whichever stat the family
  uses.
- **Piercing Shot / `Pierced Mark`** ("×1.20 final" or "ignore 20% armor") is the DCL **armor divisor**
  primitive — missiles already pierce DR by halving it. Piercing Shot becomes a temporary/boosted
  armor-divisor; the anti-armor intent is native.
- **Hit/accuracy bonuses** (+0.15, +0.10, the 0.75 floor) map onto the **3d6 skill roll** and its
  modifiers; the height bonus is engine-neutral.
- **Arrow Guard** (incoming missile hit ×0.50) re-expresses through the DCL defense roll vs ranged
  (where Dodge/Block — not Parry — cover missiles); keep it missile-only.
- **Pinned (Move −1) + CT loss** and **Aimed/Covering Shot Charging** are engine-neutral (Move, CT,
  and charged actions all exist in the DCL; charges are not damage-interrupted).
