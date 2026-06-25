# Combat Redesign Research Roadmap

Status: active compass for runtime investigation
Date: 2026-06-23

This document defines what the Generic Chronicle combat-system redesign needs from the live
runtime, what is already proven, what is still unknown, and which tests should close each gap.

The goal is to avoid testing whatever is currently convenient. A live test is valuable only if it
answers one of the gates below.

## North Star

Generic Chronicle should not be a simple damage-number patch. The target architecture is a custom
combat system where the mod can:

1. observe a battle result as it happens;
2. identify the true causal action, not just a likely recent actor;
3. build a formula context from attacker, target, action, equipment, status, and positional state;
4. compute custom damage, healing, MP, or effect outcomes;
5. apply those outcomes through a path that keeps the engine's battle state coherent;
6. log enough evidence that every custom outcome can be audited after the fact.

The expected runtime contract is:

```text
engine action/result
  -> resolved combat event
  -> action context
  -> unit/equipment/status context
  -> Generic Chronicle formula
  -> engine-safe application
  -> audit log
```

The current runtime can already do parts of this. The roadmap below identifies which parts must be
made reliable before the combat redesign can be trusted.

## Non-Negotiable Requirements

### R1. Causal action context

For every combat event, the runtime must know who caused it and which action caused it.

Required fields:

- attacker/caster unit pointer;
- target unit pointer;
- action id / ability id / weapon action id;
- event kind: HP damage, HP healing, MP loss, MP gain, status, miss, reaction, counter, etc.;
- hit index within the resolving action;
- target list or target batch when one action affects multiple units;
- confidence/source metadata.

Design rule:

- CT-based inference is not part of the final design.
- CT may remain temporarily as a diagnostic comparison source.
- The end state should be memory/action-context resolution for normal, delayed, AoE, reaction, and
  special actions.

### R2. Formula context completeness

The formula layer must be able to read enough context to implement the intended combat model.

Minimum fields:

- attacker stats: HP/MP, PA, MA, Speed, CT, Brave, Faith, level, job/team flags;
- target stats: same core stats plus current/max HP/MP;
- attacker equipment: weapon id, weapon family, WP, range, element, special flags;
- target equipment: armor/shield/accessory ids, HP/MP bonuses, equip bonuses, element/status
  responses;
- action metadata: id, family, delivery mode, damage type, element, power, CT/charge timing, MP
  cost, AoE/multi-target behavior;
- status metadata: Protect/Shell, elemental absorb/halve/weakness, evasion, reaction support, and
  any custom GC tags;
- positional metadata if formulas need it: distance, height, facing, range path, target tile.

### R3. Engine-safe result application

The custom result must not leave the engine in an incoherent state.

Required outcomes:

- nonlethal HP damage writes;
- HP healing writes;
- MP loss/gain writes;
- lethal damage and KO;
- overkill / exactly-zero / already-dead edge cases;
- multi-hit and AoE batches;
- reaction/counter side effects;
- status application/removal if the redesign uses custom status logic.

The most important unresolved part is KO. A formula system is incomplete if custom damage can kill
but the target remains alive at 1 HP.

### R4. Data-layer safety

The data mod should keep vanilla battle results safe enough for the runtime to reconcile.

Required coverage:

- ordinary weapon attacks;
- damaging abilities/spells;
- monster skills;
- Throw;
- Jump;
- Aim/Charge;
- percent/gravity-like damage;
- healing and MP actions;
- actions with side effects.

The purpose is not to make the data layer the final formula system. It is to prevent vanilla from
performing irreversible wrong outcomes before the runtime applies GC logic.

### R5. Auditability and live-test efficiency

Every live test should produce a log that answers the test question without requiring manual
guesswork.

Each event log should include:

- final chosen attacker/action source;
- all competing diagnostic sources, including CT if enabled;
- why any candidate was rejected;
- formula inputs used by the selected rule;
- vanilla observed result;
- custom computed result;
- whether the runtime wrote, dry-ran, or skipped;
- post-write verification;
- batch id / hit index for multi-target or multi-hit events.

## Current Proven Ground

### P1. Data magnitude control exists

The executable reads `OverrideAbilityActionData` formula parameters such as `X`/`Y` for damage
magnitude. The data layer can shrink many vanilla damaging actions into safe placeholder results.

Implication:

- data-only fallback is viable for some tuning;
- runtime reconciliation can rely on a safer vanilla signal for many actions.

Remaining risk:

- Throw, Jump, Aim/Charge, percent/gravity-like damage, and unusual monster/special actions still
  need spot-checks.

### P2. Runtime formula engine exists

The code mod can route observed HP events through JSON-configured formulas. Formula evaluation can
read target state, optional attacker state, action variables, constants, tables, maps, derived
variables, and event metadata.

Implication:

- the formula layer is not the bottleneck for simple custom damage;
- the investigation should now prioritize context correctness and engine-safe application.

### P3. Nonlethal HP rewrite works

The runtime can observe vanilla HP deltas and write corrected nonlethal HP values.

Implication:

- custom nonlethal damage is technically viable now.

Remaining risk:

- the final application path for lethal results is not solved.

### P4. Direct zombie KO path is refuted

