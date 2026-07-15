# RandomFire result-cardinality observation

## Purpose

Determine how the protected native `RandomFire` consumer repeats calculation, target selection,
pre-clamp staging, selector delivery, and HP/status application. The result decides whether a managed
magic multistrike may aggregate all authored strikes into one DCL apply or must own each native
strike separately.

Profile: `1784015549-battle-runtime-settings.randomfire-cardinality-observe.json`.

The profile is observe-only. It enables no DCL formula, hit/evade control, status/reaction mutation,
or result rewrite.

The DCL default remains one Magic Evade roll per final target for the whole spell, even when the
native carrier repeats projectiles. A per-strike Magic Evade policy is available only as an explicit
future design exception. The probe does not choose between those policies; it determines where the
already-authored per-target decision can be consumed safely.

## Available family

The catalog contains exactly 16 `RandomFire` actions, all enemy-usable:

```text
169..180  Heaven's Wrath through Impiety
255       Holy Breath
342..344  Tri-Thunder, Tri-Flame, Dark Whisper
```

Every record uses formula `0x1E`, `0x1F`, or `0x5E`; the common formula handler itself creates one
ordinary MA result. Action byte 4 bit `0x08` selects the protected outer repetition carrier.

## Procedure

1. Deploy the observe profile without changing the data mod.
2. Launch Enhanced through Reloaded-II, press Enter to skip the intro, choose Load, Manual Saves,
   and the first entry (save 05).
3. Enter a battle containing an enemy that can naturally use any listed action. Do not replace an
   action or grant a skill merely to satisfy this gate; if none is available, preserve the clean log
   and defer the observation to the next suitable battle.
4. Allow exactly one `RandomFire` action to complete. Record its visible target sequence, number of
   damage/status popups, and HP changes per affected unit.
5. Close the game without saving and preserve the uninterrupted log span from the first
   `[DCL-CALC-PROVENANCE]` for that ability through its final selector/action-boundary event.

## Evidence to count

For the one outer action, group events by caster, ability id, target id, and battle/action boundary:

- calc-entry provenance rows, separating forecast/AI evaluation from confirmed execution;
- `[PRECLAMP]` and `[PRECLAMP-ACTOR-CTX]` rows with staged HP debit;
- `[SELECTOR-PROBE]` rows and their target/result record;
- HP event rows and on-screen popups;
- for Celestial Void, Corporeal Void, or Dark Whisper, each visible status attempt/result.

## Decision table

| Observed execution shape | Safe implementation |
| --- | --- |
| One pre-clamp + one selector per final target, with debit already aggregating all selections | Bind the managed hit-count aggregate once per target. |
| Multiple pre-clamps/selectors for one target, one per strike | Keep a strike-generation/cache index and author each native apply separately; never write the full aggregate into each callback. |
| One calc decision reused across multiple native applies | Split decision storage from apply consumption and expose a proven native/app-owned strike generation. |
| One calc + one apply for the whole action despite multiple visual selections | Use an outer aggregate, but retain per-target allocation evidence before authoring damage/status. |
| Status rider commits once while damage repeats, or vice versa | Give the composite multistrike/status policy separate carrier ownership for damage and rider cadence. |

The status design only guarantees that a status contest happens after a successful landing. It does
not specify once-per-target versus once-per-projectile behavior for a repeated spell, so neither
cadence is armed before this observation.

## Pass criteria

- every observed popup/HP change has one attributable pre-clamp/selector path;
- execution cardinality and ordering are stable for the captured action;
- no probe setting changes damage, targets, statuses, reactions, forecast, or AI choice;
- the result selects exactly one implementation row above, or records a new explicit shape rather
  than inferring a strike id from timing.
