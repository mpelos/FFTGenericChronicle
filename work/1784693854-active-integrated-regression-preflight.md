# Active integrated regression preflight

## Result

The active job-free integrated runtime/data pair and live-regression matrix now validate offline.

## Artifacts

- Runtime/data pair: `work/1784683300-dcl-active-integrated-runtime-data-pair.json`
- Live-regression matrix: `work/1784683300-dcl-active-integrated-live-regression-matrix.json`
- Live proof sequence: `work/1784674066-dcl-live-proof-sequence.json`
- Coverage refresh: `work/1784694079-dcl-implementation-coverage.md`
- Promoted doc correction: `docs/modding/06-code-mod-runtime-dsl.md`

## Findings

- The historical clean-v1 runtime/data pair fails current validation through its nested status-duration pair.
- The failure is not only a stale hash: the current duration-counter frontier rejects generic Disable/Immobilize counter transfer while unresolved producers remain.
- The active integrated pair therefore deliberately omits `status_duration_pair`.
- `docs/modding/06-code-mod-runtime-dsl.md` now defines generic Disable/Immobilize counter transfer as fail-closed/inactive until complete producer ownership exists.
- Retired `DclApproach*` and `DclFear*` controls remain forbidden in the active path.
- Live-install validation intentionally remains failing until the active bundle is applied: installed settings hash `A0D5C6709965F157D932AE8833B8D5A97440FDE6823B8600F00BB1AD8C7D822A` differs from desired `F9C3A5BC2B70A07AF75AA25C52DA232FC320275A36362A270C70791BF6939830`, and installed DLL hash `4CF4F6B64A714CE45F356FD7A4DBE1C056AC00E57AF215CBC4613C2BC45ECA7A` differs from current Release DLL `BB6217087B3C80D22E6575448B09069B6F7D176D63483B9248D5A5569F30FBC9`.
- `validate_dcl_live_install.py`, `install_dcl_live_bundle.py`, and `validate_dcl_live_regression_matrix.py` now default to the active integrated pair/matrix rather than the historical clean-v1 artifacts. Historical clean-v1 remains explicitly named only as an inactive archive.

## Validation

- `python tools\validate_dcl_runtime_data_pair.py work\1784683300-dcl-active-integrated-runtime-data-pair.json`
- `python tools\validate_dcl_live_regression_matrix.py work\1784683300-dcl-active-integrated-live-regression-matrix.json`
- `python tools\validate_dcl_live_proof_sequence.py`
- `python tools\analyze_dcl_status_duration_frontier.py --check-only`
- `python tools\test_validate_dcl_runtime_data_pair.py`
- `python tools\test_validate_dcl_live_regression_matrix.py`
- `python tools\test_validate_dcl_live_proof_sequence.py`
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj --no-restore -- --test-dcl-canonical-runtime`
- `python tools\report_dcl_implementation_coverage.py`
- `python tools\install_dcl_live_bundle.py --pair work\1784683300-dcl-active-integrated-runtime-data-pair.json`
- `python tools\validate_dcl_live_install.py --pair work\1784683300-dcl-active-integrated-runtime-data-pair.json --allow-extra-mods`
- `python tools\validate_dcl_live_regression_matrix.py`
- `python tools\install_dcl_live_bundle.py`
- `python tools\test_validate_dcl_live_install.py`
- `python tools\test_install_dcl_live_bundle.py`
- `python tools\test_validate_dcl_live_regression_matrix.py`

## Next implication

The integrated regression is no longer blocked by missing active manifests. The installed NXD/XML/catalog data already match the active pair, but the installed runtime settings and code-mod DLL do not. The remaining live gate is applying the active bundle, validating the install, then collecting per-case evidence from the matrix.
