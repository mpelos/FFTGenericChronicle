#!/usr/bin/env python3
"""Deterministic damage simulator for Generic Chronicle formula design.

This is a design harness, not an implementation proof. It consumes a pinned
JSON input bundle so GPT and Claude can run independent implementations against
the same scenarios and compare arithmetic.
"""
from __future__ import annotations

import argparse
import csv
import json
import math
import sys
from pathlib import Path
from typing import Any

PHYSICAL_TYPES = {"swing", "thrust", "crush", "missile"}
RAW_DAMAGE_EXEMPT_FAMILIES = {"instrument"}
DUAL_WIELD_STRESS_FAMILIES = {"knife", "ninja_blade"}
VOLATILE_FAMILIES = {"axe", "flail", "bag"}

REPRESENTATIVE_ATTACKERS = {
    "sword": "Knight",
    "knight_sword": "Knight",
    "katana": "Samurai",
    "knife": "Thief",
    "ninja_blade": "Ninja",
    "longbow": "Archer",
    "crossbow": "Archer",
    "gun": "Chemist",
    "spear": "Dragoon",
    "staff": "White Mage",
    "rod": "Black Mage",
    "pole": "Mystic",
    "axe": "Geomancer",
    "flail": "Squire",
    "fists": "Monk",
    "instrument": "Bard",
    "book": "Mystic",
    "cloth_weapon": "Dancer",
    "bag": "Dancer",
}

PREFERRED_TARGETS = {
    "plate": ("Knight", "Dragoon"),
    "mail": ("Squire", "Geomancer", "Samurai"),
    "leather": ("Archer", "Thief", "Ninja", "Chemist"),
    "cloth": ("Black Mage", "White Mage", "Time Mage", "Summoner", "Mystic"),
}


def stat(source: dict[str, Any], name: str, default: int = 0) -> int:
    aliases = {
        "speed": ("speed", "spd"),
    }
    keys = aliases.get(name, (name,))
    for key in keys:
        if key in source:
            return int(source[key])
    return default


def clamp(value: float, minimum: float, maximum: float) -> float:
    return max(minimum, min(maximum, value))


def routine_values(action: dict[str, Any], attacker: dict[str, Any]) -> list[int]:
    """Return raw on-hit values before type/status/element/Zodiac responses."""

    routine = action["routine"]
    pa = stat(attacker, "pa")
    ma = stat(attacker, "ma")
    speed = stat(attacker, "speed")
    brave = stat(attacker, "brave", 70)
    faith = stat(attacker, "faith", 70)
    wp = float(action.get("wp", action.get("power", 0)))
    power = float(action.get("power", action.get("y", wp)))

    if routine == "pa_wp":
        return [pa * wp]
    if routine in {"brave_pa_wp", "br_pa_wp"}:
        return [math.floor(pa * brave / 100) * wp]
    if routine in {"speed_pa_wp", "spd_pa_wp"}:
        return [math.floor((pa + speed) / 2) * wp]
    if routine == "ma_wp":
        return [ma * wp]
    if routine == "wp_wp":
        return [wp * wp]
    if routine in {"random_pa_wp", "rdm_pa_wp"}:
        return [roll * wp for roll in range(1, max(pa, 1) + 1)]
    if routine in {"brave_pa_pa", "br_pa_pa"}:
        return [math.floor(pa * brave / 100) * pa]
    if routine in {"pa_ma_avg_wp", "pampa_wp"}:
        return [math.floor((pa + ma) / 2) * wp]
    if routine == "faith_ma_power":
        target_faith = int(action.get("target_faith", faith))
        raw = ma * power
        return [math.floor(raw * faith * target_faith / 10000)]
    if routine == "fixed_power":
        return [power]

    raise ValueError(f"unknown routine: {routine}")


def type_response(
    action: dict[str, Any], target: dict[str, Any], spec: dict[str, Any]
) -> float:
    armor_class = target.get("armor_class", "neutral")
    damage_type = action.get("damage_type", "pure")

    responses = spec.get("type_responses", {})
    if armor_class not in responses:
        if armor_class == "neutral":
            return 1.0
        raise KeyError(f"missing armor class response: {armor_class}")
    if damage_type not in responses[armor_class]:
        raise KeyError(f"missing damage type response: {armor_class}/{damage_type}")
    base = float(responses[armor_class][damage_type])
    penetration = float(action.get("penetration", 0.0))
    ceiling = spec.get("penetration_ceiling")
    if ceiling is not None and base < float(ceiling):
        return base + penetration * (float(ceiling) - base)
    return base


