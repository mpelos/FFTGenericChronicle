# LT12 — formula-driven forecast hit-percent parity

## Question

Can the forecast display the authored DCL hit percentage from the same calc-entry decision that
drives execution, without a managed callback on the UI path?

## Offline model

- The copy site is `0x227FFE`, between `movzx eax, word [rbp+0x2C]` and the renderer-buffer store at
  `0x228004`.
- The forecast object is the target battle-unit record at `target+0x1BE`.
- Calc-entry already computes and caches `DclHitDecision(Hit, Pct, Roll)` per target.
- A native buffer now reserves one signed int per target. Calc-entry writes the clamped authored
  `Pct`; failures clear that target slot to `-1`.
- The copy hook derives `(rbp - (battleUnitTable+0x1BE)) / 0x200`, validates range and alignment,
  and replaces the saved `AX` only when the slot is nonnegative. Invalid pointers and missing slots
  preserve the natural forecast value.
- The percentage mirror intentionally survives decision-cache consumption: forecast rendering can
  occur after compute/result consumers retire the action decision. A later calc-entry refreshes the
  same target slot before its next forecast copy.

## Safety gates

- `DclPreviewHitPctEnabled` requires `DclHitControlEnabled`.
- Dynamic DCL preview and a static non-log-only `PreviewHitPctForcedValue` are rejected as a hook
  ownership conflict.
- Hook activation still validates the existing expected bytes `41 BA 02 00 00 00`.
- No managed callback runs at `0x227FFE`; the hook touches only saved `RAX`, saved `RCX`, flags, the
  diagnostic buffer, and the mirrored percentage slot.

## Offline evidence

- `codemod/run-offline-checks.ps1 -SkipGitDiffCheck`: PASS, including C# build with zero warnings and
  errors, smoke tests, settings validation, and tooling checks.
- The exact dynamic asm sequence was assembled with the installed Reloaded `Reloaded.Assembler` /
  FASM runtime: PASS, 129 bytes.
- `git diff --check`: PASS before documentation/profile creation; rerun before live deployment.

## Minimal live gate

Use `1783941290-battle-runtime-settings.lt12-dcl-preview-hitpct.json`.

1. Open a basic Attack forecast against the same Manual Save 05 target used by LT11.
2. PASS forecast: the panel shows `50%`, while the action log records `pct=50 roll=90 outcome=miss`.
3. Execute once. PASS outcome: clean miss, no HP/MP loss, no Counter; this confirms the preview-only
   addition did not disturb the already-proven LT11 delivery stack.
4. Confirm install log contains `[PREVIEW-HITPCT-HOOK] ... dcl=1` and no `-SKIP`/`-FAILED` line.

Until this gate passes, the formula bridge is **Strong (offline)**, not Proven live.
