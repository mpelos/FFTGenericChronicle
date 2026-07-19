# LT41F DCL Dual Wield provenance proven-route live plan

## Purpose

Capture the second native Dual Wield strike under the calc-provenance/pre-clamp diagnostic build.
The CT/status-only shortcut is refuted, and closing at the start of Janus's enemy turn does not
update the Enhanced autosave. This gate therefore replays the shortest proven LT40 action route.

## Fixture and isolation

- Autosave: `work/1784104894-fft-autoenhanced-snapshot.png`, SHA-256
  `3A6DDE7F777690F3095FB64CC36CAB190E9AB47B0371192FC3551C0435A41CC7`.
- Runtime profile:
  `work/1784169839-battle-runtime-settings.dcl-dual-wield-provenance-live.json`.
- Reloaded application config:
  `work/1784161260-appconfig.synthetic-reaction-isolated.json`.
- Diagnostic DLL SHA-256:
  `2BDB9C28071AA2F68D94AFB34EC9945B6163E1AC208A6B7360170D177C5B49D3`.
- Diagnostic PDB SHA-256:
  `F7C4CF068B06E677B417C181018675CC7EAEFFF66D68A0B868BAA033A35017A6`.

## Action and stop rule

Load Continue, choose **Auto-battle > Attack Enemy**, rotate twice with `E`, zoom once with `Z`,
select Wenyld (Archer, HP 396), and confirm **Yes**. Allow Rion's Throw Shuriken and the subsequent
Wenyld/Janus actions. Stop immediately after Janus's Choco Beak and the two owned Counter effects,
or on the first contrary action.

## Required diagnosis

Correlate each Counter strike's calc provenance, `[DCL-HIT]`, `[DCL-PRECLAMP]` or explicit
guard/error, `[DCL]`/`[DCL-MISS]`, HP delta, and state-`0x2C` effect. No pipeline change is authorized
unless the second-strike failure has one unambiguous cause.

Use a fresh six-file backup and restore every external hash after archiving the raw log.