def response_layers(
    action: dict[str, Any], target: dict[str, Any], spec: dict[str, Any]
) -> list[dict[str, float | str]]:
    damage_type = action.get("damage_type", "pure")
    axis = action.get("axis")
    if axis is None:
        axis = "physical" if damage_type in PHYSICAL_TYPES else "magic"

    modifiers = spec.get("modifiers", {})
    layers: list[dict[str, float | str]] = [
        {"name": "type", "multiplier": type_response(action, target, spec)}
    ]

    if axis == "physical" and target.get("protect"):
        layers.append({"name": "protect", "multiplier": float(modifiers.get("protect", 0.5))})
    if axis in {"magic", "spirit"} and target.get("shell"):
        layers.append({"name": "shell", "multiplier": float(modifiers.get("shell", 0.5))})

    layers.append(
        {
            "name": "element",
            "multiplier": float(target.get("element_multiplier", action.get("element_multiplier", 1.0))),
        }
    )
    layers.append(
        {
            "name": "zodiac",
            "multiplier": float(target.get("zodiac_multiplier", action.get("zodiac_multiplier", 1.0))),
        }
    )
    return layers


def total_multiplier(layers: list[dict[str, float | str]], spec: dict[str, Any]) -> tuple[float, float]:
    uncapped = 1.0
    for layer in layers:
        uncapped *= float(layer["multiplier"])

    stacking = spec.get("stacking", {})
    minimum = float(stacking.get("min_total_multiplier", 0.0))
    maximum = float(stacking.get("max_total_multiplier", 999.0))
    return uncapped, clamp(uncapped, minimum, maximum)


def apply_damage(value: float, multiplier: float, chip_floor: int) -> int:
    final = math.floor(value * multiplier)
    if value > 0 and final < chip_floor:
        return chip_floor
    return max(0, min(65535, final))


def calculate_damage(scenario: dict[str, Any], spec: dict[str, Any]) -> dict[str, Any]:
    attacker = scenario["attacker"]
    target = scenario["target"]
    action = scenario["action"]
    pressure_multiplier = float(action.get("pressure_multiplier", 1.0))
    raw_values = [value * pressure_multiplier for value in routine_values(action, attacker)]
    layers = response_layers(action, target, spec)
    uncapped, capped = total_multiplier(layers, spec)
    chip_floor = int(spec.get("chip_floor", 1))
    routine = action["routine"]
    is_random_pa = routine in {"random_pa_wp", "rdm_pa_wp"}
    if is_random_pa:
        pa = stat(attacker, "pa")
        wp = float(action.get("wp", action.get("power", 0)))
        expected_raw = ((pa + 1) / 2) * wp * pressure_multiplier
        expected_value = apply_damage(expected_raw, capped, chip_floor)
        base_value: int | float = int(expected_raw) if expected_raw.is_integer() else expected_raw
    else:
        final_values = [apply_damage(value, capped, chip_floor) for value in raw_values]
        expected = sum(final_values) / len(final_values)
        expected_value = int(expected) if expected.is_integer() else expected
        base = sum(raw_values) / len(raw_values)
        base_value = int(base) if base.is_integer() else base

    final_values = [apply_damage(value, capped, chip_floor) for value in raw_values]
    hit_count = int(action.get("hit_count", 1))
    hit_rate = float(action.get("hit_rate", 1.0))
    expected_after_hits: int | float = expected_value * hit_count
    expected_after_hit_rate = expected_after_hits * hit_rate
    if isinstance(expected_after_hit_rate, float) and expected_after_hit_rate.is_integer():
        expected_after_hit_rate = int(expected_after_hit_rate)

    return {
        "scenario_id": scenario.get("scenario_id", ""),
        "family": action.get("family", ""),
        "routine": action.get("routine", ""),
        "damage_type": action.get("damage_type", ""),
        "armor_class": target.get("armor_class", ""),
        "type_response": float(layers[0]["multiplier"]),
        "base_pressure": base_value,
        "uncapped_total_multiplier": uncapped,
        "total_multiplier": capped,
        "damage_on_hit": expected_after_hits,
        "expected_damage_on_hit": expected_after_hits,
        "expected_damage_after_hit_rate": expected_after_hit_rate,
        "hit_count": hit_count,
        "hit_rate": hit_rate,
        "min": min(final_values) * hit_count,
        "max": max(final_values) * hit_count,
        "samples": len(final_values),
        "layers": layers,
    }


