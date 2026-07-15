# Barrage result-cardinality observation

## Purpose

Measure the protected outer repeat carrier for ability `358` without changing damage, hit chance,
targets, reactions, or action data.

Use the already validated observe-only profile
`1784015549-battle-runtime-settings.randomfire-cardinality-observe.json`. Its probes are generic and
capture Barrage's calculation, selector, pre-clamp, pending-action, and action-boundary events as
well as RandomFire.

## Procedure

1. Deploy the observe-only profile without rebuilding or enabling the data mod.
2. Launch Enhanced through Reloaded-II, press Enter to skip the intro, choose Load, Manual Saves,
   and the first entry (save 05).
3. If the loaded roster/battle exposes Piracy's Barrage naturally, use it once against a living
   single target. Do not grant or replace an ability solely for this probe.
4. Record the forecast amount/percentage, visible number of weapon strikes and damage popups, target
   HP before/after, and any reaction presentation.
5. Close without saving and preserve the uninterrupted log span from the first ability-358 calc
   event through the final action boundary.

## Evidence to count

- calc-entry provenance rows for confirmed execution, separated from forecast and AI evaluation;
- selector and pre-clamp rows, including target/result pointers and staged debit;
- HP apply events and visible damage popups;
- whether every repeat re-enters the equipped-weapon calculation and normal-attack rider path;
- whether target identity stays fixed;
- whether a reaction opportunity appears once for the outer Barrage or once per native strike.

## Decision table

| Observed shape | Runtime ownership |
| --- | --- |
| One calc/apply with aggregate debit | Reuse the managed aggregate physical carrier; authored strike contests feed one result. |
| One calc/apply per visible strike | Cache an explicit strike generation/index and consume one DCL decision per native apply. |
| Several weapon calculations but one final apply | Separate weapon-input evaluation from aggregate result delivery. |
| One damage apply but several reaction/rider passes | Keep damage aggregate while independently controlling reaction/rider cadence. |

## Pass criteria

- each visible strike and HP change is attributable to a logged execution event;
- forecast/AI calls are not counted as native strike applications;
- the profile performs no writes that alter combat behavior;
- the observation selects one carrier shape without inferring a strike index from timing alone.
