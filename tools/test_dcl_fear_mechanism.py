#!/usr/bin/env python3
"""Regression tests for the DCL Fear mechanism fixture analyzer."""
from __future__ import annotations

import copy
import json

import analyze_dcl_fear_mechanism as fear


def main() -> int:
    settings = json.loads(fear.DEFAULT_SETTINGS.read_text(encoding="utf-8-sig"))
    assert fear.analyze(settings) == []

    live = copy.deepcopy(settings)
    live["DclFearLogOnly"] = False
    assert any("DclFearLogOnly" in error for error in fear.analyze(live))

    armed = json.loads(fear.DEFAULT_LIVE_PROBE.read_text(encoding="utf-8-sig"))
    assert fear.analyze(armed) == []
    armed["DclFearForcedFleeControlEnabled"] = False
    assert any("DclFearLogOnly" in error for error in fear.analyze(armed))

    wrong_carrier = copy.deepcopy(settings)
    wrong_carrier["DclStatusRules"][0]["StatusMask"] = 8
    assert any("StatusMask" in error for error in fear.analyze(wrong_carrier))

    integrated = json.loads(fear.DEFAULT_LIVE_PROBE.read_text(encoding="utf-8-sig"))
    integrated["_note"] = (
        "Unified job-free bounded live-probe that assigns no job, contains no final balance "
        "content, and exists to compose the Fear mechanism."
    )
    integrated["DclStatusRules"].insert(0, {
        "Name": "unrelated-rule",
        "AbilityId": 37,
    })
    integrated["DclStatusRules"][1].update({
        "AbilityId": 53,
        "ActionType": -1,
        "NativeRiderPolicy": "replaced-post-calc-reskin",
        "NativePacketByteIndex": 2,
        "NativePacketMask": 8,
    })
    assert fear.analyze(integrated, expected_carrier_id=53) == []
    assert any("AbilityId" in error for error in fear.analyze(integrated, expected_carrier_id=0))
    del integrated["DclStatusRules"][1]["NativePacketMask"]
    assert any("NativePacketMask" in error for error in fear.analyze(integrated, expected_carrier_id=53))

    assigned = copy.deepcopy(settings)
    assigned["_note"] = "final job assignment"
    assert any("job-free" in error for error in fear.analyze(assigned))

    print("DCL Fear mechanism analyzer tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
