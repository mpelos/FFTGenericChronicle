# DCL Approach and Taunt checkpoint

## Scope

Checkpoint the completed Approach vertical slice and the job-free Taunt fallback before continuing
the remaining Fear/control-status investigation.

## Approach / native carrier 442

The complete live proof is isolated at movement event `15` in
`1784362189-dcl-approach-coordinate-mark-loan-live.log` and machine-gated by
`tools/analyze_dcl_approach_live.py`.

- Janus enters terminal route tile `5,3,0` from `4,3,0`, within configured reach `1-2` of Rion.
- The coordinator stages exactly one candidate (`mask=0x10000`) through native queue pass 2.
- Native delivery carrier `442` passes typed-family and final validation, materializes, and emits two
  state-`0x2C` effects for Rion's Dual Wield counter.
- The borrowed target mark and battle-unit coordinate tuple restore byte-exactly.
- The owned continuation changes native state `0x28` to movement state `0x11` once, passes its write
  audit, and releases the paused route.
- This event belongs to the later dedicated live fixture. It did not occur in the earlier battle in
  which Rion died.

Durable result: `1784362256-dcl-approach-coordinate-mark-loan-result.md`.

## Taunt fallback

The job-free technical fallback is part of the canonical unified sentinel profile:

- existing native Berserk action `241` is the carrier;
- the status write uses byte `2`, mask `0x08`, operation `add`;
- resistance uses `clamp(18 - target.brave / 10, 3, 18)`, so higher Brave is more vulnerable;
- duration is one target turn;
- no job or final ability assignment is encoded.

The canonical profile hash after composition is
`25E4329630FCD44B453C8DFB0C556981AA0B06D6666B3C6CE918A34FEE79F0B3`.
`tools/analyze_dcl_taunt_fallback.py` verifies both the runtime rule and the existing ability-catalog
row. `tools/validate_dcl_runtime_data_pair.py` verifies the settings/data pair.

Static executable analysis also anchors the native forced-control dispatcher at RVA `0x38BBFC`:
Chicken, Confusion, Charm, and Berserk are read from their effective status bytes, and Berserk enters
the native forced-target/planning branch. The raw route is documented in
`1784364118-dcl-forced-control-dispatch-analysis.md` and gated by
`tools/analyze_dcl_control_status_dispatch.py`.

## Offline verification

`powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1`

Result: PASS, exit code `0`, wall time `458.2 s`. The run covers syntax/tooling gates, Approach live
evidence parsing, Taunt fallback validation, reaction/runtime smoke tests, unified composition and
runtime/data-pair validation, forced-control dispatcher anchors, and whitespace checks.

## Remaining Fear boundary

The native Chicken branch proves a flee-control entry point, but it does not yet prove the DCL Fear
contract. Offline investigation must still establish whether that branch consumes the full action
phase and where a voluntary-target filter can forbid enemies while preserving self, ally, item, and
defensive actions. Reactions must remain on the normal reaction path. These are separate gates and
must not be inferred from the presence of Chicken alone.
