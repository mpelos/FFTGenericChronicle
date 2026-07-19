#!/usr/bin/env python3
"""Unit tests for analyze_dcl_fear_flee_live.py."""
from __future__ import annotations

import analyze_dcl_fear_flee_live as fear


def main() -> int:
    good = "\n".join(
        [
            "[DCL-FEAR-HOOK] targetRva=0x1 confirmRva=0x2 chickenRva=0x3 rule=fear logOnly=0 reactions=excluded forcedFlee=selector-planner-route-coordinator armed=1",
            "[DCL-FEAR-FLEE] event=1 state=RouteStaged stage=0 unit=0x111 actor=0x222 before=4,3,0 selected=8,7,0 restored=4,3,0 routeLength=6 cursorBefore=0 battleStateAfter=0x10",
        ]
    )
    armed, events, failures = fear.parse(good)
    assert not fear.validate(armed, events, failures)
    assert events[0].route_length == 6

    broken = good.replace("restored=4,3,0", "restored=8,7,0").replace(
        "routeLength=6", "routeLength=0"
    )
    armed, events, failures = fear.parse(broken)
    errors = fear.validate(armed, events, failures)
    assert any("mutated unit tile" in error for error in errors)
    assert any("empty route" in error for error in errors)

    armed, events, failures = fear.parse("[DCL-FEAR-FAILED] install exploded")
    errors = fear.validate(armed, events, failures)
    assert any("hook failure" in error for error in errors)
    assert any("no DCL-FEAR-FLEE" in error for error in errors)
    print("Fear forced-flee live parser tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
