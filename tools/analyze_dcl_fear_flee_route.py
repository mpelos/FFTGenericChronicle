#!/usr/bin/env python3
"""Verify the native destination selector and ordinary route handoff needed by DCL Fear."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE

CHICKEN_TEST_RVA = 0x38BC37
CHICKEN_SELECTOR_THUNK_RVA = 0x38E11C
CHICKEN_SELECTOR_BODY_RVA = 0x10D6F6CD
CHICKEN_SELECTOR_SUCCESS_RVA = 0x10D6F81E
ACTIVE_UNIT_PTR_RVA = 0x1872EA0
FLEE_X_RVA = 0x18724D0
FLEE_Y_RVA = 0x18724D2
FLEE_LAYER_RVA = 0x18724D1
PLANNING_BUFFER_RVA = 0x1871A54
WINNING_TILE_RVA = 0x1872364
PLANNER_RVA = 0x321390
MEMSET_RVA = 0x5CA420

MOVEMENT_ACTOR_RESOLVER_RVA = 0x26079C
ROUTE_RESOLVER_RVA = 0x27C7B4
ROUTE_ACCEPT_RVA = 0x20B270
ROUTE_ACCEPT_TAIL_RVA = 0x20B3D7
ROUTE_BEGIN_RVA = 0x203CC8
ROUTE_BEGIN_BODY_RVA = 0xD71C178
POST_MOVEMENT_RVA = 0x203ED4


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("chicken-test", 0x38BC37, "F6 47 63 04 74 47 E8 DA 24 00 00", "tests effective Chicken and calls its selector"),
    Anchor("selector-thunk", 0x38E11C, "E9 AC 15 9E 10", "tail-jumps to the trace selector body"),
    Anchor("selector-entry", 0x10D6F6CD, "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 41 56 41 57 48 83 EC 20", "starts the bounded native flee search"),
    Anchor("selector-scratch-candidate", 0x10D6F79C, "40 88 35 15 1F B0 F0 40 88 3D 10 1F B0 F0 88 1D 09 1F B0 F0", "publishes candidate layer/y/x into selector scratch"),
    Anchor("selector-success-write", 0x10D6F81E, "8A 05 AC 2C B0 F0 48 8B 0D 75 36 B0 F0 88 41 4F 8A 05 9E 2C B0 F0 48 8B 0D 65 36 B0 F0 88 41 50", "writes selected x/y to the active unit"),
    Anchor("selector-success-layer", 0x10D6F83E, "48 8B 15 5B 36 B0 F0 8A 0D 86 2C B0 F0 C0 E1 07 8A 42 51 24 7F 08 C1 88 4A 51 31 C0", "replaces only the active unit layer bit and returns success"),
    Anchor("selector-planner-prefix", 0x38BC56, "33 D2 48 8D 0D F5 5D 4E 01 41 B8 40 02 00 00 E8 B6 E7 23 00 BA 01 00 00 00 B9 FF 00 00 00 E8 17 57 F9 FF 8B 05 E5 66 4E 01", "clears the planning block, selects the winner, and reads its record"),
    Anchor("planner-winning-record", 0x109C9759, "48 8D 0D 40 7F EA F0 88 94 B1 C4 0C 00 00 44 88 94 B1 C6 0C 00 00 40 88 AC B1 C5 0C 00 00 44 88 84 B1 C7 0C 00 00", "publishes winning X/Y/layer/aux bytes"),
    Anchor("accepted-route-arguments", 0x20B38D, "48 8B 87 48 01 00 00 44 8B 0D 21 FC A5 00 44 8B 05 0A FA A5 00 8B 15 10 FC A5 00 0F B6 88 BC 01 00 00", "loads unit index and destination x/y/layer for the native route resolver"),
    Anchor("accepted-route-copy", 0x20B3D7, "0F 10 03 0F 11 87 A8 00 00 00 0F 10 4B 10 0F 11 8F B8 00 00 00", "copies the resolved route into movement actor +0xA8"),
    Anchor("accepted-route-tail", 0x20B418, "0F 10 43 60 0F 11 87 08 01 00 00 0F 10 4B 70 C7 47 44 00 20 00 00 0F 11 8F 18 01 00 00", "finishes the copy and primes actor movement state"),
    Anchor("route-begin-thunk", 0x203CC8, "E9 AB 84 51 0D", "enters the ordinary accepted-route transition"),
    Anchor("route-begin-state", 0xD71C193, "C7 05 2F F0 54 F3 10 00 00 00", "writes ordinary pre-movement battle state 0x10"),
    Anchor("movement-complete-state", 0x203EF3, "C7 05 CF 72 A6 00 12 00 00 00", "writes ordinary post-movement battle state 0x12"),
)


def _hex(raw: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in raw)


def _near_target(raw: bytes, rva: int, opcode: int) -> int | None:
    if len(raw) != 5 or raw[0] != opcode:
        return None
    return rva + 5 + int.from_bytes(raw[1:], "little", signed=True)


def _rip_target(instruction) -> int | None:
    for operand in instruction.operands:
        if operand.type == X86_OP_MEM and operand.mem.base == X86_REG_RIP:
            return instruction.address + instruction.size + operand.mem.disp
    return None


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        passed = actual == expected
        anchors_ok &= passed
        rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | "
            f"{'PASS' if passed else f'FAIL (`{_hex(actual)}`)'} | {anchor.meaning} |"
        )

    direct_edges = (
        (0x38BC3D, 0xE8, CHICKEN_SELECTOR_THUNK_RVA, "Chicken dispatcher -> selector"),
        (0x10D6F7B0, 0xE8, 0x388E50, "candidate route preparation"),
        (0x10D6F7B5, 0xE8, 0x38A464, "candidate route evaluation"),
        (0x38BC65, 0xE8, MEMSET_RVA, "Chicken planning-block reset"),
        (0x38BC74, 0xE8, PLANNER_RVA, "Chicken candidate winner selection"),
        (0x20B3AF, 0xE8, ROUTE_RESOLVER_RVA, "accepted destination -> route resolver"),
        (0x20B435, 0xE8, 0x255D8C, "accepted route presentation cleanup"),
        (0x20B5E8, 0xE8, ROUTE_BEGIN_RVA, "accepted route -> ordinary transition"),
    )
    edge_rows: list[str] = []
    edges_ok = True
    for source, opcode, expected, meaning in direct_edges:
        encoded = dispatch.rva_bytes(pe, raw, source, 5)
        actual = _near_target(encoded, source, opcode)
        passed = actual == expected
        edges_ok &= passed
        edge_rows.append(
            f"| `0x{source:X}` | `{('none' if actual is None else f'0x{actual:X}')}` | "
            f"`0x{expected:X}` | {'PASS' if passed else 'FAIL'} | {meaning} |"
        )

    thunk = dispatch.rva_bytes(pe, raw, CHICKEN_SELECTOR_THUNK_RVA, 5)
    route_thunk = dispatch.rva_bytes(pe, raw, ROUTE_BEGIN_RVA, 5)
    thunk_ok = (
        _near_target(thunk, CHICKEN_SELECTOR_THUNK_RVA, 0xE9) == CHICKEN_SELECTOR_BODY_RVA
        and _near_target(route_thunk, ROUTE_BEGIN_RVA, 0xE9) == ROUTE_BEGIN_BODY_RVA
    )

    rip_sites = (
        (0x10D6F81E, FLEE_X_RVA, "selected X"),
        (0x10D6F824, ACTIVE_UNIT_PTR_RVA, "active unit pointer"),
        (0x10D6F82E, FLEE_Y_RVA, "selected Y"),
        (0x10D6F834, ACTIVE_UNIT_PTR_RVA, "active unit pointer"),
        (0x10D6F83E, ACTIVE_UNIT_PTR_RVA, "active unit pointer"),
        (0x10D6F845, FLEE_LAYER_RVA, "selected layer"),
        (0x38BC58, PLANNING_BUFFER_RVA, "Chicken planning block"),
        (0x38BC79, WINNING_TILE_RVA, "winning tile record"),
        (0x109C9759, WINNING_TILE_RVA - 0xCC4, "planner winning-record array base"),
    )
    rip_rows: list[str] = []
    rip_ok = True
    for site, expected, meaning in rip_sites:
        data = dispatch.rva_bytes(pe, raw, site, 7)
        instruction = next(md.disasm(data, image_base + site), None)
        absolute = None if instruction is None else _rip_target(instruction)
        actual = None if absolute is None else absolute - image_base
        passed = actual == expected
        rip_ok &= passed
        rip_rows.append(
            f"| `0x{site:X}` | `{('none' if actual is None else f'0x{actual:X}')}` | "
            f"`0x{expected:X}` | {'PASS' if passed else 'FAIL'} | {meaning} |"
        )

    overall = anchors_ok and edges_ok and thunk_ok and rip_ok
    lines = [
        "# DCL Fear forced-flee route analysis",
        "",
        "Generated by `tools/analyze_dcl_fear_flee_route.py`.",
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
        *rows,
        "",
        "## Native call edges",
        "",
        "| Source | Actual target | Expected target | Result | Meaning |",
        "| ---: | ---: | ---: | --- | --- |",
        *edge_rows,
        "",
        f"- Selector/route thunk targets: **{'PASS' if thunk_ok else 'FAIL'}**.",
        "",
        "## Selector globals",
        "",
        "| Instruction | Actual RVA | Expected RVA | Result | Meaning |",
        "| ---: | ---: | ---: | --- | --- |",
        *rip_rows,
        "",
        "## Settled transaction boundary",
        "",
        "The Chicken selector performs a bounded native search, writes each candidate into scratch",
        "RVAs `0x18724D0/0x18724D2/0x18724D1`, evaluates it, and stores its score. Its final scratch",
        "tuple is not the winner. Native Chicken then clears planning block `0x1871A54`, calls",
        "planner `0x321390(0xFF, 1)`, and reads the winning four-byte X/Y/layer/aux record from",
        "`0x1872364`. A DCL caller must mirror that complete prefix before restoring active-unit",
        "`+0x4F/+0x50/+0x51` byte-exactly and resolving the winning route.",
        "",
        "The ordinary accepted-movement path supplies unit `+0x1BC` plus destination X/Y/layer to",
        "`0x27C7B4`, copies the returned 128-byte route to movement actor `+0xA8..+0x127`, writes",
        "actor `+0x44 = 0x2000`, and calls `0x203CC8`. That transition writes state `0x10`; the",
        "existing dispatcher advances the route through state `0x11`, and ordinary completion",
        "writes state `0x12` through `0x203ED4`.",
        "",
        "## Remaining live falsifier",
        "",
        "Static evidence does not prove that a route injected during forced-control resolution",
        "returns every player- and AI-controlled Fear unit from state `0x12` to the intended",
        "post-move action opportunity. The first control implementation must remain bounded and",
        "log the selector result, exact coordinate restoration, route length, actor identity, and",
        "state sequence `0x10 -> 0x11 -> 0x12`; live control is armed only after that observation.",
        "",
        f"Overall static gate: **{'PASS' if overall else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), overall


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-fear-flee-route-analysis.md"
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("DCL Fear flee route PASS" if ok else "DCL Fear flee route FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
