#!/usr/bin/env python3
"""Smoke tests for the fail-closed runtime/action-data pairing gate."""
from __future__ import annotations

import hashlib
import json
import sqlite3
import tempfile
from contextlib import closing
from pathlib import Path

from validate_dcl_runtime_data_pair import PairValidationError, ROOT, validate_pair


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main() -> int:
    with tempfile.TemporaryDirectory(dir=ROOT) as raw:
        temp = Path(raw)
        settings = temp / "settings.json"
        database = temp / "action.sqlite"
        nxd = temp / "action.nxd"
        metadata = temp / "ability-metadata.csv"
        manifest = temp / "pair.json"

        settings.write_text(json.dumps({
            "DclComputePointNumericEnabled": True,
            "DclInstantKoControlEnabled": True,
            "DclHitForcedRoll": -1,
            "DclStatusForcedRoll": -1,
            "DclInstantKoRules": [{
                "AbilityId": 30,
                "NativeKoSuppressedByData": True,
            }],
            "DclStatusControlEnabled": True,
            "DclStatusRules": [
                {
                    "AbilityId": 219,
                    "NativeRiderPolicy": "suppressed-by-data",
                },
            ],
            "DclApproachEnabled": False,
            "DclFearControlEnabled": False,
            "DclFearForcedFleeControlEnabled": False,
            "DclFearPlayerConfirmEnforcementEnabled": False,
            "DclFearStatusRuleName": "",
            "DclAbilityMetadataPath": str(metadata),
            "DclResultFlagsControlEnabled": True,
            "DclDamageFormula": "dcl.oldDebit",
            "DclMpDebitFormula": "dcl.oldMpDebit",
            "DclSyntheticReactionEnabled": True,
            "DclSyntheticReactionLogOnly": False,
            "DclSyntheticReactionCarrierId": 443,
            "DclSyntheticReactionDeliveryId": 442,
            "DclReactionCommitProbeEnabled": True,
            "DclReactionPreSelectorProbeEnabled": True,
            "DclReactionDeliveryValidationProbeEnabled": True,
            "DclReactionMaterializationProbeEnabled": True,
            "DclReactionEffectProbeEnabled": True,
            "DclReactionRules": [{"AbilityId": 443}],
            "DclInterruptControlEnabled": True,
            "DclInterruptLogOnly": False,
            "DclInterruptForcedRoll": -1,
            "DclInterruptRules": [{"AbilityId": 368}],
        }), encoding="utf-8")
        with closing(sqlite3.connect(database)) as con:
            con.execute(
                "CREATE TABLE OverrideAbilityActionData "
                "(Key INTEGER PRIMARY KEY, Formula INTEGER, X INTEGER, Y INTEGER, InflictStatus INTEGER)"
            )
            con.execute("INSERT INTO OverrideAbilityActionData VALUES (30, 8, 1, 1, 0)")
            con.execute("INSERT INTO OverrideAbilityActionData VALUES (219, 8, 1, 1, 0)")
            con.commit()
        nxd.write_bytes(b"round-trip-audited-fixture")
        metadata.write_text(
            "ability_id,approved,action_kind,damage_type,avoidance_policy,status_category,side_effect_policy,power,strike_count\n"
            "101,1,physical_damage,crush,physical_contest,none,managed_multistrike,40,3\n",
            encoding="utf-8",
        )

        def write_manifest() -> None:
            manifest.write_text(json.dumps({
                "settings": settings.relative_to(ROOT).as_posix(),
                "action_data_sqlite": database.relative_to(ROOT).as_posix(),
                "action_data_nxd": nxd.relative_to(ROOT).as_posix(),
                "sha256": {
                    "settings": digest(settings),
                    "action_data_sqlite": digest(database),
                    "action_data_nxd": digest(nxd),
                    "ability_metadata_csv": digest(metadata),
                },
                "required_instant_ko_abilities": [30],
                "required_status_neutralized_abilities": [219],
                "required_status_rule_abilities": [219],
                "required_status_native_rider_policies": ["suppressed-by-data"],
                "ability_metadata_csv": metadata.relative_to(ROOT).as_posix(),
                "required_managed_multistrike_abilities": [101],
                "required_synthetic_reaction": {"carrier_id": 443, "delivery_id": 442},
                "required_reaction_rule_abilities": [443],
                "required_interrupt_rule_abilities": [368],
                "require_atomic_hp_mp": True,
            }), encoding="utf-8")

        write_manifest()
        details = validate_pair(manifest)
        assert "instant_ko_abilities=30" in details
        assert "status_neutralized_abilities=219" in details
        assert "managed_multistrike_abilities=101" in details
        assert "synthetic_reaction=443->442" in details
        assert "interrupt_rule_abilities=368" in details

        settings_value = json.loads(settings.read_text(encoding="utf-8"))
        settings_value["DclApproachEnabled"] = True
        settings.write_text(json.dumps(settings_value), encoding="utf-8")
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "retired DCL controls" in str(exc)
        else:
            raise AssertionError("retired Approach control was accepted")
        settings_value["DclApproachEnabled"] = False
        settings_value["DclStatusRules"].append({"Name": "dcl-fear", "AbilityId": 53})
        settings.write_text(json.dumps(settings_value), encoding="utf-8")
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "DclStatusRules[dcl-fear]" in str(exc)
        else:
            raise AssertionError("retired Fear status rule was accepted")
        settings_value["DclStatusRules"] = [
            rule for rule in settings_value["DclStatusRules"] if rule.get("Name") != "dcl-fear"
        ]
        settings.write_text(json.dumps(settings_value), encoding="utf-8")

        write_manifest()
        retired_manifest = json.loads(manifest.read_text(encoding="utf-8"))
        retired_manifest["required_approach"] = {}
        manifest.write_text(json.dumps(retired_manifest), encoding="utf-8")
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "required_approach is retired" in str(exc)
        else:
            raise AssertionError("retired Approach manifest contract was accepted")

        write_manifest()
        retired_manifest = json.loads(manifest.read_text(encoding="utf-8"))
        retired_manifest["required_fear"] = {}
        manifest.write_text(json.dumps(retired_manifest), encoding="utf-8")
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "required_fear is retired" in str(exc)
        else:
            raise AssertionError("retired Fear manifest contract was accepted")
        write_manifest()

        with closing(sqlite3.connect(database)) as con:
            con.execute("UPDATE OverrideAbilityActionData SET InflictStatus=-1 WHERE Key=30")
            con.commit()
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "not safely neutralized" in str(exc)
        else:
            raise AssertionError("native Death was accepted beside a managed Death rule")

        with closing(sqlite3.connect(database)) as con:
            con.execute("UPDATE OverrideAbilityActionData SET InflictStatus=0 WHERE Key=30")
            con.execute("UPDATE OverrideAbilityActionData SET InflictStatus=-1 WHERE Key=219")
            con.commit()
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "not data-neutralized" in str(exc)
        else:
            raise AssertionError("native status rider was accepted beside a managed status rule")

        settings_value = json.loads(settings.read_text(encoding="utf-8"))
        settings_value["DclStatusForcedRoll"] = 18
        settings.write_text(json.dumps(settings_value), encoding="utf-8")
        with closing(sqlite3.connect(database)) as con:
            con.execute(
                "UPDATE OverrideAbilityActionData SET InflictStatus=0 WHERE Key IN (30, 219)"
            )
            con.commit()
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "probe-only DclStatusForcedRoll" in str(exc)
        else:
            raise AssertionError("forced probe roll was accepted in an integrated profile")

        settings_value["DclStatusForcedRoll"] = -1
        settings_value["DclInterruptForcedRoll"] = 18
        settings.write_text(json.dumps(settings_value), encoding="utf-8")
        write_manifest()
        try:
            validate_pair(manifest)
        except PairValidationError as exc:
            assert "probe-only DclInterruptForcedRoll" in str(exc)
        else:
            raise AssertionError("forced Interrupt probe roll was accepted in an integrated profile")

        status_only_settings = temp / "status-only-settings.json"
        status_only_manifest = temp / "status-only-pair.json"
        status_only_settings.write_text(json.dumps({
            "DclHitForcedRoll": -1,
            "DclStatusForcedRoll": -1,
            "DclStatusControlEnabled": True,
            "DclStatusRules": [{
                "AbilityId": 126,
                "NativeRiderPolicy": "suppressed-by-data",
            }],
        }), encoding="utf-8")
        with closing(sqlite3.connect(database)) as con:
            con.execute("INSERT INTO OverrideAbilityActionData VALUES (126, 36, 1, 1, 0)")
            con.commit()
        status_only_manifest.write_text(json.dumps({
            "settings": status_only_settings.relative_to(ROOT).as_posix(),
            "action_data_sqlite": database.relative_to(ROOT).as_posix(),
            "action_data_nxd": nxd.relative_to(ROOT).as_posix(),
            "sha256": {
                "settings": digest(status_only_settings),
                "action_data_sqlite": digest(database),
                "action_data_nxd": digest(nxd),
            },
            "required_instant_ko_abilities": [],
            "required_status_neutralized_abilities": [126],
            "required_status_rule_abilities": [126],
            "required_status_native_rider_policies": ["suppressed-by-data"],
        }), encoding="utf-8")
        status_only_details = validate_pair(status_only_manifest)
        assert not any(detail.startswith("instant_ko_abilities=") for detail in status_only_details)
        assert "status_neutralized_abilities=126" in status_only_details

    print("DCL runtime/data pair smoke tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
