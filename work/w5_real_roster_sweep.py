#!/usr/bin/env python3
"""Generate W5/F5 real-roster sweep evidence.

This is a design harness. It aggregates accepted W3 proxy rows plus the parent
damage simulator into party-level W5 rows. It does not claim implementation
truth; the output is evidence for doc W5 and W6 tuning.
"""
from __future__ import annotations

import importlib.util
import json
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]


def load_json(path: str) -> Any:
    return json.loads((ROOT / path).read_text())


def load_sim_damage():
    spec = importlib.util.spec_from_file_location("sim_damage", ROOT / "tools" / "sim_damage.py")
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load tools/sim_damage.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


sim = load_sim_damage()

PARENT = load_json("work/sim-inputs-v0.2.1.json")
N1 = load_json("work/sim-inputs-n1-real-roster-v0.json")
W4 = load_json("work/gpt-w4-t21-populated-incidence-v0.json")
SC = load_json("work/gpt-squire-chemist-concrete-v0.json")
KA = load_json("work/gpt-knight-archer-concrete-v0.json")
MT = load_json("work/gpt-monk-thief-concrete-v0.json")
OD = load_json("work/gpt-orator-dragoon-concrete-v0.json")
SN = load_json("work/gpt-samurai-ninja-concrete-v0.json")
VR = load_json("work/gpt-vanguard-ramza-concrete-v0.json")
WB = load_json("work/gpt-wm-bm-concrete-v0.json")
SG = load_json("work/gpt-summoner-geomancer-concrete-v0.json")
TM = load_json("work/gpt-time-mystic-concrete-v0.json")
NE = load_json("work/gpt-necromancer-concrete-v0.json")
BD = load_json("work/gpt-bard-dancer-concrete-v0.json")
RSM = load_json("work/gpt-physical-foundation-rsm-concrete-v0.json")

ARMOR_MIXES = {
    "floor_0a": {"leather": 0.65, "cloth": 0.25, "mail": 0.10},
    "first_specialist_b": {"plate": 0.20, "leather": 0.40, "cloth": 0.20, "mail": 0.20},
    "mitigation_c": {"plate": 0.35, "mail": 0.25, "leather": 0.20, "cloth": 0.20},
    "vertical_mail_c": {"mail": 0.40, "leather": 0.25, "plate": 0.20, "cloth": 0.15},
    "cluster_d": {"plate": 0.25, "mail": 0.25, "leather": 0.25, "cloth": 0.25},
    "spread_d": {"leather": 0.40, "cloth": 0.25, "mail": 0.20, "plate": 0.15},
    "boss_e": {"plate": 0.45, "mail": 0.25, "leather": 0.20, "cloth": 0.10},
    "undead_e": {"mail": 0.35, "cloth": 0.30, "plate": 0.20, "leather": 0.15},
}

BAND_PHASE = {"0": "early", "A": "early", "B": "mid", "C": "mid", "D": "late", "E": "late"}
WEAK_FAMILIES = ["axe", "flail", "book", "instrument", "cloth_weapon", "bag", "gun", "pole"]
RAW_DAMAGE_EXEMPT_FAMILIES = {"instrument"}


def weighted(table: dict[str, float | int], armor_mix: dict[str, float]) -> float:
    return round(sum(float(table.get(armor, 0)) * weight for armor, weight in armor_mix.items()), 3)


def armor_list(values: list[float | int]) -> dict[str, float | int]:
    return dict(zip(["plate", "mail", "leather", "cloth"], values, strict=True))


def scale_table(table: dict[str, float | int], multiplier: float) -> dict[str, float]:
    return {armor: round(float(value) * multiplier, 3) for armor, value in table.items()}


def row_by(rows: list[dict[str, Any]], **criteria: Any) -> dict[str, Any]:
    for row in rows:
        if all(row.get(key) == value for key, value in criteria.items()):
            return row
    raise KeyError(criteria)


def attacker_stats(job_or_profile: str, phase: str) -> dict[str, Any]:
    profiles = N1["real_roster_profiles"]
    if job_or_profile in profiles:
        stats = dict(profiles[job_or_profile]["stats"])
    else:
        stats = dict(PARENT["jobs"][job_or_profile]["bands"][phase])
    stats["brave"] = PARENT["calc"].get("default_brave", 70)
    stats["faith"] = PARENT["calc"].get("default_faith", 70)
    return stats


def weapon_table(
    job_or_profile: str,
    family: str,
    phase: str,
    *,
    pressure_multiplier: float = 1.0,
    hit_count: int = 1,
    hit_rate: float = 1.0,
) -> dict[str, int | float]:
    family_data = PARENT["families"][family]
    table: dict[str, int | float] = {}
    for armor in PARENT["armor_response"]:
        target_job = sim.target_job_for_armor(PARENT, armor)
        scenario = {
            "scenario_id": f"w5-{phase}-{job_or_profile}-{family}-vs-{armor}",
            "phase": phase,
            "attacker_profile": job_or_profile,
            "attacker_job": job_or_profile,
            "attacker": attacker_stats(job_or_profile, phase),
            "target_profile": f"T-{armor}",
            "target_job": target_job,
            "target": {
                "armor_class": armor,
                "stats": sim.job_stats(PARENT, target_job, phase),
                "zodiac_multiplier": PARENT["calc"].get("zodiac", {}).get("neutral", 1.0),
            },
            "action": {
                "family": family,
                "routine": family_data["routine"],
                "damage_type": family_data["damage_type"],
                "wp": family_data.get("wp", 0)
                * float(PARENT["calc"]["phase_wp_scalar"].get(phase, 1.0)),
                "penetration": family_data.get("penetration", 0.0),
                "axis": "physical",
                "pressure_multiplier": pressure_multiplier,
                "hit_count": hit_count,
                "hit_rate": hit_rate,
            },
            "support_context": "w5",
        }
        table[armor] = sim.calculate_damage(scenario, sim.spec_from_bundle(PARENT))[
            "expected_damage_after_hit_rate"
        ]
    return table


def avg_weapon(job: str, family: str, phase: str, armor_mix: dict[str, float], **kwargs: Any) -> float:
    return weighted(weapon_table(job, family, phase, **kwargs), armor_mix)


def source_ref(source: str, path: str, note: str) -> dict[str, str]:
    return {"source": source, "source_path": path, "note": note}


def action_derivation(kind: str, source: str, source_path: str, formula: str, **fields: Any) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "kind": kind,
        "source": source,
        "source_path": source_path,
        "formula": formula,
    }
    payload.update(fields)
    return payload


def black_magic(action: str, phase: str) -> float:
    return float(row_by(WB["black_phase_rows"], action=action, phase=phase)["neutral"])


def black_magic_kwargs(action: str, phase: str) -> dict[str, Any]:
    row = row_by(WB["black_phase_rows"], action=action, phase=phase)
    return {
        "damage": float(row["neutral"]),
        "action_id": f"Black Magic:{action}",
        "derivation": action_derivation(
            "single_target_magic",
            "work/gpt-wm-bm-concrete-v0.json",
            f"black_phase_rows[action={action},phase={phase}]",
            "round(ma * k * 0.6) at default 70/70 Faith, neutral Zodiac",
            ma=row["ma"],
            k=row["k"],
            neutral=row["neutral"],
            shell=row["shell"],
            element_weak_x2=row["element_weak_x2"],
        ),
        "axis_provenance": {
            "damage": [
                source_ref(
                    "docs/job-balance/42-white-black-mage-concrete-v0.md",
                    f"Black Mage phase row {action}/{phase}",
                    "Accepted concrete-provisional Black Magic neutral damage row.",
                )
            ]
        },
    }


def white_value(action: str, phase: str) -> float:
    return float(row_by(WB["white_phase_rows"], action=action, phase=phase)["neutral"])


def white_derivation(action: str, phase: str) -> dict[str, Any]:
    row = row_by(WB["white_phase_rows"], action=action, phase=phase)
    return action_derivation(
        "single_target_healing",
        "work/gpt-wm-bm-concrete-v0.json",
        f"white_phase_rows[action={action},phase={phase}]",
        "round(ma * k * 0.6) at default 70/70 Faith, neutral Zodiac",
        ma=row["ma"],
        k=row["k"],
        neutral=row["neutral"],
        shell=row["shell"],
    )


