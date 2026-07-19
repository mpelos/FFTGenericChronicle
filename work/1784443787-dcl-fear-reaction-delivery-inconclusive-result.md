# DCL Fear Reaction delivery: first live attempt

## Outcome

The run is an instrumentation-valid negative control, but it does not decide whether a unit under
DCL-owned Fear can deliver its equipped Reaction. Fear was successfully applied to Josephine, yet
no hostile action targeted her before the run ended.

## Evidence

- Fervor `53` resolved from Josephine (`sourceIdx=17`) onto Josephine (`targetIdx=17`).
- The Fear target planner recorded `state=0x2A`, `owned=0`, `affected=1`, `opposing=0`, and
  `decision=allow`.
- The status producer wrote one result and Josephine gained the effective Chicken carrier at
  `unit+0x63: 00 -> 04`.
- Kleobis used Choco Beak `265` on Arthur (`targetIdx=16`), not Josephine.
- Arthur's Auto-Potion `441` traversed commit, preselection, final validation, materialization, and
  effect. This proves the Reaction instrumentation was operational during the run.
- There is no `reactionId=451`, `reactorIdx=17`, or Fear-owned Reaction transaction in the frozen
  log.

## Frozen artifact

- Log: `1784443594-dcl-fear-reaction-delivery-inconclusive-live.log`
- SHA-256: `02E14C2A12DEC88A50AD7BBE0A4846C4EFB6AF65B2CBE0024831C2444BE38C7F`

## Next falsifier

Use Josephine's Fervor on Arthur instead of Josephine. Arthur is already the observed hostile AI
target and has Auto-Potion equipped, so the same Choco Beak route can exercise a Reaction while
Arthur is DCL-owned by Fear. A preexisting Chicken carrier does not count as ownership; Fervor must
resolve during the current process before the hostile hit.
