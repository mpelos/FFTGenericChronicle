#!/usr/bin/env python3
"""Unit tests for analyze_dcl_movement_route_live.py."""
from __future__ import annotations

import analyze_dcl_movement_route_live as movement


def line(event: int, cursor: int, old_x: int, target_x: int, *, name: str = "dcl-movement-arrival-native-a") -> str:
    actor = 0x12345600
    linked = 0x7FF612345000
    values = {
        0x88: bytes((old_x, 4)),
        0x8A: bytes((0, 0x16)),
        0x8C: bytes((target_x, 4)),
        0x8E: bytes((0, 0)),
        0xA4: cursor.to_bytes(2, "little"),
        0xA6: b"\x00\x00",
        0xA8: bytes((3, 0x40)),
        0x128: bytes((0x40 + cursor, 0)),
        0x148: linked.to_bytes(8, "little")[0:2],
        0x14A: linked.to_bytes(8, "little")[2:4],
        0x14C: linked.to_bytes(8, "little")[4:6],
        0x14E: linked.to_bytes(8, "little")[6:8],
    }
    raw = "/".join(f"+0x{offset:X}={value.hex().upper()}" for offset, value in values.items())
    return (
        f"[LANDMARK-HIT event={event} id=1 name={name} rva=0x1FE169 "
        f"access=observe base=rbx=0x{actor:X}:other now=1 baseRead=raw-0x200 raw={raw}] "
        "regs=rax=0x0"
    )


def main() -> int:
    text = "\n".join(
        [
            line(1, 1, 1, 2),
            line(2, 2, 2, 3),
            line(3, 3, 3, 4),
        ]
    )
    events = movement.parse_events(text)
    assert len(events) == 3
    errors, grouped, echoes = movement.analyze(events)
    assert not errors, errors
    assert not echoes
    assert events[0].cursor == 1 and events[-1].cursor == 3
    assert events[0].linked_ptr == 0x7FF612345000
    _, ok = movement.render_report(__import__("pathlib").Path("fixture.log"), events, errors, grouped, echoes, 1, 1)
    assert ok

    broken = movement.parse_events("\n".join([line(1, 1, 1, 2), line(2, 3, 9, 4)]))
    broken_errors, _, _ = movement.analyze(broken)
    assert any("cursor discontinuity" in error for error in broken_errors)
    assert any("tile discontinuity" in error for error in broken_errors)

    missing_first = movement.parse_events("\n".join([line(2, 2, 2, 3), line(3, 3, 3, 4)]))
    missing_errors, missing_grouped, missing_echoes = movement.analyze(missing_first)
    assert any("first captured cursor is 2" in error for error in missing_errors)
    assert not movement.is_complete_route(next(iter(missing_grouped.values())))
    _, missing_ok = movement.render_report(
        __import__("pathlib").Path("missing-first.log"),
        missing_first,
        missing_errors,
        missing_grouped,
        missing_echoes,
        1,
        1,
    )
    assert not missing_ok
    print("movement route live parser tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
