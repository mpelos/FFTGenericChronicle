#!/usr/bin/env python3
"""Prove the accepted pass-2 Reaction order boundary before actor construction."""
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
ACCEPTED_BOUNDARY_RVA = 0x2063BD
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
    Anchor("accepted-boundary", ACCEPTED_BOUNDARY_RVA, "0F B7 C8 C7 05 02 4E A6 00 29 00 00 00", "first accepted-only instruction; eax is the selected unit index"),
    Anchor("actor-create-call", ACTOR_CREATE_CALL_RVA, "E8 ED A6 05 00", "creates the actor only after the accepted boundary"),
    Anchor("selector-unit-table", 0x282E80, "48 8D 0D 59 0E 5D 01", "loads battle-unit table RVA 0x1853CE0"),
    Anchor("selector-order-record", 0x282EA2, "48 8D BB A0 01 00 00", "sets rdi to selected unit+0x1A0"),
    Anchor("snapshot-old-order", 0x282F03, "41 8B D1 48 8D 0D 7B 80 5E 01 4C 8B C7 E8 F7 33 DA FF", "copies the old 20-byte order to scratch before materialization"),
    Anchor("stage-and-consume-id", 0x282F17, "40 88 2F 44 88 7F 01 66 89 77 02 66 89 35 C7 80 5E 01 66 44 89 BB CE 01 00 00", "stages candidate/id, exports exact id, then clears unit+0x1CE"),
    Anchor("generic-order-target", 0x282FBA, "44 88 7F 01 C6 47 0A 05 40 88 6F 0B", "generic carrier uses type 0 and initially targets the reactor"),
    Anchor("auto-potion-order", 0x283068, "66 89 47 08 48 8B CF C6 47 01 06 C6 47 0A 05 40 88 6F 0B", "Auto-Potion writes selected item, type 6, and target metadata"),
    Anchor("counter-typed-order", 0x283137, "45 33 C0 45 8B CC 41 8B D4 48 8B CB E8 38 01 00 00", "Counter/Bonecrusher requests type 1, payload 0, source validation"),
    Anchor("special-accept-or-restore", 0x28315C, "85 C0 74 5D BA 14 00 00 00", "successful final validation skips the restore path"),
    Anchor("failure-restore", 0x283160, "BA 14 00 00 00 4C 8D 05 1C 7E 5E 01 44 8B CA 48 8B CF E8 95 31 DA FF", "failure restores the 20-byte pre-selector order from scratch"),
    Anchor("special-accepted-return", 0x2831BD, "48 8B CB E8 6F F0 FF FF 0F B7 35 24 7E 5E 01 66 41 89 36 8B C5 40 88 2F", "accepted special path returns selected index/id without restoring"),
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
        "  -> materialize carrier-specific type/payload/item/target coordinates",
        "  -> failure: restore old order at 0x283160 and keep scanning",
        "  -> success: return selected unit index + exact Reaction id without restore",
        "queue 0x2063B2 rejects -1",
        "  -> accepted-only boundary 0x2063BD",
        "  -> actor constructor call 0x2063CA",
        "  -> accepted Reaction commit 0x206421",
        "  -> state 0x2B VM execution",
        "```",
        "",
        f"The accepted-only boundary is RVA `0x{ACCEPTED_BOUNDARY_RVA:X}`. At entry, `eax` is the",
        "selected reactor index and `word[rbp-0x2E]` is the exact Reaction id. The selected unit is",
        f"`module+0x{UNIT_TABLE_RVA:X} + eax*0x200`; its materialized order is at `+0x{ORDER_OFFSET:X}`.",
        f"The original attacker/source index remains in dword RVA `0x{SOURCE_INDEX_RVA:X}`.",
        "No actor exists yet: the constructor is the next call, at `0x2063CA`.",
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
        "Counter/Bonecrusher use the typed helper with action type `1`, payload `0`, and source-target",
        "validation. The helper copies the source index and source coordinates into the order. Generic",
        "carrier `443` instead keeps type `0`, payload `443`, and reactor coordinates. Therefore a",
        "source-retarget at this boundary must update the order's unit index **and** x/layer/y fields;",
        "rewriting the later actor target list alone is neither necessary nor sufficient.",
        "",
        "## Boundary classification",
        "",
        "- **Proven:** `0x2063BD` is accepted-only, post-carrier-materialization, and pre-actor/VM.",
        "- **Proven:** failed selector candidates restore the old order; accepted candidates skip restore.",
        "- **Proven:** executable action type/payload and target coordinates are authoritative inputs at",
        "  this boundary, while the exact Reaction presentation id remains separately available.",
        "- **Strong:** changing the complete order before the actor constructor is the narrow mechanism",
        "  for action replacement or source retarget without the overwrite seen at commit `0x206421`.",
        "- A guarded observe-only live probe should correlate these order fields with the already-proven",
        "  pass-2 commit and state-`0x2C` effect rows before any write mode is enabled.",
        "",
        "## Boundary window",
        "",
        *dispatch.disassembly(md, pe, raw, 0x2063A9, 0x32),
        "",
        "## Selector accept/restore window",
        "",
        *dispatch.disassembly(md, pe, raw, 0x283137, 0xA1),
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