Direct HP=0 or simple KO-flag writes do not produce a coherent death state. They can create
zombie-like units.

Implication:

- KO must be solved by either a pre-damage hook / engine-owned damage path, or by finding and
  invoking/replicating the engine's actual KO routine.

### P5. Braver action context is proven

Cloud Braver has been live-tested as a delayed single-target action. The pending-action fields on
the caster and target-side forecast/damage fields were observed and used to understand delayed
action ownership.

Known useful fields:

- caster pending flags/timer/action id around `+0x61`, `+0x18D`, `+0x1A2`, `+0x1EF`;
- target forecast/pending damage around `+0x1C4`, `+0x1D8`, `+0x1E5`.

Important caveat:

- target-local forecast fields can clear before resolution, especially when the target becomes
  active before the delayed action resolves.

### P6. Cross Slash pending action context is proven

Cloud Cross Slash AoE has been live-tested. The `PendingActionTracker` can track Cloud/Cross Slash
from pending state into resolution and match the resulting HP events to the correct caster/action.

Important result:

- both Ninja and Agrias damage events resolved to Cloud/Cross Slash with
  `source=pending-clear`, `action.id=258`, and `confidence=damage-cache`;
- CT fallback picked the wrong source during the same event, proving CT is unsafe for delayed AoE.

### P7. Pending context reaches JSON formulas

The dry-run profile proved that formulas can read:

- `attacker.sourcePending`;
- `action.sourcePending`;
- `action.id`;
- `action.batchEvent`;
- `action.damageCacheMatch`;
- observed HP loss.

### P8. Pre-clamp staged-damage injection can produce real KO

The `ko-preclamp-force-agrias` live proof passed after recalibrating the expected Cross Slash
staged debit from `187` to the actual current-baseline value `115`.

Proven result:

- Cloud Cross Slash on Agrias previewed `115`.
- The pre-clamp hook at `0x30A66F` matched Agrias (`id=0x1E`) with `oldDebit=115` and
  `oldCredit=0`.
- The hook forced `unit+0x1C4=9999` before vanilla HP-apply consumed the staged debit.
- Vanilla HP-apply computed a negative raw HP result, clamped HP to `0`, and produced a coherent
  KO/lifecycle diff including `+0x61` KO status.
- The user observed Agrias die; UI showed `999` damage.
- The same AoE action still hit Ninja normally for `273`, proving the target guard did not corrupt
  the whole batch.

Implication:

- The desired engine-safe lethal path is viable: feed custom staged debit/credit before vanilla
  HP apply, then let the engine own HP clamp, KO, Reraise, and turn flow.
- Late direct HP writes should become fallback/legacy behavior, not the primary final design.

Remaining risk:

- This is a controlled single-target force inside one AoE action. It must be generalized to formula
  computed values and tested across immediate attacks, spells/charge actions, healing, and multi-hit
  cases.
- UI display behavior may clamp or otherwise transform large staged values (`9999` displayed as
  `999`), so preview/display policy remains a separate player-facing question.

Cross Slash dry-run correctly computed:

- Ninja `273 -> 283`;
- Agrias `187 -> 197`;

without writing HP.

Implication:

- delayed action context now reaches the formula layer.
- the next Cross Slash dry-run repetition has low research value unless it tests a new gate.

### P8. Vanilla KO state transition has been captured

The KO/pre-damage observe-only probe captured a real vanilla KO after Cross Slash left the Ninja at
low HP and Ramza used Rush.

Observed KO event:

- preview/action cache raw formula damage: `50`;
- target previous HP: `15`;
- engine-observed applied HP loss: `15 -> 0 = 15`;
- lethal classification in the old log: `lethal=1`, `overkill=0` for applied HP loss;
- raw formula overkill after clarification: `50 - 15 = 35`;
- death-state diff happened on the same HP-zero frame.

Known unit-local death diff:

- `+0x30:0F->00`;
- `+0x61:00->20`;
- `+0x63:21->20`;
- `+0x18C:00->01`;
- `+0x1BB:00->01`;
- `+0x1DB:00->20`;
- `+0x1EF:00->20`;
- `+0x1F1:01->00`;
- `+0x1F5:FF->10`.

Implication:

- real KO is a coordinated engine state transition, not just HP reaching zero;
- no delayed unit-local follow-up diff was observed in the capture window;
- preview/action cache can represent raw formula damage before HP clamping, while the HP event logs
  the applied loss capped by the target's remaining HP. Exact cache-to-HP-delta matching alone is
  not enough for lethal attribution.
- the runtime now has lethal-aware target-cache matching and a deployed immediate-action candidate
  probe, but immediate-action source/caster resolution is still open.

### P9. Immediate Rush KO boundary probe narrowed the model

The immediate-action / KO-boundary probe repeated the Cross Slash -> Rush setup with richer logging.

Confirmed:

- Cross Slash delayed AoE still resolves cleanly to Cloud/Cross Slash with two HP events.
- Nonlethal AoE events now log matching `appliedHpLoss` and `rawForecastDamage`.
- Rush KO logs separated:
  - first preview cache: `50`;
  - execution-time target cache / shown number: `33`;
  - HP-capped applied loss: `15`;
  - `hpClamp=1`;
  - `rawForecastOverkill=18`.
