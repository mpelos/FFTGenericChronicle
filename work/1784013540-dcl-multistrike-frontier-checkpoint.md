# DCL multistrike frontier checkpoint

## Scope

This pass continued the ability-mechanism closure after the paired HP/MP drain audit. It changed no
installed runtime settings, did not launch the game, and did not touch saves.

The user independently confirmed that FFT launches normally through Reloaded after choosing the
Reloaded path. The remaining launch/input problem is isolated to Codex's privileged Computer Use
channel and is recorded in `work/1783908306-fft-autonomous-control-runbook.md`.

## Reproducible current-build result

`tools/analyze_dcl_multistrike_transactions.py` now verifies the current executable and catalog.
Latest report: `work/1784013525-dcl-multistrike-native-carrier-analysis.md`.

All anchors pass against Enhanced executable SHA-256
`841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`.

- Pummel/formula `0x32` computes `floor(random15 * X / 32768) + 1` and multiplies one staged HP
  debit. Vanilla Pummel is one aggregate result, not N native result commits.
- Formulas `0x1E`, `0x5E`, and `0x5F` alias the same one-result MA handler. The ability-action
  `RandomFire` flag, not the formula handler, distinguishes repeated outer targeting.
- Celestial Void, Corporeal Void, and Dark Whisper set `RandomFire`; Nanoflare clears it.
- Nanoflare is a single-hit MA action and no longer belongs to the multistrike technical frontier.
- Barrage/formula `0x6A` performs one equipped-weapon formula dispatch and then enters the protected
  normal-attack postprocessor. Neither the handler nor that postprocessor contains the strike loop;
  any Barrage repetition is owned by an outer protected layer.
- Aligned real-code and current-action scratch xrefs expose no consumer of raw action byte `+4`
  bit `0x08`. Exact RandomFire/Barrage cardinality and apply order cannot be promoted from static
  evidence alone.

## Classification change

The 512-row classification now has:

| Scope | Count |
| --- | ---: |
| closed | 332 |
| design | 175 |
| mixed | 5 |
| technical | 0 |
| reverse-engineering | 0 |

The remaining mixed records are Pummel, Celestial Void, Corporeal Void, Dark Whisper, and Barrage.
Latest candidate/template/coverage set uses prefix `1784013532`.

The approval-gated DCL metadata overlay now includes optional `strike_count`. It is exposed to the
runtime formula DSL as `ability.dcl.strike_count`, defaults to zero, rejects negative approved
values, and never infers a count when the authoring field is empty.

## Validation

- Ability-classification smoke: pass.
- Multistrike executable/catalog anchors: pass.
- C# build: 0 warnings, 0 errors.
- Formula/runtime smoke suite: pass.
- Full `codemod/run-offline-checks.ps1`: pass, including all Python tooling, installed-executable
  static scan, settings validators/simulators, C# build/smoke, helper dry runs, and whitespace gate.

## Remaining minimal live evidence

Do not invent a strike id from HP, elapsed time, or target phase bytes. Capture actual ordering:

1. For dual wield or a RandomFire action, count calc-entry, pre-clamp, selector, Guard commit, and
   apply events in order.
2. For Barrage, capture repeat count, target stability, active hand/weapon formula, and whether each
   repeat reaches native result apply separately.
3. Use the observed ordering to choose among reuse-after-retirement, a real execution/strike
   generation, or per-pre-clamp decision generation.

Pummel does not need a native-cardinality discovery test. Its required implementation is a managed
single-target aggregate carrier: authored independent contests and Guard spends produce one summed
staged debit, while reactions remain capped once per Pummel action. The authoring surface exists;
`DclMultistrike.AggregatePhysical` now owns and smoke-tests the pure aggregation contract: landed
normal/critical strikes sum independently, successful defense cancels only its strike, failed or
successful finite-defense attempts are counted for Guard spending, and an all-miss action produces
zero debit. Runtime contest generation, atomic Guard commit, and live integration remain.
