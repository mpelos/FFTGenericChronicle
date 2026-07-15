# DCL synthetic-Reaction scope correction

## Scope decision

The technical DCL implementation excludes jobs, job abilities, names, effects, and final balance
values. The earlier carrier-443 vertical incorrectly embedded the draft job ability Hex Ward. That
vertical is retired as an implementation target. Historical reports remain in `work/` as evidence of
the investigation, but they are not current specifications or deployment profiles.

## Runtime correction

- `DclHexWard.cs` is replaced by carrier- and effect-agnostic
  `DclSyntheticReaction.cs`.
- Runtime settings are now `DclSyntheticReaction*`; no carrier id is enabled by default.
- The only currently owned synthetic trigger is explicitly named
  `successful-hit-survivor`.
- The configured carrier may use any validated courage, caution, or neutral taxonomy rule.
- The successful-result owner requires an exact equipped carrier, valid non-self source/action,
  known landed hit, surviving defender, and unused attacker-action cadence.
- The dynamic producer never overwrites a nonempty native `unit+0x1CE` slot and rejects inactive,
  zero-HP, or effectively KO units.
- Accepted-order transformation owns action replacement or source retargeting.
- Exact producer-owned pass-2 acceptance consumes cadence once.
- The synthetic transaction does not write Blind, Brave, status, stats, or any other managed effect.
  Native execution of the guarded accepted order owns delivery.

## Offline evidence

- `tools/test_dcl_synthetic_reaction_transaction.py`: PASS.
- C# Release build: zero warnings and zero errors.
- C# smoke tests: PASS after rebuilding the smoke-test project.
- `work/1784152418-battle-runtime-settings.synthetic-reaction-logonly.json`: validator PASS with
  warnings only for intentionally enabled observe-only probes.
- `work/1784152557-dcl-implementation-coverage.md`: 28 combat-infrastructure mechanisms; jobs and
  final content values are excluded from the matrix and no longer count as technical blockers.

## Remaining gate

The composed synthetic-Reaction transaction remains `partial-live-gated`. The smallest live proof is
a neutral carrier profile: first log-only, then one bounded carrier production/accepted-order delivery
transaction with wrong-source, duplicate-callback, duplicate-commit, lethal-defender, and occupied-slot
controls. This is an engine-mechanism proof, not an ability implementation.
