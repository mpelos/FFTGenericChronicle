# DCL reaction filter-capability checkpoint

## Scope

This pass audits the final 16-entry reaction roster after `ConditionFormula`, incoming source/action,
physical outcome, status, position, ability metadata, and reaction cadence primitives became
available. It separates formula visibility from trigger production, effect delivery, cadence commit,
and eventual live proof.

No game files are deployed and no live test is attempted in this pass.

## Results

- The source-derived runtime formula report now includes loop-generated unit status/element names and
  the complete `ability.*` catalog surface.
- All 16 final reaction entries have an explicit mechanism boundary in
  `1783991305-dcl-reaction-capabilities.csv` and `.md`.
- Formula-filter classification is: 7 `ready-in-context`, 5 `partial-context`, 1 `design-open`,
  1 `design-partial`, and 2 intentional open slots.
- `ready-in-context` means only that the predicate can suppress an evaluation after a reaction window
  exists. It does not supply a trigger or effect.
- True terrain line of sight, committed-damage phase/amount, and execution provenance remain absent
  reaction-context signals.
- Cadence primitives remain deliberately unconsumed because the chance callback can run in forecast
  and AI evaluation. Consumption needs an execution-only effect/trigger commit.

## Offline fixture

`1783991305-battle-runtime-settings.lt22-dcl-reaction-filters.json` validates ten representative
filters covering incoming hit/miss/defense, Invisible, HP/KO survival, team/self, ability flags,
distance, weapon range, and weapon family. It is an offline validator fixture, not a deployment
profile and not a final reaction implementation.

The settings validator reports zero errors and 12 expected warnings: ten suppress-only warnings,
the hybrid taxonomy live gate, and the managed callback caveat.

## Validation evidence

- C# Release build: 0 warnings, 0 errors.
- Formula runtime smoke tests: pass.
- Runtime formula-context report check: pass.
- Reaction capability matrix smoke/check: 16 entries, pass.
- Runtime profile audit smoke test: pass.
- LT21 + LT22 settings validation: pass, zero errors.
- Current executable reaction scope analysis: all anchors and scope checks pass; generated report
  `1783991626-dcl-reaction-scope-analysis.md`.
- `git diff --check`: pass; only the pre-existing CRLF normalization warning for
  `work/runtime_formula_context.md` remains.

## Next offline target

Locate an execution-only reaction commit boundary that distinguishes a real triggered effect from
forecast/AI evaluation. This boundary is the prerequisite for safe cadence consumption and is also
the best starting point for tracing native Auto-Potion item selection, native counter action staging,
and reusable synthesized reaction actions.

After that boundary is characterized, build the generic reaction-action producer shared by basic
melee counters, shots, Rod bolts, and tile-keyed Geomancy. True line of sight and action provenance
remain separate reverse-engineering tracks.