def spec_from_bundle(bundle: dict[str, Any]) -> dict[str, Any]:
    calc = bundle["calc"]
    clamp_values = calc.get("combined_multiplier_clamp", [0.0, 999.0])
    return {
        "version": bundle.get("version", ""),
        "type_responses": bundle["armor_response"],
        "penetration_ceiling": calc.get("penetration_ceiling"),
        "stacking": {
            "min_total_multiplier": clamp_values[0],
            "max_total_multiplier": clamp_values[1],
        },
        "modifiers": {
            "protect": calc.get("protect_multiplier", 0.667),
            "shell": bundle.get("magic", {}).get("shell_multiplier", 0.667),
        },
        "chip_floor": calc.get("chip_floor", 1),
    }


def job_stats(bundle: dict[str, Any], job: str, phase: str) -> dict[str, Any]:
    stats = dict(bundle["jobs"][job]["bands"][phase])
    stats["brave"] = bundle["calc"].get("default_brave", 70)
    stats["faith"] = bundle["calc"].get("default_faith", 70)
    return stats


def target_job_for_armor(bundle: dict[str, Any], armor_class: str) -> str:
    for candidate in PREFERRED_TARGETS.get(armor_class, ()):
        if candidate in bundle["jobs"] and bundle["jobs"][candidate].get("armor_class") == armor_class:
            return candidate
    for name, data in bundle["jobs"].items():
        if data.get("armor_class") == armor_class:
            return name
    return next(iter(bundle["jobs"]))


def attacker_job_for_family(bundle: dict[str, Any], family: str) -> str:
    preferred = REPRESENTATIVE_ATTACKERS.get(family)
    if preferred in bundle["jobs"]:
        return preferred
    return next(iter(bundle["jobs"]))


def scenario_for_family(
    bundle: dict[str, Any],
    family: str,
    attacker_job: str,
    target_armor_class: str,
    phase: str,
    support_context: str = "none",
    action_overrides: dict[str, Any] | None = None,
    attacker_overrides: dict[str, Any] | None = None,
    target_overrides: dict[str, Any] | None = None,
) -> dict[str, Any]:
    family_data = bundle["families"][family]
    target_job = target_job_for_armor(bundle, target_armor_class)
    wp_scalar = float(bundle.get("calc", {}).get("phase_wp_scalar", {}).get(phase, 1.0))
    attacker = job_stats(bundle, attacker_job, phase)
    target = {
        "armor_class": target_armor_class,
        "stats": job_stats(bundle, target_job, phase),
        "zodiac_multiplier": bundle["calc"].get("zodiac", {}).get("neutral", 1.0),
    }
    action = {
        "family": family,
        "routine": family_data["routine"],
        "damage_type": family_data["damage_type"],
        "wp": family_data.get("wp", 0) * wp_scalar,
        "penetration": family_data.get("penetration", 0.0),
        "axis": "physical",
    }
    if attacker_overrides:
        attacker.update(attacker_overrides)
    if target_overrides:
        target.update(target_overrides)
    if action_overrides:
        action.update(action_overrides)
    return {
        "scenario_id": f"{phase}-{support_context}-{family}-vs-{target_armor_class}",
        "phase": phase,
        "attacker_profile": "family-representative",
        "attacker_job": attacker_job,
        "attacker": attacker,
        "target_profile": f"T-{target_armor_class.upper()}",
        "target_job": target_job,
        "target": target,
        "action": action,
        "support_context": support_context,
    }


def calculate_family_damage(
    bundle: dict[str, Any],
    family: str,
    attacker_job: str,
    target_armor_class: str,
    phase: str,
) -> dict[str, Any]:
    scenario = scenario_for_family(bundle, family, attacker_job, target_armor_class, phase)
    result = calculate_damage(scenario, spec_from_bundle(bundle))
    result.update(
        {
            "phase": phase,
            "attacker_job": attacker_job,
            "target_job": scenario["target_job"],
            "target_armor_class": target_armor_class,
        }
    )
    return result


