#!/usr/bin/env python3
"""Unit tests for analyze_dcl_movement_convergence_live.py."""
from __future__ import annotations

from pathlib import Path

import analyze_dcl_movement_convergence_live as convergence
import analyze_dcl_movement_route_live as movement


def line(
    event: int,
    cursor: int,
    current: tuple[int, int],
    target: tuple[int, int],
    *,
    length: int = 3,
) -> str:
    actor = 0x12345600
    linked = 0x7FF612345000
    values = {
        0x88: bytes(current),
        0x8A: bytes((0, 3 if length else 0)),
        0x8C: bytes(target),
        0x8E: bytes((0, 0)),
        0xA4: cursor.to_bytes(2, "little"),
        0xA6: b"\x00\x00",
        0xA8: bytes((length, 0x00)),
        0x128: bytes((0x40 + cursor, 0)),
        0x148: linked.to_bytes(8, "little")[0:2],
        0x14A: linked.to_bytes(8, "little")[2:4],
        0x14C: linked.to_bytes(8, "little")[4:6],
        0x14E: linked.to_bytes(8, "little")[6:8],
    }
    raw = "/".join(f"+0x{offset:X}={value.hex().upper()}" for offset, value in values.items())
    return (
        f"[LANDMARK-HIT event={event} id=1 name=dcl_movement_convergence_native "
        f"rva=0x1FE793 access=observe base=rbx=0x{actor:X}:other now=1 "
        f"baseRead=raw-0x200 raw={raw}] regs=rax=0x0"
    )


def main() -> int:
    text = "\n".join(
        [
            line(1, 1, (1, 4), (2, 4)),
            line(2, 2, (2, 4), (3, 4)),
            line(3, 3, (3, 4), (3, 3)),
            line(4, 3, (3, 3), (3, 3), length=0),
        ]
    )
    events = movement.parse_events(text)
    errors, routes, idle = convergence.split_routes(events)
    report, ok = convergence.render_report(Path("fixture.log"), events, errors, routes, idle, 1, 1)
    assert ok, report
    assert len(routes) == 1 and convergence.is_complete(routes[0])

    missing_cursor = movement.parse_events(
        "\n".join(
            [
                line(1, 1, (1, 4), (2, 4)),
                line(2, 3, (3, 4), (3, 3)),
                line(3, 3, (3, 3), (3, 3), length=0),
            ]
        )
    )
    errors, routes, idle = convergence.split_routes(missing_cursor)
    _, ok = convergence.render_report(
        Path("missing-cursor.log"), missing_cursor, errors, routes, idle, 1, 1
    )
    assert not ok

    missing_terminal = movement.parse_events(
        "\n".join(
            [
                line(1, 1, (1, 4), (2, 4)),
                line(2, 2, (2, 4), (3, 4)),
                line(3, 3, (3, 4), (3, 3)),
            ]
        )
    )
    errors, routes, idle = convergence.split_routes(missing_terminal)
    _, ok = convergence.render_report(
        Path("missing-terminal.log"), missing_terminal, errors, routes, idle, 1, 1
    )
    assert not ok
    print("movement convergence live parser tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
