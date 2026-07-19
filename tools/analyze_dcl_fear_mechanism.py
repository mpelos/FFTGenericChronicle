#!/usr/bin/env python3
"""Validate the job-free DCL Fear observe fixture or explicitly armed live-probe fragment."""
from __future__ import annotations

import argparse
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SETTINGS = ROOT / "work/1784383324-battle-runtime-settings.dcl-fear-safe-builder.json"
DEFAULT_LIVE_PROBE = ROOT / "work/1784383324-battle-runtime-settings.dcl-fear-safe-builder-live.json"


def analyze(settings: dict, expected_carrier_id: int | None = None) -> list[str]:
    errors: list[str] = []
    armed = settings.get("DclFearForcedFleeControlEnabled") is True
    expected_top = {
        "DclFearControlEnabled": True,
        "DclFearLogOnly": not armed,
        "DclFearStatusRuleName": "dcl-fear",
        "DclFearTargetListRva": 0x281EC3,
        "DclFearTargetListExpectedBytes": "E8 8C 08 00 00",
        "DclFearPlayerConfirmRva": 0x20C55F,
        "DclFearChickenDispatchRva": 0x38BC37,
        "DclFearChickenDispatchExpectedBytes": "F6 47 63 04 74 47",
        "DclStatusControlEnabled": True,
    }
    for key, expected in expected_top.items():
        if settings.get(key) != expected:
            errors.append(f"{key} must be {expected!r}")

    rules = settings.get("DclStatusRules")
    if not isinstance(rules, list):
        return errors + ["DclStatusRules must be a list"]
    fear_rules = [
        rule for rule in rules
        if isinstance(rule, dict) and rule.get("Name") == settings.get("DclFearStatusRuleName")
    ]
    if len(fear_rules) != 1:
        return errors + ["DclStatusRules must contain exactly one named Fear carrier rule"]
    rule = fear_rules[0]
    carrier_id = rule.get("AbilityId")
    if expected_carrier_id is not None and carrier_id != expected_carrier_id:
        errors.append(f"Fear carrier AbilityId must be {expected_carrier_id!r}")
    carrier_policy = {
        0: (1, "absent", {}),
        53: (-1, "replaced-post-calc-reskin", {
            "NativePacketByteIndex": 2,
            "NativePacketMask": 8,
        }),
    }.get(carrier_id)
    if carrier_policy is None:
        errors.append(f"Fear carrier {carrier_id!r} is not an audited technical carrier")
        carrier_policy = (-999, "invalid", {})
    expected_rule = {
        "Name": "dcl-fear",
        "AbilityId": carrier_id,
        "ActionType": carrier_policy[0],
        "StatusByteIndex": 2,
        "StatusMask": 4,
        "Operation": "add",
        "NativeRiderPolicy": carrier_policy[1],
        "ResistanceFormula": "clamp(target.brave / 10, 3, 18)",
        "DurationTargetTurns": 1,
        **carrier_policy[2],
    }
    for key, expected in expected_rule.items():
        if rule.get(key) != expected:
            errors.append(f"Fear rule {key} must be {expected!r}")

    note = str(settings.get("_note", "")).lower()
    required_note = (
        ("job-free", "bounded live-probe", "assigns no job", "no final balance content", "compose")
        if armed
        else ("job-free", "observe-only", "does not assign fear to a job", "242 is deliberately excluded")
    )
    for required in required_note:
        if required not in note:
            errors.append(f"_note must preserve the {required!r} boundary")
    if not armed and settings.get("DclFearLogOnly") is False:
        errors.append("DclFearLogOnly may be false only with DclFearForcedFleeControlEnabled=true")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("settings", type=Path, nargs="?", default=DEFAULT_SETTINGS)
    parser.add_argument("--carrier-id", type=int)
    args = parser.parse_args()
    settings = json.loads(args.settings.read_text(encoding="utf-8-sig"))
    errors = analyze(settings, expected_carrier_id=args.carrier_id)
    for error in errors:
        print(f"ERROR: {error}")
    print(f"DCL Fear mechanism analysis {'FAIL' if errors else 'PASS'}")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
