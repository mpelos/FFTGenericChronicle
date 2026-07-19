#!/usr/bin/env python3
"""Validate the native forced-flee coordinator evidence from a DCL Fear live log."""
from __future__ import annotations

import argparse
import re
from dataclasses import dataclass
from pathlib import Path


HOOK_RE = re.compile(
    r"\[DCL-FEAR-HOOK\].*?forcedFlee=selector(?:-planner)?-route-coordinator armed=(?P<armed>[01])"
)
EVENT_RE = re.compile(
    r"\[DCL-FEAR-FLEE\] event=(?P<event>\d+) state=(?P<state>\w+) stage=(?P<stage>-?\d+) "
    r"unit=0x(?P<unit>[0-9A-Fa-f]+) actor=0x(?P<actor>[0-9A-Fa-f]+) "
    r"before=(?P<before>\d+,\d+,\d+) selected=(?P<selected>\d+,\d+,\d+) "
    r"restored=(?P<restored>\d+,\d+,\d+) routeLength=(?P<route>\d+) "
    r"cursorBefore=(?P<cursor>-?\d+) battleStateAfter=0x(?P<battle>[0-9A-Fa-f]+)"
)


@dataclass(frozen=True)
class FleeEvent:
    sequence: int
    state: str
    stage: int
    unit: int
    actor: int
    before: str
    selected: str
    restored: str
    route_length: int
    cursor_before: int
    battle_state_after: int


def parse(text: str) -> tuple[list[int], list[FleeEvent], list[str]]:
    armed = [int(match.group("armed")) for match in HOOK_RE.finditer(text)]
    events = [
        FleeEvent(
            sequence=int(match.group("event")),
            state=match.group("state"),
            stage=int(match.group("stage")),
            unit=int(match.group("unit"), 16),
            actor=int(match.group("actor"), 16),
            before=match.group("before"),
            selected=match.group("selected"),
            restored=match.group("restored"),
            route_length=int(match.group("route")),
            cursor_before=int(match.group("cursor")),
            battle_state_after=int(match.group("battle"), 16),
        )
        for match in EVENT_RE.finditer(text)
    ]
    failures = [
        line.strip()
        for line in text.splitlines()
        if "[DCL-FEAR-SKIP]" in line or "[DCL-FEAR-FAILED]" in line
    ]
    return armed, events, failures


def validate(armed: list[int], events: list[FleeEvent], failures: list[str]) -> list[str]:
    errors: list[str] = []
    if armed != [1]:
        errors.append(f"expected one armed hook install, observed {armed}")
    if failures:
        errors.extend(f"runtime hook failure: {failure}" for failure in failures)
    if not events:
        errors.append("no DCL-FEAR-FLEE event was captured")
        return errors

    sequences: set[int] = set()
    for event in events:
        if event.sequence in sequences:
            errors.append(f"event {event.sequence}: duplicate sequence")
        sequences.add(event.sequence)
        if event.state != "RouteStaged":
            errors.append(f"event {event.sequence}: state {event.state}, expected RouteStaged")
        if event.stage != 0:
            errors.append(f"event {event.sequence}: failure stage {event.stage}, expected 0")
        if event.unit == 0 or event.actor == 0:
            errors.append(f"event {event.sequence}: null unit/actor pointer")
        if event.before != event.restored:
            errors.append(
                f"event {event.sequence}: selector mutated unit tile {event.before}->{event.restored}"
            )
        if event.selected == event.before:
            errors.append(f"event {event.sequence}: selector returned the current tile")
        if event.route_length <= 0:
            errors.append(f"event {event.sequence}: empty route")
        if event.cursor_before != 0:
            errors.append(
                f"event {event.sequence}: route cursor began at {event.cursor_before}, expected 0"
            )
        if event.battle_state_after != 0x10:
            errors.append(
                f"event {event.sequence}: post-stage battle state 0x{event.battle_state_after:X}, expected 0x10"
            )
    return errors


def render(log: Path, armed: list[int], events: list[FleeEvent], errors: list[str]) -> str:
    rows = [
        f"| {event.sequence} | `{event.state}` | {event.stage} | `0x{event.unit:X}` | "
        f"`0x{event.actor:X}` | {event.before} | {event.selected} | {event.restored} | "
        f"{event.route_length} | {event.cursor_before} | `0x{event.battle_state_after:X}` |"
        for event in events
    ]
    ok = not errors
    return "\n".join(
        [
            "# DCL Fear forced-flee live analysis",
            "",
            f"- Log: `{log}`",
            f"- Armed hook observations: `{armed}`",
            f"- Coordinator events: {len(events)}",
            "",
            "| Event | State | Stage | Unit | Actor | Before | Selected | Restored | Route | Cursor | Battle state |",
            "| ---: | --- | ---: | ---: | ---: | --- | --- | --- | ---: | ---: | ---: |",
            *rows,
            "",
            "## Gate",
            "",
            *(f"- ERROR: {error}" for error in errors),
            *(["- All coordinator invariants hold."] if ok else []),
            f"- Result: **{'PASS' if ok else 'FAIL'}**.",
            "",
        ]
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    armed, events, failures = parse(args.log.read_text(encoding="utf-8", errors="replace"))
    errors = validate(armed, events, failures)
    report = render(args.log, armed, events, errors)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {args.output}")
    print(f"Fear forced-flee live {'PASS' if not errors else 'FAIL'}: events={len(events)} errors={len(errors)}")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
