#!/usr/bin/env python3
"""Fail closed when a composed DCL runtime and its data/metadata artifacts diverge."""
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import sqlite3
from contextlib import closing
from pathlib import Path
from typing import Any

import validate_dcl_status_duration_pair as status_duration


ROOT = Path(__file__).resolve().parents[1]
TABLE = "OverrideAbilityActionData"
EXPECTED_KO_ROW = {"Formula": 8, "X": 1, "Y": 1, "InflictStatus": 0}
RETIRED_RUNTIME_CONTROLS = (
    "DclApproachEnabled",
    "DclFearControlEnabled",
    "DclFearForcedFleeControlEnabled",
    "DclFearPlayerConfirmEnforcementEnabled",
)


class PairValidationError(ValueError):
    pass


def _object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise PairValidationError(f"cannot read {label} {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise PairValidationError(f"{label} must be a JSON object: {path}")
    return value


def _repo_path(raw: object, label: str) -> Path:
    if not isinstance(raw, str) or not raw:
        raise PairValidationError(f"{label} must be a non-empty repository-relative path")
    path = (ROOT / raw).resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as exc:
        raise PairValidationError(f"{label} escapes the repository: {raw}") from exc
    return path


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    try:
        with path.open("rb") as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(block)
    except OSError as exc:
        raise PairValidationError(f"cannot hash {path}: {exc}") from exc
    return digest.hexdigest().upper()


def validate_pair(manifest_path: Path) -> list[str]:
    manifest = _object(manifest_path, "pair manifest")
    settings_path = _repo_path(manifest.get("settings"), "settings")
    sqlite_path = _repo_path(manifest.get("action_data_sqlite"), "action_data_sqlite")
    nxd_path = _repo_path(manifest.get("action_data_nxd"), "action_data_nxd")
    settings = _object(settings_path, "runtime settings")

    if manifest.get("required_approach") is not None:
        raise PairValidationError("required_approach is retired; DCL effects begin after movement")
    if manifest.get("required_fear") is not None:
        raise PairValidationError("required_fear is retired from the active DCL specification")
    enabled_retired = [
        name for name in RETIRED_RUNTIME_CONTROLS if settings.get(name) is True
    ]
    status_rules_raw = settings.get("DclStatusRules", [])
    if isinstance(status_rules_raw, list) and any(
        isinstance(rule, dict) and rule.get("Name") == "dcl-fear"
        for rule in status_rules_raw
    ):
        enabled_retired.append("DclStatusRules[dcl-fear]")
    if settings.get("DclFearStatusRuleName") not in (None, ""):
        enabled_retired.append("DclFearStatusRuleName")
    if enabled_retired:
        raise PairValidationError(
            "paired settings enable retired DCL controls: " + ", ".join(enabled_retired)
        )

    hashes = manifest.get("sha256")
    if not isinstance(hashes, dict):
        raise PairValidationError("sha256 must be an object")
    for key, path in (("action_data_sqlite", sqlite_path), ("action_data_nxd", nxd_path)):
        expected = hashes.get(key)
        if not isinstance(expected, str) or len(expected) != 64:
            raise PairValidationError(f"sha256.{key} must be a 64-digit digest")
        actual = _sha256(path)
        if actual != expected.upper():
            raise PairValidationError(f"{key} hash mismatch: expected {expected.upper()}, got {actual}")
    expected_settings_hash = hashes.get("settings")
    if expected_settings_hash is not None:
        if not isinstance(expected_settings_hash, str) or len(expected_settings_hash) != 64:
            raise PairValidationError("sha256.settings must be a 64-digit digest")
        actual = _sha256(settings_path)
        if actual != expected_settings_hash.upper():
            raise PairValidationError(
                f"settings hash mismatch: expected {expected_settings_hash.upper()}, got {actual}"
            )
    paired_data_files: list[tuple[str, Path]] = []
    for key in (
        "item_weapon_data_xml",
        "ability_charge_aim_data_xml",
        "item_catalog_csv",
        "ability_catalog_csv",
        "job_catalog_csv",
        "affinity_fragment_json",
    ):
        raw = manifest.get(key)
        if raw is None:
            continue
        path = _repo_path(raw, key)
        expected = hashes.get(key)
        if not isinstance(expected, str) or len(expected) != 64:
            raise PairValidationError(f"sha256.{key} must be a 64-digit digest")
        actual = _sha256(path)
        if actual != expected.upper():
            raise PairValidationError(
                f"{key} hash mismatch: expected {expected.upper()}, got {actual}"
            )
        paired_data_files.append((key, path))
    for pair_key, setting_key in (
        ("item_catalog_csv", "ItemCatalogPath"),
        ("ability_catalog_csv", "AbilityCatalogPath"),
    ):
        raw = manifest.get(pair_key)
        if raw is None:
            continue
        paired_name = _repo_path(raw, pair_key).name
        configured = settings.get(setting_key)
        if not isinstance(configured, str) or Path(configured).name != paired_name:
            raise PairValidationError(
                f"paired settings {setting_key} does not select {paired_name}"
            )

    required_raw = manifest.get("required_instant_ko_abilities", [])
    if not isinstance(required_raw, list) or not all(isinstance(value, int) for value in required_raw):
        raise PairValidationError("required_instant_ko_abilities must be an integer list")
    required = set(required_raw)
    if len(required) != len(required_raw):
        raise PairValidationError("required_instant_ko_abilities contains duplicates")

    if required and settings.get("DclInstantKoControlEnabled") is not True:
        raise PairValidationError("paired settings do not enable DclInstantKoControlEnabled")
    if required and settings.get("DclComputePointNumericEnabled") is not True:
        raise PairValidationError("paired settings do not enable the AI-facing compute-point writer")
    for key in ("DclStatusForcedRoll", "DclHitForcedRoll"):
        if int(settings.get(key, -1)) != -1:
            raise PairValidationError(f"paired integration settings retain probe-only {key}")
    for key in ("CalcEntryProbeEnabled", "DclCalcProvenanceProbeEnabled", "StagedBundleProbeEnabled"):
        if settings.get(key, False) is True:
            raise PairValidationError(f"paired integration settings retain probe-only {key}")

    status_duration_pair_raw = manifest.get("status_duration_pair")
    status_duration_pair_path: Path | None = None
    if status_duration_pair_raw is not None:
        status_duration_pair_path = _repo_path(
            status_duration_pair_raw, "status_duration_pair"
        )
        nested = _object(status_duration_pair_path, "status duration pair")
        nested_settings = _repo_path(nested.get("settings"), "status duration pair settings")
        if nested_settings != settings_path:
            raise PairValidationError(
                "status duration pair settings mismatch: "
                f"runtime={settings_path}, duration={nested_settings}"
            )
        try:
            status_duration.validate_pair(status_duration_pair_path)
        except status_duration.DurationPairError as error:
            raise PairValidationError(f"invalid nested status duration pair: {error}") from error

    rules = settings.get("DclInstantKoRules", [])
    if not isinstance(rules, list) or (required and not rules):
        raise PairValidationError("paired settings have no DclInstantKoRules")
    runtime_ids: set[int] = set()
    for index, rule in enumerate(rules, start=1):
        if not isinstance(rule, dict):
            raise PairValidationError(f"DclInstantKoRules[{index}] must be an object")
        ability_id = rule.get("AbilityId")
        if not isinstance(ability_id, int):
            raise PairValidationError(f"DclInstantKoRules[{index}].AbilityId must be an integer")
        if rule.get("NativeKoSuppressedByData") is not True:
            raise PairValidationError(f"ability {ability_id} does not acknowledge data-side KO suppression")
        runtime_ids.add(ability_id)
    if runtime_ids != required:
        raise PairValidationError(
            f"runtime/data ability set mismatch: runtime={sorted(runtime_ids)}, required={sorted(required)}"
        )

    status_required_raw = manifest.get("required_status_neutralized_abilities", [])
    if not isinstance(status_required_raw, list) or not all(
        isinstance(value, int) for value in status_required_raw
    ):
        raise PairValidationError("required_status_neutralized_abilities must be an integer list")
    status_required = set(status_required_raw)
    if len(status_required) != len(status_required_raw):
        raise PairValidationError("required_status_neutralized_abilities contains duplicates")
    status_rules = settings.get("DclStatusRules", [])
    if not isinstance(status_rules, list):
        raise PairValidationError("paired settings DclStatusRules must be a list")
    suppressed_runtime_ids = {
        int(rule.get("AbilityId"))
        for rule in status_rules
        if isinstance(rule, dict)
        and isinstance(rule.get("AbilityId"), int)
        and str(rule.get("NativeRiderPolicy", "")).lower() == "suppressed-by-data"
    }
    status_rule_ids = {
        int(rule["AbilityId"])
        for rule in status_rules
        if isinstance(rule, dict) and isinstance(rule.get("AbilityId"), int)
    }
    status_policies = {
        str(rule.get("NativeRiderPolicy", "")).lower()
        for rule in status_rules
        if isinstance(rule, dict) and str(rule.get("NativeRiderPolicy", "")).strip()
    }
    if status_required and settings.get("DclStatusControlEnabled") is not True:
        raise PairValidationError("status-neutralized data requires DclStatusControlEnabled")
    if suppressed_runtime_ids != status_required:
        raise PairValidationError(
            "runtime/data status ability set mismatch: "
            f"runtime={sorted(suppressed_runtime_ids)}, required={sorted(status_required)}"
        )
    required_status_rules_raw = manifest.get("required_status_rule_abilities", [])
    if not isinstance(required_status_rules_raw, list) or not all(
        isinstance(value, int) for value in required_status_rules_raw
    ):
        raise PairValidationError("required_status_rule_abilities must be an integer list")
    if status_rule_ids != set(required_status_rules_raw):
        raise PairValidationError(
            "runtime status-rule set mismatch: "
            f"runtime={sorted(status_rule_ids)}, required={sorted(set(required_status_rules_raw))}"
        )
    required_policies_raw = manifest.get("required_status_native_rider_policies", [])
    if not isinstance(required_policies_raw, list) or not all(
        isinstance(value, str) and value.strip() for value in required_policies_raw
    ):
        raise PairValidationError("required_status_native_rider_policies must be a string list")
    required_policies = {value.lower() for value in required_policies_raw}
    if status_policies != required_policies:
        raise PairValidationError(
            "runtime status-policy set mismatch: "
            f"runtime={sorted(status_policies)}, required={sorted(required_policies)}"
        )

    if required:
        columns = ", ".join(EXPECTED_KO_ROW)
        placeholders = ", ".join("?" for _ in required)
        try:
            with closing(sqlite3.connect(sqlite_path)) as con:
                rows = con.execute(
                    f"SELECT Key, {columns} FROM {TABLE} WHERE Key IN ({placeholders}) ORDER BY Key",
                    tuple(sorted(required)),
                ).fetchall()
        except sqlite3.Error as exc:
            raise PairValidationError(f"cannot inspect {TABLE} in {sqlite_path}: {exc}") from exc
        found = {int(row[0]): dict(zip(EXPECTED_KO_ROW, row[1:], strict=True)) for row in rows}
        missing = required - found.keys()
        if missing:
            raise PairValidationError(f"action data lacks required ability rows: {sorted(missing)}")
        for ability_id in sorted(required):
            if found[ability_id] != EXPECTED_KO_ROW:
                raise PairValidationError(
                    f"ability {ability_id} is not safely neutralized: expected {EXPECTED_KO_ROW}, got {found[ability_id]}"
                )

    if status_required:
        placeholders = ", ".join("?" for _ in status_required)
        try:
            with closing(sqlite3.connect(sqlite_path)) as con:
                rows = con.execute(
                    f"SELECT Key, InflictStatus FROM {TABLE} WHERE Key IN ({placeholders}) ORDER BY Key",
                    tuple(sorted(status_required)),
                ).fetchall()
        except sqlite3.Error as exc:
            raise PairValidationError(f"cannot inspect status-neutralized rows in {sqlite_path}: {exc}") from exc
        status_found = {int(key): int(inflict_status) for key, inflict_status in rows}
        missing = status_required - status_found.keys()
        if missing:
            raise PairValidationError(f"action data lacks status-neutralized rows: {sorted(missing)}")
        invalid = sorted(key for key, value in status_found.items() if value != 0)
        if invalid:
            raise PairValidationError(f"status riders are not data-neutralized for abilities: {invalid}")

    metadata_raw = manifest.get("ability_metadata_csv")
    required_multistrike_raw = manifest.get("required_managed_multistrike_abilities", [])
    if not isinstance(required_multistrike_raw, list) or not all(
        isinstance(value, int) for value in required_multistrike_raw
    ):
        raise PairValidationError("required_managed_multistrike_abilities must be an integer list")
    required_multistrike = set(required_multistrike_raw)
    if len(required_multistrike) != len(required_multistrike_raw):
        raise PairValidationError("required_managed_multistrike_abilities contains duplicates")
    if metadata_raw is not None or required_multistrike:
        metadata_path = _repo_path(metadata_raw, "ability_metadata_csv")
        expected_metadata_hash = hashes.get("ability_metadata_csv")
        if not isinstance(expected_metadata_hash, str) or len(expected_metadata_hash) != 64:
            raise PairValidationError("sha256.ability_metadata_csv must be a 64-digit digest")
        actual = _sha256(metadata_path)
        if actual != expected_metadata_hash.upper():
            raise PairValidationError(
                "ability_metadata_csv hash mismatch: "
                f"expected {expected_metadata_hash.upper()}, got {actual}"
            )
        configured_metadata = settings.get("DclAbilityMetadataPath")
        if not isinstance(configured_metadata, str) or not configured_metadata:
            raise PairValidationError("paired settings do not configure DclAbilityMetadataPath")
        configured_path = Path(configured_metadata)
        if not configured_path.is_absolute():
            configured_path = (settings_path.parent / configured_path).resolve()
        else:
            configured_path = configured_path.resolve()
        if configured_path != metadata_path:
            raise PairValidationError(
                f"settings metadata path mismatch: settings={configured_path}, manifest={metadata_path}"
            )
        try:
            with metadata_path.open("r", encoding="utf-8-sig", newline="") as handle:
                rows = list(csv.DictReader(handle))
        except (OSError, csv.Error) as exc:
            raise PairValidationError(f"cannot read ability metadata {metadata_path}: {exc}") from exc
        managed_ids: set[int] = set()
        for row in rows:
            try:
                ability_id = int(row.get("ability_id", ""))
                strike_count = int(row.get("strike_count", "0"))
            except ValueError:
                continue
            if (
                row.get("approved") == "1"
                and row.get("side_effect_policy") == "managed_multistrike"
                and strike_count > 1
            ):
                managed_ids.add(ability_id)
        if managed_ids != required_multistrike:
            raise PairValidationError(
                "runtime/metadata multistrike set mismatch: "
                f"metadata={sorted(managed_ids)}, required={sorted(required_multistrike)}"
            )

    synthetic = manifest.get("required_synthetic_reaction")
    if synthetic is not None:
        if not isinstance(synthetic, dict):
            raise PairValidationError("required_synthetic_reaction must be an object")
        carrier = synthetic.get("carrier_id")
        delivery = synthetic.get("delivery_id")
        if not isinstance(carrier, int) or not isinstance(delivery, int):
            raise PairValidationError("required_synthetic_reaction ids must be integers")
        if settings.get("DclSyntheticReactionEnabled") is not True:
            raise PairValidationError("paired settings do not enable DclSyntheticReactionEnabled")
        if settings.get("DclSyntheticReactionLogOnly") is not False:
            raise PairValidationError("paired synthetic Reaction remains log-only")
        if settings.get("DclSyntheticReactionCarrierId") != carrier:
            raise PairValidationError("paired synthetic Reaction carrier does not match the manifest")
        if settings.get("DclSyntheticReactionDeliveryId") != delivery:
            raise PairValidationError("paired synthetic Reaction delivery does not match the manifest")
        for key in (
            "DclReactionCommitProbeEnabled",
            "DclReactionPreSelectorProbeEnabled",
            "DclReactionDeliveryValidationProbeEnabled",
            "DclReactionMaterializationProbeEnabled",
            "DclReactionEffectProbeEnabled",
        ):
            if settings.get(key) is not True:
                raise PairValidationError(f"paired synthetic Reaction lacks required boundary {key}")

    approach = manifest.get("required_approach")
    if approach is not None:
        if not isinstance(approach, dict):
            raise PairValidationError("required_approach must be an object")
        owner = approach.get("owner_id")
        delivery = approach.get("delivery_id")
        minimum_reach = approach.get("minimum_reach")
        maximum_reach = approach.get("maximum_reach")
        if not all(isinstance(value, int) for value in (
            owner, delivery, minimum_reach, maximum_reach
        )):
            raise PairValidationError("required_approach ids and reach bounds must be integers")
        if not isinstance(approach.get("require_same_layer"), bool):
            raise PairValidationError("required_approach.require_same_layer must be boolean")
        if settings.get("DclApproachEnabled") is not True:
            raise PairValidationError("paired settings do not enable DclApproachEnabled")
        if settings.get("DclApproachOwnerReactionId") != owner:
            raise PairValidationError("paired Approach owner does not match the manifest")
        if settings.get("DclApproachDeliveryReactionId") != delivery:
            raise PairValidationError("paired Approach delivery does not match the manifest")
        if settings.get("DclApproachMinimumReach") != minimum_reach or \
                settings.get("DclApproachMaximumReach") != maximum_reach:
            raise PairValidationError("paired Approach reach band does not match the manifest")
        if settings.get("DclApproachRequireSameLayer") is not approach.get("require_same_layer"):
            raise PairValidationError("paired Approach layer policy does not match the manifest")
        for key in (
            "DclReactionCommitProbeEnabled",
            "DclReactionDeliveryValidationProbeEnabled",
            "DclReactionMaterializationProbeEnabled",
            "DclReactionEffectProbeEnabled",
        ):
            if settings.get(key) is not True:
                raise PairValidationError(f"paired Approach lacks required boundary {key}")
        if synthetic is not None and settings.get("DclSyntheticReactionLogOnly") is False and \
                settings.get("DclReactionReservationArbitrationEnabled") is not True:
            raise PairValidationError(
                "paired live Approach/synthetic Reaction lacks first-owner-wins reservation arbitration"
            )

    fear = manifest.get("required_fear")
    if fear is not None:
        if not isinstance(fear, dict):
            raise PairValidationError("required_fear must be an object")
        int_keys = (
            "carrier_id",
            "native_byte_index",
            "native_mask",
            "output_byte_index",
            "output_mask",
            "duration_target_turns",
        )
        if not all(isinstance(fear.get(key), int) for key in int_keys):
            raise PairValidationError("required_fear carrier, packet bits, and duration must be integers")
        for key in ("forced_flee", "player_confirm_enforcement"):
            if not isinstance(fear.get(key), bool):
                raise PairValidationError(f"required_fear.{key} must be boolean")
        resistance_formula = fear.get("resistance_formula")
        if not isinstance(resistance_formula, str) or not resistance_formula.strip():
            raise PairValidationError("required_fear.resistance_formula must be a non-empty string")
        if settings.get("DclFearControlEnabled") is not True:
            raise PairValidationError("paired settings do not enable DclFearControlEnabled")
        if settings.get("DclFearLogOnly") is not False:
            raise PairValidationError("paired Fear remains log-only")
        if settings.get("DclFearForcedFleeControlEnabled") is not fear["forced_flee"]:
            raise PairValidationError("paired Fear forced-flee state does not match the manifest")
        if settings.get("DclFearPlayerConfirmEnforcementEnabled") is not fear["player_confirm_enforcement"]:
            raise PairValidationError("paired Fear player-confirm state does not match the manifest")

        fear_name = settings.get("DclFearStatusRuleName")
        matching_fear_rules = [
            rule for rule in status_rules
            if isinstance(rule, dict) and rule.get("Name") == fear_name
        ]
        if not isinstance(fear_name, str) or not fear_name or len(matching_fear_rules) != 1:
            raise PairValidationError("paired Fear must identify exactly one named status rule")
        fear_rule = matching_fear_rules[0]
        expected_fear_rule = {
            "AbilityId": fear["carrier_id"],
            "ActionType": -1,
            "StatusByteIndex": fear["output_byte_index"],
            "StatusMask": fear["output_mask"],
            "Operation": "add",
            "NativeRiderPolicy": "replaced-post-calc-reskin",
            "NativePacketByteIndex": fear["native_byte_index"],
            "NativePacketMask": fear["native_mask"],
            "ResistanceFormula": resistance_formula,
            "DurationTargetTurns": fear["duration_target_turns"],
        }
        mismatches = [
            f"{key}={fear_rule.get(key)!r} (expected {expected!r})"
            for key, expected in expected_fear_rule.items()
            if fear_rule.get(key) != expected
        ]
        if mismatches:
            raise PairValidationError("paired Fear reskin mismatch: " + "; ".join(mismatches))

        carrier_data_keys = (
            "carrier_formula",
            "carrier_x",
            "carrier_y",
            "carrier_inflict_status",
        )
        declared_carrier_data = [key for key in carrier_data_keys if key in fear]
        if declared_carrier_data and len(declared_carrier_data) != len(carrier_data_keys):
            raise PairValidationError(
                "required_fear must declare the complete carrier formula/X/Y/InflictStatus tuple"
            )
        if declared_carrier_data:
            if not all(isinstance(fear[key], int) for key in carrier_data_keys):
                raise PairValidationError("required_fear carrier data values must be integers")
            try:
                with closing(sqlite3.connect(sqlite_path)) as con:
                    carrier_row = con.execute(
                        f"SELECT Formula, X, Y, InflictStatus FROM {TABLE} WHERE Key=?",
                        (fear["carrier_id"],),
                    ).fetchone()
            except sqlite3.Error as exc:
                raise PairValidationError(
                    f"cannot inspect Fear carrier row in {sqlite_path}: {exc}"
                ) from exc
            expected_carrier_row = tuple(fear[key] for key in carrier_data_keys)
            if carrier_row is None:
                raise PairValidationError(
                    f"action data lacks Fear carrier ability {fear['carrier_id']}"
                )
            actual_carrier_row = tuple(int(value) for value in carrier_row)
            if actual_carrier_row != expected_carrier_row:
                raise PairValidationError(
                    "Fear carrier does not deterministically materialize the paired native packet: "
                    f"expected {expected_carrier_row}, got {actual_carrier_row}"
                )

    required_reactions_raw = manifest.get("required_reaction_rule_abilities", [])
    if not isinstance(required_reactions_raw, list) or not all(
        isinstance(value, int) for value in required_reactions_raw
    ):
        raise PairValidationError("required_reaction_rule_abilities must be an integer list")
    reaction_rules = settings.get("DclReactionRules", [])
    if not isinstance(reaction_rules, list):
        raise PairValidationError("paired settings DclReactionRules must be a list")
    reaction_ids = {
        int(rule["AbilityId"])
        for rule in reaction_rules
        if isinstance(rule, dict) and isinstance(rule.get("AbilityId"), int)
    }
    if reaction_ids != set(required_reactions_raw):
        raise PairValidationError(
            "runtime Reaction-rule set mismatch: "
            f"runtime={sorted(reaction_ids)}, required={sorted(set(required_reactions_raw))}"
        )

    required_interrupt_raw = manifest.get("required_interrupt_rule_abilities", [])
    if not isinstance(required_interrupt_raw, list) or not all(
        isinstance(value, int) for value in required_interrupt_raw
    ):
        raise PairValidationError("required_interrupt_rule_abilities must be an integer list")
    required_interrupt = set(required_interrupt_raw)
    if len(required_interrupt) != len(required_interrupt_raw):
        raise PairValidationError("required_interrupt_rule_abilities contains duplicates")
    interrupt_rules = settings.get("DclInterruptRules", [])
    if not isinstance(interrupt_rules, list):
        raise PairValidationError("paired settings DclInterruptRules must be a list")
    interrupt_ids = {
        int(rule["AbilityId"])
        for rule in interrupt_rules
        if isinstance(rule, dict) and isinstance(rule.get("AbilityId"), int)
    }
    if interrupt_ids != required_interrupt:
        raise PairValidationError(
            "runtime Interrupt-rule set mismatch: "
            f"runtime={sorted(interrupt_ids)}, required={sorted(required_interrupt)}"
        )
    if required_interrupt:
        if settings.get("DclInterruptControlEnabled") is not True:
            raise PairValidationError("paired settings do not enable DclInterruptControlEnabled")
        if settings.get("DclInterruptLogOnly") is not False:
            raise PairValidationError("paired Interrupt remains log-only")
        if int(settings.get("DclInterruptForcedRoll", -1)) != -1:
            raise PairValidationError("paired integration settings retain probe-only DclInterruptForcedRoll")

    if manifest.get("require_atomic_hp_mp") is True:
        if settings.get("DclResultFlagsControlEnabled") is not True:
            raise PairValidationError("atomic HP/MP pairing requires DclResultFlagsControlEnabled")
        for key in ("DclDamageFormula", "DclMpDebitFormula"):
            if not isinstance(settings.get(key), str) or not settings[key].strip():
                raise PairValidationError(f"atomic HP/MP pairing requires non-empty {key}")

    details = [
        f"settings={settings_path.relative_to(ROOT).as_posix()}",
        f"action_data_sqlite_sha256={hashes['action_data_sqlite'].upper()}",
        f"action_data_nxd_sha256={hashes['action_data_nxd'].upper()}",
    ]
    if required:
        details.append(f"instant_ko_abilities={','.join(map(str, sorted(required)))}")
    if status_required:
        details.append(f"status_neutralized_abilities={','.join(map(str, sorted(status_required)))}")
    if status_rule_ids:
        details.append(f"status_rule_abilities={','.join(map(str, sorted(status_rule_ids)))}")
    if required_multistrike:
        details.append(f"managed_multistrike_abilities={','.join(map(str, sorted(required_multistrike)))}")
    if synthetic is not None:
        details.append(
            f"synthetic_reaction={synthetic['carrier_id']}->{synthetic['delivery_id']}"
        )
    if approach is not None:
        details.append(
            "approach="
            f"{approach['owner_id']}->{approach['delivery_id']}@"
            f"{approach['minimum_reach']}-{approach['maximum_reach']}"
        )
    if fear is not None:
        details.append(
            "fear="
            f"{fear['carrier_id']}:"
            f"{fear['native_byte_index']}/0x{fear['native_mask']:02X}->"
            f"{fear['output_byte_index']}/0x{fear['output_mask']:02X}"
        )
    if reaction_ids:
        details.append(f"reaction_rule_abilities={','.join(map(str, sorted(reaction_ids)))}")
    if interrupt_ids:
        details.append(f"interrupt_rule_abilities={','.join(map(str, sorted(interrupt_ids)))}")
    if status_duration_pair_path is not None:
        details.append(
            "status_duration_pair="
            + status_duration_pair_path.relative_to(ROOT).as_posix()
        )
    for key, path in paired_data_files:
        details.append(f"{key}={path.relative_to(ROOT).as_posix()}")
    return details


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    args = parser.parse_args()
    try:
        details = validate_pair(args.manifest.resolve())
    except PairValidationError as exc:
        print(f"ERROR: {exc}")
        return 1
    print("DCL runtime/data pair validation passed")
    for detail in details:
        print(f"  {detail}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
