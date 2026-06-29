# Counter post-hit clean result

## Manual result

- Ramza used basic Attack against the former Ninja/Thief with Counter equipped.
- Preview chance: 58%.
- The original attack hit.
- Corrected defender HP readout after the hit: `145/203`.
- Counter triggered.
- Counter UI damage: 172.
- Attacker HP after counter: `30/202`.
- Ramza had not ended the turn after the sequence.
- No critical/status/special effect was reported.

## Evidence files

- Raw log: `work/1782754715-counter-post-hit-ramza-thief-log.txt`
- Analyzer report: `work/1782754715-counter-post-hit-ramza-thief-report.md`

## Runtime sequence

1. The original basic attack applies HP damage to the defender:
   - target: `0x141855CE0/id=0x80`
   - caster/source: `0x141855EE0/id=0x01`
   - action id: `0` (basic Attack / implicit weapon)
   - staged damage: 58
   - HP transition: `203 -> 145`

2. The post-hit Counter then applies HP damage back to the attacker:
   - target: `0x141855EE0/id=0x01`
   - caster/source: `0x141855CE0/id=0x80`
   - action id: `0` (basic Attack / implicit weapon)
   - staged damage: 172
   - HP transition: `202 -> 30`

## Interpretation

Counter post-hit follows the same useful authority pattern as First Strike for HP-applying reaction damage: the pre-clamp actor context resolves the caster/source and target directly, without needing CT attribution.

Unlike First Strike, the original attack is not cancelled. The runtime sees two independent HP-applying events: first the original hit, then the Counter hit. This is exactly the event shape the DCL needs for per-hit defense state and reaction handling.

## Open questions

- Whether dual-wield or multi-hit attacks can trigger one or multiple Counter events and how those map to hit indices.
- Whether Hamedo/First Strike, Counter, and other reaction families share enough phase markers to classify the reaction family rather than only the resulting basic attack.
- Whether reaction trigger authority can be controlled directly, or only the resulting hit can be rewritten after native reaction selection.
