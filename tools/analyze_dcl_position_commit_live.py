#!/usr/bin/env python3
"""Analyze caller-correlated live hits from the observe-only DCL position-commit probe."""
from __future__ import annotations

import argparse
import re
import time
from collections import Counter
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = Path(
    r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/battleprobe_log.txt"
)

EXPECTED_HOOK_RVA = 0xE7D735A
EXPECTED_CAPTURE_OFFSET = 0x58
EXPECTED_CALLERS = {
    0x1F1323,
    0x1F1405,
    0x1F14EF,
    0x1F15DC,
    0x1F1686,
    0x1F24AB,
    0x1F3022,
    0x204E1F,
    0x204E77,
    0x20B6BA,
    0x20BC4F,
    0x20BCE2,
    0x27303B,
    0x303A7A,
    0x303AD0,
    0xD43CF29,
    0xD43CF82,
    0xD737999,
    0xD7763BD,
    0xD77641F,
    0xD7E245B,
    0xD8C7D18,
    0xD8C7DD3,
    0xE46AD63,
}

CALLER_CLASS = {
    0x1F1323: "selection/state restore",
    0x1F1405: "selection/state restore",
    0x1F14EF: "selection/state restore",
    0x1F15DC: "selection/state restore",
    0x1F1686: "selection/state restore",
    0x1F3022: "battle deployment/setup commit",
    0x20BC4F: "end-turn/facing commit",
    0x303A7A: "position copy/swap",
    0x303AD0: "position copy/swap",
    0xD43CF29: "accepted movement final-tile commit",
    0xD8C7D18: "post-movement active-state duplicate",
    0xE46AD63: "actor result/presentation sync",
}

