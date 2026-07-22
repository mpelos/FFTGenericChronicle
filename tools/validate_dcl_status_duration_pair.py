#!/usr/bin/env python3
"""Fail closed when a DCL runtime does not own every neutralized native status counter."""
from __future__ import annotations

import argparse
import hashlib
import json
import xml.etree.ElementTree as ET
from functools import cache
from pathlib import Path
from typing import Any

import analyze_dcl_status_counter_authority as authority
import analyze_dcl_status_authority as status_authority
import report_dcl_status_policy as policy


ROOT = Path(__file__).resolve().parents[1]
class DurationPairError(ValueError):
    pass


def _object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as error:
        raise DurationPairError(f"cannot read {label} {path}: {error}") from error
    if not isinstance(value, dict):
        raise DurationPairError(f"{label} must be a JSON object")
    return value


def _repo_path(raw: object, label: str) -> Path:
    if not isinstance(raw, str) or not raw:
        raise DurationPairError(f"{label} must be a non-empty repository-relative path")
    path = (ROOT / raw).resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as error:
        raise DurationPairError(f"{label} escapes the repository: {raw}") from error
    return path


def _hash(path: Path) -> str:
    try:
        return hashlib.sha256(path.read_bytes()).hexdigest().upper()
    except OSError as error:
        raise DurationPairError(f"cannot hash {path}: {error}") from error


def _status_patch(path: Path) -> dict[str, int]:
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as error:
        raise DurationPairError(f"cannot parse status counter patch {path}: {error}") from error
    if root.tag != "StatusEffectTable" or root.findtext("Version") != "1":
        raise DurationPairError("status counter patch must be StatusEffectTable Version 1")
    result: dict[str, int] = {}
    rows = {row.table_index: row for row in authority.load_rows()}
    nodes = root.findall("./Entries/StatusEffect")
    if not nodes:
        raise DurationPairError("status counter patch has no entries")
    for node in nodes:
        child_tags = [child.tag for child in node]
        if child_tags != ["Id", "Counter"]:
            raise DurationPairError(
                f"status patch entries must edit only Id and Counter, got {child_tags}"
            )
        try:
            table_index = int(node.findtext("Id", "-1"))
            counter = int(node.findtext("Counter", "-1"))
        except ValueError as error:
            raise DurationPairError("status patch Id/Counter must be integers") from error
        if table_index not in rows:
            raise DurationPairError(f"status patch table index out of range: {table_index}")
        name = rows[table_index].name
        if name in result:
            raise DurationPairError(f"duplicate status patch entry: {name}")
        if counter != 0:
            raise DurationPairError(f"neutralized status {name} must set Counter=0")
        if rows[table_index].counter <= 0:
            raise DurationPairError(f"status {name} has no native nonzero counter to neutralize")
        if name in {"Doom", "Empty_32"}:
            raise DurationPairError(f"generic duration ownership is forbidden for {name}")
        result[name] = table_index
    return result


def _expected_owners(neutralized: set[str]) -> dict[tuple[int, int, int], dict[str, str]]:
    rows, errors = policy.load_manifest(policy.DEFAULT_CATALOG)
    if errors:
        raise DurationPairError("status policy manifest errors: " + " | ".join(errors))
    expected: dict[tuple[int, int, int], dict[str, str]] = {}
    unresolved_special: set[tuple[int, str]] = set()
    for row in rows:
        native = authority.DCL_TO_NATIVE[row["status"]]
        if native not in neutralized:
            continue
        if row["operation"] in {"add-harmful", "add-buff-or-trait"}:
            pair = (
                int(row["ability_id"]),
                int(row["status_byte_index"]),
                int(row["status_mask_hex"], 16),
            )
            if pair in expected:
                raise DurationPairError(f"duplicate status-policy producer pair: {pair}")
            expected[pair] = row
        elif row["operation"] == "special-operation-review":
            unresolved_special.add((int(row["ability_id"]), row["status"]))
    if unresolved_special:
        raise DurationPairError(
            "neutralized statuses retain unresolved special producers: "
            + ", ".join(f"{ability}:{status}" for ability, status in sorted(unresolved_special))
        )
    return expected


def _expected_pairs(neutralized: set[str]) -> set[tuple[int, int, int]]:
    return set(_expected_owners(neutralized))


@cache
def _native_policy_by_ability() -> dict[int, str]:
    rows = status_authority.load_rows(status_authority.DEFAULT_CATALOG)
    expected_by_authority = {
        "retained-native-carrier": "retained-as-carrier",
        "ordinary-rider-data-suppression": "suppressed-by-data",
        "conditional-rider-data-suppression": "suppressed-by-data",
        "conditional-producer": "replaced-post-calc",
        "performance-producer": "replaced-post-calc",
        "randomfire-producer": "replaced-post-calc",
    }
    result: dict[int, str] = {}
    for row in rows:
        if not row["inflict_statuses"].strip():
            continue
        authority_class, _, _, _ = status_authority.classify(row)
        expected = expected_by_authority.get(authority_class)
        if expected is not None:
            result[int(row["id_dec"])] = expected
    return result


def _expected_native_policy(ability_id: int) -> str:
    expected = _native_policy_by_ability().get(ability_id)
    if expected is None:
        raise DurationPairError(
            f"neutralized status producer {ability_id} has no supported status authority"
        )
    return expected


