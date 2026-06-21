#!/usr/bin/env python3
"""Smoke tests for the ACTOR-PROBE CT analyzer."""
from __future__ import annotations

import tempfile
from pathlib import Path

from analyze_actor_probe_ct import (
    parse_actor_probe_line,
    parse_actor_probes,
    render_markdown,
    resolve_ct_attackers,
)


def sample(unit_id: int, speed: int, ct: int) -> str:
    return f"{unit_id:02X}@{speed:02X}{ct:02X}{'00' * 17}"


def probe(target: int, values: list[tuple[int, int, int]]) -> str:
    return "[GC-Probe] [ACTOR-PROBE tgt=0x%02X off=0x40-0x52] %s" % (
        target,
        " ".join(sample(*value) for value in values),
    )


def main() -> int:
    event = parse_actor_probe_line(probe(0x1E, [(0x01, 10, 70), (0x80, 16, 12), (0x1E, 12, 84)]), 7)
    check(event is not None, "actor probe line should parse")
    check(event.line_no == 7, "line number should be preserved")
    check(event.target_id == 0x1E, "target id should parse")
    check(event.samples[0].speed == 10 and event.samples[0].ct == 70, "speed and CT should parse from window")

    lines = [
        probe(0x1E, [(0x01, 10, 70), (0x80, 16, 12), (0x1E, 12, 84), (0x32, 9, 63), (0x1F, 9, 63)]),
        probe(0x1E, [(0x01, 10, 70), (0x80, 16, 12), (0x1E, 12, 84), (0x32, 9, 63), (0x1F, 9, 63)]),
        probe(0x1F, [(0x01, 10, 90), (0x80, 16, 64), (0x1E, 12, 8), (0x32, 9, 81), (0x1F, 9, 81)]),
        probe(0x1E, [(0x01, 10, 20), (0x80, 16, 52), (0x1E, 12, 64), (0x32, 9, 28), (0x1F, 9, 8)]),
        probe(0x1E, [(0x01, 10, 0), (0x80, 16, 60), (0x1E, 12, 100), (0x32, 9, 100), (0x1F, 9, 100)]),
        probe(0x1F, [(0x01, 10, 0), (0x80, 16, 60), (0x1E, 12, 40), (0x32, 9, 0), (0x1F, 9, 100)]),
    ]
    events = parse_actor_probes(lines)
    resolutions = resolve_ct_attackers(events)
    resolved_ids = [resolution.attacker_id for resolution in resolutions]
    check(resolved_ids == [0x80, 0x80, 0x1E, 0x1F, 0x01, 0x32], f"unexpected CT attackers: {resolved_ids}")
    check(resolutions[0].source == "ct-lowest", "first event without history should resolve by lowest CT")
    check(resolutions[2].source == "ct-drop", "Agrias event should resolve by recent CT drop")
    check(resolutions[5].source == "ct-drop", "Cloud/Ramza tie should resolve by CT drop")
    cloud = next(candidate for candidate in resolutions[5].candidates if candidate.unit_id == 0x32)
    ramza = next(candidate for candidate in resolutions[5].candidates if candidate.unit_id == 0x01)
    check(cloud.ct == 0 and cloud.previous_ct == 100 and cloud.ct_drop == 100, "Cloud should show 100->0 CT reset")
    check(ramza.ct == 0 and ramza.previous_ct == 0 and ramza.ct_drop == 0, "Ramza should stay at 0 with no reset")

    report = render_markdown(resolutions)
    check("Resolved events: 6/6" in report, "report should summarize resolved events")
    check("0x32" in report and "ct-drop" in report, "report should include Cloud CT-drop resolution")

    with tempfile.TemporaryDirectory() as tmp:
        path = Path(tmp) / "actor_probe.md"
        path.write_text(report, encoding="utf-8")
        check("Actor Probe CT Analysis" in path.read_text(encoding="utf-8"), "report should be writable markdown")

    print("actor probe CT analyzer smoke test passed")
    return 0


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
