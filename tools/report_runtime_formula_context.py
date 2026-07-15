#!/usr/bin/env python3
"""Generate a formula-context capability report from the code-mod sources.

The report is a quick, offline answer to "what can formulas use right now?" It is intentionally
source-derived where practical, then rounded out with the dynamic patterns that only exist after a
settings file defines slots, action signals, or derived variables.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


REPO = Path(__file__).resolve().parents[1]
MOD_CS = REPO / "codemod/fftivc.generic.chronicle.codemod/Mod.cs"
FORMULA_CS = REPO / "codemod/fftivc.generic.chronicle.codemod/FormulaExpression.cs"
ITEM_CATALOG_CS = REPO / "codemod/fftivc.generic.chronicle.codemod/ItemCatalog.cs"
ABILITY_CATALOG_CS = REPO / "codemod/fftivc.generic.chronicle.codemod/AbilityCatalog.cs"
RUNTIME_CONTEXT_CS = REPO / "codemod/fftivc.generic.chronicle.codemod/FormulaRuntimeContextBuilder.cs"
REACTIONS_CS = REPO / "codemod/fftivc.generic.chronicle.codemod/DclReactions.cs"
OUT = REPO / "work/runtime_formula_context.md"


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except FileNotFoundError:
        raise SystemExit(f"missing source file: {path}")


def function_body(source: str, name: str) -> str:
    match = re.search(rf"(?m)^\s*(?:private|public|internal|protected)\s+[^\n;=]*\b{name}\s*\(", source)
    if not match:
        raise SystemExit(f"could not find function {name}")

    brace = source.find("{", match.end())
    if brace < 0:
        raise SystemExit(f"could not find function body for {name}")

    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[brace + 1:index]

    raise SystemExit(f"unterminated function body for {name}")


def quoted_strings(text: str) -> list[str]:
    return re.findall(r'"([^"]+)"', text)


def case_aliases(body: str) -> list[str]:
    return sorted(set(re.findall(r'case\s+"([^"]+)":', body)), key=lambda value: (value.lower(), value))


def literal_context_sets(body: str) -> list[str]:
    return sorted(set(re.findall(r'context\.Set\("([^"]+)"', body)))


def prefix_suffixes(body: str) -> list[str]:
    return sorted(set(re.findall(r'context\.Set\(\$"\{prefix\}\.([^"{]+)"', body)))


def base_name_suffixes(body: str) -> list[str]:
    return sorted(set(re.findall(r'context\.Set\(\$"\{baseName\}\.([^"{]+)"', body)))


def item_suffixes(item_source: str) -> list[str]:
    suffixes = set(re.findall(r'Set\(context,\s*prefix,\s*"([^"]+)"', item_source))
    suffixes.update(f"category_{name}" for name in extract_array(item_source, "KnownCategories"))
    suffixes.update(f"type_{name}" for name in extract_array(item_source, "KnownTypeFlags"))
    suffixes.update(["category_<normalizedItemCategory>", "type_<normalizedTypeFlag>"])
    return sorted(suffixes)


def tuple_names(source: str, name: str) -> list[str]:
    match = re.search(rf"{name}\s*=\s*\[(.*?)\];", source, flags=re.S)
    if not match:
        raise SystemExit(f"could not find tuple array {name}")
    return re.findall(r'\(\s*"([^"]+)"\s*,', match.group(1))


def unit_suffixes(runtime_context_source: str) -> list[str]:
    suffixes = set(prefix_suffixes(function_body(runtime_context_source, "AddUnitVariables")))
    for index in range(5):
        suffixes.update(
            {
                f"status.sourceByte{index}",
                f"status.immunityByte{index}",
                f"status.effectiveByte{index}",
                f"status.masterByte{index}",
            }
        )

    for name in tuple_names(runtime_context_source, "StatusBits"):
        suffixes.update(
            {
                f"status.{name}",
                f"status.source.{name}",
                f"status.immune.{name}",
                f"status.master.{name}",
            }
        )

    element_names = tuple_names(runtime_context_source, "ElementBits")
    affinity_names = extract_array(runtime_context_source, "ElementAffinityNames")
    for affinity in affinity_names:
        suffixes.add(f"element.{affinity}Mask")
        suffixes.update(f"element.{affinity}.{element}" for element in element_names)
    return sorted(suffixes)


def ability_suffixes(ability_source: str) -> list[str]:
    suffixes = set(re.findall(r'Set\(context,\s*prefix,\s*"([^"]+)"', ability_source))
    # DclAbilityMetadata writes through the nested `ability.dcl` prefix. The broad source scan
    # above sees those literal suffixes but cannot infer that extra prefix, so relocate them and
    # expand the enum-valued one-hot fields explicitly.
    for nested in ("approved", "power", "strike_count"):
        suffixes.discard(nested)
        suffixes.add(f"dcl.{nested}")
    for field, array_name in (
        ("action_kind", "ActionKinds"),
        ("damage_type", "DamageTypes"),
        ("avoidance", "AvoidancePolicies"),
        ("status_category", "StatusCategories"),
        ("side_effect", "SideEffectPolicies"),
    ):
        suffixes.update(f"dcl.{field}_{name}" for name in extract_array(ability_source, array_name))
    suffixes.update(extract_array(ability_source, "FlagVariableNames"))
    suffixes.update(f"element_{name}" for name in extract_array(ability_source, "KnownElements"))
    suffixes.update(f"inflict_{name}" for name in extract_array(ability_source, "KnownStatuses"))
    suffixes.update(f"inflict_mode_{name}" for name in extract_array(ability_source, "KnownInflictModes"))
    return sorted(suffixes)


def extract_array(source: str, name: str) -> list[str]:
    match = re.search(rf"{name}\s*=\s*\[(.*?)\];", source, flags=re.S)
    if not match:
        raise SystemExit(f"could not find array {name}")
    return quoted_strings(match.group(1))


def tick_join(values: list[str]) -> str:
    return ", ".join(f"`{value}`" for value in values)


def bullet_list(values: list[str]) -> list[str]:
    return [f"- `{value}`" for value in values]


def build_report() -> str:
    mod_source = read(MOD_CS)
    formula_source = read(FORMULA_CS)
    item_source = read(ITEM_CATALOG_CS)
    ability_source = read(ABILITY_CATALOG_CS)
    runtime_context_source = read(RUNTIME_CONTEXT_CS)
    reactions_source = read(REACTIONS_CS)

    apply_aliases = case_aliases(function_body(formula_source, "ApplyFunction"))
    table_aliases = sorted(set(quoted_strings(function_body(formula_source, "IsTableFunction"))))
    matrix_aliases = sorted(set(quoted_strings(function_body(formula_source, "IsMatrixFunction"))))
    map_aliases = sorted(set(quoted_strings(function_body(formula_source, "IsMapFunction"))))
    special_aliases = ["if"]

    build_damage = function_body(mod_source, "BuildFormulaContext")
    build_mp = function_body(mod_source, "BuildMpFormulaContext")
    response_vars = literal_context_sets(function_body(mod_source, "AddDamageResponseVariables"))
    result_vars = sorted(set(re.findall(r'formulaContext\.Set\("([^"]+)"', mod_source)))
    top_level_vars = sorted(
        set(literal_context_sets(build_damage))
        | set(literal_context_sets(build_mp))
        | set(["event.index", "event.seed"])
    )

    # Unit and equipment-slot context construction lives in the shared runtime builder. Reading
    # the forwarding wrappers in Mod.cs would silently produce empty capability lists.
    unit_suffix = unit_suffixes(runtime_context_source)
    ability_suffix = ability_suffixes(ability_source)
    action_suffix = sorted(
        set(prefix_suffixes(function_body(mod_source, "AddActionVariables")))
        | set(prefix_suffixes(function_body(mod_source, "AddMpActionVariables")))
        | set(["<ActionSignalRules.Variables>", "<ActionSignalRules.VariableFormulas>"])
    )
    slot_suffix = base_name_suffixes(function_body(runtime_context_source, "AddSlotVariables"))
    slot_suffix = sorted(set(["<slotName>", "<slotName>.itemId"] + [f"<slotName>.{suffix}" for suffix in slot_suffix]))
    catalog_suffix = [f"<slotName>.{suffix}" for suffix in item_suffixes(item_source)]
    item_rule_suffix = item_suffixes(item_source)
    reaction_vars = sorted(
        set(literal_context_sets(function_body(reactions_source, "AddRuleVariables")))
        | set(literal_context_sets(function_body(reactions_source, "AddIncomingVariables")))
    )
    dcl_vars = sorted(
        name
        for name in (
            set(literal_context_sets(function_body(runtime_context_source, "BuildDclDamageContext")))
            | set(literal_context_sets(function_body(runtime_context_source, "AddDclMultistrikeVariables")))
        )
        if name.startswith("dcl.")
    )
    guard_vars = sorted(set(re.findall(r'context\.Set\("(guard\.[^"]+)"', mod_source)))

    lines: list[str] = [
        "# Runtime Formula Context",
        "",
        "Generated by `tools/report_runtime_formula_context.py`. Do not hand-edit.",
        "",
        "This is the offline capability catalog for the Generic Chronicle battle runtime. It lists",
        "what formula expressions can use before the remaining live gates map stable equipment,",
        "attacker, and action context.",
        "",
        "## Expression Syntax",
        "",
        "- Integer math: `+`, `-`, `*`, `/`, `%`, unary `+`/`-`.",
        "- Comparisons and logic: `==`, `!=`, `<`, `<=`, `>`, `>=`, `&&`, `||`, `!`.",
        "- Numbers can be decimal or hex (`0x61`). Identifiers are case-insensitive.",
        "- `if(condition, trueExpr, falseExpr)` is lazy, so guarded attacker/slot reads are safe.",
        "",
        "## Function Aliases",
        "",
        f"- Table lookups: {tick_join(table_aliases)}",
        f"- Matrix lookups: {tick_join(matrix_aliases)}",
        f"- Map lookups: {tick_join(map_aliases)}",
        f"- Lazy branch: {tick_join(special_aliases)}",
        f"- Scalar/bit/dice/random/raw-byte helpers: {tick_join(apply_aliases)}",
        "",
        "## Top-Level Variables",
        "",
        *bullet_list(top_level_vars),
        "",
        "## Reaction Variables",
        "",
        "Available to `DclReactionRules.ConditionFormula` and `ChanceFormula`. The ordinary",
        "`attacker.*`/`a.*`, action,",
        "ability, and equipment context describes the incoming source/action when it is valid.",
        "",
        *bullet_list(reaction_vars),
        "",
        "## Result Variables",
        "",
        "Available after the final HP or MP decision is computed, mainly for trace variables and",
        "rewrite-condition checks.",
        "",
        *bullet_list(result_vars),
        "",
        "## Response Variables",
        "",
        *bullet_list(response_vars),
        "",
        "## DCL Variables",
        "",
        "The shared DCL preview, hit, and pre-clamp contexts expose the following fixed variables.",
        "Strike outcome counts are zero before a managed decision exists and contain the cached",
        "aggregate during execution.",
        "",
        *bullet_list(dcl_vars),
        "",
        "Physical-defense formulas additionally receive the live or per-strike local Guard state:",
        "",
        *bullet_list(guard_vars),
        "",
        "## Unit Variables",
        "",
        "Each suffix exists under `target.*`, `t.*`, `attacker.*`, and `a.*`. Attacker values are",
        "safe zero/defaults until attacker context is mapped for a live event.",
        "",
        *bullet_list(unit_suffix),
        "",
        "Additional attacker-source flags:",
        "",
        "- `attacker.sourceCt`, `a.sourceCt`",
        "- `attacker.sourceCounter`, `a.sourceCounter`",
        "- `attacker.inferred`, `a.inferred`",
        "- `attacker.sourceImmediate`, `a.sourceImmediate`",
        "- `attacker.sourceRecent`, `a.sourceRecent`",
        "",
        "## Action Variables",
        "",
        "Each suffix exists under both `action.*` and `act.*`. Custom action variables are defined by",
        "`ActionSignalRules.Variables` or `ActionSignalRules.VariableFormulas`; the runtime pre-seeds",
        "those custom names to zero so formulas can read them safely even when no signal matched.",
        "",
        *bullet_list(action_suffix),
        "",
        "## Ability Variables",
        "",
        "Catalog metadata for the current incoming/executing action appears under `ability.*`.",
        "Unknown or non-catalog abilities receive the same names with zero/default values.",
        "",
        *bullet_list(ability_suffix),
        "",
        "## Slot Variables",
        "",
        "Configured equipment slots appear under `slot.<name>.*`, `targetSlot.<name>.*`,",
        "`tslot.<name>.*`, `attackerSlot.<name>.*`, and `aslot.<name>.*`. The short `slot.*` alias",
        "is target-side/backwards-compatible.",
        "",
        *bullet_list(slot_suffix),
        "",
        "## Item Metadata Variables",
        "",
        "Catalog metadata is appended to every resolved slot prefix and to the temporary `item.*`",
        "context used inside equipment DR / response rule evaluation.",
        "",
        "Slot/item suffixes:",
        "",
        *bullet_list(catalog_suffix),
        "",
        "`item.*` rule-context suffixes:",
        "",
        *bullet_list(item_rule_suffix),
        "",
        "## Settings-Defined Variables",
        "",
        "- `FormulaVariables`: available as `<name>` and `const.<name>`.",
        "- `FormulaTables`: read with `table*` / `lookup*` helpers.",
        "- `FormulaMatrices`: read with `matrix*`, `lookup2d*`, or `table2d*` helpers.",
        "- `FormulaMaps`: read with `map*` / `lookupMap*` helpers.",
        "- `FormulaPreActionVariables`: evaluated after unit/slot variables and before action rules.",
        "- `ActionSignalRules.Variables` and `.VariableFormulas`: exposed as `action.<name>` / `act.<name>`.",
        "- `FormulaPreResponseVariables`: evaluated after action rules and before equipment DR / response.",
        "- `FormulaDerivedVariables`: evaluated in order before final damage / MP formulas finish.",
        "- `FormulaTraceVariables`: diagnostic formulas appended to `[RUNTIME] ... vars=...`; they do not",
        "  mutate the context by themselves.",
        "",
        "## Live Caveats",
        "",
        "- Target HP/MP/stat variables are available from the current battle-unit snapshot.",
        "- Attacker variables are implemented but depend on a mapped or inferred attacker source.",
        "- Slot metadata is implemented, but exact equipment offsets still need live confirmation.",
        "- Action variables are implemented through sentinel rules now; true ability/action ids still need",
        "  a later live context source.",
        "- Death/KO ownership is engine-owned for the current runtime path: direct HP=0 and",
        "  `+0x61 | 0x20` writes are preserved only as historical/refuted probes, while live",
        "  custom lethal formulas should use `MinHpFloor=1` and let the engine deliver KO.",
        "",
    ]

    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the runtime formula context report.")
    parser.add_argument("--check", action="store_true", help="Fail if the checked-in report is stale.")
    parser.add_argument("--output", type=Path, default=OUT, help=f"Output path. Default: {OUT}")
    args = parser.parse_args()

    report = build_report()
    output = args.output

    if args.check:
        if not output.exists():
            print(f"missing report: {output}", file=sys.stderr)
            return 1
        actual = output.read_text(encoding="utf-8")
        if actual != report:
            print(f"stale report: {output} (run python tools/report_runtime_formula_context.py)", file=sys.stderr)
            return 1
        print("runtime formula context report is current")
        return 0

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(report, encoding="utf-8")
    print(f"wrote {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
