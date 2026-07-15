# LT28 DCL calc-entry provenance live plan

## Purpose

Validate the two statically mapped `computeActionResult` callers and enumerate the fire-time battle
states used by player forecast, confirmed execution, charge polling, and AI scoring. The special
target is formula `0x25`: Rend Helm, Rend Armor, Rend Shield, and Rend Weapon temporarily replace
their outer order record with Attack `(type=1, abilityId=0)` and re-enter the same calc.

Profile: `work/1783997612-battle-runtime-settings.lt28-dcl-calc-provenance.json`.

This profile is observe-only. It records the caller return RVA, battle state, turn owner, reaction
source, forecast pointer, caster/type/ability, and target index. It does not change native state,
the DCL caches, formulas, hit decisions, damage, status, inventory, or reactions.

## Startup route

1. Launch `FFT_enhanced.exe` through Reloaded-II.
2. Choose Enhanced and press Enter during the intro to skip it.
3. Choose Load, Manual Saves, first entry `05`.
4. Enter the prepared battle without saving over the file.

## Minimal observations

1. Open a basic Attack forecast and cancel it without confirming.
2. Confirm one basic Attack on a surviving target.
3. If a charged action is available, preview and confirm it; otherwise skip this row.
4. Let one enemy AI unit take a turn.
5. If any Rend action is available, preview it against a target with the matching equipment and
   confirm it once. Prefer Rend Weapon because the equipment/no-equipment result is visually clear.
6. Close the game without saving.

## Required log shape

Ordinary actions must use:

```text
[DCL-CALC-PROVENANCE] ... origin=outer-sweep returnRva=0x281F12 ...
```

A formula-`0x25` re-entry must produce an outer row carrying the Rend action id followed by:

```text
[DCL-CALC-PROVENANCE] ... origin=nested-rend-attack returnRva=0x307ED5 ... type=0x01 abilityId=0 ...
```

No `origin=unknown` row is expected because the current executable has exactly two real-code direct
callers. Any unknown origin is a fail-stop signal for cache changes.

## Verdicts

- **Caller PASS:** every row is `outer-sweep` or `nested-rend-attack`; the nested rows occur only
  under a Rend outer calculation and carry the synthetic Attack identity.
- **Phase PASS:** forecast-only, confirmed execution, and AI scoring produce stable, distinguishable
  battle-state/turn-owner signatures, or the log proves that they deliberately share one signature.
- **Cache-design gate:** do not suppress the nested entry until the Rend execution establishes
  whether native fallback damage consumes the outer decision, a distinct inner decision, or both.
- **Safety PASS:** no DCL mutation logs, no altered forecast/result, and the game exits normally.

After the run, restore the installed LT23 profile before any unrelated test.