def build_global_sweep(
    bundle: dict[str, Any],
    phases: list[str] | None = None,
    armor_classes: list[str] | None = None,
) -> list[dict[str, Any]]:
    phases = phases or list(bundle["phase_bands"])
    armor_classes = armor_classes or list(bundle["armor_response"])
    result_class = bundle.get("provenance", {}).get("result_class", "")
    rows: list[dict[str, Any]] = []

    for phase in phases:
        for family in bundle["families"]:
            attacker_job = attacker_job_for_family(bundle, family)
            for armor_class in armor_classes:
                scenario = scenario_for_family(bundle, family, attacker_job, armor_class, phase)
                result = calculate_damage(scenario, spec_from_bundle(bundle))
                rows.append(
                    {
                        "scenario_set_version": "scenario-set-v0",
                        "scenario_id": scenario["scenario_id"],
                        "phase": phase,
                        "attacker_profile": scenario["attacker_profile"],
                        "attacker_job": attacker_job,
                        "attacker_level_or_band": phase,
                        "attacker_effective_stats": scenario["attacker"],
                        "attacker_equipment_context": "WotL-fallback",
                        "target_profile": scenario["target_profile"],
                        "target_job": scenario["target_job"],
                        "target_level_or_band": phase,
                        "target_effective_stats": scenario["target"]["stats"],
                        "target_status_or_element_context": "neutral",
                        "support_context": scenario["support_context"],
                        "target_armor_class": armor_class,
                        "formula_or_routine": scenario["action"]["routine"],
                        "damage_spec_version": bundle.get("version", "sim-inputs-v0"),
                        "family": family,
                        "damage_type": scenario["action"]["damage_type"],
                        "type_response": result["type_response"],
                        "damage_on_hit": result["damage_on_hit"],
                        "hit_rate_assumption": "100% on-hit damage; accuracy not modeled in this sweep",
                        "expected_damage_after_hit_rate": result["expected_damage_after_hit_rate"],
                        "min": result["min"],
                        "max": result["max"],
                        "mode_or_expected_value": result["expected_damage_on_hit"],
                        "baseline_comparison": "WotL-fallback / missing-weapon-baseline",
                        "player_signal_check": "pending review against 07",
                        "design_verdict": "pending scorecard",
                        "technical_verdict": result_class,
                        "open_proof_needs": "work/baseline_weapons.csv and Windows 04 proof",
                        "uncapped_total_multiplier": result["uncapped_total_multiplier"],
                        "total_multiplier": result["total_multiplier"],
                        "hit_count": result["hit_count"],
                        "hit_rate": result["hit_rate"],
                    }
                )
        if "magic" in bundle and "Black Mage" in bundle["jobs"]:
            for armor_class in armor_classes:
                rows.append(build_magic_row(bundle, phase, armor_class))
    if "stress" in phases:
        rows.extend(build_stress_engine_rows(bundle, armor_classes))
    return rows


def row_from_scenario(bundle: dict[str, Any], scenario: dict[str, Any]) -> dict[str, Any]:
    result = calculate_damage(scenario, spec_from_bundle(bundle))
    return {
        "scenario_set_version": "scenario-set-v0",
        "scenario_id": scenario["scenario_id"],
        "phase": scenario["phase"],
        "attacker_profile": scenario["attacker_profile"],
        "attacker_job": scenario["attacker_job"],
        "attacker_level_or_band": scenario["phase"],
        "attacker_effective_stats": scenario["attacker"],
        "attacker_equipment_context": "WotL-fallback",
        "target_profile": scenario["target_profile"],
        "target_job": scenario["target_job"],
        "target_level_or_band": scenario["phase"],
        "target_effective_stats": scenario["target"]["stats"],
        "target_status_or_element_context": "neutral",
        "support_context": scenario["support_context"],
        "target_armor_class": scenario["target"]["armor_class"],
        "formula_or_routine": scenario["action"]["routine"],
        "damage_spec_version": bundle.get("version", "sim-inputs-v0"),
        "family": scenario["action"]["family"],
        "damage_type": scenario["action"]["damage_type"],
        "type_response": result["type_response"],
        "damage_on_hit": result["damage_on_hit"],
        "hit_rate_assumption": "stress assumption; verify in detailed proposal",
        "expected_damage_after_hit_rate": result["expected_damage_after_hit_rate"],
        "min": result["min"],
        "max": result["max"],
        "mode_or_expected_value": result["expected_damage_after_hit_rate"],
        "baseline_comparison": "WotL-fallback / missing-weapon-baseline",
        "player_signal_check": "pending review against 07",
        "design_verdict": "pending scorecard",
        "technical_verdict": bundle.get("provenance", {}).get("result_class", ""),
        "open_proof_needs": "stress support assumptions and Windows 04 proof",
        "uncapped_total_multiplier": result["uncapped_total_multiplier"],
        "total_multiplier": result["total_multiplier"],
        "hit_count": result["hit_count"],
        "hit_rate": result["hit_rate"],
    }


