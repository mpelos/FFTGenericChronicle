# DCL clean-v1 first integrated live result: hit-parity gap

## Scope

The exact clean-v1 installation launched normally, installed every required hook, loaded Manual
Save 05, entered a one-unit Mandalia Plain encounter, and executed Ramza's basic Attack against the
adjacent Red Panther Mnestra. The game remained responsive and closed normally without saving.

## Positive evidence

- Settings, data, catalogs, and DLL matched the clean-v1 preflight before launch.
- AI movement published three accepted completed-route events without per-tile pauses or slowdown.
- Attack forecast displayed 121 damage, 4% authored hit chance, and native Counter presentation.
- Execution returned to Ramza's command menu without a crash, duplicate application, or stuck state.
- The authored miss reached compute point, zeroed the native 13 HP debit, changed the selector to
  miss, and suppressed the matching Reaction chance.
- Log SHA-256:
  `86558F522AEA90336857A02E8CB12C639BEE50AC91C036CFF414FE3FE6D41AF1`.

## Refuted contract

Forecast and execution did not share one decision. The forecast row was
`pct=4 roll=13 outcome=miss cached=0`; execution later produced
`pct=5 roll=12 outcome=miss cached=0`. The 2.5-second cache TTL expired while the forecast remained
open. The matching visual outcome happened to remain a miss, but this run cannot satisfy the
physical-fundamentals parity case.

The exact no-defense skill-5 probability is 10 successful 3d6 outcomes out of 216, rounded to 5%.
The offline probability function returns 5%; the forecast's 4% remains a separate observable to
recheck after exact cache reuse is live-proven.

## Offline correction

The hit-decision cache now owns an unconsumed forecast for its caster-turn epoch rather than for the
short delivery TTL alone. A same-epoch calc reuses the exact decision and refreshes the delivery
timestamp. A new epoch cannot reuse it, and any partially consumed execution loses the lifetime
extension. A dedicated live analyzer requires identical percentage, roll, and outcome on a cached
same-epoch row.

The installed DLL is stale as soon as this correction is built. No further clean-v1 live case is
valid until the new Release DLL is installed and the exact preflight passes again.
