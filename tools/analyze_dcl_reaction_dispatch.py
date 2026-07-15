#!/usr/bin/env python3
"""Map current-build Reaction ids, real-code gates, and the reaction-eval global."""
from __future__ import annotations

import argparse
import csv
import hashlib
import time
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_IMM, X86_OP_MEM, X86_REG_RIP


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = Path(
    r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"
)
DEFAULT_CATALOG = ROOT / "work" / "wotl_ability_action_baseline.csv"
REAL_CODE_MAX_RVA = 0x610000
REACTION_MIN = 422
REACTION_MAX = 453
REACTION_EVAL_GLOBAL_RVA = 0x186AFF0


@dataclass(frozen=True)
class Gate:
    name: str
    head_rva: int
    roll_rva: int
    id_register: str


GATES = (
    Gate("R1", 0x30BDBC, 0x30BDEE, "r11d"),
    Gate("R2", 0x30BE14, 0x30BE44, "ebx"),
    Gate("R3", 0x30BE64, 0x30BE9A, "ebx"),
    Gate("R4", 0x30BEB0, 0x30BEDA, "r11d"),
)


def hex_bytes(value: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in value)


def executable_sections(pe: pefile.PE):
    for section in pe.sections:
        if section.Characteristics & 0x20000000:
            yield section


def load_catalog(path: Path) -> dict[int, str]:
    rows: dict[int, str] = {}
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        for row in csv.DictReader(handle):
            ability_id = int(row["id_dec"])
            if REACTION_MIN <= ability_id <= REACTION_MAX:
                rows[ability_id] = row.get("name_ivc") or row.get("name_wotl") or f"Reaction {ability_id}"
    return rows


def scan_aligned_instructions(pe: pefile.PE, raw: bytes, md: Cs):
    """Decode padded real-code runs and yield each aligned instruction once."""
    for section in executable_sections(pe):
        section_rva = section.VirtualAddress
        section_raw = section.PointerToRawData
        blob = raw[section_raw : section_raw + section.SizeOfRawData]
        limit = min(len(blob), max(0, REAL_CODE_MAX_RVA - section_rva))
        cursor = 0
        while cursor < limit:
            while cursor < limit and blob[cursor] == 0xCC:
                cursor += 1
            if cursor >= limit:
                break
            end = blob.find(b"\xCC\xCC", cursor, limit)
            if end < 0:
                end = limit
            code = blob[cursor:end]
            start = pe.OPTIONAL_HEADER.ImageBase + section_rva + cursor
            for instruction in md.disasm(code, start):
                yield instruction
            cursor = max(end + 2, cursor + 1)


def direct_callers(pe: pefile.PE, raw: bytes, target_rva: int) -> list[int]:
    result: list[int] = []
    for section in executable_sections(pe):
        blob = raw[section.PointerToRawData : section.PointerToRawData + section.SizeOfRawData]
        for index in range(max(0, 0x1000 - section.VirtualAddress), len(blob) - 4):
            caller = section.VirtualAddress + index
            if caller >= REAL_CODE_MAX_RVA:
                break
            if blob[index] != 0xE8:
                continue
            relative = int.from_bytes(blob[index + 1 : index + 5], "little", signed=True)
            if caller + 5 + relative == target_rva:
                result.append(caller)
    return result


def rva_bytes(pe: pefile.PE, raw: bytes, rva: int, size: int) -> bytes:
    offset = pe.get_offset_from_rva(rva)
    return raw[offset : offset + size]


def disassembly(md: Cs, pe: pefile.PE, raw: bytes, rva: int, size: int) -> list[str]:
    result = ["```text"]
    for instruction in md.disasm(rva_bytes(pe, raw, rva, size), pe.OPTIONAL_HEADER.ImageBase + rva):
        here = instruction.address - pe.OPTIONAL_HEADER.ImageBase
        rendered = f"{instruction.mnemonic} {instruction.op_str}".strip()
        result.append(f"{here:08X}: {hex_bytes(instruction.bytes):<28} {rendered}")
    result.append("```")
    return result


