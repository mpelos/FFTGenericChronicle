# DCL position-commit caller-correlation live result

## Artifacts

- Profile: `work/1784333606-battle-runtime-settings.dcl-position-commit-observe.json`.
- Plan: `work/1784333606-dcl-position-commit-live-plan.md`.
- Raw log: `work/1784334360-dcl-position-commit-live.log`.
- Raw-log SHA-256: `E39010BF6AB0004AE7B4C23F0ECA492B9E068E899BA888E091DA5B476A9CCA40`.
- Analysis: `work/1784334360-dcl-position-commit-live-analysis.md`.
- Static gate: `work/1784333281-dcl-position-commit-analysis.md`.

## Controlled sequence

The run loaded Manual Save `05`, created a Mandalia Plain encounter, deployed only Ramza, and
reached Ramza's first turn. The position probe had seven events after battle setup and the two
early enemy turns.

1. Ramza opened Move, selected two different distant blue tiles, and cancelled without confirming.
   The probe count remained exactly seven. Cursor/path preview does not call the shared canonical
   position writer.
2. Ramza reopened Move, selected a distant tile, confirmed, and completed the walking animation.
   The count advanced from seven to nine.
3. Ramza selected Wait and completed the facing/end-turn prompt. The count advanced once more for
   Ramza, then ordinary enemy turns supplied the same three-stage pattern independently.
4. The game was closed with `Alt+F4` without saving. No hook failure, byte mismatch, ring loss, or
   crash occurred.

## Exact Ramza timeline

| Event | Caller | Destination | Meaning |
| ---: | ---: | --- | --- |
| `1` | `0x1F3022` | `5,1,0x02` | deployment/setup coordinate commit |
| `8` | `0xD43CF29` | `4,1,0x01` | first and only accepted-walk coordinate change |
| `9` | `0xD8C7D18` | `4,1,0x01` | immediate post-movement duplicate with active-state transition |
| `10` | `0x20BC4F` | `4,1,0x01` | facing/end-turn commit; no coordinate change in this selection |

The confirmed walk changed Ramza from `5,1,0x02` to `4,1,0x01`. No intermediate tile was written
to the canonical battle-unit position fields. The accepted player movement therefore emits one
final-tile commit, followed roughly 0.7 ms later by a same-destination state duplicate.

Enemy turns independently repeated the pattern:

1. `0xD43CF29` changed X/Y/layer-facing to the accepted destination.
2. `0xD8C7D18` repeated that same destination while the active marker changed.
3. `0x20BC4F` committed end-turn facing; for two enemies it changed only the low `+0x51` nibble
   from `1` to `0` while preserving X/Y.

## Conclusions

- **Proven live:** cursor/path preview and cancellation do not reach the canonical position writer.
- **Proven live:** return address `module+0xD43CF2E` (call RVA `0xD43CF29`) is the first accepted
  gameplay-movement final-tile commit for both player and AI movement.
- **Proven live:** return address `module+0xD8C7D1D` (call RVA `0xD8C7D18`) is a same-destination
  post-movement duplicate and must not produce a second approach event.
- **Proven live:** return address `module+0x20BC54` (call RVA `0x20BC4F`) is the end-turn/facing
  commit and must not be treated as movement when X/Y are unchanged.
- **Proven live:** this canonical writer does not expose traversed tiles. It is sufficient for an
  outside-final-position to inside-final-position approach rule, but insufficient by itself to
  detect a path that enters reach and then exits before the destination.

## Next investigation

Trace the producer of actor/path coordinates consumed by `0xD43CEC0` and locate the per-tile path
cursor or route array. Static analysis should begin from its writes to actor `+0x88/+0x89/+0x8A`
and from the path/relocation helper family around `0x283784`. Only if no stable per-step boundary
exists should Stop-hit be defined against final-tile entry alone.
