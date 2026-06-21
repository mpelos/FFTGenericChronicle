#!/usr/bin/env python3
"""Smoke test for the generated offline-readiness audit."""
from __future__ import annotations

import report_offline_readiness as report


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    generated = report.build_report()
    check("Overall offline status: PASS" in generated, "offline readiness should pass")
    check("custom formula demo live proof" in generated, "missing custom formula live gate")
    check("prepare-custom-formula-demo.ps1" in generated, "custom formula live gate should name the helper")
    check("trace.attackerpa" in generated, "custom formula gate should require attacker PA trace")
    check("trace.targetfaith" in generated, "custom formula gate should require target Faith trace")
    check("sentinel-coarse action identity calibration" in generated, "missing sentinel live gate")
    check("hook-register/pre-damage clue capture" in generated, "missing hook register live gate")
    check("same-hit KO path" in generated, "missing same-hit KO live gate")
    check("live gate runbook | PASS" in generated, "live gate runbook should be a passing offline check")
    check("death/KO canonical finding | PASS" in generated, "death/KO finding should be a passing offline check")
    check("death/KO doc consistency | PASS" in generated, "death/KO doc consistency should pass")
    check("static executable anchors | PASS" in generated, "static executable anchors should pass on this repo state")
    check(report.OUT.exists(), f"offline readiness report missing: {report.OUT}")
    actual = report.OUT.read_text(encoding="utf-8")
    check(actual == generated, "offline readiness report is stale; run python tools/report_offline_readiness.py")
    print("offline readiness report smoke test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
