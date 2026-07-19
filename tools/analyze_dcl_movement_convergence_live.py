#!/usr/bin/env python3
"""Validate complete route dispatches observed at movement-updater convergence."""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

from analyze_dcl_movement_route_live import Arrival, parse_events


@dataclass(frozen=True)
class Route:
    actor: int
    name: str
    ordinal: int
    events: tuple[Arrival, ...]
    terminal: Arrival | None


def split_routes(events: list[Arrival]) -> tuple[list[str], list[Route], list[Arrival]]:
    errors: list[str] = []
    idle: list[Arrival] = []
    routes: list[Route] = []
    active: dict[tuple[int, str], list[Arrival]] = {}
    ordinals: dict[tuple[int, str], int] = {}
    seen_sequences: set[int] = set()

    def finish(key: tuple[int, str], terminal: Arrival | None) -> None:
        current = active.pop(key, None)
        if not current:
            return
        ordinals[key] = ordinals.get(key, 0) + 1
        routes.append(Route(key[0], key[1], ordinals[key], tuple(current), terminal))

    for event in sorted(events, key=lambda item: item.event):
        if event.event in seen_sequences:
            errors.append(f"duplicate event sequence {event.event}")
        seen_sequences.add(event.event)
        key = (event.actor, event.name)

        if event.length == 0:
            current = active.get(key)
            if current and current[-1].cursor == current[-1].length:
                finish(key, event)
            else:
                idle.append(event)
            continue

        if event.cursor == 1:
            finish(key, None)
            active[key] = [event]
        elif key not in active:
            errors.append(
                f"actor 0x{event.actor:X} {event.name}: cursor {event.cursor} appeared before cursor 1"
            )
            active[key] = [event]
        else:
            active[key].append(event)

    for key in list(active):
        finish(key, None)
    routes.sort(key=lambda route: route.events[0].event)
    return errors, routes, idle


def route_errors(route: Route) -> list[str]:
    errors: list[str] = []
    events = list(route.events)
    prefix = f"actor 0x{route.actor:X} {route.name} route {route.ordinal}"
    lengths = {event.length for event in events}
    if len(lengths) != 1:
        errors.append(f"{prefix}: route length changed {sorted(lengths)}")
        return errors
    length = events[0].length
    cursors = [event.cursor for event in events]
    expected = list(range(1, length + 1))
    if cursors != expected:
        errors.append(f"{prefix}: cursors {cursors}, expected {expected}")
    if any(event.linked_ptr == 0 for event in events):
        errors.append(f"{prefix}: linked-record pointer is null")

    for event in events:
        dx = abs(event.target_x - event.old_x)
        dy = abs(event.target_y - event.old_y)
        if dx + dy != 1:
            errors.append(
                f"{prefix}: cursor {event.cursor} dispatch delta is ({dx},{dy}), expected one cardinal tile"
            )
    for previous, current in zip(events, events[1:]):
        if (current.old_x, current.old_y, current.old_layer) != (
            previous.target_x,
            previous.target_y,
            previous.target_layer,
        ):
            errors.append(
                f"{prefix}: cursor {current.cursor} starts at "
                f"({current.old_x},{current.old_y},{current.old_layer}), expected previous target "
                f"({previous.target_x},{previous.target_y},{previous.target_layer})"
            )

    terminal = route.terminal
    if terminal is None:
        errors.append(f"{prefix}: missing zero-length terminal observation")
    else:
        final = events[-1]
        if terminal.cursor != length:
            errors.append(f"{prefix}: terminal cursor {terminal.cursor}, expected {length}")
        final_tile = (final.target_x, final.target_y, final.target_layer)
        if (terminal.old_x, terminal.old_y, terminal.old_layer) != final_tile:
            errors.append(f"{prefix}: terminal current tile does not equal final dispatched target")
        if (terminal.target_x, terminal.target_y, terminal.target_layer) != final_tile:
            errors.append(f"{prefix}: terminal target tile does not equal final dispatched target")
        if terminal.linked_ptr != final.linked_ptr:
            errors.append(f"{prefix}: terminal linked-record pointer changed")
    return errors


def is_complete(route: Route) -> bool:
    return bool(route.events) and not route_errors(route)


def render_report(
    log: Path,
    events: list[Arrival],
    errors: list[str],
    routes: list[Route],
    idle: list[Arrival],
    require_actors: int,
    require_complete_routes: int,
) -> tuple[str, bool]:
    for route in routes:
        errors.extend(route_errors(route))
    actors = {route.actor for route in routes}
    complete = sum(1 for route in routes if is_complete(route))
    requirements: list[str] = []
    if len(actors) < require_actors:
        requirements.append(f"expected at least {require_actors} actors, found {len(actors)}")
    if complete < require_complete_routes:
        requirements.append(
            f"expected at least {require_complete_routes} complete routes, found {complete}"
        )
    ok = bool(routes) and not errors and not requirements

    rows: list[str] = []
    for route in routes:
        for event in route.events:
            rows.append(
                f"| {event.event} | `0x{route.actor:X}` | {route.ordinal} | dispatch | "
                f"({event.old_x},{event.old_y},{event.old_layer}) | "
                f"({event.target_x},{event.target_y},{event.target_layer}) | "
                f"{event.cursor}/{event.length} | `0x{event.route_byte:02X}` |"
            )
        if route.terminal is not None:
            terminal = route.terminal
            rows.append(
                f"| {terminal.event} | `0x{route.actor:X}` | {route.ordinal} | terminal | "
                f"({terminal.old_x},{terminal.old_y},{terminal.old_layer}) | "
                f"({terminal.target_x},{terminal.target_y},{terminal.target_layer}) | "
                f"{terminal.cursor}/0 | `0x{terminal.route_byte:02X}` |"
            )

    lines = [
        "# DCL movement-convergence live analysis",
        "",
        f"- Log: `{log}`",
        f"- Parsed convergence events: {len(events)}",
        f"- Route dispatch events: {sum(len(route.events) for route in routes)}",
        f"- Unassociated zero-length idle observations: {len(idle)}",
        f"- Actors: {len(actors)}",
        f"- Complete routes with terminal confirmation: {complete}",
        "- Snapshot semantics: registers are captured synchronously; actor memory is read on the next managed poll, after immediate step dispatch.",
        "",
        "| Event | Actor | Route | Phase | Current tile | Target tile | Cursor/length | Staged byte |",
        "| ---: | ---: | ---: | --- | --- | --- | ---: | ---: |",
        *rows,
        "",
        "## Gate",
        "",
        *(f"- ERROR: {error}" for error in errors),
        *(f"- REQUIREMENT: {requirement}" for requirement in requirements),
        *(["- No structural errors."] if not errors else []),
        f"- Result: **{'PASS' if ok else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--require-actors", type=int, default=1)
    parser.add_argument("--require-complete-routes", type=int, default=1)
    args = parser.parse_args()
    events = parse_events(args.log.read_text(encoding="utf-8", errors="replace"))
    errors, routes, idle = split_routes(events)
    report, ok = render_report(
        args.log,
        events,
        errors,
        routes,
        idle,
        max(0, args.require_actors),
        max(0, args.require_complete_routes),
    )
    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {args.output}")
    print(
        f"movement convergence {'PASS' if ok else 'FAIL'}: "
        f"events={len(events)} routes={len(routes)} errors={len(errors)}"
    )
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
