# DCL Interrupt — Test C no-pending result

## Scope

This bounded live run validates the explicit no-pending branch of the DCL Interrupt producer. It
uses the dedicated immutable profile whose condition is always true, because the A/B profile's
`interrupt.pending` condition would reject the event before that branch.

## Fixture and action

- Runtime profile: `work/1784272266-battle-runtime-settings.dcl-interrupt-no-pending.json`
- Profile and deployed-settings SHA-256: `94B60A31E48C9CD1ABBDC433C05C974FA1A11265E6B9CC7E8EDB535B2558DBAF`
- Enhanced autosave fixture: `work/1784266759-fft-autoenhanced-snapshot.png`
- Rion (`0x80`) used Potion (`368`) on himself while he had no pending action.
- Josephine's separate Death charge remained visible in the combat timeline during the action.

## Evidence

- Raw log: `work/1784273141-dcl-interrupt-test-c-no-pending-pass.log`
- Raw-log SHA-256: `A42AB9C2229DE63F2230C9DD333E1F7ADB85B14EBCAAE29516C310E9106510F8`
- Exactly one Interrupt row exists:

```text
[DCL-INTERRUPT] rule=Potion temporary Interrupt no-pending carrier caster=0x80 target=0x80 ability=368 outcome=no-pending-action pending=effective=0x00/source=0x00/timer=255/type=0x06/action=368/master=0x00
```

- The row reports target-local pending state as clear: both Charging mirrors are zero and the
  timer is the native inactive sentinel `255`.
- The log contains no cancellation, write-cap, or `writes=1` signal.
- The game was closed immediately after capture, before Josephine's unrelated Death resolved.

## Verdict

PASS. A matching Interrupt carrier aimed at a unit without a pending action produces exactly one
`no-pending-action` outcome and performs no cancellation write. Pending state is evaluated on the
carrier's target, not inferred from another unit's visible timeline entry.
