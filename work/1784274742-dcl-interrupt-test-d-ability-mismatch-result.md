# DCL Interrupt test D — ability-identity negative control

## Purpose

Test whether the live Interrupt producer remains restricted to the exact configured ability while
an otherwise valid target has a real pending charged command.

## Fixture and action

- The immutable pending fixture was restored under the success profile.
- Josephine had self-targeted Death visibly queued in the combat timeline.
- Rion used High Potion on Josephine.
- High Potion is a legal allied Item action in the same action family as Potion, but has ability id
  `369`; the configured Interrupt rule matches Potion ability id `368` only.

The original plan proposed Throw Shuriken, but Throw cannot target allied Josephine. No Throw was
executed. Replacing it with High Potion preserved the intended exact-ability negative control and
removed the illegal-target confound.

## Evidence

- Archived raw log: `work/1784274722-dcl-interrupt-test-d-ability-mismatch-pass.log`
- SHA-256: `31622457C0E64F3DB874DD98C93DD47EDFED29E6D68DBD3844451C0415BE9522`
- High Potion produced two observed calculation rows with
  `caster=0x80 target=0x81 ability=369 type=0x06 outcome=hit`.
- The full capture contains zero `[DCL-INTERRUPT]` rows.
- Death remained visible in the timeline after High Potion, later resolved on Josephine, and left
  her incapacitated. The log reached a non-cached Death calculation row with
  `caster=0x81 target=0x81 ability=30 type=0x0B outcome=hit cached=0`.
- The game process was closed after the outcome was observed.

## Verdict

Pass. A same-family Item with a different ability id does not enter the Interrupt rule. It neither
logs an Interrupt outcome nor cancels the independently pending charged action. This is direct
live evidence that the producer's exact-ability filter rejects High Potion before mutation.
