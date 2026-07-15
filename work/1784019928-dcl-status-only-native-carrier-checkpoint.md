# DCL status-only native-carrier checkpoint

## Hypothesis

A status-only action must not delete the native rider that causes its formula to produce an
applicable result. If its execution reaches the ordinary state apply and pre-clamp, DCL can keep the
rider as a carrier, replace every inherited status bit in the paired packet, and preserve the game's
own validation, presentation, and commit.

## Offline result

The formula-`0x22` family is the first closed subset:

- dispatch target `0x307DE4` calls the shared status-result builder at `0x306558` and finalizer at
  `0x306988`;
- the execution state-`0x15` path calls state apply at `0x30A484`;
- state apply reaches pre-clamp `0x30A5D7` without testing for positive HP/MP channels;
- formula `0x22` has exactly Kiyomori `81` (Protect/Shell) and Masamune `84` (Regen/Haste) in the
  catalog.

## Implemented boundary

`NativeRiderPolicy=retained-as-carrier` is accepted only for ids `81` and `84`. Validation requires
`ActionType=-1` and complete ownership of their native packet bits:

- Kiyomori: byte 3, masks `0x20` and `0x10`;
- Masamune: byte 3, masks `0x40` and `0x08`.

An incomplete set or any other ability fails closed. Action data must leave the rider intact. At
pre-clamp, each managed contest independently clears and re-adds its owned bit, so a resisted or
immune outcome removes the inherited native result rather than allowing the vanilla contest to leak
through. Unrelated packet bits remain untouched. Conditional rules are rejected; a runtime match or
resistance evaluation error clears the inherited bit and logs `outcome=fail-closed`.

## Verification

- Pure packet smoke covers independent replacement of Protect/Shell in a retained carrier.
- Validator smoke covers complete Kiyomori ownership, an incomplete carrier, and an unsupported id.
- `tools/analyze_dcl_support_transaction.py --check-only` passes all current-executable anchors and
  is part of the full offline runner.
- `work/1784019928-battle-runtime-settings.dcl-status-only-carrier-mechanism.json` owns both complete
  formula-`0x22` carriers and is intended for offline validation until a suitable live fixture is
  identified.

## Remaining gate

One formula-`0x22` execution must prove exactly one managed pre-clamp callback with all HP/MP channels
zero and a matching native status commit. No additional hook or data mutation is justified before
that observation. Other status-only families remain excluded until their apply/cadence path receives
the same static treatment.

Formula `0x0A/0x0B` receive that static carrier audit in
`work/1784020246-dcl-status-roll-carrier-analysis.md`. Their handlers conditionally skip the common
packet finalizer on native failure, and the legacy shared-builder chance hook does not cover either
handler. They therefore need an exact-action, per-target decision producer before finalization plus
forecast/AI/execution provenance; extending the retained-carrier allowlist alone would be incorrect.

The latest complete offline regression passes in 38.9 seconds with the support-transaction and
status-roll carrier analyzers as required gates, a clean C# build, all smoke tests and simulators,
27 established profiles, executable anchor scans, and deploy-helper dry runs. No files were deployed
to Reloaded.
