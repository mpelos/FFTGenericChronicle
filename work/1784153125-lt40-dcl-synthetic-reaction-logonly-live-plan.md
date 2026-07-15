# LT40 generic synthetic-Reaction log-only live plan

## Purpose

Prove that the generic synthetic-Reaction gate observes an exact equipped neutral carrier after a
successful nonlethal incoming hit, reserves it once, and reaches the dynamic pre-selector as a
`synthetic-would-stage` intent. This is an engine-mechanism test only: it implements no job,
ability effect, status, stat change, or final balance value.

## Offline prerequisites

- Runtime profile: `work/1784152418-battle-runtime-settings.synthetic-reaction-logonly.json`.
- Autosave fixture: `work/1784152701-synthetic-reaction-carrier443-fixture.png`.
- Fixture SHA-256: `BFD1B51AAD4DE1A941AB626B695D19CC8B696937E9A0567551D962E4E0800EE4`.
- `tools/analyze_dcl_synthetic_reaction_transaction.py`: PASS.
- `tools/test_dcl_synthetic_reaction_live.py`: PASS.
- Full offline check suite: PASS.
- Runtime settings validator: zero errors.

## Safety contract

- `DclSyntheticReactionLogOnly=true`.
- Accepted-order rewrite remains log-only.
- No synthetic carrier word, accepted order, effect, cadence, HP, MP, status, attribute, save, or
  data-table write is enabled.
- `FFT_enhanced.exe` must be stopped for every backup, fixture restore, deploy, and final restore.
- Close Reloaded-II before replacing the installed DLL, PDB, or settings.
- Back up and hash the installed DLL, PDB, settings, Reloaded AppConfig, game log, and live autosave.
- Stop immediately on an expected-byte mismatch, hook-install failure, unexpected live write, or
  missing fixture owner.
- Close the game without saving after the bounded event, archive the new log, restore all six
  pre-test artifacts, and verify every SHA-256.

## Fixture and route

1. Restore the carrier-443 autosave fixture while the game is stopped.
2. Launch through Reloaded-II's official `--launch` shortcut.
3. Use the runbook's atomic **Enhanced > Enter intro skip > Continue** burst.
4. Rion starts on an actionable ally turn. Move toward the enemy cluster and end with **Wait** until
   an enemy lands one nonlethal hit on Rion. Do not execute any save command.
5. Capture only enough events to prove the gate and pre-selector intent, then close with `Alt+F4`.

## Required positive evidence

- One installed pre-selector hook line containing `synthetic=log-only:carrier=443`.
- At least one `[DCL-SYNTHETIC-REACTION-GATE]` row with `carrier=443`, `accepted=1`, and
  `mailbox=armed`.
- At least one matching `[DCL-REACTION-PRESELECT]` row with
  `producer=synthetic-would-stage` and carrier `443`.
- Zero `synthetic-staged`, carrier-443 materialization, native commit, managed commit, hook failure,
  skip, or mailbox-loss rows.

## Validation

```powershell
python tools\analyze_dcl_synthetic_reaction_live.py <archived-log> --carrier 443 --mode log-only
```

The analyzer must return PASS. A clean miss, a lethal defender, or an unrelated source without an
accepted gate is a valid negative observation but does not satisfy the positive gate.

## Decision

PASS proves the read-only successful-result-to-pre-selector vertical. It permits one later bounded
live-write test of carrier production, accepted-order delivery, and producer-owned cadence commit.
Failure keeps all synthetic writes disabled and returns the investigation to the exact missing
boundary shown by the log.
