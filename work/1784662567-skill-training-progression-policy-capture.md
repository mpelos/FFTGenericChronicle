# Skill training progression and policy capture

Scope: offline DCL contract work for Weapon Skill aptitude/Rank inputs.

Conclusion: canonical unit snapshots now retain the native progression slice required by the Skill
training contract: JobId, JobIndex, spendable JP, total JP, and derived Job Level. The Job Level
threshold helper is shared by the formula context and the canonical Skill rules.

The job/family Aptitude Tier is represented as an explicit policy map keyed by `(JobId,
SkillFamily)`. The map normalizes SkillFamily text and fails closed when a selected weapon family
has no authored row. This is a technical mechanism only; it does not choose final job design values.

The native physical composer can consume the action-level policy map and derive each Strike's
retained `DclSkillTrainingResult` from synchronized source progression plus selected weapon
metadata.

Validation target:

- `DclCanonicalProgressionSnapshot`
- `DclCanonicalNativeSnapshotAdapter.ProjectProgression`
- `DclSkillRules.NativeJobLevelFromTotalJp`
- `DclSkillTrainingPolicy`
- `DclCanonicalCombatSnapshotProjector.ResolveWeaponSkillTraining`
- `DclCanonicalNativePhysicalExecutionComposer.Compose`
- smoke-test sentinels in `TestDclPhysicalRules` and `TestDclCanonicalRuntime`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

Remaining work: author the approved non-draft job/family Aptitude Tier table and live-prove the
native JP offsets under the integrated admission path.