def build_stress_engine_rows(bundle: dict[str, Any], armor_classes: list[str]) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    phase = "stress"
    engines = bundle.get("stress_engines", {})

    stress_specs = [
        (
            "two_hands",
            ["sword", "knight_sword", "katana", "spear"],
            {"pressure_multiplier": float(engines.get("two_hands", 2.0))},
            {},
        ),
        (
            "two_swords",
            ["knife", "ninja_blade"],
            {"hit_count": int(engines.get("two_swords_hits", 2))},
            {},
        ),
        (
            "attack_boost",
            [family for family, data in bundle["families"].items() if data["routine"] != "wp_wp"],
            {"pressure_multiplier": float(engines.get("attack_boost", 4 / 3))},
            {},
        ),
        (
            "high_brave",
            ["knight_sword", "katana", "fists"],
            {},
            {"brave": int(engines.get("high_brave", 97))},
        ),
    ]

    for support_context, families, action_overrides, attacker_overrides in stress_specs:
        for family in families:
            if family not in bundle["families"]:
                continue
            attacker_job = attacker_job_for_family(bundle, family)
            for armor_class in armor_classes:
                scenario = scenario_for_family(
                    bundle,
                    family,
                    attacker_job,
                    armor_class,
                    phase,
                    support_context=support_context,
                    action_overrides=action_overrides,
                    attacker_overrides=attacker_overrides,
                )
                rows.append(row_from_scenario(bundle, scenario))

    for family in ["knife", "ninja_blade", "longbow", "crossbow", "gun"]:
        if family not in bundle["families"]:
            continue
        scenario = scenario_for_family(
            bundle,
            family,
            attacker_job_for_family(bundle, family),
            "leather",
            phase,
            support_context="accuracy_evasive",
            action_overrides={"hit_rate": float(engines.get("accuracy_evasive_hitrate", 0.75))},
        )
        rows.append(row_from_scenario(bundle, scenario))
    return rows


def spell_tier_for_phase(phase: str) -> str:
    if phase == "early":
        return "low"
    if phase == "mid":
        return "mid"
    return "high"


def build_magic_row(bundle: dict[str, Any], phase: str, armor_class: str) -> dict[str, Any]:
    caster_job = "Black Mage"
    target_job = target_job_for_armor(bundle, armor_class)
    caster = job_stats(bundle, caster_job, phase)
    target = job_stats(bundle, target_job, phase)
    tier = spell_tier_for_phase(phase)
    spell_power = bundle["magic"]["sample_spells"][tier]
    faith = bundle["calc"].get("default_faith", 70)
    faith_product = faith * faith / 10000
    faith_factor = max(float(bundle["magic"].get("faith_factor_floor", 0.0)), faith_product)
    raw = spell_power * caster["ma"] * faith_factor
    zodiac = bundle["calc"].get("zodiac", {}).get("neutral", 1.0)
    clamp_values = bundle["calc"].get("combined_multiplier_clamp", [0.0, 999.0])
    total = clamp(zodiac, float(clamp_values[0]), float(clamp_values[1]))
    damage = apply_damage(round(raw), total, int(bundle["calc"].get("chip_floor", 1)))
    return {
        "scenario_set_version": "scenario-set-v0",
        "scenario_id": f"{phase}-black_magic_{tier}-vs-{armor_class}",
        "phase": phase,
        "attacker_profile": "A-MAG",
        "attacker_job": caster_job,
        "attacker_level_or_band": phase,
        "attacker_effective_stats": caster,
        "attacker_equipment_context": "WotL-fallback spell",
        "target_profile": f"T-{armor_class.upper()}",
        "target_job": target_job,
        "target_level_or_band": phase,
        "target_effective_stats": target,
        "target_status_or_element_context": "neutral Faith/Shell/element/Zodiac",
        "support_context": "none",
        "target_armor_class": armor_class,
        "formula_or_routine": bundle["magic"]["routine"],
        "damage_spec_version": bundle.get("version", "sim-inputs-v0"),
        "family": f"black_magic_{tier}",
        "damage_type": "magic",
        "type_response": 1.0,
        "damage_on_hit": damage,
        "hit_rate_assumption": "100% on-hit damage; CT/accuracy not modeled in this sweep",
        "expected_damage_after_hit_rate": damage,
        "min": damage,
        "max": damage,
        "mode_or_expected_value": damage,
        "baseline_comparison": "WotL-fallback / missing-weapon-baseline",
        "player_signal_check": "pending review against 07",
        "design_verdict": "pending scorecard",
        "technical_verdict": bundle.get("provenance", {}).get("result_class", ""),
        "open_proof_needs": "spell constants, Faith/Shell order, and Windows 04 proof",
        "uncapped_total_multiplier": zodiac,
        "total_multiplier": total,
        "hit_count": 1,
        "hit_rate": 1.0,
    }


