# Build Incidence Benchmark Schema V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `work/job-build-incidence-benchmark-v0.json`

## Purpose

This document defines the first pinned input bundle for T2, the build-incidence matrix.

T2 exists to answer questions such as:

- does one reaction/support/movement skill appear in too many strong builds?
- does one secondary skillset become a default answer?
- does one equipment-unlock support erase too many equipment identities?
- is the benchmark population representative enough for those percentages to mean anything?

This is a schema and coverage contract. It is not yet the final accepted strong-build population.

## Bundle

Pinned bundle:

```text
work/job-build-incidence-benchmark-v0.json
```

The bundle is the numeric/structured source of truth for T2 v0.

The document explains intent; the JSON bundle defines the exact fields Claude and GPT must consume
for independent incidence counters.

## T2.0 Scope

T2.0 defines:

- required benchmark coverage;
- anti-bias rules;
- build record schema;
- counted slots;
- warning and failure thresholds;
- open benchmark slots that must be filled before final incidence percentages are meaningful.

T2.0 does not yet claim that all canonical builds are final. Most jobs still do not have accepted
concrete skill proposals, so the bundle includes open slots rather than fake final builds.

## Counting Unit

The counting unit is a distinct combat build identity:

```text
active job
+ secondary skillset
+ reaction skill
+ support skill
+ movement skill
+ weapon family or primary pressure
+ armor/target profile
+ Brave/Faith/Speed sensitivity where relevant
```

Small equipment swaps do not create new build identities unless they change tactical identity, such
as sword to gun, leather to plate, or physical pressure to magic pressure.

Training-only builds do not count as combat builds.

## Counted Slots

The T2 counter must count incidence for:

- `secondary`;
- `reaction`;
- `support`;
- `movement`;
- `equipment_unlocks`.

It must also report coverage for:

- `primary_role`;
- `active_job`;
- `armor_profile`;
- `damage_modes`;
- `pressure_tags`;
- `sensitivity_tags`;
- `phase`.

## Thresholds

For reaction/support/movement incidence:

- warning: more than `35%` of counted late/stress builds;
- failure: more than `50%` of counted late/stress builds.

The same warning/failure posture applies to secondary and equipment-unlock incidence until a later
accepted document defines different thresholds.

The counter should report percentages and raw counts. It should not decide exceptions silently.

## Anti-Bias Rules

The benchmark set must honor `docs/job-balance/01-cross-job-build-principles.md`.

The set must not:

- over-represent already dominant builds;
- under-represent weak roles;
- omit armor classes or damage modes that make a favored skill look less universal;
- count small equipment variants as separate builds to dilute a repeated global piece;
- exclude plausible bad matchups to make a universal build look specialized.

## Required Coverage

The bundle requires coverage across:

- every primary role in the current role map;
- every armor profile: `plate`, `mail`, `leather`, `cloth`;
- every physical damage mode: `swing`, `thrust`, `crush`, `missile`;
- magic pressure;
- spirit/status pressure;
- Brave-sensitive builds;
- Faith-sensitive builds;
- Speed-sensitive builds;
- late and stress phases as mandatory incidence phases.

Early and mid phase slots are optional in T2.0. Mandatory-piece incidence is a late/stress question
because builds are complete enough for repeated global pieces to matter. Early and mid can be added
later for progression analysis, but their absence must not fail the T2.0 coverage check.

Coverage is marginal per dimension, not a cross-product. A required value is present if it appears
in at least one benchmark slot. Coverage-presence checks use all records, including `open_slot`.
Incidence checks use only counted records.

## Record Statuses

Build records use these statuses:

| Status | Meaning | Counted? |
| --- | --- | --- |
| `open_slot` | Required coverage slot exists but no accepted build fills it yet. | No |
| `draft_candidate` | A plausible build candidate exists but has not been accepted. | No |
| `accepted_provisional` | Accepted for provisional design; can be counted in provisional incidence runs. | Yes |
| `accepted_final` | Accepted after all required validation gates. | Yes |
| `retired` | Superseded or rejected build record kept for audit. | No |

T2.0 primarily defines `open_slot` records. T2.1 should begin filling them with
`accepted_provisional` build records as job slices and validation tracks mature.

The authoritative inclusion rule is `record_statuses[status].counted` in the JSON bundle. The
per-record `counting.include` value must equal that status-derived value. If they diverge, the
counter must emit a hard validation error instead of silently choosing one source.

## Build Record Shape

Each build record in the JSON bundle must provide:

- `id`: stable snake-case identifier;
- `status`: one of the record statuses;
- `phase`: `early`, `mid`, `late`, or `stress`;
- `primary_role`: compact role taxonomy value;
- `active_job`: active job name, or `TBD` for open slots;
- `coverage`: roles, armor profile, damage modes, pressure tags, and sensitivity tags;
- `slots`: secondary, reaction, support, movement, and equipment unlocks;
- `counting`: whether to include the record and why;
- `notes`: short explanation of what the slot or build is proving.

Unknown values must be explicit `TBD`, not omitted.

`NONE` means a counted build intentionally has no skill in that slot and is excluded from incidence
for that slot. `TBD` is allowed only in uncounted records. A counted record with `TBD` in a counted
slot is a hard validation error.

## Incidence Contract

For `secondary`, `reaction`, `support`, `movement`, and `equipment_unlocks`:

- denominator: counted builds in phases `late` or `stress`;
- numerator: counted late/stress builds whose relevant slot contains the value;
- array-valued slots such as `equipment_unlocks` dedupe per build, so one build counts once per
  distinct unlock;
- threshold comparison uses the raw fraction `numerator / denominator`;
- warning/fail thresholds are exclusive-greater-than;
- rounded percentages are display-only and must not be used for comparison.

When the denominator is `0`, the counter must emit the canonical no-data token
`NO_COUNTED_BUILDS`, produce empty incidence tables, and emit zero warnings/failures.

## Expected Counter Output

GPT and Claude counters should produce the same tables from the same bundle:

- total counted builds;
- ignored records by reason;
- incidence by secondary;
- incidence by reaction;
- incidence by support;
- incidence by movement;
- incidence by equipment unlock;
- missing required coverage;
- warning and failure threshold hits.

Canonical output requirements:

- rows sorted alphabetically by key;
- ignored records grouped by exact `counting.reason` string;
- raw numerator, denominator, and fraction preserved for comparison;
- display percentages rounded to six decimals;
- `0` row mismatch means equality after applying this canonical form.

The dual-independent gate from document 07 applies:

- GPT implementation;
- Claude implementation;
- same pinned bundle;
- `0` row mismatches before the T2 output can be used to accept or reject a skill.

## Resolved T2.0 Questions

- T2.1 should fill only jobs with accepted provisional skill documents, not `draft_candidate`
  placeholders.
- Ramza Chapter 4 stays in the same table with an explicit protagonist-exception flag.
- Bard/Dancer shared reaction/support/movement global pieces count once.

## Claude Review Request

Claude should review whether:

- the JSON shape is sufficient for an independent incidence counter;
- the required coverage reflects documents 01, 02, and 07;
- the status/counting rules avoid fake percentages before the build set is filled;
- any fields are missing before GPT and Claude implement separate counters.

Claude review verdict: Accepted (claude-opus-4-8, 2026-06-20).
