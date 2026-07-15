# DCL offline formula closure checkpoint

## Scope

This checkpoint closes the remaining ability-record/formula reverse-engineering queue against the
installed Enhanced executable before returning to live integration gates.

Executable SHA-256:
`841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`.

## Closed mechanisms

- Song/Dance: native periodic cadence and cleanup retained; tick outputs use ordinary staged
  HP/MP/stat/CT/status channels.
- Steal/Plunder: equipment, Gil, and EXP staging/commit paths mapped; native inventory/campaign
  transactions retained.
- Formulas `0x65..0x6A`: drain aliases, formula aliases, distance/MaxHP-derived damage, and Barrage
  weapon-formula dispatch mapped.
- Blank records 510/511: inert terminal sentinels.
- Golem formula `0x14`: four team-indexed 16-bit pools at RVA `0x186B020`; MaxHP initialization,
  saturating debit, whole-hit absorption, and native battle-state import/export mapped.
- Malboro Spores formula `0x58`: dedicated generic-unit transformation transaction, not a status
  bundle; eligibility, result bit, and native status/effect reset mapped.

Primary evidence:

- `work/1784009976-dcl-support-transaction-analysis.md`
- `work/1784009977-dcl-performance-state-analysis.md`
- `work/1784010345-dcl-steal-transaction-analysis.md`
- `work/1784010592-dcl-formula-65-6a-analysis.md`
- `work/1784010954-dcl-golem-pool-analysis.md`
- `work/1784010934-dcl-malboro-transformation-analysis.md`

The earlier `work/1784010933-dcl-golem-pool-analysis.md` is superseded: its only failure was an
incomplete expected-byte literal in the analyzer, corrected by the passing `1784010954` run.

## Ability classification coverage

Latest manifest:

- `work/1784011051-dcl-ability-classification-candidates.csv`
- `work/1784011051-dcl-ability-classification-candidates.md`
- `work/1784011051-dcl-ability-metadata-authoring-template.csv`

Coverage over all 512 records:

- unclassified: `0`
- reverse-engineering readiness: `0`
- metadata technically blocked: `0`
- candidate-complete: `332`
- design-only open: `127`
- mixed mechanism plus design: `53`

The remaining 180 authoring-open records are not unknown formulas. They are explicit design or
runtime-integration choices such as damage type, avoidance policy, status category, multistrike,
multi-unit transactions, and equipment side effects.

## Validation and deployment

`codemod/run-offline-checks.ps1` passes end to end, including:

- Python syntax and tooling tests;
- the 512-record DCL classification regression;
- installed executable static scan;
- offline readiness audit;
- C# Release build with zero warnings/errors;
- C# formula/runtime smoke tests;
- runtime-settings validation and simulation fixtures;
- helper dry runs and whitespace check.

The current source build is deployed without changing runtime settings:

- built/installed DLL SHA-256:
  `9740DE03CDAE2CF7017A8375F3EB5FEBF451ED516FF6235C633B8B3531330F7A`
- installed settings SHA-256:
  `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- Reloaded-II remained open; `FFT_enhanced` was not running during deploy.

The user independently confirmed that launching the game through Reloaded-II works normally. This
isolates the earlier launch failure to the Codex computer-control channel rather than the game/mod.

## Next gates

Offline formula identity is exhausted. Continue offline on the remaining mixed runtime mechanisms
where static evidence still exists, then use live runs only for integration facts that static files
cannot establish. The highest-value prepared live gates remain:

1. LT28 calculation provenance and AI/outer-vs-nested identity.
2. LT23 reaction three-pass commit.
3. LT31 reaction effect boundary/cadence.
4. LT27 native weapon line-of-fire verdict.
5. Targeted Golem interception-family/recast cardinality check.

No final DCL profile is approved yet; design authoring, integrated profile composition, and the
whole-DCL battle regression remain open.
