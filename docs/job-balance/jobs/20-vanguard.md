# Vanguard

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Vanguard layers — `29` (v1 proposal) and `57` (concrete-v0, Vanguard rows) —
folded into this single decision doc. Vanguard is the canonical replacement for the vanilla **Mime**
slot; the name is settled (`49`), never the placeholder "Special Knight".

> **No rediscussion yet.** Vanguard never received the bolder "good-job rediscussion" pass; this
> consolidates v1 intent + v0.2 concrete numbers. The identity is solid; the kit should still get a
> rediscussion pass on this clean doc.
>
> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**; Vanguard is a plate physical job, so most of
> it re-anchors onto the DCL's subtractive-DR weapon math and native control primitives. See *DCL
> rebase notes*.

## Identity / compass

Vanguard is the **late elite-knight** that replaces Mime — comparable in value to a Holy Knight but
**not a clone**. Its fantasy is *vanguard discipline*: shield/plate formation play, local protection
and interception, committed crush/guard-pressure arts, and setup-gated decisive strikes. It wins
through commitment, position, equipment, and setup — **not** through free ranged holy damage.

It explicitly carries **none** of Mime's identity: no automatic action copying, no mimic-all-ally
behavior, no hidden extra turns or reaction loops, no "wear nothing and copy better jobs."

It is weaker when no ally needs protection and no formation can be held, against magic/status that
bypasses guard posture, against spread enemies who refuse local fights, and against anti-plate crush.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `late-reward` |
| Secondary tags | `elite-knight`, `vanguard` |
| Growth profile | physical |
| Armor class | `plate` |
| Weapon families | sword, spear, axe, fists (swing / thrust / crush) |
| Role reason | Late vanguard comparable in value to Holy Knight but not a clone; wins through protection, formation, and setup, not free range. |

**Good at:** holding chokepoints, protecting fragile allies, guard/armor-pressure setup, plate/shield
local control, setup-gated finishers.
**Bad at / countered by:** magic/status pressure, spread enemies, mobile ranged threats, targets with
no guard/armor/weapon to exploit, anti-plate crush.

### The Knight moat (do not become "Knight plus more weapons")

Knight keeps **offensive equipment destruction** and Rend-style permanent/semi-permanent gear
attrition — Knight-exclusive. Vanguard owns **defensive projection, formation protection, and
*temporary* exposure windows**. Vanguard's exposure marks are deliberately shorter/weaker than
Knight's Rend windows. It must not become a better Dragoon (spear, but no Jump/reach mastery),
Samurai, Monk (axe is the volatile `rdm_pa_wp` crush option, not reliable Brave-crush), or Geomancer.
`knight_sword` is **not native** — only behind the optional, cuttable `Equip Knight Swords` support.

## Action skills

Output and mitigation use the **strongest single applicable channel** — they do **not** stack
multiplicatively with Protect/Shell-like or each other.

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Breach** | Committed crush/guard-pressure art (renamed from v1 "Vanguard Break"). | Axe/fists/shield-crush only; **no** sword/spear modal rider; applies a short exposure mark. *(v0.2: crush ×0.75 + exposure mark; mark Δresponse plate +0.06 / mail +0.05 / leather +0.03 / cloth 0, cap 1.15, one hit.)* |
| **Intercede** | Mark/guard a nearby ally against one incoming threat. | Nearby ally only; single direct hit; **no** global cover or broad invulnerability. *(v0.2: ally hit ×0.75, Vanguard takes 25% chip.)* |
| **Aegis Stance** | Short-lived defensive posture around the knight. | Trades offense; lasts until next Vanguard turn; **no** stacking into immunity. *(v0.2: self/adjacent incoming ×0.85, Vanguard outgoing ×0.75.)* |
| **Sunder Guard** | Break a shield/guard/armor-response window for party follow-up. | Setup tool, **not** top damage; **does not destroy gear**; weaker than Knight Rend. *(v0.2: weapon ×0.45 + exposure mark + shield/weapon guard ×0.60 next hit.)* |
| **Commanding Challenge** | Make a nearby enemy respect the knight via local pressure. | Local mark only; **no** boss hard-lock or forced-AI dependency. *(v0.2: soft challenge +25; ignored target output ×0.85.)* |
| **Decisive Strike** | Setup finisher — strong **only** against challenged/guard-broken/exposed targets. | **No** instant KO, no Holy Sword clone, no free range; normal armor response. *(v0.2: weapon ×1.20 if setup-marked, else ×0.75.)* |

