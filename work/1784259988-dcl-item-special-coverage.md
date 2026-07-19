# DCL item special-rule technical coverage

## Scope

This report classifies every `special_rule` value in the DCL item sidecar. Final numeric
item authoring and job-owned action identity are excluded; every job-free technical rule
must still name a formula, native-data, native-runtime, or code owner.

## Input

- Sidecar: `work/1783984192-dcl-item-sidecar.csv`
- Rows: 261
- Distinct rules: 14

## Classification

| Rule | Rows | Class | Owner | Evidence boundary |
| --- | ---: | --- | --- | --- |
| `arc_trajectory` | 9 | `native-live-gated` | native Arc resolver | separate Arc/Direct LoS coverage row |
| `common_unarmed_thr_minus_penalty_or_martial_arts_wmod` | 1 | `formula+job-excluded` | physical mechanism profile | common fist route; Martial Arts excluded |
| `dance_identity_job_authored` | 3 | `job-excluded` | job/content policy | Dancer action roster |
| `defender_parry_minus4_block_minus2` | 4 | `formula` | physical mechanism profile | dcl.flailParryPenalty / dcl.flailBlockPenalty |
| `draw_out_repeatable_mp_cost` | 10 | `job-excluded` | job/content policy | Samurai ability economy |
| `job_buff_or_debuff_pending` | 4 | `job-excluded` | job/content policy | Bag action identity |
| `modest_speed_grant` | 10 | `native-data` | ItemEquipBonusData | item catalog exposes bonus_speed |
| `none` | 169 | `no-special` | native/data identity | no extra DCL behavior declared |
| `offensive_magic_modifier` | 8 | `formula` | magic mechanism profile | dcl.offensiveMagicMod |
| `reach_escape_counter_pointblank` | 16 | `formula` | physical mechanism profile | dcl.pointBlank and the Attack Skill modifier |
| `song_identity_job_authored` | 3 | `job-excluded` | job/content policy | Bard action roster |
| `straight_line` | 12 | `native-live-gated` | native Direct resolver | separate Arc/Direct LoS coverage row |
| `support_heal_modifier` | 8 | `formula` | magic mechanism profile | dcl.supportMagicMod |
| `talk_identity_job_authored` | 4 | `job-excluded` | job/content policy | Orator action roster |

## Result

Every declared item special has a technical owner. Arc/Direct remain live-gated in the
separate weapon-LoS mechanism. Job/content rules remain explicitly excluded rather than
being mistaken for missing combat-runtime infrastructure.
