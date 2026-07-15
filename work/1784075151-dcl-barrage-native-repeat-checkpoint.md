# DCL Barrage native-repeat checkpoint

## Static result

The repeat initializer shared with RandomFire has an explicit formula-`0x6A` branch at
`0xEED0E0A`. It stores `4` in repeat-count RVA `0x7B0762` and enters the same result producer whose
continuation path increments repeat-index RVA `0x7B0763`.

Barrage clears action `RandomFire`, so the dedicated random-tile selector is not dispatched. Its
original single target remains selected. Each result calculation enters formula `0x6A`, delegates
to the equipped-weapon formula, and then enters the ordinary normal-attack postprocessor.

**Strong:** Barrage is a target-stable four-result native weapon transaction. It is neither a
single aggregate multiplier nor a TableData animation/effect sequence. Live testing now confirms
downstream pre-clamp/apply, active-hand, presentation, and reaction cadence; it does not discover
the count or repetition owner.

## Metadata correction

The prior conservative candidate labeled every multihit record `managed_multistrike`. That label is
unsafe for native repeated-result carriers: approving Barrage with `strike_count=4` could create
four managed contests inside each of four native results.

The metadata vocabulary now separates:

- `managed_multistrike`: DCL aggregates authored strikes into one native result, as for Pummel;
- `managed_multistrike_status_rider`: the same managed aggregation plus managed rider ownership;
- `native_multistrike`: engine repetition remains authoritative, as for Barrage and status-free
  RandomFire actions;
- `native_multistrike_status_rider`: engine repetition remains authoritative while DCL owns the
  rider packet, as for Celestial Void, Corporeal Void, and Dark Whisper's mixed policy boundary.

Only managed policies accept authored `strike_count >= 2`. Native policies require
`strike_count=0`, expose `IsNativeMultistrike`, and never enter the aggregate physical route.
Barrage therefore receives one ordinary physical contest per native result rather than 4x4
duplication. RandomFire keeps spell-level Magic Evade per target through its dedicated retention
rule and a fresh status plan per result.

The ability-classification audit now reports all 512 records as either candidate-complete or blocked
only by design authoring: `332 closed`, `180 design`, `0 technical`, and `0 mixed`.

## Validation

- Formula-`0x6A` fixed-four byte anchor: PASS.
- Multistrike carrier analyzer: PASS.
- Ability-classification smoke test: PASS.
- Metadata loader/variable smoke tests: PASS.
- C# build: PASS, zero warnings and zero errors.
- C# smoke tests: PASS.
- Whole-DCL coverage audit: PASS, 30 mechanisms.
- Full `codemod/run-offline-checks.ps1`: PASS in 47.4 seconds.

Current evidence:

- `work/1784075150-dcl-multistrike-native-carrier-analysis.md`
- `work/1784075039-dcl-ability-classification-candidates.md`
- `work/1784075039-dcl-ability-classification-candidates.csv`
- `work/1784075039-dcl-ability-metadata-authoring-template.csv`
- `work/1784075150-dcl-implementation-coverage.md`

No live test or deployment was used. Reloaded-II remains on the byte-identical LT23 settings hash
`BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`.
