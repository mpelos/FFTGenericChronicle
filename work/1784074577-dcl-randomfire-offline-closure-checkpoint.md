# DCL RandomFire offline-closure checkpoint

## Scope

This checkpoint starts from the last pending special-status probe and exhausts the current-build
offline evidence for the native RandomFire carrier before another live test.

## Static mechanism result

- Action byte 4 bit `0x08` is consumed at `0xEEBC6ED` and dispatches selector `0x2826B0`.
- The selector clears target-map bit `0x80` and sets it for exactly one eligible tile.
- Result producer `0x281CE8` invokes target selection inside its repeat and calls ordinary
  calculation `0x3099AC` once for the selected target.
- Native repeat count is byte RVA `0x7B0762`; incremented repeat index is byte RVA `0x7B0763`.
- The producer increments the index, compares it with the count, and publishes continuation at
  `0x2821EC`.
- Formulas `0x1E/0x1F` choose 1..10 repeats from weights
  `5,5,10,10,20,20,10,10,5,5` at RVA `0x9069D0`.
- Formula `0x5E` uses `X+1`: three repeats for Tri-Thunder/Tri-Flame and six for Dark Whisper.

The native carrier is therefore one random target plus one ordinary calculation per repeat, not one
aggregate calculation. Native result application still needs a live integration confirmation, but
the repetition mechanism is no longer unknown.

## DCL implementation result

- `DclNativeRepeat` mirrors the shared native counter contract and exact repeat distributions.
- Every DCL hit-decision consumer checks native continuation. A RandomFire target's cached
  spell-level Magic Evade decision survives intermediate repeats and retires after the final repeat.
- The cache remains target-keyed: a different selected target gets its own decision, while selecting
  the same target again reuses the original spell-level decision.
- Formula `0x1E/0x1F` status actions require the catalog's native RandomFire flag and enter the
  execution-only post-calc producer.
- Celestial Void `173` and Corporeal Void `179` each own the exact seven-bit catalog packet as one
  `random-one` group. A fresh status plan is produced per repeated result; status choice/resistance
  is not retained merely because the same target repeats.
- The exact 150-action status inventory now has one mechanism owner per action, zero unowned rows,
  and no remaining `special-transaction` category.

Dark Whisper `344` remains under its managed instant-KO/lifecycle owner because its payload mixes
Dead with Sleep; the generic packet producer must not write lifecycle-owned Dead.

## Validation

- C# build: PASS, zero warnings and zero errors.
- C# smoke tests: PASS, including repeat retention, Truth/Untruth percentile boundaries, formula
  `0x5E` counts, exact Celestial Void packet ownership, and Corporeal Void family membership.
- `analyze_dcl_multistrike_transactions.py`: PASS.
- `analyze_dcl_status_conditional_producer.py`: PASS.
- `analyze_dcl_status_authority.py`: PASS.
- Whole-DCL coverage reporter: PASS, 30 mechanisms.
- LT34 settings validation: PASS, zero errors.
- Full `codemod/run-offline-checks.ps1`: PASS after the forced PC restart; elapsed 47.6 seconds.

Generated evidence:

- `work/1784074309-dcl-multistrike-native-carrier-analysis.md`
- `work/1784074309-dcl-status-conditional-producer-analysis.md`
- `work/1784074309-dcl-status-action-authority.md`
- `work/1784074309-dcl-status-action-authority.csv`
- `work/1784074257-battle-runtime-settings.lt34-dcl-randomfire-status-producer.json`
- `work/1784074258-lt34-dcl-randomfire-status-producer-live-plan.md`

## Installed-state safety

The offline suite does not deploy. The Reloaded-II installation remains on the known-stable LT23
profile:

- installed settings and LT23 SHA-256:
  `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- installed DLL SHA-256:
  `9A225141E3D115531941092B74E8E3FA1B5D7A616C3525180F41B052FF3DE8E9`

LT34 is prepared in `work/` and is not deployed.

## Remaining live gate

Use LT34 only when Celestial Void is naturally available. Confirm the mapped selector, pre-clamp,
HP/status apply, and presentation order; capture a repeated target if RNG provides one. Absence of
the action in Manual Save `05` is a deferred observation, not evidence against the mechanism.