def white_axis_provenance(action: str, phase: str, axis: str) -> dict[str, list[dict[str, str]]]:
    return {
        axis: [
            source_ref(
                "docs/job-balance/42-white-black-mage-concrete-v0.md",
                f"White Mage phase row {action}/{phase}",
                "Accepted concrete-provisional White Magic HP value.",
            )
        ]
    }


def summon_total(skill: str, phase: str, field: str = "expected_total") -> float:
    return float(row_by(SG["summon_phase_rows"], skill=skill, phase=phase)[field])


def summon_area_kwargs(skill: str, phase: str) -> dict[str, Any]:
    row = row_by(SG["summon_phase_rows"], skill=skill, phase=phase)
    return {
        "area_damage": float(row["expected_total"]),
        "area_per_target": float(row["per_target"]),
        "area_targets": float(row["expected_targets"]),
        "area_basis": f"{skill} {phase} accepted expected_targets",
        "action_id": f"Summon:{skill}",
        "derivation": action_derivation(
            "area_magic",
            "work/gpt-summoner-geomancer-concrete-v0.json",
            f"summon_phase_rows[skill={skill},phase={phase}]",
            "per_target = round(ma * k * 0.6); normalized_total = per_target * expected_targets",
            ma=row["ma"],
            k=row["k"],
            per_target=row["per_target"],
            expected_targets=row["expected_targets"],
            expected_total=row["expected_total"],
            max_realistic_targets=row["max_realistic_targets"],
            max_realistic_total=row["max_realistic_total"],
        ),
        "axis_provenance": {
            "damage": [
                source_ref(
                    "docs/job-balance/43-summoner-geomancer-concrete-v0.md",
                    f"Summoner phase row {skill}/{phase}",
                    "Accepted concrete-provisional summon target-count row.",
                )
            ]
        },
    }


def meteor_area_kwargs(phase: str) -> dict[str, Any]:
    row = row_by(TM["time_meteor_phase_rows"], skill="Meteor", phase=phase)
    return {
        "area_damage": float(row["expected_total"]),
        "area_per_target": float(row["per_target"]),
        "area_targets": float(row["expected_targets"]),
        "area_basis": f"Meteor {phase} accepted expected_targets",
        "action_id": "Time Magic:Meteor",
        "derivation": action_derivation(
            "area_magic",
            "work/gpt-time-mystic-concrete-v0.json",
            f"time_meteor_phase_rows[skill=Meteor,phase={phase}]",
            "per_target = round(ma * k * 0.6); normalized_total = per_target * expected_targets",
            ma=row["ma"],
            k=row["k"],
            per_target=row["per_target"],
            expected_targets=row["expected_targets"],
            expected_total=row["expected_total"],
            max_targets=row["max_targets"],
            max_total=row["max_total"],
        ),
        "axis_provenance": {
            "damage": [
                source_ref(
                    "docs/job-balance/44-time-mystic-concrete-v0.md",
                    f"Time Mage Meteor phase row {phase}",
                    "Accepted concrete-provisional Meteor area target-count row.",
                )
            ]
        },
    }


def geomancy_adjusted(skill: str, phase: str, armor_mix: dict[str, float]) -> float:
    rows = [r for r in SG["geomancy_phase_rows"] if r["skill"] == skill and r["phase"] == phase]
    table = {r["target_armor"]: r["availability_adjusted_damage"] for r in rows}
    return weighted(table, armor_mix)


def geomancy_derivation(skill: str, phase: str, armor_mix_id: str) -> dict[str, Any]:
    return action_derivation(
        "terrain_availability_adjusted",
        "work/gpt-summoner-geomancer-concrete-v0.json",
        f"geomancy_phase_rows[skill={skill},phase={phase}] mixed by W5 armor_mix_id={armor_mix_id}",
        "sum(armor_mix[armor] * availability_adjusted_damage[armor])",
        skill=skill,
        phase=phase,
    )


def unit(
    name: str,
    job: str,
    role: str,
    *,
    damage: float = 0,
    hit_count: int = 1,
    engine_multiplier: float = 1.0,
    hit_rate: float = 1.0,
    action_multiplier: float | None = None,
    area_damage: float = 0,
    area_per_target: float | None = None,
    area_targets: float | None = None,
    area_basis: str = "no_area",
    sustain: float = 0,
    control: float = 0,
    mobility: float = 0,
    safety: float = 0,
    family: str | None = None,
    action_id: str | None = None,
    derivation: dict[str, Any] | None = None,
    axis_provenance: dict[str, list[dict[str, str]]] | None = None,
    note: str = "",
    tags: list[str] | None = None,
) -> dict[str, Any]:
    divisor = max(hit_count * engine_multiplier, 1e-9)
    per_hit = damage / divisor
    if area_damage:
        per_target = area_per_target if area_per_target is not None else area_damage
        assumed_targets = area_targets if area_targets is not None else 1.0
        normalized_total = area_damage
    else:
        per_target = 0.0
        assumed_targets = 0.0
        normalized_total = 0.0
    action_id = action_id or (family if family is not None else role)
    if derivation is None:
        derivation = action_derivation(
            "w5_proxy_or_direct_table",
            "work/w5_real_roster_sweep.py",
            f"unit(name={name},job={job},role={role})",
            "value carried from accepted concrete row, parent formula table, or explicit W5 proxy note",
        )
    if axis_provenance is None:
        axis_provenance = {}
    return {
        "name": name,
        "job_or_profile": job,
        "role": role,
        "primary_family": family,
        "action_id": action_id,
        "derivation": derivation,
        "axis_provenance": axis_provenance,
        "engine": {
            "hit_count": hit_count,
            "engine_multiplier": round(engine_multiplier, 3),
        },
        "damage": {
            "per_hit": round(per_hit, 3),
            "hit_count": hit_count,
            "engine_multiplier": round(engine_multiplier, 3),
            "hit_rate": round(hit_rate, 3),
            "action_multiplier": None if action_multiplier is None else round(action_multiplier, 3),
            "delivered_per_action": round(damage, 3),
        },
        "area_damage": {
            "per_target": round(per_target, 3),
            "assumed_targets": round(assumed_targets, 3),
            "normalized_total": round(normalized_total, 3),
            "target_count_basis": area_basis,
        },
        "sustain": round(sustain, 3),
        "control": round(control, 3),
        "mobility": round(mobility, 3),
        "safety": round(safety, 3),
        "note": note,
        "tags": tags or [],
    }


def weak_family_floor(phase: str, armor_mix: dict[str, float]) -> dict[str, Any]:
    sword_table = weapon_table("Knight", "sword", phase)
    sword = weighted(sword_table, armor_mix)
    values: dict[str, Any] = {}
    ratios: dict[str, float] = {}
    worst_ratios: dict[str, float] = {}
    for family in WEAK_FAMILIES:
        attacker = sim.attacker_job_for_family(PARENT, family)
        family_table = weapon_table(attacker, family, phase)
        value = weighted(family_table, armor_mix)
        ratios_by_armor = {
            armor: round(float(family_table[armor]) / float(sword_table[armor]), 3)
            for armor, weight in armor_mix.items()
            if weight > 0 and float(sword_table[armor]) > 0
        }
        values[family] = {
            "attacker": attacker,
            "damage": value,
            "delivered_per_action_by_armor": family_table,
            "ratios_to_sword_by_armor": ratios_by_armor,
            "raw_damage_exempt": family in RAW_DAMAGE_EXEMPT_FAMILIES,
        }
        ratios[family] = round(value / sword, 3) if sword else 0
        worst_ratios[family] = min(ratios_by_armor.values()) if ratios_by_armor else 0
    combat_ratios = {
        family: ratio
        for family, ratio in worst_ratios.items()
        if family not in RAW_DAMAGE_EXEMPT_FAMILIES
    }
    weakest = min(worst_ratios, key=worst_ratios.get)
    weakest_combat = min(combat_ratios, key=combat_ratios.get)
    threshold = 0.55
    return {
        "phase": phase,
        "sword_reference_job": "Knight",
        "sword_baseline_family": "sword",
        "sword_baseline_phase_scalar": PARENT["calc"]["phase_wp_scalar"].get(phase, 1.0),
        "sword_reference_damage": sword,
        "sword_delivered_per_action_by_armor": sword_table,
        "aggregation": "worst_case_across_row_armor_mix",
        "threshold": threshold,
        "threshold_status": "provisional_doc11_family_viability_lens",
        "raw_damage_exempt_families": sorted(RAW_DAMAGE_EXEMPT_FAMILIES),
        "family_values": values,
        "average_ratios_to_sword": ratios,
        "worst_case_ratios_to_sword": worst_ratios,
        "weakest_family": weakest,
        "weakest_ratio": worst_ratios[weakest],
        "weakest_combat_family": weakest_combat,
        "weakest_combat_ratio": combat_ratios[weakest_combat],
        "passes_threshold": combat_ratios[weakest_combat] >= threshold,
    }


