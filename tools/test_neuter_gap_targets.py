#!/usr/bin/env python3
"""Smoke test for the neuter gap target report."""
from __future__ import annotations

import report_neuter_gap_targets as gaps


def main() -> int:
    report = gaps.build_report()
    check(gaps.DEFAULT_OUT.exists(), f"gap report missing: {gaps.DEFAULT_OUT}")
    actual = gaps.DEFAULT_OUT.read_text(encoding="utf-8")
    check(actual == report, "neuter gap target report is stale; run python tools/report_neuter_gap_targets.py")

    for text in [
        "Materia Blade Plus",
        "ItemWeaponData.Id=32",
        "Gravity",
        "Gravija",
        "Omnislash",
        "Cloud Limit",
    ]:
        check(text in report, f"gap report should mention {text}")

    print("neuter gap target report smoke test passed")
    return 0


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
