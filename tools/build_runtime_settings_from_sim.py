#!/usr/bin/env python3
"""Generate code-mod runtime settings from a Generic Chronicle sim-input bundle.

The generated settings are a bridge artifact: they turn the design policy into
the JSON shape consumed by the Reloaded-II runtime. Offsets and action context
are still placeholders until live mapping confirms them.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"

DEFAULT_SOURCE = WORK / "sim-inputs-v0.2.json"
DEFAULT_OUTPUT = WORK / "battle-runtime-settings.v0.2.generated.json"

FAMILY_TO_CATEGORY_VAR = {
    "sword": "category_sword",
    "knight_sword": "category_knightsword",
    "katana": "category_katana",
    "knife": "category_knife",
    "ninja_blade": "category_ninjablade",
    "longbow": "category_bow",
    "crossbow": "category_crossbow",
    "gun": "category_gun",
    "spear": "category_polearm",
    "staff": "category_staff",
    "rod": "category_rod",
    "pole": "category_pole",
    "axe": "category_axe",
    "flail": "category_flail",
    "instrument": "category_instrument",
    "book": "category_book",
    "cloth_weapon": "category_cloth",
    "bag": "category_bag",
    "fists": "category_none",
}

DAMAGE_TYPES = ("swing", "thrust", "crush", "missile")
ARMOR_CLASSES = ("plate", "mail", "leather", "cloth")
ROUTINES = ("pa_wp", "br_pa_wp", "spd_pa_wp", "ma_wp", "rdm_pa_wp", "wp_wp", "br_pa_pa", "pampa_wp")


def permille(value: float | int) -> int:
    return round(float(value) * 1000)


def normalized_flag(name: str) -> str:
    return "".join(ch.lower() if ch.isalnum() else "_" for ch in name).strip("_")


def action_rule_for_family(index: int, family: str, spec: dict[str, Any]) -> dict[str, Any]:
    category_var = FAMILY_TO_CATEGORY_VAR.get(family)
    if category_var is None:
        raise ValueError(f"no attacker weapon category mapping for family {family!r}")

    damage_type = str(spec["damage_type"])
    routine = str(spec["routine"])
    family_flag = f"family_{normalized_flag(family)}"
    routine_flag = f"routine_{normalized_flag(routine)}"

    variables = {
        family_flag: 1,
        routine_flag: 1,
        damage_type: 1,
        "wp": int(spec.get("wp", 0)),
        "penetrationPermille": permille(float(spec.get("penetration", 0.0))),
    }

    condition = f"a.present && aslot.weapon.{category_var}"
    if family == "fists":
        condition = "a.present && (aslot.weapon.category_none || aslot.weapon.id == 0)"

    return {
        "Name": f"{family} from attacker weapon",
        "ConditionFormula": condition,
        "Signal": 100 + index,
        "Variables": variables,
        "VariableFormulas": {
            "vanillaWp": "aslot.weapon.weaponPower",
        },
    }


def damage_response_rules(armor_response: dict[str, dict[str, float]], penetration_ceiling: float) -> list[dict[str, Any]]:
    ceiling = permille(penetration_ceiling)
    rules: list[dict[str, Any]] = []
    for armor_class, responses in armor_response.items():
        for damage_type in DAMAGE_TYPES:
            if damage_type not in responses:
                continue
            base = permille(float(responses[damage_type]))
            formula = str(base)
            if base < ceiling:
                formula = f"{base} + mulDiv(action.penetrationPermille, {ceiling - base}, 1000)"
            rules.append(
                {
                    "Name": f"{armor_class} {damage_type}",
                    "ConditionFormula": f"armor.{armor_class} && action.{damage_type}",
                    "MultiplierFormula": formula,
                }
            )
    return rules


def armor_response_matrix(armor_response: dict[str, dict[str, float]]) -> list[list[int]]:
    return [
        [
            permille(float(armor_response.get(armor_class, {}).get(damage_type, 1.0)))
            for damage_type in DAMAGE_TYPES
        ]
        for armor_class in ARMOR_CLASSES
    ]


def matrix_response_variables(penetration_ceiling: float) -> list[dict[str, str]]:
    ceiling = permille(penetration_ceiling)
    return [
        {"Name": "armor.index", "Formula": "if(armor.plate, 0, if(armor.mail, 1, if(armor.leather, 2, 3)))"},
        {"Name": "damage.index", "Formula": "if(action.swing, 0, if(action.thrust, 1, if(action.crush, 2, 3)))"},
        {"Name": "response.basePermille", "Formula": "matrixClamp(armorResponsePermille, armor.index, damage.index)"},
        {
            "Name": "response.matrixPermille",
            "Formula": (
                f"if(response.basePermille < {ceiling}, "
                f"response.basePermille + mulDiv(action.penetrationPermille, {ceiling} - response.basePermille, 1000), "
                "response.basePermille)"
            ),
        },
    ]


def matrix_damage_response_rules() -> list[dict[str, Any]]:
    return [
        {
            "Name": "matrix response",
            "ConditionFormula": "action.present && (action.swing || action.thrust || action.crush || action.missile)",
            "MultiplierFormula": "response.matrixPermille",
        }
    ]


def trace_variables(response_mode: str) -> list[dict[str, str]]:
    variables = [
        {"Name": "trace.basePressure", "Formula": "basePressure"},
        {"Name": "trace.responsePermille", "Formula": "boundedResponse.permille"},
        {"Name": "trace.finalDamage", "Formula": "result.finalDamage"},
        {"Name": "trace.desiredHp", "Formula": "result.desiredHp"},
        {"Name": "trace.shouldRewrite", "Formula": "result.shouldRewrite"},
    ]
    if response_mode == "matrix":
        variables[1:1] = [
            {"Name": "trace.armorIndex", "Formula": "armor.index"},
            {"Name": "trace.damageIndex", "Formula": "damage.index"},
            {"Name": "trace.baseResponsePermille", "Formula": "response.basePermille"},
            {"Name": "trace.matrixResponsePermille", "Formula": "response.matrixPermille"},
        ]
    return variables


def slot_settings(slot_mode: str) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    if slot_mode == "scan":
        return (
            [
                {
                    "Name": "Weapon",
                    "SearchStart": 68,
                    "SearchEnd": 383,
                    "SearchWidth": "Byte",
                    "SecondaryKind": "weapon",
                    "TypeFlag": "Weapon",
                    "AllowAmbiguousSearchMatch": False,
                }
            ],
            [
                {
                    "Name": "Body",
                    "SearchStart": 68,
                    "SearchEnd": 383,
                    "SearchWidth": "Byte",
                    "SecondaryKind": "armor",
                    "TypeFlag": "Armor",
                    "AllowAmbiguousSearchMatch": False,
                }
            ],
        )

    return (
        [{"Name": "Weapon", "Offset": 80, "Width": "Byte"}],
        [{"Name": "Body", "Offset": 112, "Width": "Byte"}],
    )


def apply_profile(settings: dict[str, Any], profile: str) -> dict[str, Any]:
    if profile == "policy":
        return settings

    if profile == "live-noop":
        settings["_note"] = (
            settings.get("_note", "")
            + " Live-noop preserves vanilla HP outcomes while logging "
            "resolved attacker, slots, action, damage response, and final context."
        ).strip()
        settings["FinalDamageFormula"] = "vanillaDamage"
        settings["ApplyDamageResponseRules"] = False
        settings["ApplyEquipmentDr"] = False
        settings["RewriteObservedDamage"] = True
        settings["RewriteObservedHealing"] = False
        settings["InferAttackerFromRecentUnits"] = True
        settings["LogAttackerCandidates"] = True
        settings["LogResolvedRuntimeContext"] = True
        settings["LogUnknownFieldDiffs"] = False
        return settings

    raise ValueError(f"unknown profile {profile!r}")


def build_settings(bundle: dict[str, Any], slot_mode: str, profile: str, response_mode: str) -> dict[str, Any]:
    calc = bundle.get("calc", {})
    clamp = calc.get("combined_multiplier_clamp", [0.25, 2.5])
    families = bundle.get("families", {})
    attacker_slots, target_slots = slot_settings(slot_mode)

    family_rules = [
        action_rule_for_family(index, family, spec)
        for index, (family, spec) in enumerate(families.items(), start=1)
        if family in FAMILY_TO_CATEGORY_VAR
    ]

    routine_formula = (
        "if(action.routine_pa_wp, a.pa * action.wp, "
        "if(action.routine_br_pa_wp, floorDiv(a.pa * a.brave, 100) * action.wp, "
        "if(action.routine_spd_pa_wp, floorDiv(a.pa + a.speed, 2) * action.wp, "
        "if(action.routine_ma_wp, a.ma * action.wp, "
        "if(action.routine_rdm_pa_wp, rand(1, max(1, a.pa)) * action.wp, "
        "if(action.routine_wp_wp, action.wp * action.wp, "
        "if(action.routine_br_pa_pa, floorDiv(a.pa * a.brave, 100) * a.pa, "
        "if(action.routine_pampa_wp, floorDiv(a.pa + a.ma, 2) * action.wp, vanillaDamage))))))))"
    )

    penetration_ceiling = float(calc.get("penetration_ceiling", 1.10))
    pre_response_variables = [
        {"Name": "armor.plate", "Formula": "slot.body.category_armor && slot.body.armorHpBonus >= 60"},
        {"Name": "armor.mail", "Formula": "slot.body.category_armor && slot.body.armorHpBonus >= 40 && slot.body.armorHpBonus < 60"},
        {"Name": "armor.leather", "Formula": "slot.body.category_clothing || (slot.body.category_armor && slot.body.armorHpBonus < 40)"},
        {"Name": "armor.light", "Formula": "armor.leather"},
        {"Name": "armor.cloth", "Formula": "slot.body.category_robe"},
    ]
    formula_matrices: dict[str, list[list[int]]] = {}

    if response_mode == "matrix":
        pre_response_variables.extend(matrix_response_variables(penetration_ceiling))
        formula_matrices["armorResponsePermille"] = armor_response_matrix(bundle.get("armor_response", {}))
        response_rules = matrix_damage_response_rules()
    elif response_mode == "rules":
        response_rules = damage_response_rules(
            bundle.get("armor_response", {}),
            penetration_ceiling,
        )
    else:
        raise ValueError(f"unknown response mode {response_mode!r}")

    settings = {
        "_note": (
            f"Generated from sim inputs. slot_mode={slot_mode}. profile={profile}. response_mode={response_mode}. "
            "Exact offsets and/or scan ranges must be confirmed in live memory before trust. "
            "Action classification is weapon-family based and should be "
            "replaced or gated once true action/ability context is mapped."
        ),
        "RewriteObservedDamage": True,
        "AffectAllies": True,
        "AffectFoes": True,
        "InferAttackerFromRecentUnits": False,
        "LogResolvedRuntimeContext": True,
        "ItemCatalogPath": "item_catalog.csv",
        "FinalDamageFormula": "if(action.present && a.present, basePressure, vanillaDamage)",
        "FormulaPreResponseVariables": pre_response_variables,
        "FormulaDerivedVariables": [
            {"Name": "basePressure", "Formula": routine_formula},
        ],
        "FormulaTraceVariables": trace_variables(response_mode),
        "FormulaMatrices": formula_matrices,
        "AttackerEquipmentSlots": attacker_slots,
        "EquipmentSlots": target_slots,
        "ActionSignalRules": family_rules,
        "ApplyDamageResponseRules": True,
        "MinDamageResponsePermille": permille(float(clamp[0])),
        "MaxDamageResponsePermille": permille(float(clamp[1])),
        "DamageResponseChipFloor": int(calc.get("chip_floor", 1)),
        "DamageResponseRules": response_rules,
    }
    return apply_profile(settings, profile)


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate battle-runtime-settings JSON from sim inputs.")
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--slot-mode", choices=("exact", "scan"), default="exact")
    parser.add_argument("--profile", choices=("policy", "live-noop"), default="policy")
    parser.add_argument("--response-mode", choices=("rules", "matrix"), default="rules")
    parser.add_argument(
        "--docs-example",
        type=Path,
        default=ROOT / "docs" / "modding" / "examples" / "battle-runtime-settings.v0.2.generated.example.json",
    )
    args = parser.parse_args()

    bundle = json.loads(args.source.read_text(encoding="utf-8"))
    settings = build_settings(bundle, args.slot_mode, args.profile, args.response_mode)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(settings, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {args.output}")

    if args.docs_example:
        args.docs_example.parent.mkdir(parents=True, exist_ok=True)
        args.docs_example.write_text(json.dumps(settings, indent=2) + "\n", encoding="utf-8")
        print(f"wrote {args.docs_example}")

    print(
        "rules: "
        f"families={len(settings['ActionSignalRules'])} "
        f"responses={len(settings['DamageResponseRules'])} "
        f"matrices={len(settings.get('FormulaMatrices', {}))}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
