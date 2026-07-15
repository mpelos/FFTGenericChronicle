# DCL managed Pummel multistrike gate

## Scope

This gate validates the managed physical multistrike carrier for Pummel, ability 101. It uses
`1784014720-battle-runtime-settings.dcl-pummel-multistrike.json` and the paired approved probe-only
overlay `1784014720-dcl-pummel-probe-metadata.csv`. Power 40 and all forced values are diagnostic,
not production balance.

The native Pummel formula stages one aggregate result. The DCL replaces its random multiplier with
three authored contests and one formula-owned aggregate debit.

## Deterministic contract

All three attack rolls are 10. Attack skill is `12 - dcl.strike.index`, producing 12, 11, and 10.
All selected defense rolls are 12. The defender begins with one Block and one Parry charge.

1. Strike 1 selects Block 13. Defense 12 succeeds; Block becomes locally unavailable.
2. Strike 2 selects Parry 11. Defense 12 fails; one normal strike lands and Parry becomes locally
   unavailable.
3. Strike 3 selects Dodge 8. Defense 12 fails; a second normal strike lands. Dodge is not finite.
4. The aggregate contains three strikes, two hits, zero criticals, zero attack misses/fumbles, one
   defended strike, one Block attempt, and one Parry attempt.
5. The damage formula commits `40 * 2 = 80` HP once. The live Guard pool commits Block `1 -> 0` and
   Parry `1 -> 0` once, after the staged result is accepted.
6. The outer action offers no more than one target reaction opportunity.

## Procedure

1. Deploy the current code mod with the probe settings. The metadata path is absolute and points to
   this checkout; do not copy the CSV into the installed mod or replace a production overlay.
2. Launch Enhanced through Reloaded-II, press Enter to skip the intro, choose Load, Manual Saves,
   and the first entry (save 05).
3. Select a unit that knows Pummel and target a living enemy. Record the displayed hit percentage
   and damage amount before confirming.
4. Cancel once and reopen the forecast. No `[DCL-GUARD]` spend may occur during either forecast.
5. Execute Pummel once and preserve the uninterrupted log span from `[DCL-STRIKE] strike=1/3`
   through `[DCL-GUARD]` and the selector/reaction lines.
6. Compare on-screen HP before/after and count target reaction animations/effects.

## Pass gates

- startup reports one approved metadata row and no metadata error;
- forecast amount is 120 and the exact nominal any-hit percentage is 48%, stable across
  cancel/reopen (forced execution rolls do not alter the nominal 3d6 forecast);
- exactly three fresh `[DCL-STRIKE]` lines appear in order with the deterministic outcomes above;
- the aggregate `[DCL-HIT]` reports `strikes=3 hits=2 crits=0 misses=0 fumbles=0 defended=1`, and
  cached calc-entry refires preserve those counts without generating new strike rolls;
- `[DCL]` reports one committed debit of 80 and the target loses exactly 80 HP;
- exactly one `[DCL-GUARD]` line reports `parry=1/1 block=1/1 outcome=spent`;
- the selector delivers one outer result; Pummel produces at most one target reaction;
- no Guard charge is spent during forecast or on duplicate callbacks.

## Failure interpretation

- no `model=physical-multistrike`: metadata did not load or the physical condition did not select
  Pummel;
- three contests but vanilla/random debit: aggregate variables did not reach the pre-clamp formula;
- repeated aggregate damage or Guard logs: the native carrier fires more than one apply for Pummel
  or cache consumption is not idempotent;
- one reaction per synthetic strike: a separate reaction-cadence gate is required before Pummel can
  satisfy its job contract;
- different HP loss with the correct `[DCL] debit=80`: the native formula has another downstream
  result/rider boundary that the current pre-clamp carrier does not own.
