#!/usr/bin/env python3
"""Regression tests for the DCL Taunt fallback analyzer."""
from __future__ import annotations

from copy import deepcopy

from analyze_dcl_taunt_fallback import RESISTANCE_FORMULA, RULE_NAME, analyze


SETTINGS = {
    "DclStatusControlEnabled": True,
    "DclStatusRules": [{
        "Name": RULE_NAME,
        "AbilityId": 241,
        "ActionType": -1,
        "StatusByteIndex": 2,
        "StatusMask": 8,
        "Operation": "add",
        "NativeRiderPolicy": "replaced-post-calc",
        "ResistanceFormula": RESISTANCE_FORMULA,
        "DurationTargetTurns": 1,
    }],
}
CATALOG = [{
    "id_dec": "241",
    "inflict_statuses": "Berserk",
    "inflict_status_mode": "AllOrNothing",
    "formula_hex": "0x0A",
}]


def main() -> int:
    assert not analyze(SETTINGS, CATALOG)
    for key, bad in (
        ("AbilityId", 242),
        ("StatusMask", 4),
        ("NativeRiderPolicy", "retained-as-carrier"),
        ("ResistanceFormula", "clamp(target.brave / 10 + 5, 3, 18)"),
        ("DurationTargetTurns", 2),
    ):
        settings = deepcopy(SETTINGS)
        settings["DclStatusRules"][0][key] = bad
        assert analyze(settings, CATALOG), key
    assert analyze(SETTINGS, [{**CATALOG[0], "inflict_statuses": "Chicken"}])
    print("DCL Taunt fallback analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
