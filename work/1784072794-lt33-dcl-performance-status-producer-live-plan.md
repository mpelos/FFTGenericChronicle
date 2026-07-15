# LT33 DCL performance-status producer live plan

## Purpose

Validate Nameless Song as a native-cadence performance whose confirmed ticks reuse the execution-only post-calc producer. The DCL owns one random status contest per eligible target/tick; the engine keeps the target sweep, Performing state, tick schedule, and cleanup.

## Profile and setup

- Profile: `work/1784072793-battle-runtime-settings.lt33-dcl-performance-status-producer.json`.
- Save route: Enhanced, `Enter` to skip the intro, Load, Manual Saves, first entry `05`.
- Test action: Nameless Song (`abilityId=91`) with at least two living allies that do not already have every member of the native bundle.
- The five owned members are Reraise, Regen, Protect, Shell, and Haste in one `random-one` group.
- Forced roll `18` and resistance `0` make the selected member succeed unless equipment immunity blocks it.

## Required evidence

1. Startup reports `[POST-CALC-HOOK] ... statusProducer=1` at RVA `0x281F12`.
2. Starting or previewing the performance produces no status decision before a confirmed tick.
3. Each eligible ally receives exactly one `[DCL-STATUS-PRODUCER]` row per tick with ability `91`, `origin=outer-sweep`, battle state `0x2A`, five owned writes, and one carried result.
4. Exactly one of the five packet members per ally/tick is not `not-selected`; it uses the forced roll `18` and is reused at pre-clamp without a second roll.
5. At least two ticks preserve the same native cadence and target sweep without duplicate animations or duplicate status commits.
6. Ending the performance through a normal stop/cleanup cause produces no later producer rows. If a controlled Sleep stop is available, no packet may be staged while the caster has `effective[4] & 0x10`.

## Failure interpretation

- A producer row during forecast/start rather than a tick is an execution/cadence gate failure.
- Rows for enemies or non-targeted units mean the outer target sweep is broader than the managed producer may assume.
- Two rolls or two committed members for one ally/tick mean prepared-plan/random-one reuse failed.
- A row while the performer is asleep means the native eligibility guard failed.
- Correct packets with wrong tick count or post-cleanup rows leave native scheduler cardinality unresolved.

Close with `Alt+F4` without saving after preserving the complete log span.
