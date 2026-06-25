#!/usr/bin/env python3
"""Disassemble the KO/Reraise lifecycle landmarks found by live probes."""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_GRP_CALL, X86_GRP_JUMP, X86_GRP_RET, X86_OP_IMM


REPO = Path(__file__).resolve().parents[1]
DEFAULT_EXE = Path(
    r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"
)
DEFAULT_OUTPUT = REPO / "work" / "ko_lifecycle_disassembly_analysis.md"


@dataclass(frozen=True)
class Landmark:
    name: str
    rva: int
    meaning: str
    expected: str
    base_register: str


@dataclass(frozen=True)
class Window:
    name: str
    rva: int
    size: int
    note: str


LANDMARKS = (
    Landmark("pre-death-status-test-61", 0x30A595, "tests target +0x61 bit 0x20 before +0x1BB write", "44 84 62 61", "rdx"),
    Landmark("death-state-write-1bb-early", 0x30A5C0, "writes r13b to target +0x1BB before HP apply", "44 88 AA BB 01 00 00", "rdx"),
    Landmark("hp-read-current-30", 0x30A673, "reads current HP from target +0x30 before clamp math", "0F B7 57 30", "rdi"),
    Landmark("hp-raw-sum-test", 0x30A68C, "tests raw HP sum before floor clamp", "85 C0", "rdi"),
    Landmark("hp-change-compare-old-new", 0x30A6B6, "compares previous HP against clamped HP", "41 3B D7", "rdi"),
    Landmark("hp-write-clamped-30", 0x30A6C3, "writes clamped HP to target +0x30", "66 44 89 7F 30", "rdi"),
    Landmark("ko-write-1f5", 0x30A6D3, "writes FF to target +0x1F5 after HP write", "C6 87 F5 01 00 00 FF", "rdi"),
    Landmark("reraise-death-state-write-1bb", 0x30AAFC, "writes 02 to target +0x1BB during revive/Reraise path", "C6 80 BB 01 00 00 02", "rax"),
    Landmark("late-ko-read-1ef", 0x30D42A, "reads target +0x1EF in later KO mask cleanup", "8A 8F EF 01 00 00", "rdi"),
    Landmark("late-ko-mask-write-1ef", 0x30D433, "writes masked target +0x1EF in later KO cleanup", "88 8F EF 01 00 00", "rdi"),
    Landmark("late-ko-write-61", 0x30D43C, "writes target +0x61 in later KO cleanup", "88 4F 61", "rdi"),
)

WINDOWS = (
    Window(
        "state-apply-entry-window",
        0x30A51C,
        0x90,
        "The state-apply entry that derives unit and unit-tail state-buffer pointers.",
    ),
    Window(
        "state-apply-hp-window",
        0x30A58E,
        0x162,
        "The HP/status state-apply path that consumes the unit-tail debit/credit fields.",
    ),
    Window(
        "reraise-tail-window",
        0x30AAEA,
        0x40,
        "The revive/Reraise tail that hit death_state_write_1bb after Ramza Wait.",
    ),
    Window(
        "state-apply-status-tail-window",
        0x30A6D0,
        0x560,
        "The post-HP state/status tail, including cleanup and Reraise state transitions.",
    ),
    Window(
        "late-ko-mask-window",
        0x30D3F0,
        0x95,
        "The later mask cleanup block discovered statically but not hit before Wait.",
    ),
    Window(
        "state-simulation-wrapper-window",
        0x30B584,
        0x280,
        "A stack-copy simulation wrapper that redirects the current unit/state globals.",
    ),
    Window(
        "target-cache-table-window",
        0x2D79E8,
        0x150,
        "A separate 0x248-stride table initializer/writer that resembles target-cache handling.",
    ),
    Window(
        "scheduler-indirect-call-window",
        0x2F372C,
        0x80,
        "The scheduler loop whose stack return address appears during the HP/KO boundary.",
    ),
    Window(
        "outer-dispatch-return-window",
        0x2F2E7C,
        0x60,
        "The outer dispatch wrapper whose return address appears in the live stack.",
    ),
)


def hex_bytes(data: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in data)


def load_disassembler() -> Cs:
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    return md


def mark_instruction(rva: int, text: str) -> str:
    if any(landmark.rva == rva for landmark in LANDMARKS):
        return "ANCHOR"
    lowered = text.lower()
    if any(token in lowered for token in ("0x30", "0x61", "0x1bb", "0x1be", "0x1c4", "0x1c6", "0x1ef", "0x1f5")):
        return "field"
    return ""


def rva_to_offset(pe: pefile.PE, rva: int) -> int:
    return pe.get_offset_from_rva(rva)


def disassemble_window(md: Cs, pe: pefile.PE, data: bytes, image_base: int, window: Window) -> list[str]:
    start = rva_to_offset(pe, window.rva)
    blob = data[start : start + window.size]
    rows = [
        f"### {window.name}",
        "",
        f"- RVA: `0x{window.rva:X}..0x{window.rva + window.size:X}`",
        f"- Note: {window.note}",
        "",
        "```text",
    ]
    for insn in md.disasm(blob, image_base + window.rva):
        rva = insn.address - image_base
        rendered = f"{insn.mnemonic} {insn.op_str}".strip()
        marker = mark_instruction(rva, rendered)
        suffix = f" ; {marker}" if marker else ""
        rows.append(f"{rva:08X}: {hex_bytes(insn.bytes):<24} {rendered}{suffix}")
    rows.extend(["```", ""])
    return rows


