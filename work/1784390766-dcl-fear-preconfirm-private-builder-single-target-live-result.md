# DCL Fear pre-confirm private builder single-target live result

## Test

The corrected observation build loaded through Reloaded-II. Josephine, battle slot `17`, used Basic
Attack against Arthur, battle slot `16`. Arthur took the ordinary damage result and visibly changed
to Chicken. The game remained responsive during the post-action observation interval and did not
crash.

The decisive runtime row is:

```text
[DCL-FEAR-CONFIRM] casterIdx=0 turnOwner=17 type=0x00 ability=0 state=0x19 actor=0x140D31FE8 actorUnit=0x141855EE0 actorAction=0 actorPresentation=0 actorPrimaryTarget=0 actorTargetCount=0 actorTargets=[] expandedTargetCount=1 expandedTargets=[16] decision=allow
```

Execution then reported caster `17`, target `16`, staged Chicken byte 2 mask `0x04`, applied 14 HP
damage, and completed the visible transformation.

## Conclusions

- The actor-owned list at `actor+0x1A9/+0x1AA` is empty before confirmation and remains unusable.
- `actor+0x1BC` reported zero and is not independently authoritative for the tested Attack.
- The synchronous native builder call into a private 21-byte stack buffer is safe at the voluntary
  confirm boundary for the tested single-target action.
- The private list is current and complete for that action: its sole entry `16` matches Arthur and
  the later execution target.
- The exact six-byte Chicken dispatcher correction remains stable through visible application; the
  earlier progressive slowdown did not recur.
- Voluntary rejection remains fail-open. A known AoE must prove that the same private list contains
  every visibly affected unit before it can become the enforcement authority.

## Operational follow-up

Future repetitions use a preserved pre-action autosave rather than rebuilding the formation from
Manual Save 05. The runbook owns the fixture rule and the Josephine/Arthur autosave requirement.

The captured fixture is `work/1784390906-fft-autoenhanced-snapshot.png`, SHA-256
`A3D96C1118088D195FBD6863ECFA9B70DDE1D5188583F68D4510512BCB572204`. Its source autosave timestamp
is 13:04:33 local, one second before the first calculation row, so it represents the exact pre-action
state rather than a post-hit save.
