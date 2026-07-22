# Canonical admission live attempt exposed missing `use64`

Live attempt scope: deploy the canonical-admission sentinel, launch Enhanced through Reloaded-II,
load Manual Save 05, enter Mandalia Plain, and attempt to produce one ordinary Fire action.

Useful result: the live log did not reach the Fire proof, but it exposed a prior installation
failure for the admission hook:

- Runtime settings loaded with `DclCanonicalRuntimeEnabled=True` and
  `DclCanonicalAdmissionEnabled=True`.
- Canonical runtime loaded the authored files and one policy-ticket template.
- The battle reset logged `canonicalBattle=1`.
- The hook failed before installation:
  `DCL-CANONICAL-ADMISSION-FAILED FasmException ... IllegalInstruction, Line Number: 1`.

Offline diagnosis: the new canonical shim builders started their FASM blocks at `push ...` instead
of the repository-standard `use64` directive. Existing successful shims include `use64` first.

Implemented correction:

- Added `use64` to:
  - `BuildDclCanonicalAdmissionShimLines`
  - `BuildDclCanonicalPostApplyShimLines`
  - `BuildDclCanonicalReactionEffectCompletionShimLines`
  - `BuildDclCanonicalReactionCompletionShimLines`
- Added a static analyzer test assertion so these canonical FASM blocks must retain `use64`.

Artifacts:

- Raw log: `work/1784686740-raw-canonical-admission-live.log`
- Failed analysis: `work/1784686740-canonical-admission-template-live-analysis.md`

Verified after correction:

- `python tools\test_dcl_canonical_admission_template_live.py`
- `python -m py_compile tools\test_dcl_canonical_admission_template_live.py`
- `dotnet test codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj --no-restore`

Next live attempt must redeploy the corrected build, relaunch cleanly, and execute a real ordinary
Fire action. The current attempt does not prove the canonical-admission bridge.
