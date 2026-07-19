#!/usr/bin/env python3
"""Regression tests for the DCL Approach live-evidence analyzer."""
from __future__ import annotations

from analyze_dcl_approach_live import analyze_text


LIVE = """
[DCL-APPROACH-HOOK] owner=443 delivery=442
[DCL-APPROACH-DECISION] event=15 cursor=3/3 from=4,3,0 entered=5,3,0 candidates=1 mask=0x10000 delivery=442
[DCL-APPROACH-QUEUE] event=15 accepted=1 commitMask=0x10000 targetMark=0x20->0x60->0x20 bridgeStage=9 unitTile=2,3,0/raw51=0x03 map=13x13 unitLoan=2,3,0/raw51=0x03->5,3,0/raw51=0x03->2,3,0/raw51=0x03
[DCL-REACTION-COMMIT] event=3 pass=2 reactorIdx=4 sourceIdx=0 reactionId=442 idsAgree=True
[DCL-REACTION-DELIVERY-VALIDATION] event=1 stage=typed-family reactorIdx=16 sourceIdx=0 reactionId=442 result=0 accepted=1
[DCL-REACTION-DELIVERY-VALIDATION] event=2 stage=final reactorIdx=16 sourceIdx=0 reactionId=442 result=0 accepted=1
[DCL-REACTION-MATERIALIZED] event=1 reactorIdx=16 sourceIdx=0 reactionId=442 actionType=1 actionId=0 targetMode=5 targetIdx=0
[DCL-REACTION-EFFECT] event=3 state=0x2C sourceIdx=0 reactionId=442 actionId=0 targetCount=0 targets=[]
[DCL-REACTION-EFFECT] event=4 state=0x2C sourceIdx=0 reactionId=442 actionId=0 targetCount=0 targets=[]
[DCL-APPROACH-RESUME] event=15 native=0x28 replacement=0x11 commitMask=0x10000 audit=pass writes=1
[DCL-APPROACH-RESUME-RELEASE] event=15 audit=pass writes=1
[DCL-APPROACH-BOUNDARY] event=16 cursor=0/4 tile=6,12,0 release=new-route-origin
"""


def assert_rejected(text: str, fragment: str) -> None:
    _, errors = analyze_text(text, minimum_effects=2)
    assert errors and any(fragment in error for error in errors), errors


def main() -> int:
    counts, errors = analyze_text(LIVE, minimum_effects=2)
    assert not errors, errors
    assert counts["accepted_queues"] == counts["commits"] == counts["materialized"] == 1
    assert counts["effects"] == 2 and counts["later_events"] == 1

    assert_rejected(LIVE.replace("cursor=3/3", "cursor=2/3"), "terminal route cursor")
    assert_rejected(LIVE.replace("commitMask=0x10000", "commitMask=0x0"), "commit mask")
    assert_rejected(LIVE.replace("0x20->0x60->0x20", "0x20->0x60->0x60"), "target mark")
    assert_rejected(
        LIVE.replace(
            "2,3,0/raw51=0x03->5,3,0/raw51=0x03->2,3,0/raw51=0x03",
            "2,3,0/raw51=0x03->5,3,0/raw51=0x03->5,3,0/raw51=0x03",
        ),
        "coordinate tuple",
    )
    assert_rejected(LIVE.replace("bridgeStage=9", "bridgeStage=4"), "stage 9")
    assert_rejected(LIVE.replace("stage=final", "stage=typed-bonecrusher"), "both typed-family and final")
    assert_rejected(LIVE.replace("targetIdx=0", "targetIdx=1"), "exact source index")
    assert_rejected(LIVE.replace("replacement=0x11", "replacement=0x28"), "resume substitution")
    assert_rejected(LIVE.replace("audit=pass", "audit=fail", 1), "failed audits")
    assert_rejected(LIVE.replace("[DCL-APPROACH-BOUNDARY] event=16", "[DCL-APPROACH-BOUNDARY] event=14"), "control continued")

    print("Approach live analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

