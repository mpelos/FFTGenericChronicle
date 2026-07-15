# DCL AI-scoring boundary checkpoint

The real-code search is exhausted for the AI amount-consumption link.

- Enemy candidate evaluation is known to traverse the VM-owned sweep at `0x281CE8` and the shared
  per-target calculation at `0x3099AC`.
- Formula dispatch occurs at `0x309F4F`; the subsequent universal finalizer at `0x30A118` is
  protected. Its scratch inputs are family-dependent and do not expose a safe universal amount ABI.
- The normalized per-target staged bundle is available at `0x281F12`; all target calculations occur
  before the sweep builds its output record.
- The current DCL pre-clamp at `0x30A5D7` is apply-only and cannot retroactively alter AI utility.
- The sweep has no decoded real-code caller. Static analysis cannot see whether the protected AI
  consumer reads the normalized bundle or an earlier aggregate.

`tools/analyze_dcl_ai_scoring_boundary.py` makes these anchors reproducible. LT35 is the minimum
indispensable live comparison: observe one controlled enemy target choice, then repeat from the same
save while Ramza's post-calc candidate bundle is changed to lethal damage. No permanent AI writer is
implemented before that result.
