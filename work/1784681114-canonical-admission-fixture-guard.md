# Canonical admission fixture guard

## Context

The canonical-admission live proof should start from a verified pre-action battle state. Manual Save
05 is only a world-map baseline for constructing that state, and an unlabelled autosave snapshot is
too easy to confuse with a post-action or unrelated fixture.

## Change

- `tools/manage_fft_enhanced_autosave.ps1` can now write snapshot sidecar metadata with
  `-FixtureKind` and `-FixtureLabel`.
- The same helper can require fixture metadata during restore with `-RequireFixtureKind`.
- `tools/launch_fft_enhanced_test.ps1` can pass a required autosave fixture kind through to the
  autosave restore helper.
- `codemod/prepare-canonical-admission-live.ps1` accepts `-AutosaveSnapshot` and restores it only
  if its sidecar metadata is `canonical-admission-pre-action`.
- The canonical-admission runbook now names `canonical-admission-pre-action` as the fixture kind for
  this proof and requires the live hook log at `0x281EFA`.
- `work/1784092904-fft-autoenhanced-snapshot.png` is now tagged with that fixture kind. Existing
  journal evidence identifies it as Josephine Black Mage's actionable Mandalia Plain command-menu
  state, and the SHA-256 matches the recorded fixture hash.

## Validation

- `python tools\test_prepare_canonical_admission_live.py`
- PowerShell parser check for:
  - `tools\manage_fft_enhanced_autosave.ps1`
  - `tools\launch_fft_enhanced_test.ps1`
  - `codemod\prepare-canonical-admission-live.ps1`
- `python tools\test_dcl_canonical_admission_template_live.py`
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj --no-restore -- --test-dcl-canonical-runtime`
- `python tools\test_dcl_canonical_admission_probe_readiness.py`
- `python tools\validate_dcl_live_proof_sequence.py`

## Remaining live-proof blocker

The guard prevents the wrong autosave from being restored as the proof fixture. A clean
canonical-admission proof still requires creating or locating a real mid-battle pre-action snapshot
and tagging it with:

```powershell
tools\manage_fft_enhanced_autosave.ps1 -Action Snapshot `
  -FixtureKind canonical-admission-pre-action `
  -FixtureLabel '<acting unit / target / menu state>'
```