**Rejected:** a free modal `Vanguard Art` (one action auto-switching swing/thrust/crush riders) —
too-often-correct regardless of weapon; any later swing/thrust/crush split must be separate learned
actions with independent JP.

## Reaction / Support / Movement

*(All deferred in v0.2 pending build-incidence; boundaries below are binding.)*

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Intervention** | Protective reaction for a nearby attacked ally (preferred if only one survives). | Local only; **no** global cover; `T10` if it grants extra attacks. |
| Reaction | **Last Stand** | Bounded critical frontline survival. | **No** practical immortality. |
| Support | **Equip Knight Swords** | Optional prestige weapon unlock. | **Cuttable**; must not revive sword dominance. |
| Support | **Vanguard Training** | Vanguard-action specialization. | Narrow; not a universal physical support. |
| Support | **Armor Discipline** | Plate/shield specialization. | No mitigation-stack immunity; must not make every job want plate. |
| Movement | **Vanguard March** | Formation movement for plate/shield units. | Posture/formation-gated; **not** best-in-game mobility; must not replace Ninja/Dragoon/Time/performer movement. |

## Open items / validation hooks

- **NOTE: this job still needs a rediscussion pass** on top of this clean consolidation.
- `J-SPK-WEAPON-ACCESS` must run on **Vanguard's actual roster row** (real plate stats + sword/spear/
  axe/fists + support-gated knight_sword) in F5/no-dominance — not only the formula anchor jobs.
- `T6xPS`/`T8` guard+cover (Intercede/Aegis/Intervention/Armor Discipline ≠ practical immunity);
  `T4`/`T6xT7` exposure marks; `T5`/`T8` Commanding Challenge; `F5`/`T4`/`T6` Decisive Strike;
  `T2.1` support incidence; final display name.
- Watch: every physical build wanting `Breach`; every durable build wanting `Armor Discipline`;
  `Equip Knight Swords` as the default late support; `Intervention` as practical global cover;
  Vanguard becoming a better Knight/Dragoon/Samurai/Monk at once.

## DCL rebase notes

Vanguard echoes Knight, and the DCL provides natively much of what v0.2 hand-rolled (cf.
`jobs/03-knight.md`):

- **Exposure marks** (`Breach`/`Sunder Guard`) → the DCL's **facing/active-defense** system already
  models exposure (flank = −2 defense, back = no defense roll); the ×response-delta marks re-express
  as temporary active-defense/DR penalties rather than flat multipliers, and stay **shorter/weaker
  than Knight Rend** by construction.
- **Guard pressure** (`Sunder Guard` shield/weapon guard ×0.60) → the DCL's **depleting Parry/Block**
  and *fura-guarda* primitive; re-express as guard depletion, not a flat multiplier.
- **Intercede / Aegis Stance / Intervention** are cover/mitigation; the DCL uses **subtractive DR by
  damage type** for the physical side, and the "strongest single mitigation channel" rule carries over
  cleanly as a DCL stacking constraint. Reactions classify per the DCL reaction taxonomy (`13`) —
  protective reactions are most naturally **caution/neutral**, not Brave-scaled.
- **Commanding Challenge** → **native DCL taunt** (`13`), which is Brave-inverted (the low-Brave tank
  is the natural taunter) — sharpens the vanguard fantasy directly.
- **Weapon rows** (sword/spear/axe/fists into plate/mail/leather/cloth) re-anchor onto DCL
  type-vs-armor subtractive DR; axe stays the volatile crush option (`rdm_pa_wp`-style), not a Monk
  replacement. Any status rider runs the DCL **3d6 contest** (`13`).
- **Decisive Strike** is engine-neutral in shape (a conditional weapon multiplier gated on a setup
  mark); only the magnitude re-derives on the DCL damage scale.
