#!/usr/bin/env python3
"""Bind the post-route final-tile event boundary without interrupting movement."""
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

MOVEMENT_UPDATE_RVA = 0x1FE59C
FINAL_ROUTE_BRANCH_RVA = 0x1FE938
FINALIZER_CALL_RVA = 0x1FE93B
FINAL_TILE_HOOK_RVA = 0xD45A2A2
MOVEMENT_UPDATE_EPILOGUE_RVA = 0x1FE940
FINALIZER_THUNK_RVA = 0x1FD2D0
FINALIZER_BODY_RVA = 0xD45A0F0
POSITION_SYNC_THUNK_RVA = 0x1FD0F8
POSITION_SYNC_BODY_RVA = 0xD43CEC0
POSITION_SYNC_CALL_RVA = 0xD45A10E
POSITION_WRITER_THUNK_RVA = 0x27192C
POSITION_WRITER_CALL_RVA = 0xD43CF29
MOVEMENT_COMPLETE_CHECK_RVA = 0x211E09
STATE_12_HELPER_RVA = 0x203ED4
STATE_12_HELPER_CALL_RVA = 0x211E3B
STATE_12_WRITE_RVA = 0x203EF3


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor(
        "terminal-route-decision",
        0x1FE889,
        "84 C0 0F 84 A7 00 00 00 8B 8B A4 00 00 00 3B C8 0F 83 99 00 00 00",
        "zero length or cursor >= length enters the route-finalization branch",
    ),
    Anchor(
        "terminal-finalizer-call",
        FINAL_ROUTE_BRANCH_RVA,
        "48 8B CB E8 90 E9 FF FF",
        "passes the exact movement actor to the finalizer on the terminal branch only",
    ),
    Anchor(
        "movement-updater-epilogue",
        MOVEMENT_UPDATE_EPILOGUE_RVA,
        "48 8B 5C 24 30 48 83 C4 20 5F C3",
        "returns without pausing, queueing, or rewriting the route",
    ),
    Anchor(
        "finalizer-convergence",
        FINAL_TILE_HOOK_RVA,
        "48 8B 8C 24 A8 00 00 00 48 31 E1 E8 1E 58 FC F2 48 81 C4 B0 00 00 00 5B C3",
        "all finalizer paths converge after position synchronization and before cookie/epilogue",
    ),
    Anchor(
        "finalizer-position-sync-call",
        0xD45A10B,
        "48 89 CB E8 E5 2F DA F2",
        "the finalizer first synchronizes the actor's completed tile to battle-unit state",
    ),
    Anchor(
        "position-writer-call",
        0xD43CF1C,
        "8A 93 88 00 00 00 C1 F8 0A 88 44 24 20 E8 FE 49 E3 F2",
        "uses actor current X/Y/layer and commits them through the canonical position writer",
    ),
    Anchor(
        "dispatcher-completion-check",
        MOVEMENT_COMPLETE_CHECK_RVA,
        "44 38 AB 8B 00 00 00 75 2E 0F B6 8B A8 00 00 00 39 8B A4 00 00 00 72 1F",
        "after the updater returns, the dispatcher requires idle state and cursor >= route length",
    ),
    Anchor(
        "dispatcher-state12-call",
        0x211E2A,
        "0F B6 4B 08 E8 65 EC 04 00 48 8B C8 E8 19 91 FE FF E8 94 20 FF FF",
        "the completed mover is finalized and the state-0x12 helper is invoked",
    ),
    Anchor(
        "state12-write",
        STATE_12_WRITE_RVA,
        "C7 05 CF 72 A6 00 12 00 00 00",
        "ordinary movement advances to battle state 0x12 only after finalization returns",
    ),
)


def _hex(raw: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in raw)


