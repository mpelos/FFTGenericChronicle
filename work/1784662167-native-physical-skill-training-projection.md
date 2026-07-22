# Native physical Skill training projection

Scope: offline DCL contract work for Weapon Skill aptitude/Rank inputs.

Conclusion: the native physical composer can project a retained `DclSkillTrainingResult` from the
synchronized source snapshot, selected weapon metadata, and explicit policy inputs. The policy inputs
are `DclAptitudeTier`, native Job Level, and an explicit post-training skill modifier. The composer
does not infer job design content.

The composer validates that the projected final training score equals the supplied base weapon skill
before creating each physical Strike. The Strike then carries that retained object into canonical
execution/evaluation validation.

Validation target:

- `DclCanonicalCombatSnapshotProjector.ResolveWeaponSkillTraining`
- `DclCanonicalNativePhysicalExecutionComposer.Compose`
- `DclCanonicalPhysicalStrikeInput.WeaponSkillTraining`
- smoke-test sentinels in `TestDclCanonicalRuntime`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

Remaining work: author the approved non-draft job/family Aptitude Tier policy and bind live native
Job Level/equipment-family capture to the explicit composer inputs.
