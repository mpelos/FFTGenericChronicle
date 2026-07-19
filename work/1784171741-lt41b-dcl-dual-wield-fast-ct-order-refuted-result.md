# LT41B DCL Dual Wield fast CT order — refuted result

## Outcome

The shortened turn-order gate failed before the Dual Wield transaction. After Continue, Rion's
already-open turn was ended with Wait. The next observed active unit was Timothy at CT `100`; no
Choco Beak, `442`, or Dual Wield calc row occurred before the bounded stop.

Raw capture:
`work/1784171677-lt41b-dcl-dual-wield-fast-ct-order-refuted-live.log`
(SHA-256 `D8F03ACE0D01A9E1D035315DC211ACF0FD97FBEDE02F22ACC16CC16AFE820F51`,
17,964 bytes).

## Exact runtime evidence

The first Choco runtime row is CT `0`, then the scheduler advances it through `20` and `53`. This
refutes the assumption that changing Choco CT in `resume_en00_main`, `resume_en00_fturn`, and
`resume_enbtl_main` controls the CT loaded by Continue for the already-open Rion turn.

The source container's unmodified `resume_en00_attack` and `resume_enbtl_attack` Choco copies both
carry CT `0`, exactly matching the runtime start. This is **Strong**, not yet Proven, evidence that
the attack aliases own the resumed in-progress-turn CT snapshot. The next fixture must patch the
Choco CT byte in those two aliases as well and re-audit every delta.

## Diagnostic boundary

The new `[DCL-PRECLAMP]` row appeared during startup state restoration, proving the diagnostic build
was loaded. The test never reached either Counter strike and therefore says nothing new about the
second-strike failure.

## Restoration

FFT and Reloaded-II were closed immediately after the contrary turn-order observation. DLL, PDB,
runtime settings, AppConfig, battle log, and autosave were restored from the pre-test backup; all six
destinations matched their recorded pre-test SHA-256 hashes.
