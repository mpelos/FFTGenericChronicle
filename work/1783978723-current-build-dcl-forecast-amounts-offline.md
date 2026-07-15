# DCL formula-driven forecast amounts — offline checkpoint

## Implemented slice

The calc-entry managed callback can now compute authored forecast HP debit and HP credit for each `(caster, target, ability, action type)` using the same unit, equipment, ability-catalog, constants, tables, matrices, maps, and `DclDerivedVariables` surface used by execution.

The callback stores one debit/credit pair per target. The poller validates the native forecast-object pointer against the battle-unit table and mirrors the cached values into:

- forecast HP debit: unit `+0x1C4` / forecast object `+0x6`;
- forecast HP credit: unit `+0x1C6` / forecast object `+0x8`.

No managed code runs on the UI-copy/render path. Calc-entry produces the values; the existing guarded forecast-object relation delivers them. Static forecast pokes and static forced preview numbers are rejected as conflicting writers.

Preview formulas are separate from execution formulas because calc-entry runs before a native staged result exists. Their old-channel inputs are deliberately zero. Intrinsic action/unit/equipment formulas can therefore be identical to execution, while fallback expressions that depend on `dcl.oldDebit` or `dcl.oldCredit` remain execution-only.

Settings:

- `DclPreviewAmountEnabled`
- `DclPreviewDamageFormula`
- `DclPreviewHealingFormula`

## Offline gates passed

- Release code-mod build: zero warnings, zero errors.
- Full formula/runtime smoke suite in an isolated build configuration: passed.
- Preview-only profiles validate without enabling DCL hit control.
- Validator rejects empty preview ownership, unknown formula inputs, the static forecast poke, and static preview-number conflicts.
- Target-table alignment and 64-slot bounds fail open without writing memory.

## Remaining live boundary

The static forecast amount lever is already Proven, but the formula bridge and callback timing are Strong/offline until LT17. Specific open observations:

- whether both debit and credit can be mirrored together without the native renderer selecting the wrong channel;
- whether preview recalculation/cancellation refreshes the correct target slot without stale values;
- whether the producer restages all four execution channels after the last preview poll, as established for the static debit poke;
- whether charged spells and AI evaluation leave harmless cached preview values.
