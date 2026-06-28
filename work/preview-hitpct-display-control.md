# Preview hit-% display control — status + re-test checklist (2026-06-27)

## What's proven (autonomous, this session)
- Displayed attack hit-% lives at static buffer **`0x1407832C0`** (RVA `0x7832C0`) + 3 heap mirrors.
  Located via differential scan `work/mem_scan.py` (find 3 → filter 77 → filter 82: 462950→15→4).
- External **read AND write both work** (Denuvo virtualizes code, not data). One-shot write stuck
  (82→7) while cursor static; UI is retained-mode so the text didn't refresh without a redraw.
- Real-code writer found (`work/disasm_hitpct.py`): `0x227FFA movzx eax,[rbp+0x2C]` (computed %) →
  `0x228004 mov word[0x7832C0],ax` (copy to display). REAL code → hookable.

## What's deployed (ready for re-test)
- New mod DLL + profile `battle-runtime-settings.preview-hitpct-test.json` deployed to
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\` (old settings auto-backed-up).
- Hook at `0x227FFE` (ExecuteFirst) forces **AX=7** before the engine's own store → engine writes 7
  to `0x7832C0` at copy time → renderer draws 7. Deterministic, no race. Purely visual.

## USER re-test steps (when back at PC)
1. Launch the game via the **Reloaded GUI Launch button** (mod `fftivc.generic.chronicle.codemod`
   enabled, data mod OFF — unchanged).
2. Enter any battle, pick Attack, hover **several different targets**.
3. EXPECT: every target shows **hit chance = 7%** regardless of the real odds. Report what you see.
   (The attack still rolls its true odds — only the forecast number is painted.)

## CLAUDE verification (no screen needed; run while a preview is open)
- `python work/read_hitpct_hook.py`  → reads battleprobe_log.txt for the hook buffer addr, then
  prints fire count / last natural % / forced / site, plus `0x1407832C0` and the natural source.
  - fire count > 0  → hook is firing for the preview.
  - `0x1407832C0` == 7  → engine wrote our value (display is ours).
  - last natural %  → the true computed % (for sanity vs an unhooked expectation).

## If it does NOT show 7
- Buffer fire count 0 → `0x227FFE` isn't the attack-preview writer; switch `PreviewHitPctRva` to a
  cluster site (0x2C7F98 / 0x2C8C16 / 0x2C8E70 / 0x2C9806, expected `66 89 05 .. .. .. ..`) and
  rebuild. (Hooking the store directly needs RIP-relative relocation; prefer the preceding non-RIP
  instruction at each.)
- Crash → revert with `codemod\build-deploy.ps1 -RuntimeSettings work\battle-runtime-settings.clean-observe.json`.

## Revert to clean
`codemod\build-deploy.ps1 -RuntimeSettings work\battle-runtime-settings.clean-observe.json` (close game first).
