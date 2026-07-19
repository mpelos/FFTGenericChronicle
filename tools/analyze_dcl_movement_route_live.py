#!/usr/bin/env python3
"""Analyze raw-base landmark events from the DCL per-tile movement-route live probe."""
from __future__ import annotations

import argparse
import re
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path


EVENT_RE = re.compile(
    r"\[LANDMARK-HIT event=(?P<event>\d+).*?name=(?P<name>dcl[_-]movement[_-](?:arrival|convergence)[_-][^ ]+) "
    r"rva=0x(?P<rva>[0-9A-Fa-f]+).*?base=rbx=0x(?P<actor>[0-9A-Fa-f]+).*? "
    r"baseRead=raw-0x200 raw=(?P<raw>.*?)\] regs="
)
OFFSET_RE = re.compile(r"\+0x(?P<offset>[0-9A-Fa-f]+)=(?P<value>[0-9A-Fa-f]{2,4})")


@dataclass(frozen=True)
class Arrival:
    event: int
    name: str
    rva: int
    actor: int
    old_x: int
    old_y: int
    old_layer: int
    state: int
    target_x: int
    target_y: int
    target_layer: int
    cursor: int
    length: int
    route_byte: int
    linked_ptr: int


def _pair_bytes(offsets: dict[int, str], offset: int) -> tuple[int, int]:
    value = offsets.get(offset, "")
    if len(value) != 4:
        raise ValueError(f"missing two-byte raw capture at +0x{offset:X}")
    return int(value[:2], 16), int(value[2:4], 16)


def _dword(offsets: dict[int, str], offset: int) -> int:
    lo = _pair_bytes(offsets, offset)
    hi = _pair_bytes(offsets, offset + 2)
    return lo[0] | (lo[1] << 8) | (hi[0] << 16) | (hi[1] << 24)


def _qword(offsets: dict[int, str], offset: int) -> int:
    return _dword(offsets, offset) | (_dword(offsets, offset + 4) << 32)


def parse_events(text: str) -> list[Arrival]:
    events: list[Arrival] = []
    for line in text.splitlines():
        match = EVENT_RE.search(line)
        if match is None:
            continue
        offsets = {
            int(item.group("offset"), 16): item.group("value").upper()
            for item in OFFSET_RE.finditer(match.group("raw"))
        }
        old_x, old_y = _pair_bytes(offsets, 0x88)
        old_layer, state = _pair_bytes(offsets, 0x8A)
        target_x, target_y = _pair_bytes(offsets, 0x8C)
        target_layer, _ = _pair_bytes(offsets, 0x8E)
        length, _ = _pair_bytes(offsets, 0xA8)
        route_byte, _ = _pair_bytes(offsets, 0x128)
        events.append(
            Arrival(
                event=int(match.group("event")),
                name=match.group("name"),
                rva=int(match.group("rva"), 16),
                actor=int(match.group("actor"), 16),
                old_x=old_x,
                old_y=old_y,
                old_layer=old_layer,
                state=state,
                target_x=target_x,
                target_y=target_y,
                target_layer=target_layer,
                cursor=_dword(offsets, 0xA4),
                length=length,
                route_byte=route_byte,
                linked_ptr=_qword(offsets, 0x148),
            )
        )
    return events


