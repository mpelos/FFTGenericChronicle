# Weapon Skill training to Strike binding

Scope: offline DCL contract work for Weapon Skill aptitude/Rank inputs.

Conclusion: physical Strike input can carry the resolved `DclSkillTrainingResult`. When present,
canonical physical execution and evaluation validate that this retained training result matches the
Strike's base weapon skill. Ranged Strikes compare against `BaseWeaponSkill` before distance, Aim,
location, and state modifiers produce `EffectiveAttackSkill`; simple melee Strikes compare against
the supplied effective skill.

Validation target:

- `DclCanonicalPhysicalStrikeInput`
- `DclCanonicalPhysicalExecutor.ResolveAttackSkillAndRangedDefense`
- smoke-test sentinels in `TestDclCanonicalRuntime`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

Remaining work: author the non-draft job/family Aptitude Tier policy and bind native snapshot
construction to produce this retained training object from live unit/job/equipment inputs.