def _validate_duration_owners(settings: dict[str, Any], neutralized: set[str]) -> int:
    """Validate complete runtime ownership before or after a counter patch is built."""
    if settings.get("DclStatusControlEnabled") is not True:
        raise DurationPairError("counter-neutralized data requires DclStatusControlEnabled")
    if int(settings.get("DclStatusForcedRoll", -1)) != -1:
        raise DurationPairError("integrated status-duration ownership cannot retain DclStatusForcedRoll")

    rules = settings.get("DclStatusRules")
    if not isinstance(rules, list):
        raise DurationPairError("paired settings DclStatusRules must be a list")
    actual: set[tuple[int, int, int]] = set()
    actual_rules: dict[tuple[int, int, int], dict[str, Any]] = {}
    zero_duration: list[tuple[int, int, int]] = []
    for rule in rules:
        if not isinstance(rule, dict):
            continue
        if str(rule.get("Operation", "add")).strip().lower() != "add":
            continue
        try:
            pair = (
                int(rule["AbilityId"]),
                int(rule["StatusByteIndex"]),
                int(rule["StatusMask"]),
            )
        except (KeyError, TypeError, ValueError):
            continue
        native = next(
            (
                name
                for token, name in authority.DCL_TO_NATIVE.items()
                if policy.STATUS_BITS[token][0] == pair[1]
                and policy.STATUS_BITS[token][1] == pair[2]
            ),
            None,
        )
        if native not in neutralized:
            continue
        if pair in actual_rules:
            raise DurationPairError(f"duplicate runtime duration owner: {pair}")
        actual_rules[pair] = rule
        if int(rule.get("DurationTargetTurns", 0)) <= 0:
            zero_duration.append(pair)
        actual.add(pair)
    if zero_duration:
        raise DurationPairError(
            f"neutralized status rules require positive DurationTargetTurns: {sorted(zero_duration)}"
        )
    expected_owners = _expected_owners(neutralized)
    expected = set(expected_owners)
    if actual != expected:
        raise DurationPairError(
            "runtime/catalog duration-owner mismatch: "
            f"runtime={sorted(actual)}, expected={sorted(expected)}"
        )
    for pair, catalog_row in expected_owners.items():
        rule = actual_rules[pair]
        ability_id = pair[0]
        if int(rule.get("ActionType", -2)) != -1:
            raise DurationPairError(f"duration owner {pair} must use ActionType=-1")
        actual_policy = str(rule.get("NativeRiderPolicy", "")).lower()
        expected_policy = _expected_native_policy(ability_id)
        if actual_policy != expected_policy:
            raise DurationPairError(
                f"duration owner {pair} requires NativeRiderPolicy={expected_policy}, got {actual_policy}"
            )
        formula = str(rule.get("ResistanceFormula", ""))
        formula_lower = formula.lower()
        category = catalog_row["resist_category"]
        duration = int(rule.get("DurationTargetTurns", 0))
        if category == "physical-health":
            if "target.ht" not in formula_lower and "t.ht" not in formula_lower:
                raise DurationPairError(f"physical-health duration owner {pair} must resist on target HT")
            if duration_policy := catalog_row.get("duration_policy"):
                if duration_policy == "1-target-turn" and duration != 1:
                    raise DurationPairError(
                        f"physical Stun/Knockdown owner {pair} must last one target turn"
                    )
        elif category == "mental-will":
            if "target.will" not in formula_lower and "t.will" not in formula_lower:
                raise DurationPairError(f"mental-will duration owner {pair} must resist on target Will")
        elif category == "beneficial":
            if str(rule.get("ResistanceFormula", "")).strip():
                raise DurationPairError(f"beneficial duration owner {pair} must not roll resistance")
        else:
            raise DurationPairError(f"duration owner {pair} has unsupported category {category}")
    return len(expected)


def validate_pair(manifest_path: Path) -> list[str]:
    manifest = _object(manifest_path, "duration pair manifest")
    settings_path = _repo_path(manifest.get("settings"), "settings")
    patch_path = _repo_path(manifest.get("status_effect_data_xml"), "status_effect_data_xml")
    settings = _object(settings_path, "runtime settings")
    hashes = manifest.get("sha256")
    if not isinstance(hashes, dict):
        raise DurationPairError("sha256 must be an object")
    for key, path in (("settings", settings_path), ("status_effect_data_xml", patch_path)):
        expected_hash = hashes.get(key)
        if not isinstance(expected_hash, str) or len(expected_hash) != 64:
            raise DurationPairError(f"sha256.{key} must be a 64-digit digest")
        actual_hash = _hash(path)
        if actual_hash != expected_hash.upper():
            raise DurationPairError(
                f"{key} hash mismatch: expected {expected_hash.upper()}, got {actual_hash}"
            )

    declared = manifest.get("neutralized_statuses")
    if not isinstance(declared, list) or not declared or not all(
        isinstance(value, str) and value for value in declared
    ):
        raise DurationPairError("neutralized_statuses must be a nonempty string list")
    if len(set(declared)) != len(declared):
        raise DurationPairError("neutralized_statuses contains duplicates")
    patched = _status_patch(patch_path)
    if set(declared) != set(patched):
        raise DurationPairError(
            f"manifest/XML status mismatch: manifest={sorted(declared)}, xml={sorted(patched)}"
        )
    owned_count = _validate_duration_owners(settings, set(patched))
    return [
        f"settings={settings_path.relative_to(ROOT).as_posix()}",
        f"status_effect_data_xml={patch_path.relative_to(ROOT).as_posix()}",
        f"neutralized_statuses={','.join(sorted(patched))}",
        f"owned_ability_status_pairs={owned_count}",
    ]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    args = parser.parse_args()
    try:
        details = validate_pair(args.manifest.resolve())
    except DurationPairError as error:
        print(f"ERROR: {error}")
        return 1
    print("DCL status-duration runtime/data pair validation passed")
    for detail in details:
        print(f"  {detail}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