def analyze(events: list[Arrival]) -> tuple[list[str], dict[int, list[Arrival]], list[Arrival]]:
    errors: list[str] = []
    grouped: dict[int, list[Arrival]] = defaultdict(list)
    terminal_echoes: list[Arrival] = []
    seen_events: set[int] = set()
    for event in events:
        if event.event in seen_events:
            errors.append(f"duplicate event sequence {event.event}")
        seen_events.add(event.event)
        if event.length == 0 and (event.old_x, event.old_y, event.old_layer) == (
            event.target_x,
            event.target_y,
            event.target_layer,
        ):
            terminal_echoes.append(event)
            continue
        grouped[event.actor].append(event)
        if event.cursor < 1:
            errors.append(f"event {event.event}: cursor {event.cursor} is not post-consumption")
        if event.cursor > event.length:
            errors.append(f"event {event.event}: cursor {event.cursor} exceeds length {event.length}")
        if (event.old_x, event.old_y, event.old_layer) == (
            event.target_x,
            event.target_y,
            event.target_layer,
        ):
            errors.append(f"event {event.event}: old and target tile are identical")

    for actor, actor_events in grouped.items():
        actor_events.sort(key=lambda item: item.event)
        names = {item.name for item in actor_events}
        if len(names) != 1:
            errors.append(f"actor 0x{actor:X}: route switched implementations {sorted(names)}")
        lengths = {item.length for item in actor_events}
        if len(lengths) != 1:
            errors.append(f"actor 0x{actor:X}: route length changed {sorted(lengths)}")
        linked = {item.linked_ptr for item in actor_events}
        if 0 in linked:
            errors.append(f"actor 0x{actor:X}: linked-record pointer is null")
        if actor_events[0].cursor != 1:
            errors.append(
                f"actor 0x{actor:X}: first captured cursor is {actor_events[0].cursor}, expected 1"
            )
        for previous, current in zip(actor_events, actor_events[1:]):
            if current.cursor != previous.cursor + 1:
                errors.append(
                    f"actor 0x{actor:X}: cursor discontinuity {previous.cursor}->{current.cursor}"
                )
            if (current.old_x, current.old_y, current.old_layer) != (
                previous.target_x,
                previous.target_y,
                previous.target_layer,
            ):
                errors.append(
                    f"actor 0x{actor:X}: tile discontinuity "
                    f"({previous.target_x},{previous.target_y},{previous.target_layer})->"
                    f"({current.old_x},{current.old_y},{current.old_layer})"
                )
    return errors, dict(grouped), terminal_echoes


def is_complete_route(route: list[Arrival]) -> bool:
    if not route:
        return False
    length = route[0].length
    return (
        length > 0
        and len(route) == length
        and all(event.length == length for event in route)
        and [event.cursor for event in route] == list(range(1, length + 1))
    )


def render_report(
    log: Path,
    events: list[Arrival],
    errors: list[str],
    grouped: dict[int, list[Arrival]],
    terminal_echoes: list[Arrival],
    require_actors: int,
    require_complete_routes: int,
) -> tuple[str, bool]:
    complete = sum(1 for route in grouped.values() if is_complete_route(route))
    requirements: list[str] = []
    if len(grouped) < require_actors:
        requirements.append(f"expected at least {require_actors} actors, found {len(grouped)}")
    if complete < require_complete_routes:
        requirements.append(
            f"expected at least {require_complete_routes} complete routes, found {complete}"
        )
    arrivals = sum(len(route) for route in grouped.values())
    ok = arrivals > 0 and not errors and not requirements

    rows: list[str] = []
    for actor, route in sorted(grouped.items()):
        for event in route:
            rows.append(
                f"| {event.event} | `0x{actor:X}` | `{event.name}` | "
                f"({event.old_x},{event.old_y},{event.old_layer}) | "
                f"({event.target_x},{event.target_y},{event.target_layer}) | "
                f"{event.cursor}/{event.length} | `0x{event.route_byte:02X}` | "
                f"`0x{event.linked_ptr:X}` |"
            )

    lines = [
        "# DCL movement-route live analysis",
        "",
        f"- Log: `{log}`",
        f"- Parsed hook events: {len(events)}",
        f"- Real tile arrivals: {arrivals}",
        f"- Filtered terminal same-tile echoes: {len(terminal_echoes)}",
        f"- Actors: {len(grouped)}",
        f"- Complete routes: {complete}",
        "",
        "| Event | Actor | Implementation | Old tile | Target tile | Cursor/length | Route byte | Linked record |",
        "| ---: | ---: | --- | --- | --- | ---: | ---: | ---: |",
        *rows,
        "",
        "## Gate",
        "",
        *(f"- ERROR: {error}" for error in errors),
        *(f"- REQUIREMENT: {requirement}" for requirement in requirements),
        *( ["- No structural errors."] if not errors else [] ),
        f"- Result: **{'PASS' if ok else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), ok


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--require-actors", type=int, default=1)
    parser.add_argument("--require-complete-routes", type=int, default=1)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    events = parse_events(args.log.read_text(encoding="utf-8", errors="replace"))
    errors, grouped, terminal_echoes = analyze(events)
    report, ok = render_report(
        args.log,
        events,
        errors,
        grouped,
        terminal_echoes,
        max(0, args.require_actors),
        max(0, args.require_complete_routes),
    )
    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {args.output}")
    print(
        f"movement route live {'PASS' if ok else 'FAIL'}: "
        f"events={len(events)} actors={len(grouped)} errors={len(errors)}"
    )
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