def render_report(exe: Path, catalog_path: Path, output: Path) -> str:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    catalog = load_catalog(catalog_path)

    eval_xrefs: list[tuple[int, str, str]] = []
    id_uses: dict[int, list[tuple[int, str]]] = defaultdict(list)
    for instruction in scan_aligned_instructions(pe, raw, md):
        rva = instruction.address - image_base
        for operand_index, operand in enumerate(instruction.operands):
            if operand.type == X86_OP_MEM and operand.mem.base == X86_REG_RIP:
                target_rva = instruction.address + instruction.size + operand.mem.disp - image_base
                if target_rva == REACTION_EVAL_GLOBAL_RVA:
                    access = "write" if instruction.mnemonic.startswith("mov") and operand_index == 0 else "read"
                    eval_xrefs.append((rva, access, f"{instruction.mnemonic} {instruction.op_str}"))
            elif operand.type == X86_OP_IMM and REACTION_MIN <= operand.imm <= REACTION_MAX:
                id_uses[operand.imm].append((rva, f"{instruction.mnemonic} {instruction.op_str}"))

    lines = [
        "# DCL Reaction Dispatch Analysis",
        "",
        "Generated by `tools/analyze_dcl_reaction_dispatch.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Catalog: `{catalog_path}`",
        f"- Output: `{output}`",
        "",
        "## Reaction catalog and aligned real-code id uses",
        "",
        "| Id | Name | Aligned immediate uses |",
        "| ---: | --- | ---: |",
    ]
    for ability_id in range(REACTION_MIN, REACTION_MAX + 1):
        lines.append(f"| `{ability_id}` | {catalog.get(ability_id) or '(unnamed)'} | {len(id_uses[ability_id])} |")

    lines.extend([
        "",
        "## Reaction-evaluation global xrefs",
        "",
        f"Target: `word[0x{image_base + REACTION_EVAL_GLOBAL_RVA:X}]` (RVA `0x{REACTION_EVAL_GLOBAL_RVA:X}`).",
        "",
        "| RVA | Access | Instruction |",
        "| ---: | --- | --- |",
    ])
    for rva, access, rendered in sorted(set(eval_xrefs)):
        lines.append(f"| `0x{rva:X}` | {access} | `{rendered}` |")
    if not eval_xrefs:
        lines.append("| — | none in aligned real-code runs | VM-owned/indirect access only |")

    lines.extend([
        "",
        "## Four real-code Brave gates",
        "",
        "| Gate | Function | Roll call | Exact-id register | Direct callers |",
        "| --- | ---: | ---: | --- | --- |",
    ])
    caller_map: dict[str, list[int]] = {}
    for gate in GATES:
        callers = direct_callers(pe, raw, gate.head_rva)
        caller_map[gate.name] = callers
        caller_text = ", ".join(f"`0x{rva:X}`" for rva in callers) or "none"
        lines.append(
            f"| {gate.name} | `0x{gate.head_rva:X}` | `0x{gate.roll_rva:X}` | `{gate.id_register}` | {caller_text} |"
        )

    lines.extend([
        "",
        "## Per-id aligned immediate-use index",
        "",
    ])
    for ability_id in range(REACTION_MIN, REACTION_MAX + 1):
        uses = sorted(set(id_uses[ability_id]))
        lines.extend([f"### `{ability_id}` — {catalog.get(ability_id) or '(unnamed)'}", ""])
        if uses:
            for rva, rendered in uses:
                lines.append(f"- `0x{rva:X}` — `{rendered}`")
        else:
            lines.append("- No aligned real-code immediate use; resolution is table/bitfield/VM-owned.")
        lines.append("")

    lines.extend([
        "## Gate caller windows",
        "",
    ])
    for gate in GATES:
        for caller in caller_map[gate.name]:
            start = max(0x1000, caller - 0x30)
            lines.extend([f"### {gate.name} caller `0x{caller:X}`", ""])
            lines.extend(disassembly(md, pe, raw, start, 0x50))
            lines.append("")

    lines.extend([
        "## Static boundary",
        "",
        "Aligned real-code uses can prove hard-coded dispatch and exact-id registers. Absence here does",
        "not prove that an ability is unused: table lookups, bitfields, and Denuvo/VM code do not need an",
        "immediate id in a real-code instruction. A VM-owned/indirect row remains an explicit live or",
        "data-mapping gate.",
        "",
    ])
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--output", type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-reaction-dispatch-analysis.md"
    report = render_report(args.exe, args.catalog, output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(report, encoding="utf-8")
    print(f"wrote {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
