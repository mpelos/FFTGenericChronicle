# DCL status-duration frontier and counter-build gate

## Scope

This checkpoint audits the complete job-free status layer without assigning jobs or choosing open
balance values. It separates technical status producer authority from exclusive duration authority.

## Native frontier

`tools/analyze_dcl_status_duration_frontier.py` joins the 40-row native `StatusEffectData` table,
the 294-row ability/status policy expansion, and validated runtime/XML duration-pair manifests.
The generated evidence is:

- `work/1784396741-dcl-status-duration-frontier.md`;
- `work/1784396741-dcl-status-duration-frontier.csv`.

The current Enhanced table has 16 nonzero counters. The exact disposition is:

- 2 exclusive DCL-owned rows: Disable and Immobilize;
- 12 ordinary timed rows still blocked on complete duration ownership;
- Doom remains the engine-owned countdown-to-KO lifecycle;
- Empty_32 remains a system row and is not a generic duration carrier.

The Disable/Immobilize pair validates all fourteen ability/status add producers across twelve
abilities. The physical Stun/Knockdown producers use base HP and one target turn. The magical
producers use inverse target Faith plus caster MA. Their fixture duration is technically complete
but its final numeric calibration remains open.

## Explicit design/mechanism gaps

The DCL specifies the major category axes but does not select a harmful-status nature for Darkness,
Silence, BloodSuck, Oil, or Undead. Disease is named as a physical/base-HP status but has no assigned
native carrier, producer mapping, or ongoing-effect mechanism. These are job-independent inputs and
cannot be resolved by reverse engineering alone.

## Fail-closed construction

`tools/build_dcl_status_counter_patch.py` now requires runtime settings before it writes XML. It
invokes the complete duration-owner validator and refuses missing or duplicate producers, nonpositive
durations, incompatible category formulas, invalid carrier policies, forced probe rolls, Doom, and
Empty_32. An incomplete profile therefore cannot create a counter-zero native status that the DCL
does not expire.

`tools/validate_dcl_status_duration_pair.py` derives each producer's expected native-rider policy
from the complete status-action authority catalog rather than from a Disable/Immobilize-only id list.
This makes the safety contract reusable for later status families without weakening the current pair.

## Regression coverage

- `python tools/test_dcl_status_duration_pair.py`: PASS;
- `python tools/test_dcl_status_duration_frontier.py`: PASS;
- `python tools/analyze_dcl_status_duration_frontier.py --check-only`: PASS;
- `python tools/validate_dcl_status_duration_pair.py work/1784395365-dcl-da-dm-status-duration-pair.json`: PASS;
- `python tools/check_docs_timeless.py docs/modding`: PASS;
- `git diff --check`: PASS, with existing line-ending warnings only.

The new test and analyzer gate are included in `codemod/run-offline-checks.ps1`.

## Next technical gates

1. The complete offline suite passes in 93.5 seconds after coverage regeneration.
2. Keep the five unresolved harmful natures and Disease carrier decision explicit; do not invent them.
3. Do not neutralize another native counter until a positive-duration rule owns every add producer
   and the corresponding action-data suppressions are hash-bound.
4. Resume the Fire/Fear AoE authority probe only after the data mod and codemod are enabled together.

The regenerated whole-DCL matrix is:

- `work/1784396924-dcl-implementation-coverage.md`;
- `work/1784396924-dcl-implementation-coverage.csv`.
