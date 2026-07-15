# DCL current-job progression formula surface

## Offline implementation

The formula context now exposes the current job's proven battle-unit progression data for both attacker and target aliases:

- `<unit>.job` and `<unit>.jobId`: job id from `+0x03`;
- `<unit>.jobIndex`: `jobId - 0x4A` for the 23-entry JP arrays;
- `<unit>.jobJp`: spendable JP from `+0xF0 + 2*jobIndex`;
- `<unit>.jobTotalJp`: total JP from `+0x11E + 2*jobIndex`;
- `<unit>.jobLevel`: level 1–8 derived from total-JP thresholds `0, 200, 400, 700, 1100, 1600, 2200, 3000`.

Jobs outside the 23-entry progression array expose zero for index-dependent values and job level. This keeps monsters/special jobs explicit: their DCL weapon progression must come from authored synthetic job/family metadata rather than accidentally reading unrelated bytes.

## Gates passed

- Formula evaluation reads a synthetic Squire with spendable JP 123 and total JP 700 as job level 4.
- Boundary tests cover just below/at every representative threshold and cap values above 3000 at level 8.
- Debug build and the full formula/runtime smoke suite pass with zero warnings/errors.
- Existing generated damage scenarios remain unchanged after keeping the progression fixture isolated from their equipment-scan raw bytes.

## DCL consequence

The runtime now has every live unit input needed by the Weapon Skill growth equation in `docs/deep-combat-layer/10-weapon-skill.md`: job id, current-job mastery, character level, and equipped weapon family. What remains is authored content/calibration: the job×weapon-family grade matrix, grade rates, coefficients, cap, Sword Master mapping, and monster synthetic grades.

The current-job surface does not expose mastery in inactive jobs because the DCL attack formula only needs the unit's active job. Cross-job unlock and menu authoring remain data/progression concerns outside this combat slice.