def rows_by_group(rows: list[dict[str, Any]]) -> dict[tuple[str, str, str], list[dict[str, Any]]]:
    grouped: dict[tuple[str, str, str], list[dict[str, Any]]] = {}
    for row in rows:
        if row.get("damage_type") not in PHYSICAL_TYPES:
            continue
        key = (str(row["phase"]), str(row["target_armor_class"]), str(row.get("support_context", "none")))
        grouped.setdefault(key, []).append(row)
    return grouped


def build_scorecard(bundle: dict[str, Any], rows: list[dict[str, Any]]) -> dict[str, Any]:
    grouped = rows_by_group(rows)
    near_best_threshold = 0.65
    viable: set[str] = set()
    best_counts: dict[str, int] = {}

    def metric_damage(row: dict[str, Any]) -> float:
        family = str(row["family"])
        value = float(row["expected_damage_after_hit_rate"])
        if family in DUAL_WIELD_STRESS_FAMILIES and row.get("phase") == "stress":
            value *= 2
        if family in VOLATILE_FAMILIES:
            value = max(value, float(row.get("max", value)))
        return value

    for key_rows in grouped.values():
        combat_rows = [row for row in key_rows if row.get("family") not in RAW_DAMAGE_EXEMPT_FAMILIES]
        best_damage = max(float(row["expected_damage_after_hit_rate"]) for row in combat_rows)
        for row in key_rows:
            if row.get("family") in RAW_DAMAGE_EXEMPT_FAMILIES:
                continue
            if metric_damage(row) >= best_damage * near_best_threshold:
                viable.add(str(row["family"]))

    late_stress_groups = {
        key: key_rows for key, key_rows in grouped.items() if key[0] in {"late", "stress"}
    }
    for key_rows in late_stress_groups.values():
        best = max(key_rows, key=lambda row: float(row["expected_damage_after_hit_rate"]))
        best_counts[str(best["family"])] = best_counts.get(str(best["family"]), 0) + 1

    total_late_stress = max(1, len(late_stress_groups))
    dominance_limit = 0.50
    dominant = {
        family: count
        for family, count in best_counts.items()
        if count / total_late_stress > dominance_limit
    }

    ratios = []
    for row in rows:
        target_stats = row.get("target_effective_stats")
        if isinstance(target_stats, dict) and target_stats.get("hp"):
            ratios.append(float(row["expected_damage_after_hit_rate"]) / float(target_stats["hp"]))
    max_hp_ratio = max(ratios) if ratios else 0.0
    scale_pass = max_hp_ratio <= 1.25

    magic_rows = [row for row in rows if str(row.get("damage_type")) == "magic" and row["phase"] in {"late", "stress"}]
    physical_rows = [
        row for row in rows if row.get("damage_type") in PHYSICAL_TYPES and row["phase"] in {"late", "stress"}
    ]
    magic_max = max((float(row["expected_damage_after_hit_rate"]) for row in magic_rows), default=0.0)
    physical_max = max((float(row["expected_damage_after_hit_rate"]) for row in physical_rows), default=1.0)
    magic_ratio = magic_max / physical_max if physical_max else 0.0

    def average_for(armor_class: str, damage_type: str) -> float:
        values = [
            float(row["expected_damage_after_hit_rate"])
            for row in rows
            if row.get("target_armor_class") == armor_class and row.get("damage_type") == damage_type
        ]
        return sum(values) / len(values) if values else 0.0

    plate_crush = average_for("plate", "crush")
    plate_swing = average_for("plate", "swing")
    plate_thrust = average_for("plate", "thrust")
    mail_thrust = average_for("mail", "thrust")
    mail_swing = average_for("mail", "swing")
    plate_pass = plate_crush > plate_swing and plate_crush > plate_thrust and mail_thrust > mail_swing

    all_families = set(bundle.get("families", {})) - RAW_DAMAGE_EXEMPT_FAMILIES
    missing = sorted(all_families - viable)
    return {
        "input_version": bundle.get("version", ""),
        "family_viability": {
            "pass": not missing,
            "near_best_threshold": near_best_threshold,
            "raw_damage_exempt_families": sorted(RAW_DAMAGE_EXEMPT_FAMILIES),
            "dual_wield_stress_families": sorted(DUAL_WIELD_STRESS_FAMILIES),
            "volatile_families_credit_max": sorted(VOLATILE_FAMILIES),
            "viable_families": sorted(viable),
            "missing_families": missing,
        },
        "no_dominance": {
            "pass": not dominant,
            "late_stress_best_counts": dict(sorted(best_counts.items())),
            "dominance_limit_share": dominance_limit,
            "dominant_families": dominant,
        },
        "scale_band": {
            "pass": scale_pass,
            "max_damage_to_target_hp_ratio": max_hp_ratio,
            "limit": 1.25,
        },
        "magic_coexistence": {
            "pass": magic_ratio >= 0.50,
            "magic_to_top_physical_ratio": magic_ratio,
            "magic_max_late_stress": magic_max,
            "physical_max_late_stress": physical_max,
        },
        "plate_matchup_observable": {
            "pass": plate_pass,
            "plate_crush_average": plate_crush,
            "plate_swing_average": plate_swing,
            "plate_thrust_average": plate_thrust,
            "mail_thrust_average": mail_thrust,
            "mail_swing_average": mail_swing,
        },
    }


