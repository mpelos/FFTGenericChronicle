#!/usr/bin/env python3
"""Regression tests for the DCL status-counter patch and pair validator."""
from __future__ import annotations

import hashlib
import json
import tempfile
from pathlib import Path

import build_dcl_status_counter_patch as build
import validate_dcl_status_duration_pair as validate


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def rule(
    ability: int,
    byte_index: int,
    mask: int,
    catalog_row: dict[str, str],
) -> dict[str, object]:
    category = catalog_row["resist_category"]
    formula = "target.will"
    duration = 2
    if category == "physical-health":
        formula = "target.ht"
        duration = 1 if catalog_row["duration_policy"] == "1-target-turn" else 2
    elif category == "beneficial":
        formula = ""
    return {
        "AbilityId": ability,
        "ActionType": -1,
        "StatusByteIndex": byte_index,
        "StatusMask": mask,
        "ResistanceFormula": formula,
        "DurationTargetTurns": duration,
        "NativeRiderPolicy": validate._expected_native_policy(ability),
    }


def main() -> int:
    # Keep transient regression artifacts out of work/: every file in work/ is a
    # timestamped investigation artifact, while these fixtures disappear at exit.
    with tempfile.TemporaryDirectory(
        dir=validate.ROOT / "tools", prefix=".tmp_dcl_status_duration_"
    ) as temp_raw:
        temp = Path(temp_raw)
        patch = temp / "StatusEffectData.xml"
        expected_owners = validate._expected_owners({"Charmed"})
        expected = set(expected_owners)
        settings = temp / "settings.json"
        settings_value = {
            "DclStatusControlEnabled": True,
            "DclStatusRules": [
                rule(*pair, expected_owners[pair]) for pair in sorted(expected)
            ] + [{
                "Name": "unrelated cure of a neutralized bit",
                "AbilityId": 252,
                "ActionType": -1,
                "StatusByteIndex": 4,
                "StatusMask": 8,
                "Operation": "remove",
                "NativeRiderPolicy": "suppressed-by-data",
                "ResistanceFormula": "",
                "DurationTargetTurns": 0,
            }],
        }
        settings.write_text(json.dumps(settings_value), encoding="utf-8")

        selected = build.build_patch(("Charmed",), patch, settings_value)
        assert [(row.table_index, row.name) for row in selected] == [(34, "Charmed")]
        parsed = validate._status_patch(patch)
        assert parsed == {"Charmed": 34}
        try:
            build.build_patch(("Doom",), temp / "bad.xml", settings_value)
        except ValueError as error:
            assert "forbidden" in str(error)
        else:
            raise AssertionError("Doom counter neutralization must fail")

        incomplete_settings = json.loads(json.dumps(settings_value))
        incomplete_settings["DclStatusRules"].pop(0)
        incomplete_patch = temp / "incomplete.xml"
        try:
            build.build_patch(
                ("Immobilize", "Disable"), incomplete_patch, incomplete_settings
            )
        except ValueError as error:
            assert "incomplete duration ownership" in str(error)
            assert "duration-owner mismatch" in str(error)
            assert not incomplete_patch.exists()
        else:
            raise AssertionError("counter patch build must fail before writing incomplete ownership")

        manifest = temp / "manifest.json"
        manifest.write_text(json.dumps({
            "settings": settings.relative_to(validate.ROOT).as_posix(),
            "status_effect_data_xml": patch.relative_to(validate.ROOT).as_posix(),
            "neutralized_statuses": ["Charmed"],
            "sha256": {
                "settings": digest(settings),
                "status_effect_data_xml": digest(patch),
            },
        }), encoding="utf-8")
        details = validate.validate_pair(manifest)
        assert any("owned_ability_status_pairs=4" == detail for detail in details)

        undecided_owners = validate._expected_owners({"Immobilize", "Disable"})
        undecided_settings = {
            "DclStatusControlEnabled": True,
            "DclStatusRules": [
                rule(*pair, undecided_owners[pair]) for pair in sorted(undecided_owners)
            ],
        }
        try:
            build.build_patch(("Immobilize", "Disable"), temp / "undecided.xml", undecided_settings)
        except ValueError as error:
            assert "unsupported category unresolved-nature" in str(error)
        else:
            raise AssertionError("undecided Immobilize/Disable ownership must fail closed")

        broken = json.loads(settings.read_text(encoding="utf-8"))
        broken["DclStatusRules"].pop(0)
        settings.write_text(json.dumps(broken), encoding="utf-8")
        manifest_value = json.loads(manifest.read_text(encoding="utf-8"))
        manifest_value["sha256"]["settings"] = digest(settings)
        manifest.write_text(json.dumps(manifest_value), encoding="utf-8")
        try:
            validate.validate_pair(manifest)
        except validate.DurationPairError as error:
            assert "duration-owner mismatch" in str(error)
        else:
            raise AssertionError("incomplete runtime ownership must fail")
    print("DCL status-duration pair tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
