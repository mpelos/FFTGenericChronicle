#!/usr/bin/env python3
"""Regression tests for the DCL status-duration completion frontier."""
from __future__ import annotations

import analyze_dcl_status_duration_frontier as frontier


def main() -> int:
    rows, covered, errors = frontier.analyze([])
    assert not errors, errors
    assert len(rows) == 40
    assert set(covered) == set()
    by_native = {row.native_status: row for row in rows}

    for name in ("Disable", "Immobilize"):
        row = by_native[name]
        assert row.transfer_state == "blocked-category"
        assert row.category_state == "design-open"
        assert row.duration_design_state == "design-open"
        assert row.duration_pair == ""

    poison = by_native["Poison"]
    assert poison.native_counter == 36
    assert poison.category_state == "fixed"
    assert poison.transfer_state == "blocked-duration-ownership"
    assert len(poison.add_producers) == 10

    blindness = by_native["Blindness"]
    assert blindness.dcl_status == "Darkness"
    assert blindness.category_state == "design-open"
    assert blindness.native_counter == 0
    assert blindness.transfer_state == "not-required-counter-zero"

    assert by_native["Doom"].transfer_state == "lifecycle-forbidden"
    assert by_native["Empty_32"].transfer_state == "system-forbidden"
    assert by_native["Chicken"].transfer_state == "not-required-counter-zero"

    unresolved = {
        row.dcl_status for row in rows if row.category_state == "design-open"
    }
    assert {
        "Darkness", "Silence", "BloodSuck", "Oil", "Undead",
        "DontMove", "DontAct", "Slow", "Stop", "Sleep", "Petrify", "Frog",
    }.issubset(unresolved)

    report = frontier.markdown_text(rows, covered, [])
    assert "Disease" in report
    assert "producer authority alone" in report
    assert "Historical pairs with stale hashes" in report
    print("DCL status-duration frontier tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
