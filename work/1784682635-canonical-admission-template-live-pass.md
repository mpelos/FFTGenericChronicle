# Canonical admission template bridge live pass

## Result

The canonical admission/template bridge proof passed with:

- raw live log: `work/1784693279-raw-canonical-admission-live.log`
- analysis: `work/1784693279-canonical-admission-template-live-analysis.md`
- hook RVA: `0x281EFA`
- ability: `16`
- target count: `1`
- strikes: `1`
- matching ActionInstance: `1`
- statuses: `Published` admission, `Built` template, `Published` ticket, `Published` bridge
- canonical admission errors: `0`

The proof used the tagged autosave fixture:

- `work/1784092904-fft-autoenhanced-snapshot.png`
- `work/1784092904-fft-autoenhanced-snapshot.png.fixture.json`
- fixture kind: `canonical-admission-pre-action`

## Corrections made during the proof

- The selected-unit global at `0x7B0792` can diverge from the admitted TargetBatch for a
  unit-targeted Fire admission. The TargetBatch is the declared-target authority for DirectNumeric
  unit-target composition.
- The single-target magic policy provider and confirmed native request composer now use the complete
  TargetBatch/frozen declared-target tile contract instead of requiring `selectedUnit == target`.
- The admission hook now ignores unbound native abilities. A later enemy ability `280` should not
  disable the Fire admission proof profile.
- The collector's default gate now validates the bridge proof without requiring a later HP-damage
  sampler line. `--require-damage` remains available for stricter delivery-adjacent runs.
- The autosave helper/launcher/prep path now supports fixture metadata and the canonical proof
  snapshot is tagged as `canonical-admission-pre-action`.

## Validation

- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj --no-restore -- --test-dcl-canonical-runtime`
- `python tools\test_collect_dcl_canonical_admission_live_log.py`
- `python tools\test_dcl_canonical_admission_template_live.py`
- `python tools\test_prepare_canonical_admission_live.py`
- `python tools\collect_dcl_canonical_admission_live_log.py`

## Next implication

The DirectNumeric template bridge is live-proven for one ordinary Fire admission. Broader DCL
implementation still needs production policy-source capture and representative live regressions for
the other family templates and native-rich facts.