- The live lethal-clamp target-cache path works.
- Ramza appears near the immediate HP event with `act=147`, but stale Cloud/Cross Slash can still
  tie as a `source-like` candidate.
- Reraise after Ramza's Wait produced a revive event: Ninja `0 -> 28`, next active Ninja.

Implication:

- The probe is good enough for raw-vs-applied damage, but the immediate-action candidate score is
  not yet a resolver in this first capture.
- Initial offline stale-action suppression/current-action tie-break scoring has been implemented,
  and was later live-validated in P10.
- A dedicated offline analyzer now reconstructs the Rush boundary and re-ranks candidates from the
  captured snapshot.
- The `50 -> 33 -> applied 15` transition is a useful guide for finding the pre-commit damage
  boundary.
- Reraise/revive should be modeled as an engine-owned state transition, separate from ordinary
  healing attribution.

### P10. Action-boundary live validation confirmed the Rush KO model

The follow-up `[ACTION-BOUNDARY]` profile was deployed and run through the same controlled path:
Cloud Cross Slash left the Ninja at low HP, then Ramza used Rush to kill him.

Confirmed:

- Cross Slash still resolved through pending action context:
  - Ninja `288 -> 15 = 273`, `rawForecastDamage=273`;
  - Agrias `470 -> 283 = 187`, `rawForecastDamage=187`;
  - source `Cloud`, `act=258`.
- Rush preview/cache entered as `dmg1C4=50/chg1D8=130/f1E5=128`.
- Ramza then appeared as the fresh immediate actor:
  - `act=147`;
  - `ba=1`;
  - `actionIdAgeMs=1179`;
  - `activeActionAgeMs=1179`;
  - `freshAct=1/freshActive=1`;
  - score `2150`.
- The stale Cloud action holder was demoted:
  - `act=258`;
  - `actionIdAgeMs=434376`;
  - `activeActionAgeMs=434376`;
  - `staleAct=1/staleActive=1`;
  - score `-250`.
- The Rush target cache changed from preview/raw `50` to execution/raw `33`.
- The game-visible/raw Rush value is `33`; the applied HP loss is `15` because the Ninja only had
  `15` HP remaining.
- HP zero and KO/death fields landed together:
  `+0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10`.
- Reraise revived the Ninja with an engine-owned multi-field transition:
  `0 -> 28`, `f1E5=72`, `b8=1`, `bb=2`.

Implication:

- For the covered immediate Rush case, memory/action freshness is now a better attribution source
  than CT fallback.
- The next blocker is no longer "can we see the actor?" for this path; it is finding where to
  intervene before or inside the engine-owned damage/KO commit.
- The static/offline search should center on the very short `dmg1C4=33 -> HP zero / KO flags`
  boundary.

### P11. Static KO-field targets now exist

The offline/static pass used the action-boundary Rush KO log as the seed and scanned the local
`FFT_enhanced.exe` install.

Artifacts:

- `work\static_code_pattern_scan.local.md`;
- `work\ko_boundary_static_target_analysis.md`;
- `tools\analyze_ko_boundary_static_targets.py`.

Confirmed:

- The local executable still matches known anchors:
  - `battle_base_ptr` at `0x226D98`;
  - `damage_mult_2` at `0x30A685`.
- The old direct static `damage_multiplier` pattern remains absent, so the direct damage write is
  not solved by that AOB.
- The live hook snapshot around Rush KO gives action context, not the exact HP write:
  - `rcx`/`rdi`/`hookPtr` identify Ramza;
  - `targetPtr` identifies the Ninja.
- The live stack addresses are stable static landmarks:
  - `0x2F2EC1`;
  - `0x2F37A2`;
  - `0x2F3884`.
- The highest-value static KO/death-state field leads near `damage_mult_2` are:
  - `0x30A6D3`: probable `rdi+0x1F5` write;
  - `0x30A908`: probable `rdi+0x61` write;
  - `0x30A912`: probable `rdi+0x1EF` write;
  - `0x30AAFC`: probable `rax+0x1BB` write;
  - `0x30D42A` / `0x30D433`: probable `rdi+0x1EF` read/write path;
  - `0x30D43C`: probable `rdi+0x61` write;
  - `0x2D7AC0` / `0x2D7AEC`: probable target-cache `+0x1C4` writes.

Implication:

- The next KO work can be targeted. The current problem is no longer "find anything near damage";
  it is "prove which of these candidate RVAs participates in the engine-owned KO commit and whether
  a pre-commit value can be altered before that commit."
- The deployed probe now labels module addresses and these landmarks in hook-register/stack logs,
  so the next live run should be easier to interpret.
- A targeted `ko-landmark-probe` profile is now deployed and hooks the byte-verified starts
  `0x2D7AC0`, `0x2D7AEC`, `0x30A6D3`, `0x30A908`, `0x30A912`, `0x30AAFC`,
  `0x30D42A`, `0x30D433`, and `0x30D43C`.

### P12. Executing-action context found via battle actor array