def unit_axis_value(unit_data: dict[str, Any], axis: str) -> float:
    if axis == "damage":
        return max(
            float(unit_data["damage"]["delivered_per_action"]),
            float(unit_data["area_damage"]["normalized_total"]),
        )
    if axis == "safety":
        return float(unit_data["safety"])
    return float(unit_data[axis])


def dominance_report(units: list[dict[str, Any]], extra_flags: list[str]) -> dict[str, Any]:
    axes = ["damage", "sustain", "control", "mobility", "safety"]
    best_or_tied: dict[str, list[str]] = {u["name"]: [] for u in units}
    worst: dict[str, list[str]] = {u["name"]: [] for u in units}
    eps = 0.001
    for axis in axes:
        values = {u["name"]: unit_axis_value(u, axis) for u in units}
        max_value = max(values.values())
        min_value = min(values.values())
        for name, value in values.items():
            if abs(value - max_value) <= eps:
                best_or_tied[name].append(axis)
            if abs(value - min_value) <= eps:
                worst[name].append(axis)
    majority = [
        name
        for name in best_or_tied
        if len(best_or_tied[name]) >= 3 and not worst[name]
    ]
    return {
        "best_or_tied_axes": {k: v for k, v in best_or_tied.items() if v},
        "worst_axes": {k: v for k, v in worst.items() if v},
        "majority_pareto_dominant": majority,
        "stress_probe_involved": any("STRESS-PROBE" in u.get("tags", []) for u in units),
        "assumption_gated_involved": any(
            "AI-BEHAVIOR-ASSUMPTION" in u.get("tags", []) for u in units
        ),
        "flags": extra_flags,
    }


def proxy_axis_provenance(units: list[dict[str, Any]]) -> dict[str, list[dict[str, Any]]]:
    provenance: dict[str, list[dict[str, Any]]] = {}
    for axis in ["sustain", "control", "mobility", "safety"]:
        entries: list[dict[str, Any]] = []
        for unit_data in units:
            value = unit_axis_value(unit_data, axis)
            if abs(value) <= 0.001:
                continue
            entries.append(
                {
                    "unit": unit_data["name"],
                    "job_or_profile": unit_data["job_or_profile"],
                    "value": round(value, 3),
                    "sources": unit_data.get("axis_provenance", {}).get(
                        axis,
                        [
                            source_ref(
                                "work/w5_real_roster_sweep.py",
                                f"unit(name={unit_data['name']}).{axis}",
                                "W5 local proxy score; see unit note and accepted job document cited by the unit derivation.",
                            )
                        ],
                    ),
                }
            )
        provenance[axis] = entries
    return provenance


def belief_oil_payload(row_id: str) -> dict[str, Any]:
    if row_id not in {"W5-P3-C-BELIEF-OIL", "W5-P5-D-CONV"}:
        return {"present": False}
    return {
        "present": True,
        "source_chain": ["Mystic Belief", "Geomancer Magma Surge/Oil", "Summoner fire area"],
        "baseline_total": 415,
        "fire_multiplier": 2.30,
        "resulting_total": 681,
        "vs_baseline": round(681 / 415, 3),
        "vectors": [
            {
                "action": "Ifrit",
                "fire_multiplier": 2.30,
                "weak_oil_total": 486,
                "resulting_total": 558,
                "vs_baseline": round(558 / 415, 3),
            },
            {
                "action": "Salamander",
                "fire_multiplier": 2.30,
                "weak_oil_total": 594,
                "resulting_total": 681,
                "vs_baseline": round(681 / 415, 3),
            },
        ],
        "max_ratio_to_top_physical": round(681 / 415, 3),
    }


def floor_envelope(row_id: str) -> dict[str, Any] | None:
    if row_id == "W5-FLOOR-0A":
        return {
            "party_model": "P0_naive_thematic",
            "routing": "ordinary_non_optimized",
            "jp_boost": "removed_not_available",
            "guide_routing": False,
            "optimized_rsm": False,
            "deep_secondaries_on_shallow_chassis": False,
            "equipment_policy": "existing equipment only; no gil/economy assumptions",
            "bands": ["0", "A"],
            "rough_level": "1-7",
            "ordinary_donor_jp": {"0": "0-80", "A": "150-250"},
        }
    if row_id == "W5-FLOOR-B":
        return {
            "party_model": "P0_naive_thematic",
            "routing": "ordinary_non_optimized",
            "jp_boost": "removed_not_available",
            "guide_routing": False,
            "optimized_rsm": False,
            "deep_secondaries_on_shallow_chassis": False,
            "equipment_policy": "existing equipment only; no gil/economy assumptions",
            "bands": ["B"],
            "rough_level": "8-15",
            "ordinary_donor_jp": {"B": "350-650"},
        }
    return None


def aggregate(row_id: str, band: str, armor_mix_id: str, units: list[dict[str, Any]]) -> dict[str, Any]:
    handoff = next((r for r in W4["w5_handoff_rows"] if r["id"] == row_id), {})
    armor_mix = ARMOR_MIXES[armor_mix_id]
    phase = BAND_PHASE[band]
    total_damage = sum(float(u["damage"]["delivered_per_action"]) for u in units)
    total_area = sum(float(u["area_damage"]["normalized_total"]) for u in units)
    total_primary_damage = sum(unit_axis_value(u, "damage") for u in units)
    total_sustain = sum(u["sustain"] for u in units)
    total_control = sum(u["control"] for u in units)
    total_safety = sum(u["safety"] for u in units)
    avg_mobility = round(sum(u["mobility"] for u in units) / len(units), 3)
    top_unit = max(units, key=lambda u: unit_axis_value(u, "damage"))
    top_value = unit_axis_value(top_unit, "damage")
    top_share = round(top_value / max(total_primary_damage, 1), 3)
    weak_floor = weak_family_floor(phase, armor_mix)
    belief_oil = belief_oil_payload(row_id)

    flags = []
    if top_share >= 0.45:
        flags.append("single_source_damage_share")
    if total_sustain >= 250 and total_safety >= 250:
        flags.append("sustain_safety_compression")
    if total_control >= 180 and avg_mobility >= 4.2:
        flags.append("control_mobility_compression")
    if not weak_floor["passes_threshold"]:
        flags.append("weak_family_floor_gap")
    if belief_oil["present"] and belief_oil["max_ratio_to_top_physical"] > 1.25:
        flags.append("belief_oil_dominance")
    if row_id in {"W5-P5-E-LATE", "W5-RAMZA-C4-BREADTH"} and any(
        u["job_or_profile"] == "Ramza Chapter 4" for u in units
    ):
        flags.append("ramza_breadth_watch")

    verdict = "watch" if flags else "pass"
    dominance = dominance_report(units, flags)
    if dominance["majority_pareto_dominant"]:
        flags.append("majority_pareto_dominance")
        dominance = dominance_report(units, flags)

    if "belief_oil_dominance" in flags or "majority_pareto_dominance" in flags:
        verdict = "fail"

    return {
        "id": row_id,
        "band": band,
        "phase": phase,
        "armor_mix_id": armor_mix_id,
        "armor_mix": armor_mix,
        "risk_register": handoff.get("risk_register", []),
        "required_axes": handoff.get("axes", []),
        "units": units,
        "floor_envelope": floor_envelope(row_id),
        "proxy_axis_provenance": proxy_axis_provenance(units),
        "totals": {
            "single_target_damage": round(total_damage, 3),
            "area_damage": round(total_area, 3),
            "primary_damage_axis": round(total_primary_damage, 3),
            "sustain": round(total_sustain, 3),
            "control": round(total_control, 3),
            "average_mobility": avg_mobility,
            "safety_defense": round(total_safety, 3),
        },
        "top_source": {
            "unit": top_unit["name"],
            "job_or_profile": top_unit["job_or_profile"],
            "value": round(top_value, 3),
            "share": top_share,
        },
        "weakest_combat_ratio_to_sword": {
            "threshold": weak_floor["threshold"],
            "threshold_status": weak_floor["threshold_status"],
            "family": weak_floor["weakest_combat_family"],
            "family_delivered_per_action_by_armor": weak_floor["family_values"][
                weak_floor["weakest_combat_family"]
            ]["delivered_per_action_by_armor"],
            "sword_baseline_family": weak_floor["sword_baseline_family"],
            "sword_baseline_phase_scalar": weak_floor["sword_baseline_phase_scalar"],
            "sword_delivered_per_action_by_armor": weak_floor["sword_delivered_per_action_by_armor"],
            "aggregation": weak_floor["aggregation"],
            "ratio": weak_floor["weakest_combat_ratio"],
        },
        "floor_proxy_per_weak_family": weak_floor,
        "belief_oil_named_risk": belief_oil,
        "dominance_flags": dominance,
        "verdict": verdict,
    }


