#!/usr/bin/env python3
"""Regression tests for the DCL native weapon line-of-fire analyzer."""
from __future__ import annotations

from pathlib import Path

import analyze_dcl_weapon_line_of_fire as lof


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    report, ok = lof.render_report(
        lof.DEFAULT_EXE,
        lof.DEFAULT_ITEMS,
        Path("work/test-dcl-weapon-line-of-fire-analysis.md"),
    )
    check(ok, "expected all weapon line-of-fire anchors, family flags, and callers to pass")

    for anchor in lof.ANCHORS:
        check(f"`{anchor.name}`" in report, f"missing anchor row for {anchor.name}")
        check(f"`0x{anchor.rva:X}`" in report, f"missing RVA for {anchor.name}")

    required_fragments = (
        "| Gun | 6 | `Direct`",
        "| Crossbow | 6 | `Direct`",
        "| Bow | 9 | `Arc`",
        "| Pole | 8 | `Lunging`",
        "candidate == resolver_result",
        "Arc resolver callers: `0x280306`.",
        "Direct resolver callers: `0x28039E`.",
        "Lunging resolver callers: `0x2803ED`.",
        "Implementation boundary",
    )
    for fragment in required_fragments:
        check(fragment in report, f"missing report fragment: {fragment}")

    print("DCL weapon line-of-fire analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
