#!/usr/bin/env python3
"""Smoke-test the final-roster DCL reaction capability matrix."""
from __future__ import annotations

import report_dcl_reaction_capabilities as report


def main() -> int:
    errors = report.validate()
    if errors:
        raise AssertionError("; ".join(errors))
    if len(report.ROWS) != 16:
        raise AssertionError(f"expected 16 final reaction entries, got {len(report.ROWS)}")
    ready = [row for row in report.ROWS if row.filter_readiness == "ready-in-context"]
    if len(ready) < 6:
        raise AssertionError("capability audit unexpectedly lost ready formula-filter coverage")
    for row in report.ROWS:
        if row.reaction != "Open" and row.live_gate == "none":
            raise AssertionError(f"authored reaction lacks a live gate: {row.job}/{row.reaction}")
    print("DCL reaction capability matrix smoke test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
