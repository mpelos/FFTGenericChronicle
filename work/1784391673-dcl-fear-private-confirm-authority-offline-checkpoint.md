# DCL Fear private-confirm authority offline checkpoint

## Input evidence

The single-target live row returned private list `[16]` for Josephine slot `17` attacking Arthur
slot `16`, while the actor-owned list stayed empty. Execution affected Arthur and visually applied
Chicken without progressive slowdown. The exact pre-action autosave is
`work/1784390906-fft-autoenhanced-snapshot.png`, SHA-256
`A3D96C1118088D195FBD6863ECFA9B70DDE1D5188583F68D4510512BCB572204`.

## Offline implementation

Player confirmation no longer consumes the default/stale calculation-entry forecast cache. The
managed callback now:

1. resolves the caster from the current actor's linked battle-unit pointer;
2. reads action type/id from caster `unit+0x1A0`;
3. requires the resolved caster index to equal the live turn owner;
4. validates every non-`0xFF` private target against the live unit table;
5. derives DCL Fear ownership from the duration-owned Chicken carrier;
6. assesses the complete private list for opposing targets;
7. fails open if any caster/list/target identity is unreadable.

`DclFearPlayerConfirmEnforcementEnabled` is a separate default-off arm. Validation rejects that arm
while `DclFearLogOnly` is true or the forced-flee transaction is unavailable. The current build
therefore observes authoritative single-target decisions without blocking player input.

The obsolete `DclFearForecastDecision`, `_dclFearForecastDecision`, and `_dclFearForecastGate` paths
are removed. `tools/analyze_dcl_fear_preconfirm.py` enforces both their absence and the new private
authority source contract.

## Offline gates

- release build: PASS, zero warnings/errors;
- codemod smoke tests: PASS;
- pre-confirm executable/source analyzer: PASS;
- complete `codemod/run-offline-checks.ps1`: PASS in 117 seconds.

## Remaining falsifiers

1. Restore the exact pre-action autosave and select Josephine **Black Magicks > Fire** on adjacent
   Arthur. Fire is ability `16`, a native AoE and the first menu entry.
2. Require the forecast-visible affected set to match `expandedTargets` exactly. With Josephine and
   Arthur both inside the area, the expected set is their two battle slots; do not assume two targets
   unless the forecast visibly names/highlights both.
3. Keep enforcement disabled for that capture.
4. Only after AoE completeness passes, arm player-confirm enforcement and test one DCL-Fear-owned
   unit attempting an opposing target, plus one self/ally allow control.

No additional live test starts while Codex-owned `taskkill.exe` processes remain active.
