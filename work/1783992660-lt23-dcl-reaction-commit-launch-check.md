# LT23 reaction-commit launch check

## Outcome

The current Release build and LT23 observe-only settings loaded through Reloaded-II. The game
reached the Enhanced/Classic version selector, and the runtime installed the guarded hook at
`0x206421` with the expected bytes. No reaction scenario was executed, so the runtime behavior of
the candidate remains unproven.

## Confirmed evidence

- Executable SHA-256: `841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`
- Reloaded profile: `fftivc.utility.modloader`, `fftivc.generic.chronicle.codemod`
- Installed settings: `work/1783992200-battle-runtime-settings.lt23-dcl-reaction-commit.json`
- Hook log: `[DCL-REACTION-COMMIT-HOOK] rva=0x206421 addr=0x140206421 maxLogs=64 expected=40 88 B3 D3 01 00 00 (observe-only)`
- Captured log: `work/1783992660-lt23-dcl-reaction-commit-launch.log`
- Version-selector screenshot: `work/1783992660-lt23-version-selector.png`
- The process closed cleanly through `Process.CloseMainWindow()`; no save or battle state changed.

## Control limitation

The privileged Computer Use channel failed to initialize before any UI action. The visible game
process rejected ordinary `SendKeys`, mouse injection, and non-privileged `SendInput`, consistent
with a Windows integrity-level boundary. Screen capture remained available. Because input could not
be verified, navigation stopped at the version selector instead of attempting blind sequences.

## Interpretation

- **Proven:** current executable AOB, code-mod loading, LT23 settings loading, and hook installation.
- **Strong:** the static queue-boundary interpretation at `0x206421`.
- **Pending live gate:** zero forecast fires, one event per accepted Reaction, id agreement, and
  source/reactor direction across Counter plus one distinct reaction family.

Do not consume cadence or replace queued reaction actions at this boundary until the pending live
gate in `work/1783992200-lt23-dcl-reaction-commit-live-plan.md` passes.
