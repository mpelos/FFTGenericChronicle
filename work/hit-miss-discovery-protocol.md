# Hit/Miss Discovery — live capture protocol

Status: ready to run · Date: 2026-06-25 · Profile: `work/battle-runtime-settings.hit-miss-discovery-probe.json`

## Question
Where does the engine store the **attack result** (HIT vs MISS/evaded) that its non-virtualized
code reads to branch between "apply damage" and "show MISS"? The hit/evade *calc* is Denuvo-
virtualized (we can't hook the roll), but — like staged damage — its **outcome must land in normal
memory**, so a full-struct diff of a HIT vs a MISS on the same target should expose it. Finding this
field is the one true blocker for the Deep Combat Layer's two-roll (hit, then defend) core.

## Why this works
The probe polls every unit ~20ms and logs `[DIFF]` of the whole struct (0x00..0x1FF) vs the previous
poll. A HIT fires an HP event (+0x30 changes) plus any result/anim state; a MISS fires **no** HP
event but still changes a result/anim marker. The byte(s) that distinguish HIT from MISS — and are
not just position/CT/turn noise — are the target. Attacker + action are auto-labelled.

## Setup
1. Claude deploys the profile (copies it to the Reloaded mod dir as `battle-runtime-settings.json`)
   and confirms the mod hot-reloaded it. **Nothing is rewritten — observe-only.**
2. Pick **one attacker** and **one evasive target**, and keep them fixed for the whole capture.
   - Good evasive target: a high-Speed / shielded / Ninja-type unit, attacked **from the front**
     (front has the most evade, so misses are frequent). The on-screen hit% should be well under
     100% (ideally ~50–75%) so both outcomes happen.
   - Use a plain **basic Attack** (simplest action; no element/status to muddy the diff).

## Action (repeat until you have ≥2 HITs and ≥2 MISSes)
For each attempt, same attacker → same target → same facing:
1. Select Attack, hover the target, and **note the shown hit%** before confirming.
2. Confirm. Watch the result: **HIT** (a damage number appears) or **MISS** (the "Miss" popup, no
   damage).
3. Say the outcome out loud / jot it: e.g. "attempt 3: HIT 42" or "attempt 4: MISS".

Aim for at least **2 clean HITs and 2 clean MISSes**. More is better; the more hit/miss pairs that
differ *only* by the roll, the cleaner the isolation.

### Optional secondary (cheap, do if convenient)
Before confirming an attack, hover **two different targets** with clearly different hit% (e.g. a
squishy ~95% vs an evasive ~60%). If a forecast field stores the previewed hit%, this lets me locate
the **hit-chance** storage too (useful for overriding hit chance directly later).

## Report back to Claude
- Attacker name, target name, facing (front/side/back).
- An ordered list of attempts with **shown hit% → outcome (HIT n / MISS)**.
- The battleprobe log file (the `battleprobe_log*.txt` the mod writes).

## Pass condition
The log contains ≥2 HITs and ≥2 MISSes on the same target with full-struct `[DIFF]` coverage, so a
byte (or small field) can be shown to read one value on HIT and another on MISS, distinct from
move/CT/turn noise.

## Fail / escalation
If no **unit-struct** byte cleanly separates HIT from MISS, the result likely lives in the
**actor/action-context struct**. Pass 2 then dumps the actor struct on the action boundary of a miss
(the probe already dumps it on hits via `[PRECLAMP-ACTOR-DUMP]`); Claude adjusts the profile and we
recapture.

## Decision unlocked
Confirms whether the DCL hit/miss outcome is overridable via the proven pre-clamp/staged-result
pattern (override the flag + force/zero the staged damage). If yes → we can build the two-roll
contest. If the field is write-protected/virtualized → fall back to the data-layer MVP (force vanilla
accuracy ~100%, compute our hit roll in the pre-clamp hook, render a miss as 0 damage; proper Miss
visuals become a later polish item).
