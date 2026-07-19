# DCL Interrupt test E — live cancellation

## Purpose

Validate that one matched Interrupt rule can remove a real charged action from the native pending
command state, rather than merely suppressing its forecast or presentation.

## Fixture and action

- Josephine self-targeted Death (`ability 30`) and its entry was visibly present in the combat
  timeline.
- Rion used Potion (`ability 368`) on Josephine.
- The temporary live rule matched Potion exactly, used the Caution resistance contest, and allowed
  one pending-state write.

## Evidence

- Archived raw log: `work/1784277689-dcl-interrupt-test-e-cancelled-pass.log`
- Bytes: `12248`
- SHA-256: `CFE90AFD8AACF4E8349A9A15D4512C119E379B1B80ED79E59B7E62D7A9ADB20B`
- Exact producer row:

  ```text
  [DCL-INTERRUPT] rule=Potion temporary Interrupt probe carrier caster=0x80 target=0x81 ability=368 resistance=14 roll=18 outcome=cancelled before=effective=0x08/source=0x00/timer=9/type=0x0B/action=30/master=0x08 after=effective=0x00/source=0x00/timer=255/type=0x0B/action=30/master=0x00 writes=1
  ```

- The before/after state clears both the effective and master pending masks, replaces timer `9`
  with the empty sentinel `255`, and retains the diagnostic type/action bytes.
- Death disappeared from the timeline and did not execute.

## Verdict

Pass. The matched Potion contest mutates the native pending-action owner and cancels the charged
Death transaction. This proves one successful live cancellation for the exact test carrier; it
does not by itself establish final DCL ability policy or balance.