Live-proven (one battle, 2026-06-24): the engine exposes a per-participant battle actor array,
contiguous with stride `0x548`, each actor linking to its unit struct at `+0x148`. At the native
pre-clamp damage frame the resolving caster's actor struct is present on the stack alongside the
current target's actor struct, and the resolving action id is stored inside the caster actor at
`+0x142` (also `0x17A/0x18C/0x1BC`).

Damage-time memory-only context:

```text
target   = pre-clamp unit pointer (per HP event)
caster   = stack actor whose +0x148 != target      (charged AND immediate)
actionId = caster_actor + 0x142                     (258 Cross Slash, 257 Braver, 0 basic)
```

Validated across action families (Cross Slash, Braver, basic attack). This is the strongest path to
retiring CT (U2) and a robust delayed/overlapping resolver (U4).

Update (2026-06-24): an observe-only memory-only resolver (`[PRECLAMP-ACTOR-CTX]`) is implemented and
validated head-to-head for Cross Slash AoE - it resolved both hits to `caster=Cloud actionId=258`
from the actor array, agreeing 100% with the pending tracker, no CT; and returned `no-caster-actor`
for credit/tick events. Remaining: overlapping pending casters, counters, immediate-basic head-to-head,
RVA stability across a different battle, then promote to primary (gated on oldDebit>0).

Implication:

- attacker/action identity no longer depends on CT or the pending-clear heuristic at damage time;
- basic attacks resolve weapon identity from equipment (U5/P13): `attacker_unit+0x20`.

### P13. Equipment block located in the battle unit struct

Live-proven (2026-06-24), zero new captures. Equipped item ids are 16-bit little-endian words in
the unit struct: `+0x1A` head, `+0x1C` body, `+0x1E` accessory, `+0x20`/`+0x22` right-hand
weapon/shield, `+0x24`/`+0x26` left-hand weapon/shield. The word is the `item_catalog.csv`
`item_id`. Triple-confirmed by equip-screen ground truth across 8 units, covering dual-wield
(Ninja Iga+Koga; a unit with Excalibur+Defender), two-handed (Cloud Materia Blade Plus, only
`+0x20`), weapon+shield (Ramza Chaos Blade + Venetian Shield at `+0x26`), and a monster
(all-zero slots). Empty hand sentinel = `0x00FF`; monster = `0x0000`.

Implication:

- the formula context can now read attacker and target equipment of both sides directly;
- basic-attack weapon identity (action id 0) is solved via `attacker_unit+0x20`;
- no roster/ENTD mapping needed - equipment is self-contained in the unit struct;
- equipment is in the unit struct, not the 0x548 actor struct.

Evidence: `work/equipment-block-offsets-2026-06-24.md`; tool `tools/analyze_equipment_dumps.py`.

## Critical Unknowns

## U1. KO / lethal custom damage path

Why this matters:

- If the GC formula says a hit kills and vanilla would not kill, leaving the target at 1 HP is not
  acceptable for the redesign.
- KO is a structural requirement, not polish.

Questions to answer:

1. Is there a stable hook point before the engine commits HP damage and KO logic?
2. Can the runtime alter the damage amount before the engine evaluates death?
3. If not, can we find the engine's KO/death routine and invoke it safely?
4. What state changes happen during real KO besides HP reaching zero?
5. Are death, crystal/treasure timers, targetability, CT, animation, status, and AI state all tied
   to the same routine?

Evidence needed:

- more comparisons of vanilla nonlethal hit vs vanilla lethal hit on representative targets;
- memory diffs around HP, flags, CT, status, targetability, animation/death state;
- hook/register snapshots around the moment vanilla applies lethal damage;
- confirmation that a custom lethal result can produce the same post-KO state as vanilla.

Current evidence:

- A vanilla KO was captured for Ramza Rush on Ninja after Cross Slash left the target at `15` HP.
- The first Rush preview/action cache showed `50`, but the execution-time target cache changed to
  `33`, matching the number shown by the game.
- The HP event reported the clamped applied loss `15 -> 0 = 15`, with
  `rawForecastDamage=33`, `hpClamp=1`, and `rawForecastOverkill=18`.
- The observed death-state transition touched multiple fields (`+0x61`, `+0x1EF`, `+0x1DB`,
  `+0x18C`, `+0x1BB`, `+0x1F1`, `+0x1F5`, etc.).
- Lethal clamping is now recognized as target-cache evidence.
- The action-boundary live validation exposed Ramza/Rush as the fresh actor and demoted stale
  Cloud/Cross Slash:
  Ramza `act=147`, `freshAct=1/freshActive=1`, score `2150`;
  Cloud `act=258`, `staleAct=1/staleActive=1`, score `-250`.
- The narrowed boundary is execution cache `33` to HP-zero/KO flags, about `65 ms` apart in the
  latest polling capture.
- The action-boundary probe directly logged the execution cache change (`50 -> 33`) and the KO
  commit frame. The next work should use this as a static/offline search guide.
- Reraise produced an engine-owned revive transition (`0 -> 28`) that clears many KO flags; it is
  useful evidence for death/revive state but not ordinary heal attribution.
- Static analysis now has concrete KO/death-state field candidates near `damage_mult_2`,
  especially `0x30A6D3`, `0x30A912`, `0x30D433`, and `0x30D43C`.
