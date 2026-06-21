#!/usr/bin/env python3
"""Smoke test for the generated live-gate plan."""
from __future__ import annotations

import report_live_gate_plan as plan


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    generated = plan.build_report()
    required = [
        "prepare-custom-formula-demo.ps1",
        "trace.attackerpa",
        "trace.targetfaith",
        "prepare-sentinel-coarse.ps1",
        "prepare-live-mapping.ps1",
        "promote_runtime_offsets.py",
        "prepare-death-gate.ps1 -DryRun -NeuterSpotcheck",
        "battle-runtime-settings.hook-register-probe.json",
        "--hook-regs 12",
        "MinHpFloor=1",
        "Direct HP=0",
    ]
    for text in required:
        check(text in generated, f"live gate plan should mention {text}")

    stale_or_unsafe = [
        "If it dies -> we can cause death ourselves",
        "Outcome A: HP=0 alone produced death evidence",
        "Preparing Generic Chronicle death gate",
    ]
    for text in stale_or_unsafe:
        check(text not in generated, f"live gate plan contains stale death-gate text: {text}")

    check(plan.OUT.exists(), f"live gate plan missing: {plan.OUT}")
    actual = plan.OUT.read_text(encoding="utf-8")
    check(actual == generated, "live gate plan is stale; run python tools/report_live_gate_plan.py")
    print("live gate plan smoke test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
