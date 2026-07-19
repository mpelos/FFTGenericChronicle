#!/usr/bin/env python3
"""Tests for the DCL Fear technical-carrier audit."""
from __future__ import annotations

import copy

import analyze_dcl_fear_carriers as audit


def main() -> int:
    catalog = audit._rows(audit.DEFAULT_CATALOG)
    settings = audit._settings(audit.DEFAULT_SETTINGS)
    ranked = audit.candidates(catalog, settings)
    assert not audit.validate(catalog, settings, ranked)
    assert ranked[0].ability_id == 53
    assert ranked[0].native_status == "Berserk"
    assert 0 not in {candidate.ability_id for candidate in ranked}
    assert 16 not in {candidate.ability_id for candidate in ranked}

    occupied = copy.deepcopy(settings)
    occupied.setdefault("DclStatusRules", []).append({"AbilityId": 53})
    occupied_ranked = audit.candidates(catalog, occupied)
    errors = audit.validate(catalog, occupied, occupied_ranked)
    assert errors and any("expected Fervor 53" in error for error in errors)

    broken_catalog = catalog[:-1]
    errors = audit.validate(broken_catalog, settings, audit.candidates(broken_catalog, settings))
    assert any("0..511" in error for error in errors)

    print("DCL Fear carrier audit tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