- The live stack RVAs `0x2F2EC1`, `0x2F37A2`, and `0x2F3884` are action-resolution caller
  landmarks; they should be used to classify/log runtime context, not assumed to be the HP write.

Success criteria:

- A custom formula result that would kill but vanilla would not kill produces a real engine KO.
- The unit is not targetable/acting as alive after KO.
- The battle continues normally.
- Logs show the selected action context, formula result, KO path, and post-KO verification.

Preferred investigation path:

1. Determine whether the `0x30A6D3` / `0x30A912` / `0x30D43C` KO-field cluster is inside the
   engine-owned KO commit path or only adjacent cleanup/state code.
2. Determine whether the `0x2D7AC0` / `0x2D7AEC` target-cache writes are reachable before the
   engine calculates HP/KO, and whether they expose a pre-commit damage value.
3. Use live tests only to validate a specific candidate hook/classifier from those RVAs.
4. Only then attempt a custom lethal write/apply test.

Do not assume:

- `HP=0` is enough;
- a single known flag is enough;
- post-damage HP reconciliation can solve KO by itself.

## U2. Full replacement of CT fallback

Update (2026-06-24): the primary question below ("where is the current executing action context
stored") now has a concrete answer at damage time. The engine keeps a per-participant battle actor
array (stride `0x548`, `actor+0x148` -> unit); at the native pre-clamp frame the resolving caster's
actor is on the stack and the resolving action id is in the caster actor at `+0x142`. So
`caster = stack actor whose +0x148 != target` and `actionId = caster_actor+0x142`, validated live for
Cross Slash (258), Braver (257), and basic (0). See P12 and `docs/modding/12-...` section 2.4. CT can
be retired for caster/action identity once this is implemented as a live resolver and validated for
overlapping pending actions and counters.

Why this matters:

- CT identifies temporal symptoms, not causality.
- The final combat system must resolve actions from memory/action context.

Questions to answer:

1. Where is the current executing action context stored during HP/MP/status application?
2. Is there a real action queue or pending action table outside unit structs?
3. Can we identify caster, action id, target list, and batch id directly from that structure?
4. Do immediate actions, delayed actions, counters, reactions, and AoE all pass through the same
   context object?
5. What memory source should become primary when pending tracker and current executing context
   disagree?

Evidence needed:

- action-context logs with `primarySource=memory/pending/...`;
- diagnostic CT result logged separately as `ctDiagnostic`;
- mismatch reports showing when CT disagrees with memory;
- test coverage across action families.

Success criteria:

- Braver and Cross Slash resolve without CT.
- Simple weapon attacks resolve without CT.
- Magic/charge resolves without CT.
- Counter/reaction resolves without CT.
- Multi-target AoE resolves one shared action batch without CT.
- CT fallback can be disabled without losing expected attribution in the covered matrix.

Preferred investigation path:

1. Keep CT as diagnostic-only in logs.
2. Add explicit coverage counters: `resolvedByMemory`, `resolvedByPending`, `resolvedByCtOnly`,
   `unresolved`.
3. Treat every `resolvedByCtOnly` case as a research bug, not as acceptable behavior.
4. Search for current executing action pointer / action queue using already-known anchors:
   caster pointer, action id, target pointer(s), target damage, charge timer, and batch timing.

## U3. Action-family coverage

Why this matters:

- Braver and Cross Slash prove the direction, not the whole combat system.
- The redesign must survive the weird parts of FFT.

Coverage matrix:

| Family | Current confidence | Needed discovery |
| --- | --- | --- |
| Braver delayed single-target | proven | keep as regression baseline |
| Cross Slash delayed AoE | proven through formula dry-run | real rewrite still separate |
| basic weapon attack | partial / older CT path | memory-only attacker/action source |
| spell charge | unknown | caster/action/target list during charge and resolution |
| summon / large AoE magic | unknown | target batch and multi-hit/multi-target semantics |
| Jump | unknown | caster disappears/returns timing, target persistence, KO path |
| Throw | unknown | action id, weapon/item identity, data neuter coverage |
| Aim/Charge | unknown | secondary power table, charge state, action id |
| counter/reaction | partial / fallback ideas | true source/action and reaction ownership |
| healing | partially supported by engine | action context and rewrite safety |
| MP damage/restoration | runtime support exists | action context and rewrite safety |
| status-only actions | unknown | event detection beyond HP/MP deltas |
| absorb/drain | unknown | split damage/heal context and order |
| percent/gravity damage | unknown | data neuter and formula replacement strategy |

Success criteria:

- Each family has one representative live test with a clean audit log.
- Each family is classified as:
  - `covered-by-current-context`;
  - `covered-with-family-specific-source`;
  - `needs-new-hook`;
  - `not-yet-supported`.

Priority order:

1. Basic weapon attack memory-only attribution.
2. Spell/charge action.
3. Counter/reaction.
4. Jump or Throw.
5. Healing/MP.
6. Status-only and exotic damage.

## U4. Target list and batch semantics

Why this matters:

- AoE formulas may need to know whether several HP events belong to one action.
- Some formulas may scale by number of targets, hit index, target relation, or splash rules.
- Multi-target disambiguation is required when several pending actions coexist.

Questions to answer:

1. Where does the engine store the target list for a pending/resolving action?
2. Can the runtime see all targets before the first HP event?
3. Does the engine resolve AoE in a deterministic order?
4. Can target misses, absorptions, zero-damage hits, and dead targets be represented in the same
   batch?
5. What happens when two pending actions are ready in close succession?

Evidence needed:

- logs showing `batchId`, `batchEvent`, `batchMaxEvents`, and all known targets;
- tests where AoE hits two or more targets;
- tests where a target is in area but takes zero/miss/absorb if possible;
- tests with multiple pending actions if the scenario can be constructed.

Success criteria:

- One action with multiple HP events is logged as one batch.
- Each target event knows its hit index.
- The runtime can reject events that do not belong to the current batch.
- Multiple pending actions do not cross-attribute damage.

## U5. Equipment and derived defense context

Update (2026-06-24): Q1 is answered. Equipped item ids live in the battle unit struct as 16-bit
words: `+0x1A` head, `+0x1C` body, `+0x1E` accessory, `+0x20`/`+0x22` right-hand weapon/shield,
`+0x24`/`+0x26` left-hand weapon/shield. Triple-confirmed by equip-screen ground truth across 8
units (incl. dual-wield, two-handed, shield, and a monster with all-zero slots), mined from
existing dumps with zero new captures. Q2 (roster mapping) is moot - equipment is self-contained in
the unit struct. See P13, `12-...` 3.1.5, and `work/equipment-block-offsets-2026-06-24.md`. Remaining:
live read-back validated at the damage frame (Ramza basic attack on Ninja, fresh session: the
runtime read both sides' full block correctly via `[PRECLAMP-EQUIP]`). Remaining: expose to formula
context (weapon/armor/family/element) and design GC DR tags (Q3-Q5).

Why this matters:

- The formula design frame assumes the runtime can read attacker/target equipment and build
  mitigation/type-response variables.
- If equipment mapping is weak, the combat model must either be simpler or rely on static data in
  a different way.

Questions to answer:

1. Where are equipped item ids stored for each live battle unit?
2. Can battle units be mapped back to persistent roster/ENTD units with equipment slots?
3. Are item ids stable across story battles, roster units, guests, monsters, and generated enemies?
4. Can we read weapon family and armor/shield/accessory reliably from live runtime context?
5. How should custom GC tags be attached: item id map, equip bonus id, item category, or new table?

Evidence needed:

- live snapshots with known equipment changes;
- static catalog joins from `ItemData`, `ItemWeaponData`, `ItemArmorData`, `ItemEquipBonusData`;
- exact offsets or robust table mapping;
- formula dry-runs that use weapon/armor context and log resolved item ids.

Success criteria:

- A formula can branch on attacker weapon family.
- A formula can branch on target armor/shield/accessory.
- Logs show item id, category/family, and GC derived tags for both sides.
- Ambiguous scans are rare or eliminated.

## U6. Preview and player-facing consistency

Why this matters:

- A custom combat system is hard to play if the UI forecast lies badly.
- This may not block early proof, but it matters for a playable redesign.

Questions to answer:

1. Can we alter preview/forecast damage to match GC formulas?
2. Is the preview computed from the same data tables we can neuter, or from another runtime path?
3. Can we at least keep vanilla preview close enough through placeholder data during development?
4. What should the UI show for formulas involving custom DR, random rolls, or conditional effects?

Evidence needed:

- preview state fields for selected actions;
- relation between preview damage and target-local `+0x1C4`;
- ability to influence preview through data or runtime writes;
- tolerance decision for early releases.

Success criteria:

- Either preview matches custom formula, or the project explicitly accepts a known mismatch for a
  given milestone.

## U7. Reactions, counters, and follow-up events

Why this matters:

- Reactions are a major FFT combat feature and can invert attacker/target relationships.
- A post-damage observer can easily misattribute a counter to the previous attacker.

Questions to answer:

1. How does the engine represent a reaction event?
2. Is there a distinct reaction action id/source?
3. Does the counter owner appear as current executing actor during HP application?
4. Can reactions chain into further reactions?
5. Should GC formulas use the original trigger context as well as the reaction context?

Evidence needed:

- controlled counter/reaction tests with both actor and target known;
- logs that distinguish `triggerAction` from `reactionAction`;
- confirmation of source/target inversion.

Success criteria:

- Counter damage is attributed to the countering unit, not the original attacker.
- The formula can read both immediate reaction source and original trigger if needed.

## U8. Non-HP event model

Why this matters:

- A full combat redesign may alter MP damage, healing, drain, status, buffs/debuffs, breaks, and
  other non-HP outcomes.

Questions to answer:

1. Are MP deltas observed and reconciled with the same reliability as HP?
2. How do healing and damage share or differ in context?
3. Can status application/removal be observed as an event?
4. Can break/equipment damage be observed safely?
5. Can drain/absorb be represented as one linked action with two result events?

Evidence needed:

- representative MP loss/gain tests;
- healing tests;
- drain/absorb tests;
- status-only tests;
- logs with linked action batch ids.

Success criteria:

- The runtime can classify and formula-route HP damage, HP healing, MP loss, and MP gain.
- Status-only and equipment-break actions are either supported or explicitly out of scope for the
  milestone.

## U9. Determinism, randomness, and replayability

Why this matters:

- GC formulas may include dice/randomness.
- Logs and tests must be reproducible enough to debug.

Questions to answer:

1. What seed should identify a combat event?
2. Is the seed stable across multi-hit or AoE event ordering?
3. Can formula randomness be deterministic per event while still feeling random in play?
4. How do dry-run, simulation, and live execution stay comparable?

Evidence needed:

- formulas using `rand`, `randAt`, `diceRoll`, or `diceRollAt`;
- live logs showing event seed and rolled values;
- simulation/live parity checks.

Success criteria:

- The same event context produces explainable random outputs.
- Logs can explain why a roll happened.

## U10. Performance and safety envelope

Why this matters:

- Polling and memory probes must not destabilize battle.
- Formula evaluation and logging must not create frame hitches.

Questions to answer:

1. What polling interval is safe for all observed units?
2. How expensive is rich logging during AoE/multi-hit actions?
3. Can probes be toggled by profile without rebuilding?
4. Can failed settings reloads or malformed formulas fail safely?

Evidence needed:

- stress logs from AoE/multi-hit scenarios;
- reload failure tests;
- performance observations during battle.

Success criteria:

- Default profiles are safe and quiet.
- Investigation profiles are explicit and reversible.
- A bad formula/settings file does not crash the game or corrupt battle state.

## Recommended Investigation Order

### Phase 0. Consolidate the compass

Purpose:

- align docs, tools, and profiles with the latest Braver/Cross Slash evidence;
- stop relying on stale architecture notes that still treat CT as acceptable.

Tasks:

1. Update canonical docs to say CT is diagnostic-only, not a final fallback.
2. Add the pending-context dry-run profile to the runtime profile audit.
3. Record Braver and Cross Slash as proven baselines.
4. Record KO as a blocking requirement for the redesign.
5. Keep this roadmap linked from the active checkpoint.

Exit criteria:

- anyone reading the repo understands that the next research blockers are KO/pre-damage and
  memory-only action coverage, not another Cross Slash attribution repeat.

### Phase 1. KO / pre-damage investigation

Purpose:

- discover an engine-safe path for custom lethal formulas.

Tests:

1. Vanilla nonlethal controlled hit.
2. Vanilla lethal controlled hit. Initial Rush KO capture is complete.
3. Compare pre-hit, HP-write, post-hit, and next-turn states.
4. Fix probe attribution for clamped lethal events. Initial lethal-aware target-cache fix is complete.
5. Add hook/register probes around suspected damage application boundaries.
6. Attempt a dry-run custom lethal calculation with no write.
7. Only after evidence, attempt custom lethal application.

Exit criteria:

- either a pre-damage modification path is found, or a reliable engine KO invocation path is found.

### Phase 2. CT retirement path

Purpose:

- make memory/action context primary across common combat events.

Tests:

1. Basic weapon attack with CT disabled or diagnostic-only.
2. Braver regression.
3. Cross Slash regression.
4. Spell/charge representative.
5. Counter/reaction representative.

Exit criteria:

- covered tests resolve attacker/action without CT.
- any CT-only case becomes an explicit open bug.

### Phase 3. Action-family breadth

Purpose:

- classify action families by context source and data-layer safety.

Tests:

1. Spell/charge.
2. Summon or larger AoE magic.
3. Jump.
4. Throw.
5. Aim/Charge.
6. Healing.
7. MP loss/gain.
8. Status-only or break-like effect.

Exit criteria:

- each family has a known source strategy or is marked unsupported for the milestone.

### Phase 4. Equipment and formula richness

Purpose:

- unlock the intended combat formula design frame.

Tests:

1. Attacker weapon id/family mapping.
2. Target armor/shield/accessory mapping.
3. Formula dry-run using attacker weapon family.
4. Formula dry-run using target armor/type response.
5. Real nonlethal rewrite using equipment-derived formula.

Exit criteria:

- GC formulas can use weapon family and target equipment reliably.

### Phase 5. Real rewrite milestones

Purpose:

- turn proven context into visible gameplay changes.

Tests:

1. Pre-clamp staged-damage lethal proof for Cross Slash.
2. Nonlethal or lethal staged-damage rewrite for basic weapon attack with memory-only context.
3. Nonlethal or lethal staged-damage rewrite for spell/charge.
4. Late HP rewrite only as fallback/legacy proof, not as the desired architecture.

Exit criteria:

- formula output changes live battle results in multiple action families.
- KO works when custom formula kills through an engine-owned state path.

### Phase 6. Preview/player-facing pass

Purpose:

- make the system playable rather than only technically correct.

Tests:

1. Determine whether preview can be altered or approximated.
2. Compare preview vs final GC formula result.
3. Decide milestone policy for mismatches.

Exit criteria:

- preview behavior is either corrected or explicitly documented as a known milestone limitation.

## Live-Test Design Rules

Every live test should state:

```text
Question:
Expected useful evidence:
Setup:
Action:
User observations needed:
Artifacts to capture:
Pass condition:
Fail condition:
Decision unlocked:
```

Avoid tests that only repeat an already-proven path.

Good next tests answer one of these:

- Can custom lethal damage produce real KO?
- Can an action resolve without CT?
- Can a new action family expose the same context model?
- Can formula context read the equipment data needed by the design?
- Can one action batch be tracked across multiple targets without cross-attribution?

## Immediate Recommendation

The next research focus should stay on generalizing engine-safe custom result application and
retiring CT as a design dependency. The active staged-damage proof has passed for a controlled
Cloud Cross Slash / Agrias KO case.

Recommended next concrete work:

1. Generalize the pre-clamp staged-damage hook from one forced Agrias proof into a runtime path that
   consumes resolved formula output.
2. Link staged-damage writes to the resolved action context:
   pending tracker / memory context -> target HP-apply event -> staged debit/credit rewrite.
3. Prove the same path for:
   - one nonlethal custom damage result;
   - one lethal custom damage result;
   - one immediate weapon/action case;
   - one delayed or charged action case;
   - one AoE/multi-target batch where only selected targets are changed.
4. Keep CT in diagnostic logs, but treat memory/pending action context as the intended source.
5. Separately investigate preview/display behavior, since `9999` staged debit displayed as `999` in
   the passing proof.

Why this is the right next test:

- The final redesign needs custom formulas to apply results through engine-safe state transitions.
- We now know the HP apply routine consumes `unit+0x1C4` / `unit+0x1C6` before vanilla clamp.
- A late write to `unit+0x30` is known to be architecturally suspect for lethal custom formulas.
- The pre-clamp proof directly tested the insertion point most likely to retire the CT/late-HP
  fallback path and become the foundation for real custom combat results; the next step is turning
  that proof into a configurable formula application pipeline.

Latest update:

- The observe-only formula candidate probe passed for delayed/AoE Cross Slash:
  - `PRECLAMP-FORMULA-CANDIDATE` appeared before the HP event;
  - Cloud/Cross Slash resolved via `pending-clear`;
  - vanilla debits were Ninja `273`, Agrias `115`;
  - formula debits were Ninja `68`, Agrias `77`.
- This confirms the managed side can prepare formula-backed plan entries in time for this action
  family.
- The native one-shot plan table consumed by the pre-clamp hook is now implemented and deployed via
  `work\battle-runtime-settings.preclamp-plan-cross-slash-demo.json`.
- The live plan-table proof passed:
  - Agrias changed from vanilla `115` to formula `77`, HP `322 -> 245`;
  - Ninja changed from vanilla `273` to formula `68`, HP `276 -> 208`;
  - both UI damage numbers and final HP used the forced formula values;
  - logs show matching `[PRECLAMP-PLAN-QUEUE]`, `[PRECLAMP-REWRITE]`, and final `[DAMAGE]` lines.
- Next live proofs should generalize this path:
  - immediate single-target attack;
  - another delayed/charged action family;
  - overlapping pending actions.
- Formula-backed lethal same-hit KO has now passed with Cloud Braver:
  - vanilla Braver `153` on Beowulf was replaced by formula `9999`;
  - UI displayed `999`;
  - Beowulf HP became `0/314`;
  - KO lifecycle included `+0x61:00->20`.
- This confirms the primary final damage architecture can be:
  `pending/action memory context -> formula -> pre-clamp staged debit/credit rewrite -> vanilla apply/KO`.
- Remaining high-value work:
  - prove immediate/basic attacks through this same route;
  - validate non-Cloud charged actions and skills without MP cost;
  - expose reliable action/equipment/DR data to formulas;
  - stress overlapping pending actions and reactions/counters.

Latest implementation update:

- Immediate/basic attacks now have an experimental pre-clamp formula-plan path, disabled by default.
- New runtime settings:
  - `PreClampFormulaCandidateAllowImmediateAction`;
  - `PreClampImmediateActionMinScore`;
  - `PreClampImmediateActionMinMargin`;
  - `PreClampImmediateActionMaxAgeMs`;
  - `PreClampImmediateActionRequireFreshActive`.
- New formula flags:
  - `a.sourceImmediate` / `attacker.sourceImmediate`;
  - `action.sourceImmediate` / `act.sourceImmediate`;
  - immediate diagnostics such as `action.freshActiveAction`, `action.actionIdAgeMs`,
    `action.activeActionAgeMs`, `action.margin`, and `action.runnerUpScore`.
- Prepared profile:
  - `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`.
- Intended next test:
  - Agrias basic-attacks Beowulf with data mod disabled and CT/recent attacker fallback disabled.
  - Expected pass result is formula damage, not vanilla, with logs proving `source=immediate-action`.
- First live attempt failed safely because basic Attack used `act=0`.
- Second live attempt proved Agrias becomes `active-source-like` with `act=0/ba=1`, but the mandatory
  freshness gate rejected her because the marker was about 29s old by HP apply.
- The next profile/version splits `currentActiveAction` from `freshActiveAction`, gates the formula on
  current `ba=1`, and treats freshness as diagnostic for the basic-attack path.
- This 7c implementation has passed build/smoke/settings validation and is waiting for a clean
  redeploy after Reloaded-II is closed.
