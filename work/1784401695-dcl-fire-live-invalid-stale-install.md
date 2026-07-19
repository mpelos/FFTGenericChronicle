# DCL v4 Fire live attempt — invalid stale installation

## Test action

The player cast Fire. The game-side runtime log was last written at `2026-07-18 13:04:37.875`.

## Runtime evidence

- The runtime initialized with `EnabledMods: fftivc.utility.modloader, fftivc.generic.chronicle.codemod`.
- The runtime explicitly reported `data mod fftivc.generic.chronicle loaded? False`.
- The only calculated action was recorded as `abilityId=0`, action type `0x01`, and catalog name `<Nothing>` instead of Fire ability 16.
- The confirmation row had no authoritative actor targets: `actorTargetCount=0`, `actorTargets=[]`; its expanded list contained only target 16.
- The old installed Fear rule staged Chicken directly for ability 0: `rule=dcl-fear`, byte 2, mask `0x04`.
- The exact v4 live-install preflight failed for four independent reasons: missing data mod in the Reloaded profile, stale settings, stale action NXD, and stale code-mod DLL.

## Conclusion

This execution is not evidence about Fire AoE target expansion or the v4 Fervor-to-Fear reskin. The running installation is not the exact v4 bundle, and the runtime misidentified the cast as basic action 0. The observed Chicken staging belongs to the stale rule and must not be attributed to the v4 mechanism.

## Retry gate

Do not repeat the live action until `python tools/validate_dcl_live_install.py --pair work/1784399746-dcl-unified-sentinel-v4-runtime-data-pair.json` passes. A valid Fire probe must record ability 16 and authoritative/expanded target evidence; it must not use Fire as the Fear carrier.