def run_bundle(bundle: dict[str, Any]) -> list[dict[str, Any]]:
    if "families" in bundle and "armor_response" in bundle and "jobs" in bundle:
        return build_global_sweep(bundle)

    spec = bundle.get("calc_spec") or bundle.get("spec")
    if not isinstance(spec, dict):
        raise ValueError("bundle must contain calc_spec or spec")
    scenarios = bundle.get("scenarios")
    if not isinstance(scenarios, list):
        raise ValueError("bundle must contain scenarios list")
    return [calculate_damage(scenario, spec) for scenario in scenarios]


def write_csv(rows: list[dict[str, Any]], out: Any) -> None:
    preferred = [
        "scenario_set_version",
        "scenario_id",
        "phase",
        "attacker_profile",
        "attacker_job",
        "target_profile",
        "target_job",
        "target_armor_class",
        "family",
        "damage_type",
        "formula_or_routine",
        "damage_on_hit",
        "expected_damage_after_hit_rate",
        "min",
        "max",
        "type_response",
        "uncapped_total_multiplier",
        "total_multiplier",
        "technical_verdict",
    ]
    extras = sorted({key for row in rows for key in row} - set(preferred))
    fields = preferred + extras
    writer = csv.DictWriter(out, fieldnames=fields)
    writer.writeheader()
    for row in rows:
        flat = dict(row)
        for key, value in list(flat.items()):
            if isinstance(value, (dict, list)):
                flat[key] = json.dumps(value, separators=(",", ":"))
        writer.writerow(flat)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Run Generic Chronicle damage simulations.")
    parser.add_argument("bundle", type=Path, help="Path to pinned sim input JSON bundle")
    parser.add_argument("--format", choices=("json", "csv"), default="json")
    parser.add_argument("--scorecard", action="store_true", help="Include metrics scorecard")
    args = parser.parse_args(argv)

    with args.bundle.open(encoding="utf-8") as fh:
        bundle = json.load(fh)
    rows = run_bundle(bundle)

    if args.format == "json":
        payload: Any = rows
        if args.scorecard:
            payload = {"rows": rows, "scorecard": build_scorecard(bundle, rows)}
        json.dump(payload, sys.stdout, indent=2)
        sys.stdout.write("\n")
    else:
        write_csv(rows, sys.stdout)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