HOOK_RE = re.compile(
    r"\[LANDMARK-HOOK dcl_position_commit\].*?rva=0x(?P<rva>[0-9A-F]+).*?"
    r"battleUnitIndex=rdi.*?captureStack=\+0x(?P<capture>[0-9A-F]+)",
    re.IGNORECASE,
)
EVENT_RE = re.compile(r"\[LANDMARK-HIT event=(?P<event>\d+).*?name=dcl_position_commit\b", re.IGNORECASE)
INDEX_RE = re.compile(r"base=unit-index:rdi=(?P<index>\d+)->0x[0-9A-F]+", re.IGNORECASE)
CAPTURE_RE = re.compile(
    r"capturedStack=\+0x(?P<offset>[0-9A-F]+)=0x(?P<address>[0-9A-F]+):"
    r"module\+0x(?P<return_rva>[0-9A-F]+)",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class PositionEvent:
    sequence: int
    unit_index: int
    x: int
    y: int
    layer_high: int
    low_nibble: int
    return_rva: int
    caller_rva: int
    line_number: int

    @property
    def destination(self) -> tuple[int, int, int]:
        return self.x, self.y, self.layer_high | self.low_nibble


def _register(line: str, name: str) -> int | None:
    match = re.search(rf"(?:^|\s){re.escape(name)}=0x([0-9A-F]+):", line, re.IGNORECASE)
    return int(match.group(1), 16) if match else None


def parse_log(text: str) -> tuple[list[tuple[int, int]], list[PositionEvent], list[str]]:
    hooks: list[tuple[int, int]] = []
    events: list[PositionEvent] = []
    errors: list[str] = []
    for line_number, line in enumerate(text.splitlines(), 1):
        hook = HOOK_RE.search(line)
        if hook:
            hooks.append((int(hook.group("rva"), 16), int(hook.group("capture"), 16)))

        if any(marker in line for marker in ("[LANDMARK-SKIP", "[LANDMARK-FAILED", "[LANDMARK-LOST")):
            errors.append(f"line {line_number}: {line.strip()}")

        event_match = EVENT_RE.search(line)
        if not event_match:
            continue
        index_match = INDEX_RE.search(line)
        capture_match = CAPTURE_RE.search(line)
        r8 = _register(line, "r8")
        r9 = _register(line, "r9")
        r10 = _register(line, "r10")
        r14 = _register(line, "r14")
        if index_match is None or capture_match is None or None in (r8, r9, r10, r14):
            errors.append(f"line {line_number}: incomplete position event")
            continue

        capture_offset = int(capture_match.group("offset"), 16)
        if capture_offset != EXPECTED_CAPTURE_OFFSET:
            errors.append(
                f"line {line_number}: capture offset 0x{capture_offset:X}, expected 0x{EXPECTED_CAPTURE_OFFSET:X}"
            )
        return_rva = int(capture_match.group("return_rva"), 16)
        events.append(
            PositionEvent(
                sequence=int(event_match.group("event")),
                unit_index=int(index_match.group("index")),
                x=int(r10) & 0xFF,
                y=int(r9) & 0xFF,
                layer_high=int(r14) & 0x80,
                low_nibble=int(r8) & 0x0F,
                return_rva=return_rva,
                caller_rva=return_rva - 5,
                line_number=line_number,
            )
        )
    return hooks, events, errors


def analyze(
    log: Path,
    output: Path,
    require_coordinate_changes: int = 0,
    require_callers: tuple[int, ...] = (),
) -> tuple[str, bool]:
    text = log.read_text(encoding="utf-8", errors="replace")
    hooks, events, errors = parse_log(text)
    valid_hook = any(rva == EXPECTED_HOOK_RVA and capture == EXPECTED_CAPTURE_OFFSET for rva, capture in hooks)
    unknown = [event for event in events if event.caller_rva not in EXPECTED_CALLERS]

    prior: dict[int, tuple[int, int, int]] = {}
    changes = 0
    event_rows: list[str] = []
    for event in events:
        destination = event.destination
        old = prior.get(event.unit_index)
        changed = old is not None and old != destination
        if changed:
            changes += 1
        prior[event.unit_index] = destination
        old_text = "initial" if old is None else f"{old[0]},{old[1]},0x{old[2]:02X}"
        caller_status = "known" if event.caller_rva in EXPECTED_CALLERS else "UNKNOWN"
        event_rows.append(
            f"| {event.sequence} | {event.line_number} | {event.unit_index} | "
            f"`{event.x},{event.y},0x{destination[2]:02X}` | `{old_text}` | "
            f"{'yes' if changed else 'no'} | `0x{event.caller_rva:X}` | "
            f"{CALLER_CLASS.get(event.caller_rva, 'unclassified statically')} | {caller_status} |"
        )

    counts = Counter(event.caller_rva for event in events)
    caller_rows = [
        f"| `0x{caller:X}` | {count} | {CALLER_CLASS.get(caller, 'unclassified statically')} |"
        for caller, count in sorted(counts.items())
    ]
    missing_required = [caller for caller in require_callers if counts[caller] == 0]
    requirements_ok = changes >= require_coordinate_changes and not missing_required
    ok = valid_hook and bool(events) and not errors and not unknown and requirements_ok

    lines = [
        "# DCL position-commit live analysis",
        "",
        "Generated by `tools/analyze_dcl_position_commit_live.py`.",
        "",
        "## Input and gate",
        "",
        f"- Log: `{log}`",
        f"- Output: `{output}`",
        f"- Exact hook installed: **{'PASS' if valid_hook else 'FAIL'}**",
        f"- Parsed events: **{len(events)}**",
        f"- Coordinate changes after each unit's first observation: **{changes}** "
        f"(required {require_coordinate_changes})",
        f"- Unknown direct callers: **{len(unknown)}**",
        f"- Probe errors/losses: **{len(errors)}**",
        "",
        "## Event timeline",
        "",
        "| Event | Log line | Unit index | Destination X,Y,layer/facing | Prior destination | Changed | Caller | Static class | Caller set |",
        "| ---: | ---: | ---: | --- | --- | --- | ---: | --- | --- |",
        *event_rows,
        "",
        "## Caller counts",
        "",
        "| Caller RVA | Hits | Static class |",
        "| ---: | ---: | --- |",
        *caller_rows,
        "",
        "## Errors",
        "",
        *([f"- {error}" for error in errors] or ["- None."]),
        *([f"- Unknown caller `0x{event.caller_rva:X}` at event {event.sequence}." for event in unknown]),
        *([f"- Required caller `0x{caller:X}` was absent." for caller in missing_required]),
        "",
        "The report establishes exact commits and direct callers. The caller labels that mention",
        "accepted movement, post-movement duplication, or end-turn/facing are live classifications",
        "from the controlled sequence; other labels remain static classifications.",
        "",
        f"Overall live gate: **{'PASS' if ok else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), ok


def _parse_int(value: str) -> int:
    return int(value, 0)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path, nargs="?", default=DEFAULT_LOG)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--require-coordinate-changes", type=int, default=0)
    parser.add_argument("--require-caller", type=_parse_int, action="append", default=[])
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-position-commit-live-analysis.md"
    report, ok = analyze(
        args.log,
        output,
        max(0, args.require_coordinate_changes),
        tuple(args.require_caller),
    )
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {output}")
    print("position commit live PASS" if ok else "position commit live FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
