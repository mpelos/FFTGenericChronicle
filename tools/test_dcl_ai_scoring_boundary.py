#!/usr/bin/env python3
"""Regression tests for the DCL AI-scoring boundary analyzer."""
from __future__ import annotations

from pathlib import Path

import analyze_dcl_ai_scoring_boundary as ai


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    report, ok = ai.render(
        ai.DEFAULT_EXE,
        Path("work/test-dcl-ai-scoring-boundary-analysis.md"),
        ai.DEFAULT_BASELINE_LOG,
        ai.DEFAULT_FORCED_LOG,
        ai.DEFAULT_LT36_RANKING_LOG,
        ai.DEFAULT_LT36_DELIVERY_LOG,
    )
    check(ok, "expected all AI-scoring boundary anchors, source checks, and live-evidence logs to pass")

    for anchor in ai.ANCHORS:
        check(f"`{anchor.name}`" in report, f"missing anchor row for {anchor.name}")
        check(f"`0x{anchor.rva:X}`" in report, f"missing RVA for {anchor.name}")

    required_fragments = (
        "Expected VM-owned sweep and three calc origins: **PASS**.",
        "the existing staged-bundle probe owns RVA `0x281F12`",
        "the execution damage rewrite owns RVA `0x30A5D7`",
        "confirmed execution results are cached at compute point",
        "baseline forecast selects Rion",
        "forced forecast switches to Ramza",
        "ranking pass selects Ramza for confirmed execution",
        "confirmed Rion result is cached at compute point",
        "pre-clamp consumes the exact cached Rion result",
        "the permanent writer belongs at `0x281F12`",
    )
    for fragment in required_fragments:
        check(fragment in report, f"missing report fragment: {fragment}")

    print("DCL AI-scoring boundary analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
