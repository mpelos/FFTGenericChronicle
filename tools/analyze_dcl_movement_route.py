#!/usr/bin/env python3
"""Verify the accepted-movement route handoff, cursor, and per-step boundaries."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_calc_provenance as provenance
import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE

ACTOR_CURRENT_X = 0x88
ACTOR_CURRENT_Y = 0x89
ACTOR_CURRENT_LAYER = 0x8A
ACTOR_STATE = 0x8B
ACTOR_TARGET_X = 0x8C
ACTOR_TARGET_Y = 0x8D
ACTOR_TARGET_LAYER = 0x8E
ACTOR_ROUTE_CURSOR = 0xA4
ACTOR_ROUTE_LENGTH = 0xA8
ACTOR_ROUTE_BYTES = 0xA9
ACTOR_CURRENT_ROUTE_BYTE = 0x128

ROUTE_ACCEPT_RVA = 0x20B270
ROUTE_ACCEPT_END_RVA = 0x20B611
ROUTE_RESOLVER_RVA = 0x27C7B4
MOVEMENT_UPDATE_RVA = 0x1FE59C
MOVEMENT_UPDATE_END_RVA = 0x1FE94B
TRACE_UPDATE_RVA = 0xD574F92
TRACE_UPDATE_END_RVA = 0xD5751A1
ARRIVAL_A_RVA = 0x1FE108
ARRIVAL_A_END_RVA = 0x1FE1AC
ARRIVAL_B_RVA = 0x1FE304
ARRIVAL_B_END_RVA = 0x1FE423


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor(
        "accepted-route-resolver-call-a",
        0x20B3A8,
        "0F B6 88 BC 01 00 00 E8 00 14 07 00",
        "loads the battle-unit index and resolves a route for the accepted destination",
    ),
    Anchor(
        "accepted-route-copy-a-head",
        0x20B3D7,
        "0F 10 03 0F 11 87 A8 00 00 00 0F 10 4B 10 0F 11 8F B8 00 00 00",
        "copies the first 32 route-record bytes to actor +0xA8",
    ),
    Anchor(
        "accepted-route-copy-a-tail",
        0x20B418,
        "0F 10 43 60 0F 11 87 08 01 00 00 0F 10 4B 70 C7 47 44 00 20 00 00 0F 11 8F 18 01 00 00",
        "finishes the 128-byte route-record copy through actor +0x127",
    ),
    Anchor(
        "accepted-route-resolver-call-b",
        0x20B528,
        "48 8B 87 48 01 00 00 44 0F BF 8B 7A 01 00 00 44 0F BF 83 7C 01 00 00 0F BF 93 78 01 00 00 0F B6 88 BC 01 00 00 E8 62 12 07 00",
        "resolves the delayed/alternate accepted destination for the same actor",
    ),
    Anchor(
        "accepted-route-copy-b-head",
        0x20B58A,
        "0F 10 06 0F 11 87 A8 00 00 00 0F 10 4E 10 0F 11 8F B8 00 00 00",
        "copies the alternate route record to the identical actor layout",
    ),
    Anchor(
        "route-length-read",
        0x1FE793,
        "0F B6 83 A8 00 00 00",
        "reads the byte-sized route length from actor +0xA8",
    ),
    Anchor(
        "route-cursor-read",
        0x1FE891,
        "8B 8B A4 00 00 00 3B C8",
        "reads the dword route cursor and compares it with route length",
    ),
    Anchor(
        "route-byte-consume",
        0x1FE8BE,
        "8B C2 8A 8C 18 A9 00 00 00 88 0D 68 0F DD 02 48 8B CB FF 83 A4 00 00 00",
        "loads route[cursor] from actor +0xA9 and increments actor +0xA4",
    ),
    Anchor(
        "route-byte-stage-and-setup",
        0x1FE8D6,
        "8A 05 59 0F DD 02 88 83 28 01 00 00 E8 3D FB FF FF",
        "stages the consumed byte at actor +0x128 and prepares the next step",
    ),
    Anchor(
        "arrival-a-current-x",
        0x1FE169,
        "8A 8B 8C 00 00 00 83 63 38 00 98 C1 E0 0C 89 43 28 C1 F8 0C 80 8B 19 03 00 00 01 88 8B 88 00 00 00",
        "on completed step A, copies target X from +0x8C to current X at +0x88",
    ),
    Anchor(
        "arrival-a-current-y-and-idle",
        0x1FE18A,
        "8A 8B 8D 00 00 00 66 89 43 4E 88 8B 89 00 00 00 C6 83 8B 00 00 00 00",
        "copies target Y to current Y and clears movement state",
    ),
    Anchor(
        "arrival-b-current-xy-and-idle",
        0x1FE3EE,
        "8A 83 8C 00 00 00 88 83 88 00 00 00 8A 83 8D 00 00 00 88 83 89 00 00 00 C6 83 8B 00 00 00 00",
        "completed step B performs the same target-to-current X/Y commit and clears state",
    ),
    Anchor(
        "animation-current-layer",
        0x1FC64A,
        "8A 83 8E 00 00 00 40 3A F0 74 09 40 8A F0 88 83 8A 00 00 00",
        "when the interpolated tile reaches its target, copies target layer +0x8E to current layer +0x8A",
    ),
    Anchor(
        "post-arrival-convergence",
        0x1FE786,
        "40 38 BB 8B 00 00 00 0F 85 AD 01 00 00",
        "all native movement states converge on an idle-state check before route consumption",
    ),
    Anchor(
        "trace-route-consume",
        0xD575143,
        "0F B6 83 A8 00 00 00 84 C0 74 4D 8B 8B A4 00 00 00 39 C1 73 3B 8A 8C 19 A9 00 00 00",
        "trace-equivalent updater uses the same length/cursor/route-byte layout",
    ),
    Anchor(
        "trace-route-stage-and-setup",
        0xD575165,
        "48 89 D9 FF 83 A4 00 00 00 8A 05 C1 A6 A5 F5 88 83 28 01 00 00 E8 A5 92 C8 F2",
        "trace-equivalent updater increments and stages one byte before the same setup thunk",
    ),
)


THUNKS = (
    ("step-animation-a", 0x1FDC70, 0xD52F750),
    ("route-direction-setup", 0x1FC234, 0xD40FB2D),
    ("route-tile-setup", 0x1FE424, 0xD555D8E),
    ("final-movement-commit", 0x1FD0F8, 0xD43CEC0),
)


def _hex(raw: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in raw)


def _jump_target(pe: pefile.PE, raw: bytes, rva: int) -> int | None:
    encoded = dispatch.rva_bytes(pe, raw, rva, 5)
    if len(encoded) != 5 or encoded[0] != 0xE9:
        return None
    return rva + 5 + int.from_bytes(encoded[1:], "little", signed=True)


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)

    anchor_rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        passed = actual == expected
        anchors_ok &= passed
        result = "PASS" if passed else f"FAIL (`{_hex(actual)}`)"
        anchor_rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | {anchor.meaning} |"
        )

    thunk_rows: list[str] = []
    thunks_ok = True
    for name, source, expected in THUNKS:
        actual = _jump_target(pe, raw, source)
        passed = actual == expected
        thunks_ok &= passed
        actual_text = "none" if actual is None else f"`0x{actual:X}`"
        thunk_rows.append(
            f"| `{name}` | `0x{source:X}` | {actual_text} | `0x{expected:X}` | "
            f"{'PASS' if passed else 'FAIL'} |"
        )

    accept_callers = tuple(provenance.all_direct_callers(pe, raw, ROUTE_ACCEPT_RVA))
    resolver_callers = tuple(provenance.all_direct_callers(pe, raw, ROUTE_RESOLVER_RVA))
    update_callers = tuple(provenance.all_direct_callers(pe, raw, MOVEMENT_UPDATE_RVA))
    callers_ok = (
        accept_callers == (0x211ED8,)
        and resolver_callers == (0x20B3AF, 0x20B54D)
        and update_callers == (0x20DCC0, 0x211E04, 0xD95C505)
    )

    overall = anchors_ok and thunks_ok and callers_ok
    lines = [
        "# DCL movement-route and per-step analysis",
        "",
        "Generated by `tools/analyze_dcl_movement_route.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Output: `{output}`",
        "",
        "## Byte anchors",
        "",
        "| Name | RVA | Result | Meaning |",
        "| --- | ---: | --- | --- |",
        *anchor_rows,
        "",
        "## Route record and actor layout",
        "",
        "The accepted-movement handler calls `0x27C7B4` with the battle-unit index in `ecx` and "
        "destination coordinates in `dl`/`r8b`/`r9b`. A non-null result is copied as eight "
        "16-byte blocks to actor `+0xA8..+0x127`.",
        "",
        "| Actor field | Meaning |",
        "| ---: | --- |",
        f"| `+0x{ACTOR_CURRENT_X:X}` / `+0x{ACTOR_CURRENT_Y:X}` / `+0x{ACTOR_CURRENT_LAYER:X}` | current logical tile X/Y/layer |",
        f"| `+0x{ACTOR_STATE:X}` | movement animation/state; zero means idle/step complete |",
        f"| `+0x{ACTOR_TARGET_X:X}` / `+0x{ACTOR_TARGET_Y:X}` / `+0x{ACTOR_TARGET_LAYER:X}` | next tile X/Y/layer |",
        f"| `+0x{ACTOR_ROUTE_CURSOR:X}` | dword index of the next route byte |",
        f"| `+0x{ACTOR_ROUTE_LENGTH:X}` | byte route length, copied from route-record byte zero |",
        f"| `+0x{ACTOR_ROUTE_BYTES:X} + index` | inline route byte array, copied from route-record byte one onward |",
        f"| `+0x{ACTOR_CURRENT_ROUTE_BYTE:X}` | route byte staged for the current step |",
        "",
        "Once the previous state returns to zero, `0x1FE59C` checks cursor against length, loads "
        "exactly one byte from `actor + 0xA9 + cursor`, increments cursor, stages that byte at "
        "`+0x128`, and calls the next-step setup thunk. The trace-equivalent updater at "
        "`0xD574F92` performs the same sequence.",
        "",
        "## Thunks",
        "",
        "| Role | Source | Actual target | Expected target | Result |",
        "| --- | ---: | ---: | ---: | --- |",
        *thunk_rows,
        "",
        "## Exact direct callers",
        "",
        f"- Accepted-route handler `0x{ROUTE_ACCEPT_RVA:X}`: "
        + ", ".join(f"`0x{x:X}`" for x in accept_callers),
        f"- Route resolver `0x{ROUTE_RESOLVER_RVA:X}`: "
        + ", ".join(f"`0x{x:X}`" for x in resolver_callers),
        f"- Native movement updater `0x{MOVEMENT_UPDATE_RVA:X}`: "
        + ", ".join(f"`0x{x:X}`" for x in update_callers),
        f"- Caller-set gate: **{'PASS' if callers_ok else 'FAIL'}**.",
        "",
        "## What static evidence settles",
        "",
        "- **Proven:** the engine hands the accepted path to the actor as an existing route record; "
        "the mod does not need to reproduce pathfinding to observe the path.",
        "- **Proven:** route length, cursor, and each route byte are synchronously available before "
        "each step, with exactly one cursor increment per prepared step.",
        "- **Proven:** completed-step handlers copy next X/Y to current X/Y and clear movement state "
        "before the updater tests whether it should consume another route byte.",
        "- **Strong:** `0x1FE786` is the native state convergence and `0x1FE793` is the first "
        "idle-only instruction immediately before the next route-byte decision. The trace-equivalent "
        "boundary is `0xD575143`.",
        "- **Static limit:** disassembly alone does not select which equivalent path a movement class "
        "uses. `tools/analyze_dcl_movement_convergence_live.py` owns that runtime distinction.",
        "",
        "## Accepted-route handoff",
        "",
        *dispatch.disassembly(md, pe, raw, ROUTE_ACCEPT_RVA, ROUTE_ACCEPT_END_RVA - ROUTE_ACCEPT_RVA),
        "",
        "## Native movement update",
        "",
        *dispatch.disassembly(md, pe, raw, MOVEMENT_UPDATE_RVA, MOVEMENT_UPDATE_END_RVA - MOVEMENT_UPDATE_RVA),
        "",
        "## Step-completion handlers",
        "",
        *dispatch.disassembly(md, pe, raw, ARRIVAL_A_RVA, ARRIVAL_A_END_RVA - ARRIVAL_A_RVA),
        "",
        *dispatch.disassembly(md, pe, raw, ARRIVAL_B_RVA, ARRIVAL_B_END_RVA - ARRIVAL_B_RVA),
        "",
        "## Trace-equivalent update",
        "",
        *dispatch.disassembly(md, pe, raw, TRACE_UPDATE_RVA, TRACE_UPDATE_END_RVA - TRACE_UPDATE_RVA),
        "",
        f"Overall static gate: **{'PASS' if overall else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), overall


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument(
        "--check-only",
        action="store_true",
        help="validate and print the gate without writing a work report",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-movement-route-analysis.md"
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("movement route PASS" if ok else "movement route FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
