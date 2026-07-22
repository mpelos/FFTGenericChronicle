# Status-duration documentation realignment

## Context

The current status-policy catalog leaves most shared Disable/Immobilize producers in
`unresolved-nature`. Only ability ids `213/214` are explicit physical Stun/Knockdown candidates for
those native flags.

## Work completed

- Updated `docs/modding/08-dcl-information-requirements.md` so it no longer presents the historical
  Disable/Immobilize duration-transfer pair as current truth.
- Reworded the status nature requirement around the current categories: physical-health/HT,
  mental-Will, beneficial, lifecycle, campaign, and explicit special policy.
- Kept native counter transfer fail-closed unless every producer for a timed status has supported
  category and owned duration policy.
- Reworded stale tooling text that still referred to base-HP and mental/Brave status categories.

## Validation

- `python tools\check_docs_timeless.py docs\modding docs\deep-combat-layer`
- `python tools\test_dcl_status_duration_pair.py`
- `python tools\test_dcl_status_duration_frontier.py`
- `python tools\analyze_dcl_status_duration_frontier.py --check-only`
- `python tools\test_dcl_ability_classification.py`
- `rg "magical/inverse-Faith|mental/Brave|base-HP|all fourteen|validated Disable/Immobilize|exclusive duration ownership is implemented|Disable/Immobilize pair owns" docs\modding docs\deep-combat-layer tools -n`
- `python tools\report_dcl_implementation_coverage.py --check`

All passed.
