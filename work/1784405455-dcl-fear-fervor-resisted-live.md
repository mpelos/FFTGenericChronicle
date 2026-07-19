# DCL Fear — Fervor resisted live result

## Test observation

Josephine used Fervor (`ability 53`) on Arthur. The forecast displayed `100%`, the action animation
completed, and Arthur showed no visible status or behavioral change.

## Runtime evidence

The fresh `battleprobe_log.txt` records the complete execution path:

- calculation: `abilityId=53`, `type=0x10`, target index `17`;
- native hit contest: `pct=100`, `roll=14`, `outcome=hit`;
- outer-sweep result before managed status ownership: `flags=0x08` (native Berserk carrier);
- DCL Fear rule: `resistance=9`, `roll=7`, `outcome=resisted`;
- status producer: `flags=0x08->0x00`;
- final action row: `flags=0x08->0x00`, with no status packet added.

The absence of an effect is therefore the expected resisted transaction, not a missing ability
identity, missing native carrier, or failed post-calculation hook. The DCL correctly removes the
owned native Berserk bit when Fear is resisted.

## Consequence

The native forecast percentage represents Fervor's connection chance but does not include the
separate DCL 3d6 status-resistance contest. This is a player-facing readability gap. The successful
Fear application, forced-flee continuation, hostile-action rejection, and allowed self/ally action
remain live gates.

