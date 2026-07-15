# DCL Bequeath Bacon / Crystal offline checkpoint

## Question

The status manifest left Bequeath Bacon's Crystal rider as the only unresolved status-byte-0
lifecycle mechanism. The offline question was whether DCL needs to reproduce it or should preserve a
complete native special.

## Current-build evidence

- Executable SHA-256: `841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`.
- The formula dispatch table at RVA `0x682BC8` maps formula `0x57` to RVA `0x30905C` in this build.
  The historical `0x3090F4` mapping is stale for the current executable.
- The handler reads target level at `unit+0x29`, rejects level `99`, and stages native flag `0x80`
  at `result+0x12`.
- The state-apply tail interprets that flag as level +1, caps at `99`, writes `unit+0x29`, and enters
  native status processing.
- Ability id `314` is the only formula-`0x57` route. Its data describes `+Lvl(1)` and Crystal on the
  caster; the handler redirects native actor/result globals before its protected finalizer.
- The state tail tests `unit+0x61 & 0x60`, grouping Crystal and KO as lifecycle gates.
- `tools/analyze_bequeath_crystal_disasm.py` makes the dispatch and 13 byte anchors reproducible;
  `work/1783987217-bequeath-crystal-disassembly-analysis.md` passes all anchors.

## Decision

Bequeath Bacon is a preserved native special, not a missing DCL runtime mechanism. DCL design keeps
FFT's native KO-to-crystal/treasure lifecycle and does not prescribe a replacement for Bequeath
Bacon. Formula `0x57`, its bounded target level gain, and its caster Crystal/campaign transition stay
unchanged.

The generic status path still rejects Crystal, and the lethal-debit instant-KO path still excludes
it. No DCL code writes or clears the Crystal bit.

## Coverage change

- Ability id `314`: `native-special-preserved` / `preserve_native_bequeath`.
- Crystal status row: `native-lifecycle-preserved` / `preserve_native_bequeath`.
- Status manifest: `195` surface-ready, `5` native-lifecycle-preserved, `9`
  data-authoring-required, `44` authoring-required, `39` design-decision-required, `2`
  campaign-mechanism-required; no lifecycle-mechanism-required row remains.

## Live consequence

No live test is necessary to implement the current DCL ownership boundary because the mod preserves
this path unchanged. Bequeath Bacon becomes a live regression gate only if formula `0x57`, its status
data, or the native lifecycle tail is later modified.
