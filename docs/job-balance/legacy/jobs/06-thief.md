# Thief

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Thief layers — `22`, `54` (Thief rows), and `73` — folded into this single
decision doc.

> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> values in parentheses are **v0.2-era and provisional**. Thief fits the DCL especially well: **facing
> is native and central** to the engine (a rear attack already bypasses the defense roll), so the
> positional identity is amplified rather than hand-rolled — see *DCL rebase notes*.

## Identity / compass

Thief is the **fast precision opportunist**: knife pressure, rear attacks, poison, arm/leg
disruption, low-odds high-emotion steals, and mobility. It should feel like a D&D thief translated
into FFT — the player cares about reaching a rear arc, poisoning a target, disabling a key enemy long
enough to steal from it, or planning a whole battle around stealing one enemy's special gear.

It is weaker in fair front-line trades, against equipment-less targets, against status immunity, into
heavy evasion without setup, and whenever leather units are punished for overextending.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `specialist` |
| Secondary tags | `fast`, `knife` |
| Growth profile | physical |
| Armor class | `leather` |
| Weapon families | knife, fists (thrust / crush) |
| Role reason | Fast utility and precision; uses Speed, stealing/disruption, and thrust identity without becoming pure damage. |

**Good at:** rear-arc burst, attrition (poison), action/movement denial, stealing special gear, map
mobility.
**Bad at / countered by:** fair front trades, equipment-less targets, status immunity, heavy evasion
without setup, leather overextension.

## Shared status & equipment vocabulary

| Effect | Meaning | Thief source |
|--------|---------|--------------|
| `Poison` | attrition over time | `Venom Knife` |
| `Disable` | target cannot act | `Arm Aim` |
| `Immobilize` | target cannot move (not `Stop`) | `Leg Aim` |
| `Charm` | target acts under opposing influence (breaks on damage) | `Steal Heart` |
| Equipment stolen | actual enemy gear removed and kept | equipment steals |

Steals stay **real** — a successful steal shows the actual stolen item and the enemy losing it, never
an invisible exposure status.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Backstab** | Primary positional damage button; rewards reaching a vulnerable angle. | Knife/sword; no bonus from the front (falls back to a normal attack); no status/steal rider. *(v0.2: rear ×1.50 hit +0.15; side ×1.20 hit +0.05.)* |
| **Venom Knife** | Readable attrition pressure. | Knife/sword; visible `Poison`; immunity respected. *(v0.2: ×0.75; Poison 60%, +10 side/rear.)* |
| **Arm Aim** | Action-denial opportunity strike. | Knife/sword; visible `Disable`; low reliability; bounded duration. *(v0.2: ×0.55; Disable 35%, +10 side/rear.)* |
| **Leg Aim** | Movement-denial opportunity strike (more reliable than Arm Aim — denying move is weaker than denying action). | Knife/sword; visible `Immobilize`; no `Stop`. *(v0.2: ×0.55; Immobilize 45%, +10 side/rear.)* |
| **Steal Heart** | Classic charm flavor (leaves Orator room for deeper social control). | No gender restriction; visible `Charm`; damage breaks it. *(v0.2: Charm 30%; side +5, rear +15.)* |
| **Equipment steals** (`Steal Helm/Armor/Shield/Weapon/Accessory`) | Real low-odds permanent theft — planning around stealing a specific item is the fun. | Removes the actual equipped item; only what the enemy has; no new equipment; no damage rider; failure spends the action. *(v0.2 base: Helm/Armor/Shield 35%, Weapon/Accessory 25%; facing front −10 / side +5 / rear +15; `speed_mod = clamp(3·(spdU−spdT), −12, +12)`.)* |
| **Steal Gil** | Conservative flavor/economy action. | No Gil tuning in this pass. |

**Cut:** `Steal EXP` (EXP/JP economy out of scope).

## Reaction / Support / Movement

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Sticky Fingers** | Opportunity riposte when a melee attacker misses the Thief. | **Non-Brave**; no steal reward; once/round; legal weapon range only. *(v0.2: 55% trigger; ×0.60, rear ×0.75.)* |
| Support | **Light Fingers** | The committed stealing support. | Steal actions only; not ordinary accuracy or attack skills; no immunity bypass. *(v0.2: steal success +15pp.)* |
| Support | **Poach** | (deferred) | Monster/economy scope; name and fantasy preserved for a later pass. |
| Movement | **Move +2 / Jump +2** | Bold mobility options (horizontal vs vertical). | Movement slot; no terrain/elevation bypass. |
| Movement | **Treasure Hunter** | (deferred) | Campaign/map reward hook; no Gil edits; no combat mobility. |

## Open items / validation hooks

- Watch: `Backstab` spammed / outdamaging dedicated physical jobs; `Arm Aim`/`Leg Aim` making Thief a
  better controller than Time/Mystic/Orator; `Light Fingers` + rear making rare-gear theft too
  reliable; permanent theft breaking campaign equipment pacing.
- `T4 accuracy/evasion`, `T5 status/duration`, `T7 equipment` (permanent theft + pacing), deferred
  economy policy (`Steal Gil`, `Treasure Hunter`, `Poach`), `F5 real-roster`.
- Control-identity sweep: `Arm Aim`/`Leg Aim`/`Steal Heart` vs Time/Mystic/Orator.

## DCL rebase notes

- **Facing is native and central in the DCL** — a rear attack gets **no defense roll**, a side attack
  is **−2 defense**. So `Backstab`'s rear/side bonus is partly the engine's own structure; under the
  DCL it becomes a damage bonus layered on top of an already-bypassed defense, and the whole Thief
  positional identity is **amplified** by the engine rather than bolted on.
- **Knife = thrust ×2** in the DCL (`14`, the "assassin's finisher," + a Speed grant). `Backstab` on a
  knife is rear thrust ×2 — native burst against the unarmored.
- **Status infliction** runs the DCL **3d6 contest** (`13`): `Poison` is physical (resisted by
  base-HP); `Disable`/`Immobilize` map onto the stun/knockdown (Don't-Act/Don't-Move) family,
  physical-source → base-HP; `Charm` is mental → Brave (will). The % values re-express as 3d6
  resist numbers.
- **Steal mechanics** are engine-neutral; the `speed_mod` carries over (Speed exists as turn
  frequency + as a contest input).
- **Sticky Fingers** triggers on an enemy **miss** (failed 3d6 hit, or defended) and is non-Brave →
  the DCL **neutral reaction** category.
