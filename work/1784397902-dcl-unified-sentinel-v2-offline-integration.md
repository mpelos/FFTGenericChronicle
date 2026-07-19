# DCL unified sentinel v2 offline integration

## Scope

This checkpoint composes the latest job-free combat mechanisms into one exact offline-validation
bundle. It does not assign production jobs, abilities, or balance values.

## Result

- The v2 settings compose the previous unified sentinel, the exclusive Don't Move/Don't Act
  duration mechanism, and one natural-roll Interrupt rule carried technically by Potion `368`.
- The paired action-data artifact suppresses native riders for abilities `126`, `131`, `219`,
  `252`, `277`, and `357` while preserving the existing KO, support, conditional, and multistrike
  ownership contracts.
- The duration pair binds the two-row `StatusEffectData` counter patch to all fourteen managed add
  producers. Remove rules do not create duration ownership and are excluded from that requirement.
- The runtime/data validator binds the settings, action data, metadata, item sidecar, charge data,
  duration pair, Interrupt rule, synthetic owner/delivery pair, Reaction rules, managed multistrike,
  and atomic HP/MP contract.

## Taunt ownership correction

Berserk ability `241` uses formula `0x0A`. Its conditional handler can skip the ordinary native
packet finalizer, so its DCL Taunt packet policy is `replaced-post-calc`, not
`retained-as-carrier`. Native Berserk still owns the ongoing forced-aggression state after the
managed packet is applied. The Taunt analyzer and unified settings were regenerated with that
split ownership.

## Exact v2 artifacts

- settings: `work/1784397292-battle-runtime-settings.dcl-unified-sentinel-v2.json`
  - SHA-256: `DC9853CE65DB1818F4FBBA323C4661A5F063E03717A77FC7F4F038216CD99E73`
- action data: `work/1784397292-dcl-unified-sentinel-v2-overrideabilityactiondata.nxd`
  - SHA-256: `44B1E65F33FA5AF1C0A075645B898C5BDCC543F5D2DDF832017571B5C12741A9`
- status-duration pair: `work/1784397292-dcl-unified-sentinel-v2-status-duration-pair.json`
- runtime/data pair: `work/1784397292-dcl-unified-sentinel-v2-runtime-data-pair.json`
- composition manifest: `work/1784397292-dcl-runtime-composition-manifest-v2.json`

The isolated SQLite/NXD source round-trips exactly through FF16Tools. Settings validation reports
zero errors; the remaining warnings are expected technical-carrier warnings. Both nested pair
validators pass.

## Remaining integration boundaries

- Fear stays isolated. Its current Basic Attack `0` carrier conflicts with the core physical
  sentinel, and expanded AoE target authority still requires a valid instrumented Fire run.
- Approach stays isolated. It shares synthetic owner `443` and delivery carrier `442`; integration
  requires one explicit coordinator so cadence and delivery cannot be owned twice.
- The Fire execution observed before this checkpoint is inconclusive because only the Reloaded
  utility mod loaded; neither Generic Chronicle package participated and the probe logged no Fire
  action.

## Verification

- Focused Taunt, integration-data, runtime-pair, status-duration, frontier, composition, coverage,
  and timeless-documentation checks pass.
- The complete `codemod/run-offline-checks.ps1` gate passes: 1,461 output lines in 114.4 seconds,
  including Python syntax/tooling, FF16Tools fixture round-trips, .NET build/smoke/settings checks,
  hook scanners, documentation checks, dry-run deploy checks, and Git whitespace validation.