def build_rows() -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []

    floor_mix = ARMOR_MIXES["floor_0a"]
    rows.append(
        aggregate(
            "W5-FLOOR-0A",
            "A",
            "floor_0a",
            [
                unit("Ramza C1", "Ramza Chapter 1", "starter flex", damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_1_ramza_sword"], floor_mix), mobility=3, safety=40, family="sword"),
                unit("Squire melee", "Squire", "starter physical", damage=avg_weapon("Squire", "sword", "early", floor_mix), sustain=20, mobility=3, safety=40, family="sword"),
                unit(
                    "Squire utility",
                    "Squire",
                    "starter utility",
                    damage=18,
                    sustain=20,
                    control=8,
                    mobility=3,
                    safety=35,
                    family="flail",
                    action_id="Fundaments:Dash",
                    derivation=action_derivation(
                        "fixed_action_damage",
                        "docs/job-balance/52-squire-chemist-concrete-v0.md",
                        "Squire Dash fixed 18 row",
                        "fixed 18 HP chip action; primary_family is starter flail texture, not a normal flail attack",
                    ),
                ),
                unit("Chemist", "Chemist", "items", damage=avg_weapon("Chemist", "gun", "early", floor_mix), sustain=30, mobility=3, safety=35, family="gun"),
                unit("Trainee", "Squire", "open slot", damage=avg_weapon("Squire", "knife", "early", floor_mix), mobility=3, safety=30, family="knife"),
            ],
        )
    )

    b_mix = ARMOR_MIXES["first_specialist_b"]
    rows.append(
        aggregate(
            "W5-FLOOR-B",
            "B",
            "first_specialist_b",
            [
                unit("Ramza C2", "Ramza Chapter 2", "chapter utility", damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_2_ramza_sword"], b_mix), sustain=35, control=10, mobility=3.2, safety=55, family="sword"),
                unit(
                    "Knight",
                    "Knight",
                    "frontline",
                    damage=weighted(KA["simulation_rows"]["knight_mid_action_damage"]["Guarded Strike sword x0.85"], b_mix),
                    action_multiplier=0.85,
                    control=35,
                    mobility=3,
                    safety=95,
                    family="sword",
                    action_id="Knight:Guarded Strike",
                    derivation=action_derivation(
                        "action_multiplier_weapon_table",
                        "work/gpt-knight-archer-concrete-v0.json",
                        "simulation_rows.knight_mid_action_damage.Guarded Strike sword x0.85",
                        "accepted per-armor Guarded Strike table, x0.85 action multiplier already floored per armor",
                        action_multiplier=0.85,
                    ),
                ),
                unit(
                    "Archer",
                    "Archer",
                    "range",
                    damage=weighted(KA["simulation_rows"]["archer_mid_action_damage"]["Piercing Shot longbow x1.10 pen+0.20"], b_mix),
                    action_multiplier=1.10,
                    control=25,
                    mobility=3.5,
                    safety=60,
                    family="longbow",
                    action_id="Archer:Piercing Shot",
                    derivation=action_derivation(
                        "action_multiplier_weapon_table",
                        "work/gpt-knight-archer-concrete-v0.json",
                        "simulation_rows.archer_mid_action_damage.Piercing Shot longbow x1.10 pen+0.20",
                        "accepted per-armor Piercing Shot table, x1.10 action multiplier plus penetration 0.20 already floored per armor",
                        action_multiplier=1.10,
                        penetration=0.20,
                    ),
                ),
                unit(
                    "White Mage",
                    "White Mage",
                    "healer",
                    damage=white_value("Cure", "mid"),
                    sustain=108,
                    mobility=3,
                    safety=35,
                    action_id="White Magic:Cure/Cura",
                    derivation=white_derivation("Cure", "mid"),
                    axis_provenance={
                        **white_axis_provenance("Cure", "mid", "damage"),
                        **white_axis_provenance("Cura", "mid", "sustain"),
                    },
                ),
                unit(
                    "Black Mage",
                    "Black Mage",
                    "burst",
                    **black_magic_kwargs("Fira", "mid"),
                    mobility=3,
                    safety=30,
                ),
            ],
        )
    )

    rows.append(
        aggregate(
            "W5-P5-B-FULL",
            "B",
            "first_specialist_b",
            [
                unit("Knight shell", "Knight", "durable carrier", damage=avg_weapon("Knight", "sword", "mid", b_mix), control=45, mobility=3, safety=105, family="sword"),
                unit("Monk route", "Monk", "sustain damage", damage=avg_weapon("Monk", "fists", "mid", b_mix), sustain=40, control=10, mobility=3.5, safety=70, family="fists"),
                unit(
                    "Archer utility",
                    "Archer",
                    "range/control",
                    damage=weighted(KA["simulation_rows"]["archer_mid_action_damage"]["Pinning Shot longbow x0.70"], b_mix),
                    action_multiplier=0.70,
                    control=35,
                    mobility=3.5,
                    safety=60,
                    family="longbow",
                    action_id="Archer:Pinning Shot",
                    derivation=action_derivation(
                        "action_multiplier_weapon_table",
                        "work/gpt-knight-archer-concrete-v0.json",
                        "simulation_rows.archer_mid_action_damage.Pinning Shot longbow x0.70",
                        "accepted per-armor Pinning Shot table, x0.70 action multiplier already floored per armor",
                        action_multiplier=0.70,
                    ),
                ),
                unit("Chemist", "Chemist", "instant sustain", damage=avg_weapon("Chemist", "gun", "mid", b_mix), sustain=70, mobility=3, safety=55, family="gun"),
                unit(
                    "White/Black flex",
                    "White Mage",
                    "sustain or burst",
                    damage=black_magic("Fira", "mid"),
                    sustain=76,
                    control=10,
                    mobility=3,
                    safety=35,
                    action_id="Black Magic:Fira / White Magic:Cure",
                    derivation=black_magic_kwargs("Fira", "mid")["derivation"],
                    note="Damage axis uses Fira; sustain axis uses Cure mid as the alternate flex action.",
                    axis_provenance={
                        **black_magic_kwargs("Fira", "mid")["axis_provenance"],
                        **white_axis_provenance("Cure", "mid", "sustain"),
                    },
                ),
            ],
        )
    )

    c_mix = ARMOR_MIXES["mitigation_c"]
    rows.append(
        aggregate(
            "W5-P5-C-MIT",
            "C",
            "mitigation_c",
            [
                unit(
                    "Ramza C3",
                    "Ramza Chapter 3",
                    "hybrid ward",
                    damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_3_spellblade_x0.85"], c_mix),
                    action_multiplier=0.85,
                    sustain=35,
                    control=15,
                    mobility=3.2,
                    safety=65,
                    family="sword",
                    action_id="Ramza:Spellblade Ward",
                    derivation=action_derivation(
                        "chapter_fixed_hybrid_action_table",
                        "work/gpt-vanguard-ramza-concrete-v0.json",
                        "simulation_rows.ramza_weapon_and_hybrid_rows.chapter_3_spellblade_x0.85",
                        "chapter-fixed Ramza C3 mid Spellblade/Ward table, x0.85 action multiplier already floored per armor",
                        action_multiplier=0.85,
                    ),
                ),
                unit("Knight/Monk", "Knight", "mitigation frontline", damage=avg_weapon("Knight", "sword", "mid", c_mix), sustain=40, control=45, mobility=3, safety=125, family="sword"),
                unit("Time/Mystic", "Time Mage", "tempo/control", damage=0, sustain=0, control=90, mobility=4.5, safety=35),
                unit("Summoner/Black", "Summoner", "area pressure", damage=0, **summon_area_kwargs("Titan", "mid"), sustain=0, control=10, mobility=3, safety=30),
                unit(
                    "Geomancer/Orator",
                    "Geomancer",
                    "terrain/status",
                    damage=geomancy_adjusted("Magma Surge", "mid", c_mix),
                    control=45,
                    mobility=3.5,
                    safety=70,
                    family="axe",
                    action_id="Geomancy:Magma Surge",
                    derivation=geomancy_derivation("Magma Surge", "mid", "mitigation_c"),
                ),
            ],
        )
    )

    rows.append(
        aggregate(
            "W5-P3-C-BELIEF-OIL",
            "C",
            "mitigation_c",
            [
                unit("Ramza C3", "Ramza Chapter 3", "hybrid", damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_3_ramza_sword_mid"], c_mix), sustain=35, control=15, mobility=3.2, safety=60, family="sword"),
                unit("Mystic Belief", "Mystic", "faith setup", damage=0, control=65, mobility=3, safety=35),
                unit(
                    "Geomancer Oil",
                    "Geomancer",
                    "oil setup",
                    damage=geomancy_adjusted("Magma Surge", "mid", c_mix),
                    control=55,
                    mobility=3.5,
                    safety=65,
                    family="axe",
                    action_id="Geomancy:Magma Surge/Oil setup",
                    derivation=geomancy_derivation("Magma Surge", "mid", "mitigation_c"),
                    note="Primary family is axe because Geomancer carries axe texture; modeled action is terrain/Oil setup.",
                ),
                unit("Summoner Ifrit", "Summoner", "area payoff", damage=0, **summon_area_kwargs("Ifrit", "mid"), control=0, mobility=3, safety=30),
                unit(
                    "Black Mage fire",
                    "Black Mage",
                    "single burst",
                    **black_magic_kwargs("Firaga", "mid"),
                    mobility=3,
                    safety=30,
                ),
            ],
        )
    )

    d_mix = ARMOR_MIXES["cluster_d"]
    rows.append(
        aggregate(
            "W5-P3-D-CASTER",
            "D",
            "cluster_d",
            [
                unit("Ramza C3", "Ramza Chapter 3", "hybrid", damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_3_ramza_sword_mid"], d_mix), sustain=35, control=15, mobility=3.2, safety=60, family="sword"),
                unit("Summoner Bahamut", "Summoner", "area burst", **summon_area_kwargs("Bahamut", "late"), mobility=3, safety=30),
                unit("Time Mage", "Time Mage", "tempo/meteor", **meteor_area_kwargs("late"), control=90, mobility=4.5, safety=30),
                unit("Mystic", "Mystic", "status/drain", damage=72, sustain=36, control=65, mobility=3, safety=35),
                unit(
                    "White/Bard",
                    "White Mage",
                    "recovery",
                    sustain=white_value("Curaga", "late"),
                    control=20,
                    mobility=3,
                    safety=45,
                    action_id="White Magic:Curaga",
                    derivation=white_derivation("Curaga", "late"),
                    axis_provenance=white_axis_provenance("Curaga", "late", "sustain"),
                ),
            ],
        )
    )

    spread_mix = ARMOR_MIXES["spread_d"]
    rows.append(
        aggregate(
            "W5-P2-D-PHYS",
            "D",
            "spread_d",
            [
                unit("Ramza C3", "Ramza Chapter 3", "hybrid", damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_3_ramza_sword_mid"], spread_mix), sustain=35, control=15, mobility=3.2, safety=60, family="sword"),
                unit("Ninja", "Ninja", "native dual", damage=weighted(SN["simulation_rows"]["ninja_melee"]["ninja_blade_dual_late"], spread_mix), hit_count=2, mobility=4.5, safety=45, family="ninja_blade"),
                unit("Samurai", "Samurai", "doublehand/iaido", damage=weighted(SN["simulation_rows"]["samurai_baseline"]["doublehand_katana_late_x1_80"], spread_mix), engine_multiplier=1.8, control=25, mobility=3, safety=55, family="katana"),
                unit("Dragoon", "Dragoon", "jump reach", damage=weighted(OD["simulation_rows"]["dragoon_spear_damage"]["late_jump_x1_25"], spread_mix), engine_multiplier=1.25, control=10, mobility=4, safety=90, family="spear"),
                unit(
                    "Archer",
                    "Archer",
                    "range accuracy",
                    damage=avg_weapon("Archer", "longbow", "late", spread_mix, hit_rate=0.9),
                    hit_rate=0.9,
                    control=35,
                    mobility=3.8,
                    safety=60,
                    family="longbow",
                    action_id="Archer:Longbow accuracy row",
                    derivation=action_derivation(
                        "weapon_table_with_hit_rate",
                        "work/sim-inputs-v0.2.1.json",
                        "families.longbow with Archer late stats and W5 spread_d armor mix",
                        "parent longbow table with hit_rate 0.90 applied after per-armor floor",
                        hit_rate=0.90,
                    ),
                ),
            ],
        )
    )

    rows.append(
        aggregate(
            "W5-P5-D-CONV",
            "D",
            "cluster_d",
            [
                unit(
                    "Ramza C3",
                    "Ramza Chapter 3",
                    "hybrid",
                    damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_3_ramza_sword_mid"], d_mix),
                    sustain=35,
                    control=15,
                    mobility=3.2,
                    safety=60,
                    family="sword",
                    action_id="Ramza:Chapter 3 hybrid sword",
                    derivation=action_derivation(
                        "chapter_fixed_hybrid_weapon",
                        "work/gpt-vanguard-ramza-concrete-v0.json",
                        "simulation_rows.ramza_weapon_and_hybrid_rows.chapter_3_ramza_sword_mid",
                        "chapter-fixed Ramza C3 mid scalar, armor-mixed in W5",
                    ),
                    axis_provenance={
                        "sustain": [
                            source_ref(
                                "docs/job-balance/57-vanguard-ramza-concrete-v0.md",
                                "Ramza Chapter 3 hybrid bridge row",
                                "W5 local sustain proxy for Ramza's hybrid utility.",
                            )
                        ],
                        "control": [
                            source_ref(
                                "docs/job-balance/57-vanguard-ramza-concrete-v0.md",
                                "Ramza Chapter 3 hybrid bridge row",
                                "W5 local control proxy; Ramza bridges but does not beat Time/Mystic.",
                            )
                        ],
                        "mobility": [
                            source_ref(
                                "docs/job-balance/57-vanguard-ramza-concrete-v0.md",
                                "Ramza Chapter 3 hybrid bridge row",
                                "W5 local mobility proxy for normal chapter body.",
                            )
                        ],
                        "safety": [
                            source_ref(
                                "docs/job-balance/57-vanguard-ramza-concrete-v0.md",
                                "Ramza Chapter 3 hybrid bridge row",
                                "W5 local safety proxy below dedicated frontliners.",
                            )
                        ],
                    },
                ),
                unit(
                    "Ninja/Samurai",
                    "Ninja",
                    "physical ceiling",
                    damage=weighted(SN["simulation_rows"]["ninja_melee"]["ninja_blade_dual_late"], d_mix),
                    hit_count=2,
                    mobility=4.5,
                    safety=45,
                    family="ninja_blade",
                    action_id="Ninja:Innate dual ninja blade",
                    derivation=action_derivation(
                        "native_dual_weapon_table",
                        "work/gpt-samurai-ninja-concrete-v0.json",
                        "simulation_rows.ninja_melee.ninja_blade_dual_late",
                        "accepted late innate-dual table, armor-mixed in W5",
                    ),
                    axis_provenance={
                        "mobility": [
                            source_ref(
                                "docs/job-balance/56-samurai-ninja-concrete-v0.md",
                                "Ninja active fast physical identity",
                                "W5 mobility proxy for native Ninja shell.",
                            )
                        ],
                        "safety": [
                            source_ref(
                                "docs/job-balance/56-samurai-ninja-concrete-v0.md",
                                "Ninja active fast physical identity",
                                "W5 safety proxy keeps Ninja fragile relative to armor bodies.",
                            )
                        ],
                    },
                ),
                unit(
                    "Time/Summoner",
                    "Summoner",
                    "area tempo",
                    area_damage=summon_total("Bahamut", "late"),
                    area_per_target=float(row_by(SG["summon_phase_rows"], skill="Bahamut", phase="late")["per_target"]),
                    area_targets=float(row_by(SG["summon_phase_rows"], skill="Bahamut", phase="late")["expected_targets"]),
                    area_basis="Bahamut late accepted expected_targets",
                    action_id="Summon:Bahamut",
                    derivation=summon_area_kwargs("Bahamut", "late")["derivation"],
                    control=70,
                    mobility=4.5,
                    safety=30,
                    axis_provenance={
                        **summon_area_kwargs("Bahamut", "late")["axis_provenance"],
                        "control": [
                            source_ref(
                                "docs/job-balance/44-time-mystic-concrete-v0.md",
                                "time_late_premium_pressure",
                                "W5 local control proxy for Time/Summoner tempo package.",
                            )
                        ],
                        "mobility": [
                            source_ref(
                                "docs/job-balance/63-w4-t21-populated-incidence-v0.md",
                                "Teleport/late caster mobility incidence",
                                "W5 mobility proxy for late caster mobility route.",
                            )
                        ],
                        "safety": [
                            source_ref(
                                "docs/job-balance/43-summoner-geomancer-concrete-v0.md",
                                "Summoner area caster fragility context",
                                "W5 safety proxy for fragile area caster.",
                            )
                        ],
                    },
                ),
                unit(
                    "Performer",
                    "Bard",
                    "global sustain/tempo",
                    sustain=304,
                    control=50,
                    mobility=3,
                    safety=40,
                    action_id="Performance:Seraph Song",
                    derivation=action_derivation(
                        "global_hp_over_time",
                        "work/gpt-bard-dancer-concrete-v0.json",
                        "simulations.bard_hp_over_time[target_max_hp=390].seraph_cap4_full",
                        "Seraph Song cap4 full value at target_max_hp=390",
                        target_max_hp=390,
                        seraph_cap4_full=304,
                    ),
                    axis_provenance={
                        "sustain": [
                            source_ref(
                                "docs/job-balance/46-bard-dancer-concrete-v0.md",
                                "Seraph Song hp390 cap4 full value",
                                "Accepted performer sustain row used as the D convergence sustain proxy.",
                            )
                        ],
                        "control": [
                            source_ref(
                                "docs/job-balance/46-bard-dancer-concrete-v0.md",
                                "Rousing/Slow Dance tempo layer and performance interruption model",
                                "W5 local tempo proxy for performer support pressure.",
                            )
                        ],
                        "mobility": [
                            source_ref(
                                "docs/job-balance/46-bard-dancer-concrete-v0.md",
                                "Bard/Dancer performance body with shared RSM parity",
                                "W5 mobility proxy for baseline performer body without late movement export.",
                            )
                        ],
                        "safety": [
                            source_ref(
                                "docs/job-balance/46-bard-dancer-concrete-v0.md",
                                "performance interruption model",
                                "W5 safety proxy for interruptible performer body.",
                            )
                        ],
                    },
                ),
                unit(
                    "Dragoon/Archer flex",
                    "Dragoon",
                    "reach",
                    damage=weighted(OD["simulation_rows"]["dragoon_spear_damage"]["late_jump_x1_25"], d_mix),
                    engine_multiplier=1.25,
                    control=10,
                    mobility=4,
                    safety=90,
                    family="spear",
                    action_id="Dragoon:Jump late x1.25",
                    derivation=action_derivation(
                        "timed_reach_weapon_table",
                        "work/gpt-orator-dragoon-concrete-v0.json",
                        "simulation_rows.dragoon_spear_damage.late_jump_x1_25",
                        "accepted late Jump x1.25 table, armor-mixed in W5",
                    ),
                    axis_provenance={
                        "control": [
                            source_ref(
                                "docs/job-balance/55-orator-dragoon-concrete-v0.md",
                                "Jump reach and landing counterplay rows",
                                "W5 local control proxy for reach/space pressure.",
                            )
                        ],
                        "mobility": [
                            source_ref(
                                "docs/job-balance/55-orator-dragoon-concrete-v0.md",
                                "Horizontal Jump +3 / Ignore Elevation context",
                                "W5 mobility proxy for Dragoon reach route.",
                            )
                        ],
                        "safety": [
                            source_ref(
                                "docs/job-balance/39-timed-untargetability-composition-schema.md",
                                "T5xT8 timed untargetability policy",
                                "W5 safety proxy for Jump exposure window; not permanent safety.",
                            )
                        ],
                    },
                ),
            ],
        )
    )

    rows.append(
        aggregate(
            "W5-P6-DE-PARITY",
            "D",
            "cluster_d",
            [
                unit("Ramza C3", "Ramza Chapter 3", "hybrid", damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_3_ramza_sword_mid"], d_mix), sustain=35, control=15, mobility=3.2, safety=60, family="sword"),
                unit(
                    "Bard",
                    "Bard",
                    "global sustain",
                    sustain=304,
                    control=45,
                    mobility=3,
                    safety=40,
                    action_id="Performance:Seraph Song",
                    derivation=action_derivation(
                        "global_hp_over_time",
                        "work/gpt-bard-dancer-concrete-v0.json",
                        "simulations.bard_hp_over_time[target_max_hp=390].seraph_cap4_full",
                        "Seraph Song cap4 full value at target_max_hp=390",
                        target_max_hp=390,
                        seraph_cap4_full=304,
                    ),
                    axis_provenance={
                        "sustain": [
                            source_ref(
                                "docs/job-balance/46-bard-dancer-concrete-v0.md",
                                "Seraph Song hp390 cap4 full value",
                                "Accepted performer sustain row; interrupted row is lower.",
                            )
                        ]
                    },
                    note="Life/Seraph full value; interrupted row is lower.",
                ),
                unit(
                    "Dancer",
                    "Dancer",
                    "global pressure",
                    area_damage=240,
                    area_per_target=60,
                    area_targets=4,
                    area_basis="Last Waltz hp624 cap4 total",
                    control=65,
                    mobility=3,
                    safety=35,
                    action_id="Dance:Last Waltz",
                    derivation=action_derivation(
                        "global_hp_over_time",
                        "work/gpt-bard-dancer-concrete-v0.json",
                        "simulations.capstone_and_throughput_ratios.last_waltz_hp624_cap4_total",
                        "Last Waltz hp624 cap4 total, nonlethal capstone pressure",
                        target_max_hp=624,
                        cap4_total=240,
                    ),
                    axis_provenance={
                        "damage": [
                            source_ref(
                                "docs/job-balance/46-bard-dancer-concrete-v0.md",
                                "Last Waltz hp624 cap4 total",
                                "Accepted performer nonlethal capstone pressure row.",
                            )
                        ],
                        "control": [
                            source_ref(
                                "docs/job-balance/46-bard-dancer-concrete-v0.md",
                                "Slow Dance/Forbidden Dance performer control model",
                                "W5 local control proxy for performer pressure.",
                            )
                        ],
                    },
                    note="Mincing/Last Waltz normalized, nonlethal capstone.",
                ),
                unit("Summoner/Time", "Summoner", "area", **summon_area_kwargs("Bahamut", "late"), control=40, mobility=4.5, safety=30),
                unit("Physical specialist", "Samurai", "single target", damage=weighted(SN["simulation_rows"]["samurai_baseline"]["samurai_katana_late"], d_mix), control=20, mobility=3, safety=55, family="katana"),
            ],
        )
    )

    e_mix = ARMOR_MIXES["boss_e"]
    rows.append(
        aggregate(
            "W5-P5-E-LATE",
            "E",
            "boss_e",
            [
                unit(
                    "Ramza C4",
                    "Ramza Chapter 4",
                    "final hybrid",
                    damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_4_arc_blade_x1.00"], e_mix),
                    area_damage=158,
                    area_per_target=158,
                    area_targets=1,
                    area_basis="doc57 Ultima small-area proxy counted single target",
                    sustain=50,
                    control=35,
                    mobility=3.5,
                    safety=80,
                    family="sword",
                    action_id="Ramza:Arc Blade / Ultima proxy",
                    derivation=action_derivation(
                        "hybrid_weapon_magic",
                        "work/gpt-vanguard-ramza-concrete-v0.json",
                        "simulation_rows.ramza_weapon_and_hybrid_rows.chapter_4_arc_blade_x1.00 + ramza_magic_rows_faith_70_70.chapter_4_ultima_K22_late",
                        "armor-mixed Arc Blade plus accepted K22 Ultima proxy counted as one realistic target for area axis",
                        ultima_k22_late=158,
                    ),
                    note="Primary family is sword; modeled unit has separate Arc Blade and Ultima proxy axes.",
                ),
                unit("Vanguard", "Vanguard", "protection/setup", damage=weighted(VR["simulation_rows"]["vanguard_action_damage_late"]["Decisive Strike spear x1.20 setup"], e_mix), engine_multiplier=1.2, control=55, mobility=3, safety=150, family="spear", note="setup payoff assumes mark is available.", tags=["AI-BEHAVIOR-ASSUMPTION"]),
                unit(
                    "Necromancer",
                    "Necromancer",
                    "state/corpse",
                    damage=86,
                    sustain=43,
                    control=95,
                    mobility=3,
                    safety=45,
                    action_id="Necromancy:Drain/state mix",
                    derivation=action_derivation(
                        "dark_state_proxy",
                        "work/gpt-necromancer-concrete-v0.json",
                        "drain_rows[skill=Drain,phase=late] plus necromancer_late_dark_mix",
                        "Drain late hp_damage 86 and caster_hp_recovery 43; control proxy comes from accepted late dark mix.",
                        hp_damage=86,
                        caster_hp_recovery=43,
                    ),
                    axis_provenance={
                        "damage": [
                            source_ref(
                                "docs/job-balance/45-necromancer-concrete-v0.md",
                                "Drain late hp_damage",
                                "Accepted Necromancer Drain row.",
                            )
                        ],
                        "sustain": [
                            source_ref(
                                "docs/job-balance/45-necromancer-concrete-v0.md",
                                "Drain late caster_hp_recovery",
                                "Accepted Necromancer Drain recovery row.",
                            )
                        ],
                        "control": [
                            source_ref(
                                "docs/job-balance/45-necromancer-concrete-v0.md",
                                "necromancer_late_dark_mix",
                                "W5 local control proxy for dark-state/corpse pressure.",
                            )
                        ],
                    },
                ),
                unit("Ninja/Samurai", "Ninja", "physical ceiling", damage=weighted(SN["simulation_rows"]["ninja_melee"]["ninja_blade_dual_late"], e_mix), hit_count=2, mobility=4.5, safety=45, family="ninja_blade"),
                unit("Time/Summoner/White", "Summoner", "area/sustain flex", **summon_area_kwargs("Bahamut", "late"), sustain=92, control=70, mobility=4.5, safety=35),
            ],
        )
    )

    rows.append(
        aggregate(
            "W5-RAMZA-C4-BREADTH",
            "E",
            "boss_e",
            [
                unit(
                    "Ramza C4",
                    "Ramza Chapter 4",
                    "final hybrid",
                    damage=weighted(VR["simulation_rows"]["ramza_weapon_and_hybrid_rows"]["chapter_4_arc_blade_x1.00"], e_mix),
                    area_damage=158,
                    area_per_target=158,
                    area_targets=1,
                    area_basis="doc57 Ultima small-area proxy counted single target",
                    sustain=50,
                    control=35,
                    mobility=3.5,
                    safety=80,
                    family="sword",
                    action_id="Ramza:Arc Blade / Ultima proxy",
                    derivation=action_derivation(
                        "hybrid_weapon_magic",
                        "work/gpt-vanguard-ramza-concrete-v0.json",
                        "simulation_rows.ramza_weapon_and_hybrid_rows.chapter_4_arc_blade_x1.00 + ramza_magic_rows_faith_70_70.chapter_4_ultima_K22_late",
                        "armor-mixed Arc Blade plus accepted K22 Ultima proxy counted as one realistic target for area axis",
                        ultima_k22_late=158,
                    ),
                    note="Primary family is sword; modeled unit has separate Arc Blade and Ultima proxy axes.",
                ),
                unit(
                    "Black Mage ref",
                    "Black Mage",
                    "specialist burst",
                    **black_magic_kwargs("Flare", "late"),
                    mobility=3,
                    safety=30,
                ),
                unit("Summoner ref", "Summoner", "specialist area", **summon_area_kwargs("Bahamut", "late"), mobility=3, safety=30),
                unit("Vanguard ref", "Vanguard", "specialist defense", damage=weighted(VR["simulation_rows"]["vanguard_action_damage_late"]["Decisive Strike spear x1.20 setup"], e_mix), engine_multiplier=1.2, control=55, mobility=3, safety=150, family="spear", tags=["AI-BEHAVIOR-ASSUMPTION"]),
                unit("Ninja ref", "Ninja", "specialist physical", damage=weighted(SN["simulation_rows"]["ninja_melee"]["ninja_blade_dual_late"], e_mix), hit_count=2, mobility=4.5, safety=45, family="ninja_blade"),
            ],
        )
    )

    rows.append(
        aggregate(
            "W5-EQUIP-BREAKPOINTS",
            "E",
            "boss_e",
            [
                unit("Knight native sword", "Knight", "native equipment", damage=avg_weapon("Knight", "knight_sword", "late", e_mix), mobility=3, safety=110, family="knight_sword"),
                unit("Knight native Doublehand", "Knight", "native+support", damage=weighted(armor_list(row_by(RSM["simulation_rows"], id="RSM-KNIGHT-DOUBLEHAND-KNIGHT-SWORD-LATE")["expected"]["doublehand"]), e_mix), engine_multiplier=1.8, mobility=3, safety=110, family="knight_sword"),
                unit("Samurai Doublehand", "Samurai", "support engine", damage=weighted(SN["simulation_rows"]["samurai_baseline"]["doublehand_katana_late_x1_80"], e_mix), engine_multiplier=1.8, mobility=3, safety=55, family="katana"),
                unit("Gun export/native", "Orator", "stat independent", damage=weighted(OD["simulation_rows"]["orator_gun_baseline"]["orator_gun_late"], e_mix), control=30, mobility=3.5, safety=55, family="gun"),
                unit("Armor export proxy", "Geomancer", "defense export", damage=avg_weapon("Geomancer", "axe", "late", e_mix), control=25, mobility=3, safety=125, family="axe"),
            ],
        )
    )

    return rows


def ceiling_probe_rows() -> list[dict[str, Any]]:
    """Report reconciled P2-E/P5-E ceiling candidates without canonizing Attack Boost."""

    e_mix = ARMOR_MIXES["boss_e"]
    p2_mix = ARMOR_MIXES["spread_d"]
    ninja_late = SN["simulation_rows"]["ninja_melee"]["ninja_blade_dual_late"]
    ninja_ab_stress = armor_list(
        row_by(RSM["simulation_rows"], id="RSM-NINJA-DUAL-WIELD-NINJA-BLADE-STRESS")[
            "expected"
        ]["dual_wield_attack_boost"]
    )
    samurai_late = SN["simulation_rows"]["samurai_baseline"]["doublehand_katana_late_x1_80"]
    samurai_stress_probe = scale_table(
        SN["simulation_rows"]["samurai_baseline"]["samurai_katana_stress"], 1.8
    )
    vanguard_decisive = VR["simulation_rows"]["vanguard_action_damage_late"][
        "Decisive Strike spear x1.20 setup"
    ]

    def candidate(context: str, armor_mix: dict[str, float], name: str, table: dict[str, Any], *, status: str, tags: list[str]) -> dict[str, Any]:
        return {
            "context": context,
            "candidate": name,
            "weighted_damage": weighted(table, armor_mix),
            "armor_table": table,
            "status": status,
            "tags": tags,
        }

    rows = [
        candidate("P2-E", p2_mix, "Ninja native dual, no Attack Boost", ninja_late, status="canon_candidate", tags=[]),
        candidate("P2-E", p2_mix, "Ninja native dual + Attack Boost", ninja_ab_stress, status="stress_probe", tags=["STRESS-PROBE", "attack_boost_unassigned"]),
        candidate("P2-E", p2_mix, "Samurai Doublehand katana", samurai_late, status="canon_candidate", tags=[]),
        candidate("P2-E", p2_mix, "Samurai Doublehand katana stress-stat probe", samurai_stress_probe, status="stress_probe", tags=["STRESS-PROBE"]),
        candidate("P5-E", e_mix, "Ninja native dual, no Attack Boost", ninja_late, status="canon_candidate", tags=[]),
        candidate("P5-E", e_mix, "Ninja native dual + Attack Boost", ninja_ab_stress, status="stress_probe", tags=["STRESS-PROBE", "attack_boost_unassigned"]),
        candidate("P5-E", e_mix, "Samurai Doublehand katana", samurai_late, status="canon_candidate", tags=[]),
        candidate("P5-E", e_mix, "Vanguard Decisive spear setup", vanguard_decisive, status="assumption_gated_candidate", tags=["AI-BEHAVIOR-ASSUMPTION", "setup_required"]),
    ]
    by_context: dict[str, list[dict[str, Any]]] = {}
    for row in rows:
        by_context.setdefault(row["context"], []).append(row)
    winners = {}
    for context, candidates in by_context.items():
        winners[context] = {
            "highest_numeric": max(candidates, key=lambda row: row["weighted_damage"]),
            "highest_canon_or_assumption": max(
                [row for row in candidates if row["status"] != "stress_probe"],
                key=lambda row: row["weighted_damage"],
            ),
        }
    return [{"candidates": rows, "winners": winners}]


def magic_area_constants() -> dict[str, Any]:
    def black(action: str, phase: str) -> dict[str, Any]:
        row = row_by(WB["black_phase_rows"], action=action, phase=phase)
        return {
            "source": "work/gpt-wm-bm-concrete-v0.json",
            "source_path": f"black_phase_rows[action={action},phase={phase}]",
            "ma": row["ma"],
            "k": row["k"],
            "formula": "round(ma * k * 0.6)",
            "neutral": row["neutral"],
        }

    def white(action: str, phase: str) -> dict[str, Any]:
        row = row_by(WB["white_phase_rows"], action=action, phase=phase)
        return {
            "source": "work/gpt-wm-bm-concrete-v0.json",
            "source_path": f"white_phase_rows[action={action},phase={phase}]",
            "ma": row["ma"],
            "k": row["k"],
            "formula": "round(ma * k * 0.6)",
            "neutral": row["neutral"],
        }

    def summon(skill: str, phase: str) -> dict[str, Any]:
        row = row_by(SG["summon_phase_rows"], skill=skill, phase=phase)
        return {
            "source": "work/gpt-summoner-geomancer-concrete-v0.json",
            "source_path": f"summon_phase_rows[skill={skill},phase={phase}]",
            "ma": row["ma"],
            "k": row["k"],
            "formula": "per_target = round(ma * k * 0.6); expected_total = per_target * expected_targets",
            "per_target": row["per_target"],
            "expected_targets": row["expected_targets"],
            "expected_total": row["expected_total"],
            "max_realistic_targets": row["max_realistic_targets"],
            "max_realistic_total": row["max_realistic_total"],
        }

    meteor = row_by(TM["time_meteor_phase_rows"], skill="Meteor", phase="late")
    return {
        "rounding": "nearest integer for spell per-target HP values; JSON area totals preserve decimal target-count products",
        "black_magic": {
            "Fira_mid": black("Fira", "mid"),
            "Firaga_mid": black("Firaga", "mid"),
            "Flare_late": black("Flare", "late"),
        },
        "white_magic": {
            "Cure_mid": white("Cure", "mid"),
            "Curaga_late": white("Curaga", "late"),
        },
        "summons": {
            "Titan_mid": summon("Titan", "mid"),
            "Ifrit_mid": summon("Ifrit", "mid"),
            "Bahamut_late": summon("Bahamut", "late"),
        },
        "time_magic": {
            "Meteor_late": {
                "source": "work/gpt-time-mystic-concrete-v0.json",
                "source_path": "time_meteor_phase_rows[skill=Meteor,phase=late]",
                "ma": meteor["ma"],
                "k": meteor["k"],
                "formula": "per_target = round(ma * k * 0.6); expected_total = per_target * expected_targets",
                "per_target": meteor["per_target"],
                "expected_targets": meteor["expected_targets"],
                "expected_total": meteor["expected_total"],
                "max_targets": meteor["max_targets"],
                "max_total": meteor["max_total"],
            }
        },
        "ramza_magic": {
            "chapter_4_ultima_K22_late": {
                "source": "work/gpt-vanguard-ramza-concrete-v0.json",
                "source_path": "simulation_rows.ramza_magic_rows_faith_70_70.chapter_4_ultima_K22_late",
                "formula": "accepted doc57 K22 late Ultima proxy at 70/70 Faith",
                "neutral": VR["simulation_rows"]["ramza_magic_rows_faith_70_70"][
                    "chapter_4_ultima_K22_late"
                ],
            }
        },
    }


def main() -> int:
    rows = build_rows()
    ceiling_rows = ceiling_probe_rows()
    stress_probe_contexts = sorted(
        {
            row["context"]
            for group in ceiling_rows
            for row in group["candidates"]
            if "STRESS-PROBE" in row["tags"]
        }
    )
    assumption_gated_contexts = sorted(
        {
            row["context"]
            for group in ceiling_rows
            for row in group["candidates"]
            if "AI-BEHAVIOR-ASSUMPTION" in row["tags"]
        }
    )
    payload = {
        "schema_version": "w5_f5_real_roster_sweep_v0",
        "artifact": "W5 F5 Real-Roster Sweep V0",
        "status": "accepted_claude_approved",
        "source_parent_bundle_sha256": N1["parent_bundle_sha256"],
        "source_n1_extension_status": N1["status"],
        "review_tolerance": {
            "deterministic_damage": "exact integer match",
            "weighted_averages": 0.001,
            "ratios": 0.001,
            "rounding_policy": "effwp_rounding=none inherited from parent d57c4688",
        },
        "effwp_rounding": PARENT["calc"]["effwp_rounding"],
        "verdict_policy": {
            "axes": ["damage", "sustain", "control", "mobility", "safety"],
            "damage_axis": "single-target engines use damage.delivered_per_action; area engines use area_damage.normalized_total; hybrid units use max(delivered_per_action, normalized_total)",
            "majority_pareto_dominance": "best-or-tied in at least 3 of 5 axes and not worst in any axis",
            "verdicts": ["pass", "watch", "fail"],
        },
        "method": {
            "description": "Aggregates accepted W3 proxy rows and parent formula damage into W5 real roster party rows.",
            "damage_units": "HP-equivalent expected action value; area rows keep separate area_damage.",
            "non_damage_axes": "Proxy points from accepted concrete rows: sustain HP, control/tempo/status pressure, mobility, and safety/defense.",
            "damage_conventions": {
                "physical_formula": "per_armor_damage = floor(raw_pressure * engine_multiplier * response_layers[armor]); delivered_per_action = hit_count * hit_rate * sum(armor_mix[armor] * per_armor_damage)",
                "floor_order": "engine_multiplier/pressure support is inside the per-armor floor; hit_count is applied after per-armor floor and armor-mix weighting",
                "display_identity": "damage.per_hit * hit_count * engine_multiplier equals displayed delivered_per_action; this is a reporting identity, not the physical recomputation rule",
                "top_source": "top_source uses max(single_target delivered_per_action, area normalized_total) per unit and never sums alternative actions from the same unit",
            },
            "ramza_chapter_phase_map": {
                "Ramza Chapter 1": "early",
                "Ramza Chapter 2": "mid",
                "Ramza Chapter 3": "mid",
                "Ramza Chapter 4": "late",
            },
            "magic_area_constants": magic_area_constants(),
            "limitations": [
                "No ENTD map geometry.",
                "No final T1 weapon dump.",
                "Vanguard Decisive setup payoff is an assumption row.",
                "Ramza defense armor class is a W5 proxy, not final equipment data.",
            ],
        },
        "rows": rows,
        "ceiling_probe_rows": ceiling_rows,
        "summary": {
            "row_count": len(rows),
            "verdict_counts": {
                verdict: sum(1 for row in rows if row["verdict"] == verdict)
                for verdict in sorted({row["verdict"] for row in rows})
            },
            "fail_rows": [row["id"] for row in rows if row["verdict"] == "fail"],
            "watch_rows": [row["id"] for row in rows if row["verdict"] == "watch"],
            "canon_rows": [
                row["id"]
                for row in rows
                if not row["dominance_flags"]["stress_probe_involved"]
                and not row["dominance_flags"]["assumption_gated_involved"]
            ],
            "stress_probe_contexts": stress_probe_contexts,
            "assumption_gated_contexts": assumption_gated_contexts,
            "stress_probe_policy": "STRESS-PROBE rows feed W6 candidates but are excluded from canon ceilings.",
            "assumption_gated_policy": "AI-BEHAVIOR-ASSUMPTION rows feed W6 candidates but are excluded from canon ceilings by themselves.",
        },
    }
    out = ROOT / "work" / "w5-real-roster-sweep-v0.json"
    out.write_text(json.dumps(payload, indent=2, ensure_ascii=True) + "\n")
    print(out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
