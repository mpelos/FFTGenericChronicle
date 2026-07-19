#!/usr/bin/env python3
"""Classify special and generic pass-2 Reaction materialization paths."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE

SELECTOR_RVA = 0x282E38
SELECTOR_CALL_RVA = 0x2063AD
PRE_TARGET_BUILD_RVA = 0x2831BD
TARGET_LIST_BUILD_CALL_RVA = 0x2831C0
GENERIC_FINALIZE_JUMP_RVA = 0x283003
COMMON_FINALIZE_ID_RVA = 0x2831C5
COMMON_FINALIZE_OUTPUT_RVA = 0x2831CC
POST_SELECTOR_BOUNDARY_RVA = 0x2063BD
ACTOR_CREATE_RVA = 0x260ABC
ACTOR_CREATE_CALL_RVA = 0x2063CA
TYPED_ORDER_HELPER_RVA = 0x283280
ACCEPT_HELPER_RVA = 0x282234
UNIT_TABLE_RVA = 0x1853CE0
SOURCE_INDEX_RVA = 0x186AFF4
ORDER_OFFSET = 0x1A0


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("selector-call", SELECTOR_CALL_RVA, "E8 86 CA 07 00", "pass 2 calls the real-code Reaction selector"),
    Anchor("selected-index-and-reject", 0x2063B2, "8B D0 83 F8 FF 0F 84 3B 02 00 00", "copies the returned unit index and rejects only -1"),
    Anchor("special-pre-target-build-boundary", PRE_TARGET_BUILD_RVA, "48 8B CB E8 6F F0 FF FF", "special carriers 434/435/436/437/440/441/442 reach this accepted boundary"),
    Anchor("special-target-builder-call", TARGET_LIST_BUILD_CALL_RVA, "E8 6F F0 FF FF", "only the accepted special branch calls this VM-owned helper"),
    Anchor("generic-finalize-jump", GENERIC_FINALIZE_JUMP_RVA, "E9 C4 01 00 00", "generic carriers jump directly to common finalization and skip 0x2831BD/0x2831C0"),
    Anchor("common-finalize-id", COMMON_FINALIZE_ID_RVA, "0F B7 35 24 7E 5E 01", "all accepted paths load the exact Reaction id before returning"),
    Anchor("common-finalize-output", COMMON_FINALIZE_OUTPUT_RVA, "66 41 89 36 8B C5 40 88 2F", "all accepted paths publish id/index/order caster at the common return"),
    Anchor("post-selector-boundary", POST_SELECTOR_BOUNDARY_RVA, "0F B7 C8 C7 05 02 4E A6 00 29 00 00 00", "selector has returned only after target-list construction; eax is the selected unit index"),
    Anchor("actor-create-call", ACTOR_CREATE_CALL_RVA, "E8 ED A6 05 00", "creates the actor only after the accepted boundary"),
    Anchor("selector-unit-table", 0x282E80, "48 8D 0D 59 0E 5D 01", "loads battle-unit table RVA 0x1853CE0"),
    Anchor("selector-order-record", 0x282EA2, "48 8D BB A0 01 00 00", "sets rdi to selected unit+0x1A0"),
    Anchor("snapshot-old-order", 0x282F03, "41 8B D1 48 8D 0D 7B 80 5E 01 4C 8B C7 E8 F7 33 DA FF", "copies the old 20-byte order to scratch before materialization"),
    Anchor("stage-and-consume-id", 0x282F17, "40 88 2F 44 88 7F 01 66 89 77 02 66 89 35 C7 80 5E 01 66 44 89 BB CE 01 00 00", "stages candidate/id, exports exact id, then clears unit+0x1CE"),
    Anchor("generic-order-target", 0x282FBA, "44 88 7F 01 C6 47 0A 05 40 88 6F 0B", "generic carrier uses type 0 and initially targets the reactor"),
    Anchor("auto-potion-order", 0x283068, "66 89 47 08 48 8B CF C6 47 01 06 C6 47 0A 05 40 88 6F 0B", "Auto-Potion writes selected item, type 6, and target metadata"),
    Anchor("counter-typed-order", 0x283008, "45 33 C0 45 8B CC 41 8B D4 48 8B CB E8 67 02 00 00", "Counter requests type 1, payload 0, source validation through the shared typed-family call"),
    Anchor("typed-family-result", 0x283019, "85 C0 0F 84 33 01 00 00 E9 3A 01 00 00", "ids 435/436/437/442 restore on nonzero typed-family result"),
    Anchor("bonecrusher-typed-order", 0x283137, "45 33 C0 45 8B CC 41 8B D4 48 8B CB E8 38 01 00 00", "Bonecrusher requests type 1, payload 0, source validation through its separate call"),
    Anchor("bonecrusher-typed-result", 0x283148, "85 C0 75 14 44 88 67 01 C6 47 0A 05", "Bonecrusher restores on nonzero typed result"),
    Anchor("special-accept-or-restore", 0x28315C, "85 C0 74 5D BA 14 00 00 00", "successful final validation skips the restore path"),
    Anchor("failure-restore", 0x283160, "BA 14 00 00 00 4C 8D 05 1C 7E 5E 01 44 8B CA 48 8B CF E8 95 31 DA FF", "failure restores the 20-byte pre-selector order from scratch"),
    Anchor("special-accepted-return", 0x2831C5, "0F B7 35 24 7E 5E 01 66 41 89 36 8B C5 40 88 2F", "accepted special path returns selected index/id without restoring"),
    Anchor("typed-source-index", 0x283294, "48 63 1D 59 7D 5E 01 48 8D B9 A0 01 00 00", "typed helper reads source global and addresses reactor order record"),
    Anchor("typed-type-and-payload", 0x2832AD, "88 57 01 48 8B F1 66 44 89 47 02", "typed helper writes executable order type and payload"),
    Anchor("typed-target", 0x283314, "C6 47 0A 05 48 8B CF 8A 05 D3 7C 5E 01 88 47 0B", "typed helper writes target kind and source unit index"),
)


def _parse_hex(value: str) -> bytes:
    return bytes.fromhex(value)


def _hex(value: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in value)


def validate_anchors(pe: pefile.PE, raw: bytes) -> list[tuple[Anchor, bytes, bool]]:
    results: list[tuple[Anchor, bytes, bool]] = []
    for anchor in ANCHORS:
        expected = _parse_hex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        results.append((anchor, actual, actual == expected))
    return results


def validate_call_graph(pe: pefile.PE, raw: bytes) -> dict[str, tuple[list[int], list[int], bool]]:
    expected = {
        "selector": ([SELECTOR_CALL_RVA], dispatch.direct_callers(pe, raw, SELECTOR_RVA)),
        "typed-order-helper": ([0x283014, 0x283143], dispatch.direct_callers(pe, raw, TYPED_ORDER_HELPER_RVA)),
        "accepted-helper": ([0x2831C0], dispatch.direct_callers(pe, raw, ACCEPT_HELPER_RVA)),
    }
    return {name: (wanted, actual, actual == wanted) for name, (wanted, actual) in expected.items()}


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    anchor_results = validate_anchors(pe, raw)
    graph_results = validate_call_graph(pe, raw)
    ok = all(passed for _, _, passed in anchor_results) and all(passed for _, _, passed in graph_results.values())

    anchor_rows: list[str] = []
    for anchor, actual, passed in anchor_results:
        result = "PASS" if passed else f"FAIL (`{_hex(actual)}`)"
        anchor_rows.append(f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | `{anchor.expected}` | {anchor.meaning} |")

    graph_rows: list[str] = []
    for name, (expected, actual, passed) in graph_results.items():
        expected_text = ", ".join(f"`0x{x:X}`" for x in expected)
        actual_text = ", ".join(f"`0x{x:X}`" for x in actual) or "none"
        graph_rows.append(f"| `{name}` | {expected_text} | {actual_text} | {'PASS' if passed else 'FAIL'} |")

    lines = [
        "# DCL Reaction materialization-boundary analysis",
        "",
        "Generated by `tools/analyze_dcl_reaction_materialization.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Output: `{output}`",
        "",
        "## Byte anchors",
        "",
        "| Name | RVA | Result | Expected bytes | Meaning |",
        "| --- | ---: | --- | --- | --- |",
        *anchor_rows,
        "",
        "## Call graph",
        "",
        "| Callee | Expected direct callers | Actual direct callers | Result |",
        "| --- | --- | --- | --- |",
        *graph_rows,
        "",
        "## Proven ordering",
        "",
        "```text",
        "pass-2 selector 0x282E38",
        "  -> snapshot old unit+0x1A0 order (20 bytes)",
        "  -> generic carrier (including 443): self/native order -> 0x283003 -> 0x2831CC",
        "  -> special carrier 434/435/436/437/440/441/442:",
        "       native order -> path-specific validation -> final validation -> 0x2831BD",
        "       -> call 0x282234 -> 0x2831C5",
        "  -> failure: restore old order at 0x283160 and keep scanning",
        "  -> common 0x2831CC: publish exact Reaction id and selected unit index",
        "queue 0x2063B2 rejects -1",
        "  -> selector returns to post-selector boundary 0x2063BD",
        "  -> actor constructor call 0x2063CA",
        "  -> accepted Reaction commit 0x206421",
        "  -> state 0x2B VM execution",
        "```",
        "",
        f"RVA `0x{PRE_TARGET_BUILD_RVA:X}` is a **special-family** accepted boundary, not a common",
        "selector exit. At entry, `ebp` is the selected reactor index, `rbx` is the selected battle unit, and `rdi` is its materialized",
        f"order at `unit+0x{ORDER_OFFSET:X}`. The exact Reaction id is already published at RVA",
        f"`0x{0x186AFF0:X}` and the original attacker/source index remains at RVA `0x{SOURCE_INDEX_RVA:X}`.",
        "The immediately following call belongs only to the accepted special branch. Generic carriers",
        "finish their native order and jump from `0x283003` directly to common finalization at",
        "`0x2831CC`, skipping both the hook and helper. All accepted paths then return to `0x2063BD`;",
        "actor resolution follows at `0x2063CA`.",
        "",
        "## Order ownership",
        "",
        "| Order offset | Width | Meaning at this boundary | Evidence |",
        "| ---: | ---: | --- | --- |",
        "| `+0x00` | byte | reactor/caster unit index | selector accepted-return write |",
        "| `+0x01` | byte | executable action type | generic/type helper/Auto-Potion writes |",
        "| `+0x02` | word | executable payload/action id | typed helper; Counter writes `0`, Magick Counter copies incoming ability |",
        "| `+0x08` | word | selected item for item orders | Auto-Potion branch |",
        "| `+0x0A` | byte | target-mode value `5` for unit/tile delivery | all mapped accepted branches |",
        "| `+0x0B` | byte | target unit index associated with the coordinates | generic and typed helper writes |",
        "| `+0x0C/+0x0E/+0x10` | words | target x/layer/y coordinates | generic and typed helper coordinate copies |",
        "",
        "Counter and Bonecrusher both use the typed helper with action type `1`, payload `0`, and",
        "source-target validation, but they have separate call/result sites. Counter `442` shares",
        "result RVA `0x283019` with ids `435/436/437`; Bonecrusher `434` tests its result at",
        "`0x283148`. The helper copies the source index and source coordinates into the order. Generic",
        "carrier `443` instead keeps type `0`, payload `443`, and reactor coordinates, then bypasses",
        "`0x2831BD`. Therefore this hook can transform only the special carrier family. A synthetic",
        "owner that needs source-target basic retaliation must stage a distinct native delivery carrier",
        "such as Counter `442`, rather than assuming the blank owner id is also its delivery semantics.",
        "",
        "## Boundary classification",
        "",
        "- **Proven:** `0x2831BD` is accepted-only for special ids `434/435/436/437/440/441/442`,",
        "  post-materialization and immediately before their VM helper call at `0x2831C0`.",
        "- **Proven:** generic carrier `443` jumps from `0x283003` to common finalization `0x2831CC`,",
        "  so it never executes the hook or helper above.",
        "- **Proven:** `0x2063BD` is post-selector and pre-actor, but downstream of target-list build.",
        "- **Proven:** failed selector candidates restore the old order; accepted candidates skip restore.",
        "- **Proven:** Counter `442` uses typed-family result RVA `0x283019`; Bonecrusher `434`",
        "  uses the separate result RVA `0x283148`; both converge on final result RVA `0x28315C`.",
        "- **Proven:** executable action type/payload and target coordinates are complete at this",
        "  boundary for the special family, while the exact Reaction presentation id remains separate.",
        "- **Refuted live:** treating `0x2831BD` as a universal accepted boundary. A staged `443`",
        "  committed and executed without one materialization event at this hook.",
        "- **Strong:** owner identity and native delivery identity must be modeled separately when a",
        "  synthetic rule uses a blank/generic carrier whose native effect has different semantics.",
        "",
        "## Boundary window",
        "",
        *dispatch.disassembly(md, pe, raw, 0x2063A9, 0x32),
        "",
        "## Selector accept/restore window",
        "",
        *dispatch.disassembly(md, pe, raw, 0x282F31, 0x2A7),
        "",
        "## Typed-order helper window",
        "",
        *dispatch.disassembly(md, pe, raw, 0x283280, 0x109),
        "",
        f"Overall static gate: **{'PASS' if ok else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), ok


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-reaction-materialization-analysis.md"
    report, ok = render_report(args.exe, output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {output}")
    print("reaction materialization boundary PASS" if ok else "reaction materialization boundary FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
