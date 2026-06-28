# DCL Implementation Readiness & Gap Analysis

Date: 2026-06-26
What this is: the **feasibility / gap pass** that `docs/deep-combat-layer/12-open-questions.md` §7
explicitly defers ("a feasibility pass against the formula-balance envelope is a future step before
any of this could be built"). It maps every DCL system to the runtime surface it needs and marks
what is READY / NEARLY-READY / BLOCKED, then lists the discovery, build, and authoring backlogs.
Method: read all 15 DCL docs (00–14) + a full capability inventory of the current mod
(`Mod.cs`, `ItemCatalog.cs`, `FormulaExpression.cs`, `docs/modding/*`, `docs/formula-balance/00-envelope.md`),
cross-checked by a subagent on 2026-06-26.

## Bottom line

- The **damage layer** (the project's core goal — a deterministic formula depending on attributes
  AND equipment of both sides) is **READY NOW**, no new reverse-engineering required.
- **Four systems are blocked** on surfaces still in DEFER or unbuilt: weapon-skill, facing/reach/
  positional, magic, statuses/reactions.
- The DCL was, by design, written without checking implementability. This pass finds **two hard
  feasibility facts the design assumed away** (preview-number virtualization; action identity is
  inferred, not real).

## What is PROVEN / READY (levers + data we already have)

Two runtime control levers, both live-proven:
- **Pre-clamp staged-damage write** (`record+0x1C4`, hook RVA `0x30A66F`): rewrites the staged debit
  before vanilla applies HP, so vanilla owns HP-clamp + KO + UI in the **same hit**. Formula-owned
  lethal KO proven. This is the damage lever.
- **Evade-type / result-selector control** (`record+0x1C0`, selector RVA `0x205210`): forces hit vs
  which evade animation (0x00 hit / 0x01 cloak / 0x02 weapon-parry / 0x03 shield / 0x04 class-evade /
  0x06 miss), renders cleanly regardless of the unit's real equipment. This is the hit/defense
  **outcome** lever.

Formula context (live, both sides, proven): attacker+target stats, raw PA/MA/Speed, Brave/Faith
(+max), zodiac, job, gender/monster, equipment ids (`+0x1A..0x26`) joined to the static item catalog
(weaponPower, weaponParry, shield phys/mag parry, physEva, armorHpBonus, category/type flags), the
job growth/mult block, and a full arithmetic/bit/dice/table DSL. Architecture proven end-to-end:
**neuter vanilla → compute custom → write**; **death is engine-owned** (delivered only via the
pre-clamp lethal debit; writing the KO bit yields a zombie). Data layer is **~90% Tier-1**.

## Readiness by DCL system

| System (doc) | State | Gate |
|---|---|---|
| Damage model — subtractive DR, deterministic, atk+tgt+equip (`02`) | READY | none (RE). Arithmetic in the DSL; pre-clamp lever applies it |
| Damage types × armor matrix (`03`) | READY | plumbing exists (type×class response + penetration in the engine); only numbers (data) |
| Brave/Faith damage multipliers (`07/08`) | READY | mapped + exposed |
| Zodiac → element (`09`) | NEARLY | `zodiac` exposed; confirm field is the 12-value sign; element assigned by us |
| Hit/Defense — 2× 3d6 + depletion (`04`) | NEARLY | (a) validate bidirectional hit/miss+damage; (b) guard-depletion state + turn-reset signal; (c) weapon-skill numbers (need Job Level) |
| Weapon skill (`10`) | BLOCKED | **Job Level** unmapped |
| Facing / Reach / positional (`05/06`) | BLOCKED | X needs 1 confirm; Dir→facing unmapped; height/elevation unmapped; none wired |
| Magic (`11`) | BLOCKED | real action/spell id; Magic-Evade unit field; MP-write; spell element wiring |
| Statuses & Reactions (`13`) | BLOCKED | full status bitfield + proof statuses can be applied (KO is engine-owned → may force data InflictStatus); reaction-trigger hooks |

## NEW feasibility findings (the design assumed these away)

1. **The previewed/forecast number is virtualized and NOT freely writable.** The pre-clamp lever
   makes the *applied* HP exactly our number, but the on-screen **preview** shows the neutered
   placeholder, and the **forecast hit %** cannot be set arbitrarily (only the quantized evade-input
   bytes `+0x4B/0x4A/0x4E/0x46/0x47` nudge the shown number; arbitrary-% is virtualized, RE-only).
   This **collides with the DCL's "preview must equal result" + legibility principle** (`00`, `02`).
   The DCL needs a presentation answer: either drive the preview from a moddable UI surface, accept
   that the preview ≠ the shown native number, or render our own overlay. Open design item, not
   covered anywhere in the DCL docs.

2. **Action identity is a SENTINEL channel, not a real ability id.** Today `action.*` family tags
   (swing/thrust/spell/…) are **inferred from controlled vanilla damage deltas** via
   `ActionSignalRules`. The true engine ability id exists (`actor+0x142`, result `+0x1A2`) but is
   **observe-only** (`PreClampActorStructDump` / `[PRECLAMP-ACTOR-CTX]`), NOT promoted to the primary
   `DamageEvent.Action` and NOT exposed as a formula key. So "which spell/ability is this" is
   currently guesswork-from-damage — fine for basic weapon hits, useless for magic tiers, status
   riders, multi-hit, charge, AoE shape. **But it is RE-proven and observable**, so promoting it is a
   **build task, not unknown RE** — which is why it is the highest-leverage next move.

## Discovery / RE backlog (prioritized)

1. **Promote the actor-array action-id resolver** (`actor+0x142`, stride `0x548`, `actor+0x148`→unit).
   RE-proven, observe-only → wire as the primary attacker+action context AND expose the ability id as
   a formula key, with a mod-side ability→properties table (element, tier, AoE, status, strikes,
   charge). **Single biggest unlock — gates magic, statuses, multi-hit, reach abilities.**
2. **Status bitfield + apply mechanism.** Map which bits = which status across `+0x61` (only KO bit
   0x20 known) and the wider status region; then prove a status can be *applied* by write, or confirm
   it must go through the data-layer `InflictStatus` (which reshapes the `13` 3d6-contest design).
3. **Job Level** (per-job development level, the primary weapon-skill driver) — gates `10`, hence the
   hit roll, parry, and crossbow/gun damage. Only char `level` is exposed today.
4. **Magic-Evade unit field** — only shield mag-parry (`+0x4E`) is mapped; accessory/innate live
   magic-evasion offsets are unmapped. Gates the per-target magic resist (`11`).
5. **Finish geometry** — confirm X (`+0x4F`, one E-W capture), map Dir (`+0x51`)→front/side/back,
   locate height/elevation. None are read/exposed in code yet. Gates facing (`05`), reach (`06`),
   ranged terrain identities, AoE positional.
6. **Base HP** (excl. equipment) for physical status-resist — computable as `maxHp − armorHpBonus`
   (both available); confirm no separate raw-HP field is needed. Minor.

## Build / validation backlog (not RE, but prerequisite)

7. **Validate bidirectional hit/miss + damage control.** Proven: evade→evade (block→dodge, no
   damage). Still to compose the two levers in the other quadrants: force a HIT on a natural miss
   (apply our damage) and force a MISS on a natural hit (zero damage + evade animation). Base of the
   contest — must be reliable before building on it.
8. **Guard-depletion state machine** + a reliable turn-boundary reset signal (candidate `+0x1B9`
   "acted/moved", or CT-reset detection). Mod-side state, but needs a solid trigger.
9. **MP-write lever** (magic budget) — analogous to HP-write, flagged "for later", not yet built.
10. **Tier-2 hooks the design itself flags** (`12` item 7): Weight→Move/Dodge curve, the 3d6 status
    contest, the Fear "no offensive action" filter, inverse/flat reaction triggers, directed-taunt AI.
11. **Status apply-route decision** (struct-write vs data `InflictStatus`) — depends on #2; decides
    whether the 3d6-contest is mod-computed-then-applied or data-driven.
12. **Presentation answer for the preview-number gap** (finding #1).

## Authoring / calibration backlog (design data — no discovery)

Job×family grade matrix (A–F); per-job-level skill tables; per-weapon dials (wmod / type / reach /
parry / divisor); per-armor-class type-DR + Weight; spell tiers; and all `12` calibration constants
(G, G_m, pen_floor, PA→ST offset, DR-scaling curve, Faith/Zodiac/Shell bands, MP pool/trickle,
base(MA) curve). Volume, not discovery.

## Strategic recommendation

1. **Decide the track first.** The DCL is, by its own front matter, a **separate, uncommitted** track
   parallel to the *validated* v0.2 `formula-balance` work, and the two **disagree at the base**
   (DCL = subtractive DR; v0.2 = multiplicative C-bounded). The BLOCKED systems above only justify
   their RE cost if the DCL is the intended ship target. Resolve: DCL ships, or v0.2 ships and DCL
   stays exploratory?
2. **Build inward-out — damage-model vertical slice first.** READY now, zero new RE, proves the core
   thesis end-to-end (deterministic subtractive DR + type matchup + Brave, depending on
   attacker+target+equipment) on proven levers. Delivers value before any discovery.
3. **Then the hit/defense contest** (validate the bidirectional lever + build depletion) — close now
   that `+0x1C0` is proven.
4. **Then magic / statuses / positional**, each behind its own RE push, led by **#1 (promote the
   action-id resolver)** which unblocks magic and statuses together.

## Open decision for Marcelo

Is the DCL the **implementation target**, or still **exploratory** beside the validated v0.2 track?
Everything in the BLOCKED rows and the discovery backlog is conditional on that answer.
