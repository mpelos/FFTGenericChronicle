# DCL partial HP/MP result-flag live plan

## Purpose

Validate only the current-build presentation of a structurally valid combined `0xA0` result. The
offline mechanism already owns atomic numeric HP/MP application and selector priority.

## Safety and setup

- This is a mechanism fixture, not the final Mana Shield ratio.
- It changes only character id `0x1F` and only after native Mana Shield has redirected a positive HP
  debit fully into MP.
- Launch Enhanced through Reloaded-II, press Enter to skip the intro, choose Load, Manual Saves, and
  the first save `05`.
- Do not grant or replace abilities. Continue only if character `0x1F` naturally has Mana Shield and
  can receive one ordinary damaging hit.

## Observation

1. Deploy `work/1784017665-battle-runtime-settings.dcl-result-flags-mechanism.json` through the normal
   guarded build/deploy helper.
2. Record HP and MP before the hit.
3. Receive one ordinary hit that naturally triggers Mana Shield.
4. Confirm one `[DCL]` line reports `oldDebit=0`, positive `oldMpDebit`, positive final `debit`,
   positive final `mpDebit`, and `flags=0x20->0xA0`.
5. Confirm HP and MP both decrease by the logged amounts exactly once.
6. Record whether the primary popup is HP damage and whether the Mana Shield reaction presentation
   still plays. Record any separate MP popup; none is required by the static selector contract.

## Pass boundary

Pass requires exact atomic HP+MP deltas and an HP-damage primary presentation with no duplicate apply.
The reaction animation result decides only presentation integration. It does not ratify the fixture's
half-split ratio.
