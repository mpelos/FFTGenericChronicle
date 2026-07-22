# Native Area policy provider

## Context

After Direct, auxiliary single-target magic, and Physical policy providers, AreaNumeric was the last major admitted-family composition path whose declaration identity was still hand-authored in the smoke sentinel. The goal was to derive only the native declaration identity from the admitted sequence while keeping every target-local policy explicit.

## Result

Added `DclCanonicalNativeAreaActionPolicyProvider`.

The provider accepts a complete contiguous AreaNumeric admission sequence and derives declaration identity from the normalized target mode:

- unit-target Area uses the admitted selected unit;
- fixed-tile Area uses the admitted selected tile plus an explicit fixed-tile height;
- caster Area carries no separate declared unit or fixed tile.

The provider keeps these inputs explicit:

- tradition and tradition skill;
- learned/source/prerequisite/overcast gates;
- caster state penalty;
- per-target Dodge, resistance, status riders, forced movement, Injury movement, and target-relative modifiers;
- Reaction candidates.

The Area smoke sentinel now uses this provider and includes `SelectedUnit` in the admitted native repeat sequence.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeAreaActionPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

The main remaining production gap is no longer an offline family-policy shape. It is the live callback bridge that creates policy sources from real native context, composes retained admitted actions, and publishes execution/forecast/AI carriers safely.
