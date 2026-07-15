# Current executable migration and DCL status mechanism — offline checkpoint

## Executable compatibility

Steam build `23901820` supplies `FFT_enhanced.exe` SHA-256 `841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`. The update shifted most real-code RVAs by `-0x78` or `-0x98`; the result selector and result-kind store remained fixed. Runtime defaults, diagnostics, static-pattern expectations, and profile-audit compatibility now account for the current layout.

`tools/audit_runtime_hook_anchors.py` verifies the installed executable against every active runtime hook guard and emits a timestamped `work/` report. The current audit passes all 18 anchors. The independent static pattern scanner and runtime-profile tests also pass.

## Status implementation

The runtime now supports exact per-ability `DclStatusRules` in the successful DCL apply window. Each rule owns one native status bit and declares add/remove, an optional condition, a formula-produced 3d6 resistance target, and an optional target-turn duration.

Add flow:

1. match exact ability and optional action type;
2. reject equipment immunity;
3. evaluate the resistance target;
4. roll 3d6, where `roll <= resistance` resists;
5. on failure to resist, OR the bit into durable master and effective arrays;
6. optionally register duration ownership if the bit was not already present.

Remove flow clears durable master and clears effective while restoring any source/equipment bit. A native cure observed before expiry retires the duration record. Unit-pointer reuse is guarded by character id.

Duration is expressed in completed turns belonging to the target. A falling edge of `unit+0x1B8` spends one turn. If application occurs during the target's already-active turn, that first falling edge is skipped; the duration begins with the target's next turn. Expiry clears only the authored master bit and preserves the source bit in effective state.

## Offline gates passed

- Release build: zero warnings, zero errors.
- Formula/runtime smoke tests: passed.
- Exact 3d6 distribution checks: passed (`R=3` = 1/216, `R=10` = 50%, `R=18` = 100%).
- Duration transition checks: inactive application, active application skip, multi-turn decrement, final expiry passed.
- Settings validation rejects wildcard abilities, byte-zero lifecycle states, multi-bit masks, unsupported operations, missing resistance formulas, invalid duration ranges, and duration on remove rules.
- Current executable hook audit: 18/18 passed.

## Remaining live boundary

The apply-window write timing and the `+0x1B8` duration semantics still require in-game proof. LT14 uses basic Attack to isolate the write from native status riders, then forces both sides of the 3d6 contest. Status-only abilities and forecast status probability remain separate validation slices after the basic mechanism passes.