def _relative_target(pe: pefile.PE, raw: bytes, rva: int, opcode: int) -> int | None:
    encoded = dispatch.rva_bytes(pe, raw, rva, 5)
    if len(encoded) != 5 or encoded[0] != opcode:
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

    targets = {
        "finalizer thunk": (
            _relative_target(pe, raw, FINALIZER_THUNK_RVA, 0xE9),
            FINALIZER_BODY_RVA,
        ),
        "terminal finalizer call": (
            _relative_target(pe, raw, FINALIZER_CALL_RVA, 0xE8),
            FINALIZER_THUNK_RVA,
        ),
        "position-sync thunk": (
            _relative_target(pe, raw, POSITION_SYNC_THUNK_RVA, 0xE9),
            POSITION_SYNC_BODY_RVA,
        ),
        "finalizer position-sync call": (
            _relative_target(pe, raw, POSITION_SYNC_CALL_RVA, 0xE8),
            POSITION_SYNC_THUNK_RVA,
        ),
        "canonical position-writer call": (
            _relative_target(pe, raw, POSITION_WRITER_CALL_RVA, 0xE8),
            POSITION_WRITER_THUNK_RVA,
        ),
        "state-0x12 helper call": (
            _relative_target(pe, raw, STATE_12_HELPER_CALL_RVA, 0xE8),
            STATE_12_HELPER_RVA,
        ),
    }
    target_rows: list[str] = []
    targets_ok = True
    for name, (actual, expected) in targets.items():
        passed = actual == expected
        targets_ok &= passed
        actual_text = "none" if actual is None else f"`0x{actual:X}`"
        target_rows.append(
            f"| {name} | {actual_text} | `0x{expected:X}` | {'PASS' if passed else 'FAIL'} |"
        )

    finalizer_callers = tuple(
        provenance.all_direct_callers(pe, raw, FINALIZER_THUNK_RVA)
    )
    state12_callers = tuple(
        provenance.all_direct_callers(pe, raw, STATE_12_HELPER_RVA)
    )
    caller_sets_ok = (
        finalizer_callers == (FINALIZER_CALL_RVA,)
        and state12_callers == (STATE_12_HELPER_CALL_RVA,)
    )

    overall = anchors_ok and targets_ok and caller_sets_ok
    lines = [
        "# DCL final-tile movement-completion analysis",
        "",
        "Generated by `tools/analyze_dcl_final_tile.py`.",
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
        "## Exact call chain",
        "",
        "| Edge | Actual target | Expected target | Result |",
        "| --- | ---: | ---: | --- |",
        *target_rows,
        "",
        "## Caller exclusivity",
        "",
        f"- Finalizer thunk callers: {', '.join(f'`0x{x:X}`' for x in finalizer_callers)}.",
        f"- State-`0x12` helper callers: {', '.join(f'`0x{x:X}`' for x in state12_callers)}.",
        f"- Exact caller-set gate: **{'PASS' if caller_sets_ok else 'FAIL'}**.",
        "",
        "## Boundary contract",
        "",
        f"The native movement updater reaches `0x{FINAL_ROUTE_BRANCH_RVA:X}` only when the route is "
        "empty or its cursor has consumed the complete accepted route. It passes the movement actor "
        f"to the finalizer and calls `0x{FINALIZER_THUNK_RVA:X}`. That finalizer synchronizes the "
        "actor's current X/Y/layer through the canonical battle-unit position writer before it "
        "returns.",
        "",
        f"An `ExecuteFirst` hook at the finalizer convergence point `0x{FINAL_TILE_HOOK_RVA:X}` therefore "
        "runs after final-coordinate commit and before the finalizer cookie check/return and the "
        "dispatcher advance to state `0x12`. The hook can read `rbx` as the preserved exact "
        "movement actor. It must be observe/publish-only: it does not pause, invoke a Reaction queue, "
        "change route bytes/cursor, alter actor state, or replace battle state.",
        "",
        "Battle generation plus ring publication sequence identifies each distinct finalizer "
        "convergence. Actor/unit identity, cursor/cleared length, route hash, and final tile are event "
        "payload. The post-finalizer route hash is diagnostic, not a cross-movement deduplication key. "
        "Managed consumers may evaluate authored predicates only after receiving this one-shot event.",
        "",
        "## Confidence",
        "",
        "- **Proven static:** the exact terminal branch, finalizer/position-writer chain, subsequent "
        "  idle/cursor completion gate, and state-`0x12` write are byte-bound.",
        "- **Proven live components:** ordinary player and AI routes each reach this convergence point "
        "  once with a consumed cursor, cleared route length, and converged final coordinates.",
        f"- **Proven combined boundary:** `ExecuteFirst 0x{FINAL_TILE_HOOK_RVA:X}` publishes exactly one "
        "  accepted event for the observed player and AI movements. Preview, cancellation, Wait "
        "  without movement, and current-tile selection publish no event.",
        "",
        "## Terminal movement updater",
        "",
        *dispatch.disassembly(md, pe, raw, 0x1FE889, 0xC2),
        "",
        "## Finalizer prefix",
        "",
        *dispatch.disassembly(md, pe, raw, FINALIZER_BODY_RVA, 0x55),
        "",
        "## Canonical final-position synchronization",
        "",
        *dispatch.disassembly(md, pe, raw, POSITION_SYNC_BODY_RVA, 0x78),
        "",
        "## Dispatcher completion and state transition",
        "",
        *dispatch.disassembly(md, pe, raw, MOVEMENT_COMPLETE_CHECK_RVA, 0x3C),
        "",
        *dispatch.disassembly(md, pe, raw, STATE_12_HELPER_RVA, 0x30),
        "",
        f"Overall final-tile static gate: **{'PASS' if overall else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), overall


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-final-tile-analysis.md"
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("final-tile boundary PASS" if ok else "final-tile boundary FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
