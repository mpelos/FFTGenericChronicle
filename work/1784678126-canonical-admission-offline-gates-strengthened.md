# Canonical admission offline gates strengthened

## Scope

This note records the offline gates added for the canonical admission sentinel after the live probe exposed two probe-readiness gaps:

- native HP/MP pool synchronization must be an explicit template opt-in for the sentinel, not a global native adapter inference;
- the sentinel item bundle must include canonical metadata for every equipped native item observed in the live-save source/target rows.

## Offline evidence

- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs` now checks that a policy-ticket template without `SynchronizeNativePools` keeps `NonEquipmentSecondary` absent.
- The same smoke path checks that `SynchronizeNativePools: true` materializes secondary inputs from deliberately divergent native pools and lets `ProjectUnit` preserve native MaxHP/MaxMP/current HP/current MP.
- The item metadata smoke path now projects a live-save-like equipment block with native ids `30`, `167`, `207`, and `216`, preserving the expected weapon/head/body/accessory slots before snapshot batching.
- The previous fail-closed behavior remains covered: an equipped native item without canonical metadata still throws before combat input construction.

## Gates run

- `dotnet test codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj --no-restore`
- `python tools\test_dcl_canonical_admission_template_live.py`
- `python tools\test_prepare_canonical_admission_live.py`
- `python tools\test_dcl_canonical_admission_probe_readiness.py`
- `python tools\validate_dcl_live_proof_sequence.py`
- `python -m json.tool work\1784673033-dcl-policy-ticket-templates.json`
- `python -m json.tool work\1784673033-dcl-items.json`

All gates passed.

## Current conclusion

The canonical admission sentinel is offline-ready for the next clean live proof. The remaining live proof is to execute the Fire sequence from the autosave and collect one log containing MPLOSS plus a completed `[DCL-CANONICAL-ADMISSION]` line with no canonical admission error.
