# Reaction activation reference closure

Scope: offline DCL contract work for Brave reaction inputs.

Conclusion: canonical Reaction activation no longer has a permissive enum path. `ActivationRoll`
definitions validate against the explicit DX/HT/IQ/Will/named-Skill set; unknown or future enum
values fail before execution or forecast. This closes the offline authoring route that could have
let raw Brave return as a canonical Reaction chance input.

Validation target:

- `DclReactionContract.Validate`
- smoke-test sentinels in `TestDclReactionContract`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

Remaining live/native work: bind canonical Reaction candidate production/evaluation to the native
post-action window and keep the historical courage/caution Brave taxonomy out of the integrated DCL
profile.
