# DCL facing geometry — offline checkpoint

## Result

The physical mechanism profile now derives front/side/back from the live attacker/target coordinates
and defender facing byte, then feeds the existing contest policies directly:

```text
front -> defense roll, no facing modifier
side  -> defense roll, -2 fixture modifier
back  -> no defense roll
```

Brave remains a separate shared active-defense modifier. A Brave-70 defender therefore receives
`-1` front, `-3` side (`-1 + -2`), and no roll from behind.

## Geometry

The live-calibrated `unit+0x51 & 0x7F` compass enum is:

| Facing | Direction |
| ---: | --- |
| 0 | `-Y` |
| 1 | `-X` |
| 2 | `+Y` |
| 3 | `+X` |

The formula computes the source direction from `(attacker.x-target.x, attacker.y-target.y)`. The axis
with the larger absolute distance wins; a tie uses Y. Same-tile/self cases default to front rather
than accidentally disabling defense. The target's opposite direction identifies back; the remaining
two directions are side.

This dominant-axis rule is a mechanism policy for ranged/diagonal sources, not a live-proven engine
rule. Cardinal adjacent front/side/rear cases agree with the LT9 correlation. Diagonal attacks and
large/multi-tile units remain explicit live/design gates.

## Offline assertions

The target stands at `(5,5)` facing `-Y` with Brave 70:

| Attacker tile | Relation | Defense allowed | Combined modifier |
| --- | --- | ---: | ---: |
| `(5,4)` | front | 1 | -1 |
| `(6,5)` | side | 1 | -3 |
| `(5,6)` | back | 0 | irrelevant |

Validation passed under `FacingAudit` / `FacingValidate`: build 0 warnings/errors, full smoke suite
passed, profile JSON parsed, and settings validator reported zero errors.

## Remaining gates

- Confirm one more cardinal compass sample in the current executable build.
- Cast/attack from diagonal and ranged positions to validate or replace the dominant-axis policy.
- Decide footprint ownership for large/multi-tile units; the single-origin formula must not silently
  govern them until their occupied-tile geometry is exposed.

