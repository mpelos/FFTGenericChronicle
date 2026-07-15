# DCL reach — offline checkpoint

## Result

The DCL's two-tier reach structure needs no new targeting hook for its ordinary weapon identity:

- every Pole and Polearm row in the current item catalog already has native `weaponRange=2`;
- ordinary melee/fists remain range 1;
- attacking from range 2 already uses the engine's targeting surface;
- the DCL point-blank weakness now enters the physical attack-skill formula from grid distance.

The fixture rule is:

```text
pointBlank = equipped right weapon has Range 2 && Manhattan distance == 1
attackSkill += pointBlank ? -2 : 0
```

The `-2` is calibration, not a locked number. It penalizes landing rather than damage, keeping the
reach weakness inside the physical contest and leaving the typed damage pipeline unchanged.

## Offline assertions

A level-99 Ninja with an unmapped/F-grade Spear reaches capped inputs that produce attack skill 13,
then receives Best Zodiac `+1`:

| Distance | Point blank | Final attack target |
| ---: | ---: | ---: |
| 1 | yes | 12 (`13 + 1 - 2`) |
| 2 | no | 14 (`13 + 1`) |

Build `ReachAudit`, the full smoke suite, JSON parsing, and validator `ReachValidate` all pass with
zero errors.

## Evidence boundary

Target range is data-driven and already present. Two remaining behavior claims are not yet proved for
the current build:

1. a range-2 attack against a reach-1 defender must not trigger an adjacent Counter;
2. Manhattan distance must match the engine's notion on slopes, bridges, and multi-tile units.

Stop-hit is intentionally not universal. It remains a separately authored lancer ability and should
not be implemented as movement friction for every reach-2 weapon.

