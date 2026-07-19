#!/usr/bin/env python3
"""Verify the independent order-payload and native active-weapon carriers used by Attack."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_AC_WRITE, CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86_const import X86_OP_MEM, X86_REG_RIP

import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("order-copy-size", 0x3099CF, "B8 14 00 00 00", "copy the complete 0x14-byte order record"),
    Anchor("payload-load", 0x309AF1, "0F B7 74 24 28", "load local order-record word +8 into SI"),
    Anchor("payload-stage", 0x309E52, "66 89 35 21 69 4A 00", "stage that order payload for the Attack formula path"),
    Anchor("weapon-resolver", 0x309E5F, "E8 9C EF FA FF", "resolve the formula row addressed by the order payload"),
    Anchor("repeat-weapons-init", 0xEED0C8D,
           "0F B7 43 20 66 89 05 CC FA 8D F1 0F B7 4B 24 66 89 0D C3 FA 8D F1",
           "copy unit right/left weapon ids into the native repeat globals"),
    Anchor("repeat-primary-normalize", 0xEED0CAE,
           "0F B7 43 24 89 E9 66 89 05 A9 FA 8D F1 66 89 0D A4 FA 8D F1",
           "promote a lone left weapon to the normalized primary slot"),
    Anchor("dual-wield-count", 0xEED0E97,
           "41 F6 C0 01 0F B6 C2 41 0F 45 C7 88 05 BA F8 8D F1",
           "select and store the native weapon repeat count"),
    Anchor("active-weapon-selector", 0x309AB5,
           "80 3D A6 6C 4A 00 02 88 05 AD 6C 4A 00 76 0A 44 0F B7 0D 98 6C 4A 00 EB 18 44 38 3D 8E 6C 4A 00 44 0F B7 0D 89 6C 4A 00 66 44 0F 44 0D 7E 6C 4A 00",
           "select normalized right/left weapon from repeat count and index"),
)


def parse_hex(value: str) -> bytes:
    return bytes.fromhex(value)


def hex_bytes(value: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in value)


def rip_references(
    md: Cs,
    pe: pefile.PE,
    raw: bytes,
    start_rva: int,
    size: int,
    target_rva: int,
) -> list[tuple[int, bool, str]]:
    image_base = int(pe.OPTIONAL_HEADER.ImageBase)
    code = dispatch.rva_bytes(pe, raw, start_rva, size)
    references: list[tuple[int, bool, str]] = []
    for instruction in md.disasm(code, image_base + start_rva):
        for operand in instruction.operands:
            if operand.type != X86_OP_MEM or operand.mem.base != X86_REG_RIP:
                continue
            target = instruction.address + instruction.size + operand.mem.disp
            if target != image_base + target_rva:
                continue
            references.append((
                instruction.address - image_base,
                bool(operand.access & CS_AC_WRITE),
                f"{instruction.mnemonic} {instruction.op_str}",
            ))
    return references


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = parse_hex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        ok = actual == expected
        anchors_ok &= ok
        result = "PASS" if ok else f"FAIL (`{hex_bytes(actual)}`)"
        rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | `{anchor.expected}` | {anchor.meaning} |"
        )

    codemod = ROOT / "codemod" / "fftivc.generic.chronicle.codemod"
    mod_source = (codemod / "Mod.cs").read_text(encoding="utf-8")
    context_source = (codemod / "FormulaRuntimeContextBuilder.cs").read_text(encoding="utf-8")
    action_cache_source = (codemod / "DclActionContextCache.cs").read_text(encoding="utf-8")
    repeat_source = (codemod / "DclNativeRepeat.cs").read_text(encoding="utf-8")
    entry_index_refs = rip_references(md, pe, raw, 0x3099AC, 0x109, 0x7B0763)
    preselector_helper_refs = rip_references(md, pe, raw, 0x3095E0, 0x2E6, 0x7B0763)
    producer_index_refs = rip_references(md, pe, raw, 0x281CE8, 0x548, 0x7B0763)
    producer_index_writes = [rva for rva, write, _ in producer_index_refs if write]
    integration_checks = {
        "native global RVAs are explicit": all(
            token in repeat_source for token in (
                "RepeatCountRva = 0x7B0762",
                "RepeatIndexRva = 0x7B0763",
                "RightWeaponRva = 0x7B0764",
                "LeftWeaponRva = 0x7B0766",
            )
        ),
        "managed selector mirrors the native branch": all(
            token in repeat_source for token in (
                "SelectActiveWeaponItemId",
                "repeatCount > 2 || repeatIndex == 0",
            )
        ),
        "repeat index is stable from calc entry through the active-weapon selector": (
            not entry_index_refs and not preselector_helper_refs
        ),
        "outer producer advances repeat index only after the calculation sweep": (
            producer_index_writes == [0x282113, 0x2821FA]
        ),
        "calc ring captures payload and all four repeat fields synchronously": all(
            token in mod_source for token in (
                '"mov dword [rsi+36], ebx"',
                '"mov dword [rsi+48], ebx"',
                '"mov dword [rsi+52], ebx"',
                '"mov dword [rsi+56], ebx"',
                '"mov dword [rsi+60], ebx"',
            )
        ),
        "action cache persists native hand identity": all(
            token in action_cache_source for token in (
                "ActiveWeaponItemId",
                "NativeRepeatCount",
                "NativeRepeatIndex",
                "NativeRightWeaponItemId",
                "NativeLeftWeaponItemId",
            )
        ),
        "formula context prefers native active weapon and publishes provenance": all(
            token in context_source for token in (
                "nativeWeaponKnown ? activeWeaponItemId : actionPayload",
                'context.Set("action.weaponNativeKnown"',
                'context.Set("action.weaponRepeatIndex"',
                'context.Set("action.weaponSide"',
            )
        ),
    }
    integration_ok = all(integration_checks.values())

    lines = [
        "# DCL order-payload and active-weapon routing analysis",
        "",
        "Generated by `tools/analyze_dcl_active_weapon_payload.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Output: `{output}`",
        "",
        "## Native byte anchors",
        "",
        "| Name | RVA | Result | Expected bytes | Meaning |",
        "| --- | ---: | --- | --- | --- |",
        *rows,
        "",
        "## Managed integration",
        "",
        *[f"- **{'PASS' if ok else 'FAIL'}:** {name}." for name, ok in integration_checks.items()],
        "",
        "## Repeat-index timing",
        "",
        f"- Direct references to `0x7B0763` from calc entry through selector: `{entry_index_refs or 'none'}`.",
        f"- Direct references to `0x7B0763` in the only continuing pre-selector helper: `{preselector_helper_refs or 'none'}`.",
        "- Direct writes to `0x7B0763` in the outer result producer: " +
        ", ".join(f"`0x{rva:X}`" for rva in producer_index_writes) + ".",
        "",
        "## Relevant native flow",
        "",
        "### Order payload and native formula dispatch",
        "",
        *dispatch.disassembly(md, pe, raw, 0x3099CF, 0x130),
        "",
        "### Protected repeat initializer",
        "",
        *dispatch.disassembly(md, pe, raw, 0xEED0C8D, 0x22B),
        "",
        "### Active-weapon selector",
        "",
        *dispatch.disassembly(md, pe, raw, 0x309AB5, 0x38),
        "",
        "## Static interpretation",
        "",
        "- **Proven:** `orderRecord+8` is an order payload consumed by the Attack formula path, but",
        "  it is not the native per-repeat hand selector. It can remain unchanged across two strikes.",
        "- **Proven:** the protected repeat initializer copies and normalizes `unit+0x20/+0x24` into",
        "  globals `0x7B0764/0x7B0766`, stores repeat count/index at `0x7B0762/0x7B0763`, and the",
        "  calculation path chooses the active weapon independently from those four values.",
        "- **Proven:** for action type `1`, repeat index zero selects normalized right/primary; index",
        "  one selects left/off-hand when count is two; sequences longer than two use the primary.",
        "- **Proven offline:** the repeat index has no direct reference between calc entry and the",
        "  active-weapon selector, including the only helper that can return to that selector. The",
        "  outer producer advances it only after a completed calculation sweep. The calc-entry value",
        "  is therefore the same value the native selector consumes on that invocation.",
        "- **Proven offline:** the calc-entry ring captures payload and native repeat/weapon state in",
        "  one synchronous hook, the action cache retains both identities, and formula contexts route",
        "  `action.weapon.*` through the native active item while preserving `action.payload*`.",
        "- **Live interpretation rule:** a `0x2F` battle-state label is not itself an off-hand id.",
        "  `tools/analyze_dcl_active_weapon_live.py` requires a completed ordinary-owner pair with",
        "  positive native debits on both transactions and checks the captured index/item directly.",
        "",
    ]
    return "\n".join(lines), anchors_ok and integration_ok


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-active-weapon-routing-analysis.md"
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all active-weapon routing checks PASS" if ok else "one or more active-weapon routing checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
