# DCL runtime composition checkpoint

## Objective

Remove silent profile-merging risk before assembling the canonical DCL runtime settings.

## Result

`tools/compose_runtime_settings.py` composes JSON mechanism profiles structurally and rejects every
divergent value unless the composition manifest resolves that top-level setting explicitly. Named
arrays merge by `Name`; maps merge recursively; unnamed arrays require equality.

The first manifest combines:

- the physical Weapon Skill, typed damage, facing, Dodge/Parry/Block, Weight, and outcome profile;
- the numeric magic, Faith, element/affinity, Shell, Zodiac, absorb/null, Undead, forecast, and
  Magic Evade profile.

The only divergent setting is `DclDamageFormula`. Its explicit resolution retains the weapon route
for basic physical Attack, the magic damage route, magic absorption/nullification, the Undead heal
inversion, and the native debit fallback for every other action.

Observe-only result-selector logging is explicitly disabled in the scaffold. No design-open status,
reaction, lifecycle, item, job, or multistrike rule is imported from a live-test fixture.

Numeric result-flag ownership is enabled in the scaffold. The integrated HP routes therefore clear
stale native numeric bits and derive HP debit/credit presentation from the final composed physical or
magic channels while preserving low nonnumeric effect bits.

## Validation

- Composer unit tests reject an unresolved scalar conflict and an unused resolution.
- The generated output passes freshness checking.
- The C# runtime-settings validator reports zero errors; its warnings are the expected mechanism
  safety warnings for enabled DCL hooks.
- The C# smoke suite loads the generated scaffold itself and proves that basic Attack selects the
  physical Weapon Skill/DR/wound route, Fire selects the magic/Faith/affinity route, Cure preserves
  the Undead debit/credit inversion, and unauthored Barrage falls back to its native staged debit.
- The scaffold is an offline integration artifact and is not authorized for deployment.

## Remaining integration frontier

Extend the manifest only with ratified policy fragments. Each extension must keep conflict
resolution explicit, pass the runtime validator, and add an integrated simulation/regression case
before deployment becomes eligible.