def find_direct_callers(md: Cs, pe: pefile.PE, data: bytes, image_base: int, target_rva: int) -> list[int]:
    callers: list[int] = []
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        blob = data[section.PointerToRawData : section.PointerToRawData + section.SizeOfRawData]
        for insn in md.disasm(blob, image_base + section.VirtualAddress):
            if not (insn.group(X86_GRP_CALL) or insn.group(X86_GRP_JUMP)):
                continue
            if not insn.operands or insn.operands[0].type != X86_OP_IMM:
                continue
            if insn.operands[0].imm - image_base == target_rva:
                callers.append(insn.address - image_base)
    return callers


def render_report(exe: Path, output: Path) -> str:
    pe = pefile.PE(str(exe), fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = load_disassembler()
    direct_callers = find_direct_callers(md, pe, data, image_base, 0x30A51C)

    lines: list[str] = [
        "# KO Lifecycle Disassembly Analysis",
        "",
        "Generated by `tools/analyze_ko_lifecycle_disasm.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- PE image base: `0x{image_base:X}`",
        f"- Output: `{output}`",
        "",
        "## Landmark Candidates",
        "",
        "| Name | RVA | Base | Expected bytes | Meaning |",
        "| --- | ---: | --- | --- | --- |",
    ]
    for landmark in LANDMARKS:
        lines.append(
            f"| `{landmark.name}` | `0x{landmark.rva:X}` | `{landmark.base_register}` | "
            f"`{landmark.expected}` | {landmark.meaning} |"
        )

    callers = ", ".join(f"`0x{rva:X}`" for rva in direct_callers) if direct_callers else "none found"
    lines.extend(
        [
            "",
            "## Control-Flow Read",
            "",
            f"- Direct calls/jumps to the state-apply routine start `0x30A51C`: {callers}.",
            "- The live stack instead points at the scheduler/dispatch family `0x2F3799`, `0x2F37A2`,",
            "  `0x2F3884`, and `0x2F2EC1`, which is consistent with an indirect function-pointer call.",
            "- The routine derives the live battle-unit pointer from the unit index, then sets the state",
            "  buffer pointer to the tail of the same unit struct: `rbp = unit + 0x1BE`.",
            "- The HP path reads old HP from `unit+0x30`, max HP from `unit+0x32`, and computes:",
            "  `newRawHp = oldHp + s16[unit+0x1C6] - s16[unit+0x1C4]`.",
            "- It then floors at zero, caps against max HP, and writes the clamped value to `unit+0x30`",
            "  at `0x30A6C3`.",
            "- The proven live landmark `0x30A6D3` happens after that HP write, so it is lifecycle evidence,",
            "  not the earliest damage/KO decision point.",
            "- Landmark registers are exact at hook time, but `[LANDMARK-HIT ... fields=...]` is read later",
            "  by the poller. Treat fields as near-event state, not guaranteed pre-instruction memory.",
            "- The status writes around `0x30A908`/`0x30A912` are cleanup/clear writes in the post-HP tail,",
            "  not the producer that arms KO before the HP arithmetic.",
            "",
            "## Disassembly Windows",
            "",
        ]
    )
    for window in WINDOWS:
        lines.extend(disassemble_window(md, pe, data, image_base, window))

    lines.extend(
        [
            "## Live Probe Read",
            "",
            "The 2026-06-23 `ko-hp-apply-probe` live run answered the old next-probe question:",
            "",
            "- Preview produced no landmark hits; these landmarks are resolution/apply-only.",
            "- Lethal Rush on the Ninja hit the HP apply path before Ramza Wait and then the Reraise tail after Wait.",
            "- At `0x30A68C`, exact registers showed old HP `rdx=0xF`, raw signed HP result",
            "  `rax=0xFFFFFFFF`, and pre-clamp max HP `r15=0x120`.",
            "- At `0x30A6B6`, exact registers showed old HP `rdx=0xF` and clamped new HP `r15=0`.",
            "- At `0x30A6C3`, the routine wrote clamped HP zero to `unit+0x30`.",
            "- After Ramza Wait, Reraise hit `0x30AAFC` with the revived Ninja at HP `28`.",
            "- The `fields=` snapshots around early landmarks showed KO-like fields, but those fields are",
            "  poller reads after the hook event. The exact pre-instruction proof comes from registers.",
            "",
            "Conclusion:",
            "",
            "- The HP apply routine is downstream of vanilla damage staging, but it is the first proven",
            "  engine-owned clamp/write site for lethal HP application.",
            "- A late write to `unit+0x30` is still too late for a robust custom lethal design.",
            "- A better custom-lethal proof is to alter the staged debit/credit before the clamp math, e.g.",
            "  feed `unit+0x1C4`/`unit+0x1C6` before `0x30A68C`, then let the vanilla tail perform HP zero,",
            "  KO lifecycle, and Reraise handling.",
            "",
            "## Current Next Experiment",
            "",
            "- Experiment: `ko-preclamp-force-agrias`.",
            "- Question: can custom lethal damage feed the engine through staged damage (`unit+0x1C4`) before",
            "  vanilla HP clamp, instead of writing final HP late at `unit+0x30`?",
            "- Setup: Cloud-active baseline, Cross Slash preview on Agrias for `187` damage.",
            "- Mechanism: hook `0x30A66F`, guard on Agrias (`id=0x1E`) and staged debit `187`, then force",
            "  `unit+0x1C4=9999` once before vanilla executes `movsx eax, word ptr [rbp+6]`.",
            "- Pass signal: Agrias becomes a true KO through the vanilla downstream state path, with coherent",
            "  HP clamp, KO lifecycle, displayed damage behavior, and turn flow.",
            "- Why now: the final combat redesign needs engine-safe custom lethal application. This test checks",
            "  the most promising engine-owned insertion point we have discovered, while bypassing the harder",
            "  producer search for one controlled proof.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    if not args.exe.exists():
        raise SystemExit(f"executable not found: {args.exe}")
    report = render_report(args.exe, args.output)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
