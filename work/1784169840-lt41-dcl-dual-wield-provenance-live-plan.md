# LT41 DCL Dual Wield provenance live plan

## Question

Why does the managed DCL pre-clamp pipeline rewrite the first native Dual Wield strike but fail open
to vanilla on the second, even though LT40 records a second calc and a second delivered effect?

## Offline evidence exhausted

- Both native debits reach the pre-clamp boundary.
- `orderRecord+8` is the exact weapon payload used by native calculation.
- LT40 records two Counter calcs with payload `124`, two HP applies, and two state-`0x2C` effects.
- Only the first HP apply has a matching `[DCL]` commit.
- LT40 did not capture calc provenance, and cache misses 2..63 were sampled out. Its archive cannot
  determine why the second managed callback failed open.

## Bounded setup

- Profile: `work/1784169839-battle-runtime-settings.dcl-dual-wield-provenance-live.json`.
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- Isolated Reloaded profile: mod loader plus Generic Chronicle Battle Probe only.
- Damage remains fixed to one point solely to preserve the already proven nonlethal Counter fixture.
- Hit is fixed to `100`/roll `0` with the validated zero-evade baseline.
- Calc provenance is enabled for every captured row. The runtime logs the first sixteen pre-clamp
  cache misses and includes the latest origin/state/caster/action/payload on a missing confirmed
  execution context.

## Action and stop rule

Use the verified fast Continue path and reproduce **Auto-battle > Attack Enemy > Wenyld**. Ignore the
distant rejected request. Stop immediately after adjacent Choco Beak produces the two Counter
state-`0x2C` effects, without waiting for Rion's next turn.

## Required correlation

For each Counter strike, correlate in order:

1. calc provenance: return RVA, battle state, caster `16`, target `0`, action `1/0`, payload;
2. DCL hit-decision row and exact payload;
3. pre-clamp outcome: either one `[DCL] oldDebit -> 1` commit or an explicit `[DCL-MISS]`/guard
   reason;
4. HP delta on source `0`;
5. state-`0x2C` Reaction `442` effect on source `0`.

The gate passes as a diagnosis only when the second-strike failure has one unambiguous cause. It is
not a mechanism pass. Do not change action-context lifetime until this event order is known.

Close FFT and Reloaded-II, archive the raw log, and restore the DLL, PDB, settings, AppConfig,
battle log, and autosave byte-for-byte before interpretation.
