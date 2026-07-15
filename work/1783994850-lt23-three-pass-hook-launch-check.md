# LT23 three-pass hook launch check

## Result

The current LT23 Release build loaded through Reloaded-II and installed all three guarded
reaction-commit hooks:

```text
[DCL-REACTION-COMMIT-HOOK] pass=2 rva=0x206421 addr=0x140206421 actor=rbx maxLogs=64 expected=40 88 B3 D3 01 00 00 replacement=off/log-only (guarded commit probe/control)
[DCL-REACTION-COMMIT-HOOK] pass=0 rva=0x2066AE addr=0x1402066AE actor=rbx maxLogs=64 expected=40 88 B3 D3 01 00 00 replacement=off/log-only (guarded commit probe/control)
[DCL-REACTION-COMMIT-HOOK] pass=1 rva=0x206743 addr=0x140206743 actor=rdi maxLogs=64 expected=40 88 B7 D3 01 00 00 replacement=off/log-only (guarded commit probe/control)
```

The runtime settings line confirmed `DclReactionCommitProbeEnabled=True`,
`DclReactionPreSelectorProbeEnabled=False`, and action replacement disabled.

The game reached its normal main window and remained responsive. It was closed with
`CloseMainWindow`; the process exited cleanly. No menu input, save load, or battle was performed.

## Interpretation boundary

- **Proven for this build:** all three hook anchors pass and the Reloaded assembler accepts/activates
  the three shims together.
- **Not tested:** event counts, queue-pass ownership by reaction family, target/source direction,
  forecast silence, cadence, or action replacement.
- LT23 remains behaviorally pending and must follow
  `work/1783992200-lt23-dcl-reaction-commit-live-plan.md`.
