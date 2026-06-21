#!/usr/bin/env python3
"""Find static RIP-relative table-reference candidates in FFT_enhanced.exe.

This is an offline research aid for RuntimeSettings.MemoryTableProbes. It does not know which
candidate is correct; it only finds normal x64 RIP-relative LEA/MOV instructions that point into
the same PE image and scores the ones that look table-like.
"""
from __future__ import annotations

import argparse
import csv
import struct
from dataclasses import dataclass, replace
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_EXE = Path(
    r"D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe"
)
DEFAULT_OUT = ROOT / "work" / "memtable_rip_candidates.csv"

IMAGE_SCN_CNT_CODE = 0x00000020
IMAGE_SCN_MEM_EXECUTE = 0x20000000
IMAGE_SCN_MEM_READ = 0x40000000
CODE_SECTION_NAMES = {".text", ".xcode"}
DATA_SECTION_NAMES = {".data", ".rdata", ".rodata", ".bss", ".sbss", ".udata", ".impdata", ".sdata", ".tls$"}
LOW_VALUE_SECTION_PREFIXES = (".debug",)
LOW_VALUE_SECTION_NAMES = {".edata", ".idata", ".pdata", ".rsrc", ".reloc"}


@dataclass(frozen=True)
class Section:
    name: str
    virtual_address: int
    virtual_size: int
    raw_pointer: int
    raw_size: int
    characteristics: int

    @property
    def span(self) -> int:
        return max(self.virtual_size, self.raw_size)

    @property
    def is_executable(self) -> bool:
        return bool(self.characteristics & (IMAGE_SCN_CNT_CODE | IMAGE_SCN_MEM_EXECUTE))

    @property
    def is_readable(self) -> bool:
        return bool(self.characteristics & IMAGE_SCN_MEM_READ)


@dataclass(frozen=True)
class Candidate:
    score: int
    file_offset: int
    instr_rva: int
    kind: str
    length: int
    target_rva: int
    disp: int
    source_section: str
    target_section: str
    pattern: str
    context_pattern: str
    context_matches: int
    context_rip_relative_offset: int
    context_instruction_length: int
    reasons: tuple[str, ...]


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Find RIP-relative table-reference candidates.")
    p.add_argument("exe", nargs="?", type=Path, default=DEFAULT_EXE)
    p.add_argument("-o", "--output", type=Path, default=DEFAULT_OUT)
    p.add_argument("--nearby-window", type=int, default=128)
    p.add_argument("--stride", type=int, default=0x258, help="Stride immediate to score, default 0x258.")
    p.add_argument("--min-score", type=int, default=5)
    p.add_argument("--limit", type=int, default=200)
    p.add_argument("--context-before", type=int, default=8, help="Bytes before the instruction in the contextual AOB.")
    p.add_argument("--context-after", type=int, default=8, help="Bytes after the instruction in the contextual AOB.")
    p.add_argument("--skip-context", action="store_true", help="Do not build contextual AOBs or count matches.")
    p.add_argument(
        "--include-all-executable",
        action="store_true",
        help="Scan every executable section. By default only normal code sections such as .xcode/.text are scanned.",
    )
    p.add_argument(
        "--include-low-value-executable",
        action="store_true",
        help="Also scan low-value executable sections such as .edata/.debug. Usually noisy and slow.",
    )
    p.add_argument("--source-section", action="append", help="Only include candidates from this section name. Repeatable.")
    p.add_argument("--target-section", action="append", help="Only include candidates pointing to this section name. Repeatable.")
    p.add_argument("--all", action="store_true", help="Ignore --min-score and write all candidates.")
    return p.parse_args()


def main() -> int:
    args = parse_args()
    if not args.exe.exists():
        raise SystemExit(f"exe not found: {args.exe}")

    data = args.exe.read_bytes()
    sections = parse_pe_sections(data)
    source_sections = source_scan_sections(args.source_section, args.include_all_executable)
    include_low_value_sources = args.include_low_value_executable or bool(args.source_section)
    candidates = find_candidates(
        data,
        sections,
        max(8, args.nearby_window),
        args.stride,
        source_sections,
        include_low_value_sources,
    )
    candidates = filter_sections(candidates, args.source_section, args.target_section)
    if not args.all:
        candidates = [candidate for candidate in candidates if candidate.score >= args.min_score]
    candidates = sorted(candidates, key=lambda candidate: (-candidate.score, candidate.instr_rva))
    if args.limit > 0:
        candidates = candidates[: args.limit]
    if not args.skip_context:
        candidates = annotate_context_patterns(
            data,
            sections,
            candidates,
            max(0, args.context_before),
            max(0, args.context_after),
            source_sections,
        )
        candidates = sorted(candidates, key=lambda candidate: (-candidate.score, context_match_sort_key(candidate), candidate.instr_rva))

    args.output.parent.mkdir(exist_ok=True)
    write_candidates(args.output, candidates)
    print(f"wrote {args.output} ({len(candidates)} candidate(s))")
    return 0


def context_match_sort_key(candidate: Candidate) -> int:
    if not candidate.context_pattern:
        return 999_999_999
    if candidate.context_matches <= 0:
        return 999_999_998
    return candidate.context_matches


def parse_pe_sections(data: bytes) -> list[Section]:
    if len(data) < 0x40 or data[:2] != b"MZ":
        raise ValueError("not an MZ executable")

    pe_offset = read_u32(data, 0x3C)
    if data[pe_offset : pe_offset + 4] != b"PE\0\0":
        raise ValueError("PE signature not found")

    coff = pe_offset + 4
    section_count = read_u16(data, coff + 2)
    optional_size = read_u16(data, coff + 16)
    section_table = coff + 20 + optional_size

    sections: list[Section] = []
    for index in range(section_count):
        offset = section_table + index * 40
        raw_name = data[offset : offset + 8].split(b"\0", 1)[0]
        name = raw_name.decode("ascii", errors="replace")
        virtual_size = read_u32(data, offset + 8)
        virtual_address = read_u32(data, offset + 12)
        raw_size = read_u32(data, offset + 16)
        raw_pointer = read_u32(data, offset + 20)
        characteristics = read_u32(data, offset + 36)
        sections.append(Section(name, virtual_address, virtual_size, raw_pointer, raw_size, characteristics))

    return sections


def source_scan_sections(source_sections: list[str] | None, include_all_executable: bool) -> set[str] | None:
    if source_sections:
        return {section.lower() for section in source_sections}
    if include_all_executable:
        return None
    return {section.lower() for section in CODE_SECTION_NAMES}


def find_candidates(
    data: bytes,
    sections: list[Section],
    nearby_window: int,
    stride: int,
    source_scan_names: set[str] | None = None,
    include_low_value_sources: bool = False,
) -> list[Candidate]:
    candidates: list[Candidate] = []
    for source in sections:
        if not source.is_executable or source.raw_size == 0:
            continue
        if not include_low_value_sources and is_low_value_section(source.name):
            continue
        if source_scan_names is not None and source.name.lower() not in source_scan_names:
            continue
        start = source.raw_pointer
        end = min(len(data), source.raw_pointer + source.raw_size)
        offset = start
        while offset < end - 7:
            decoded = decode_rip_relative(data, offset)
            if decoded is None:
                offset += 1
                continue

            kind, length, disp = decoded
            instr_rva = source.virtual_address + (offset - source.raw_pointer)
            target_rva = instr_rva + length + disp
            target = section_by_rva(sections, target_rva)
            if target is None:
                offset += 1
                continue

            score, reasons = score_candidate(data, offset, nearby_window, stride, source, target, kind, target_rva)
            candidates.append(
                Candidate(
                    score,
                    offset,
                    instr_rva,
                    kind,
                    length,
                    target_rva,
                    disp,
                    source.name,
                    target.name,
                    format_pattern(data, offset, length),
                    "",
                    0,
                    0,
                    0,
                    tuple(reasons),
                )
            )
            offset += length

    return candidates


def filter_sections(
    candidates: list[Candidate],
    source_sections: list[str] | None,
    target_sections: list[str] | None,
) -> list[Candidate]:
    if source_sections:
        wanted_sources = {section.lower() for section in source_sections}
        candidates = [candidate for candidate in candidates if candidate.source_section.lower() in wanted_sources]
    if target_sections:
        wanted_targets = {section.lower() for section in target_sections}
        candidates = [candidate for candidate in candidates if candidate.target_section.lower() in wanted_targets]
    return candidates


def decode_rip_relative(data: bytes, offset: int) -> tuple[str, int, int] | None:
    if offset + 7 <= len(data) and data[offset] in (0x48, 0x4C):
        op = data[offset + 1]
        modrm = data[offset + 2]
        if op in (0x8D, 0x8B) and (modrm & 0xC7) == 0x05:
            return ("lea" if op == 0x8D else "mov", 7, read_i32(data, offset + 3))

    if offset + 6 <= len(data):
        op = data[offset]
        modrm = data[offset + 1]
        if op in (0x8D, 0x8B) and (modrm & 0xC7) == 0x05:
            return ("lea32" if op == 0x8D else "mov32", 6, read_i32(data, offset + 2))

    return None


def score_candidate(
    data: bytes,
    offset: int,
    nearby_window: int,
    stride: int,
    source: Section,
    target: Section,
    kind: str,
    target_rva: int,
) -> tuple[int, list[str]]:
    score = 0
    reasons: list[str] = []
    nearby = data[max(0, offset - nearby_window) : min(len(data), offset + nearby_window)]

    if source.name in CODE_SECTION_NAMES:
        score += 2
        reasons.append(f"source-{source.name}")
    elif source.is_executable:
        score += 1
        reasons.append("source-exec")

    if target.name in DATA_SECTION_NAMES:
        score += 2
        reasons.append(f"target-{target.name}")
    elif target.is_readable:
        score += 1
        reasons.append(f"target-readable-{target.name}")

    if is_low_value_section(source.name):
        score -= 3
        reasons.append(f"low-source-{source.name}")
    if is_low_value_section(target.name):
        score -= 3
        reasons.append(f"low-target-{target.name}")

    if kind.startswith("lea"):
        score += 1
        reasons.append(kind)
    if target_rva % 8 == 0:
        score += 1
        reasons.append("target-aligned")

    stride_bytes = struct.pack("<I", stride & 0xFFFFFFFF)
    if stride_bytes in nearby:
        score += 5
        reasons.append(f"near-stride-0x{stride:X}")

    count_bytes = struct.pack("<I", 55)
    if count_bytes in nearby:
        score += 1
        reasons.append("near-count-55")

    return score, reasons


def is_low_value_section(name: str) -> bool:
    return name in LOW_VALUE_SECTION_NAMES or any(name.startswith(prefix) for prefix in LOW_VALUE_SECTION_PREFIXES)


def annotate_context_patterns(
    data: bytes,
    sections: list[Section],
    candidates: list[Candidate],
    context_before: int,
    context_after: int,
    source_scan_names: set[str] | None,
) -> list[Candidate]:
    search_sections = pattern_search_sections(sections, source_scan_names)
    annotated: list[Candidate] = []
    for candidate in candidates:
        pattern = build_context_pattern(data, candidate.file_offset, candidate.length, context_before, context_after)
        prefix_bytes = context_prefix_bytes(candidate.file_offset, context_before)
        annotated.append(
            replace(
                candidate,
                context_pattern=format_aob_pattern(pattern),
                context_matches=count_pattern_matches(data, search_sections, pattern),
                context_rip_relative_offset=prefix_bytes + rip_relative_disp_offset(candidate.length),
                context_instruction_length=prefix_bytes + candidate.length,
            )
        )
    return annotated


def context_prefix_bytes(instr_offset: int, context_before: int) -> int:
    return instr_offset - max(0, instr_offset - context_before)


def pattern_search_sections(sections: list[Section], source_scan_names: set[str] | None) -> list[Section]:
    result: list[Section] = []
    for section in sections:
        if not section.is_executable or section.raw_size == 0:
            continue
        if source_scan_names is not None and section.name.lower() not in source_scan_names:
            continue
        result.append(section)
    return result


def build_context_pattern(data: bytes, instr_offset: int, instr_length: int, context_before: int, context_after: int) -> list[int | None]:
    start = max(0, instr_offset - context_before)
    end = min(len(data), instr_offset + instr_length + context_after)
    pattern: list[int | None] = [byte for byte in data[start:end]]

    disp_start = instr_offset + rip_relative_disp_offset(instr_length)
    disp_end = disp_start + 4
    wildcard_start = max(start, disp_start)
    wildcard_end = min(end, disp_end)
    for index in range(wildcard_start, wildcard_end):
        pattern[index - start] = None

    return pattern


def rip_relative_disp_offset(instr_length: int) -> int:
    return 3 if instr_length == 7 else 2


def format_aob_pattern(pattern: list[int | None]) -> str:
    return " ".join("??" if byte is None else f"{byte:02X}" for byte in pattern)


def count_pattern_matches(data: bytes, sections: list[Section], pattern: list[int | None]) -> int:
    if not pattern:
        return 0

    anchor = first_concrete_pattern_byte(pattern)
    if anchor is None:
        return 0
    anchor_index, anchor_value = anchor
    anchor_bytes = bytes([anchor_value])

    matches = 0
    for section in sections:
        start = section.raw_pointer
        end = min(len(data), section.raw_pointer + section.raw_size)
        search_from = start + anchor_index
        while search_from < end:
            found = data.find(anchor_bytes, search_from, end)
            if found < 0:
                break
            candidate_start = found - anchor_index
            if candidate_start >= start and candidate_start + len(pattern) <= end and pattern_matches_at(data, candidate_start, pattern):
                matches += 1
            search_from = found + 1
    return matches


def first_concrete_pattern_byte(pattern: list[int | None]) -> tuple[int, int] | None:
    for index, value in enumerate(pattern):
        if value is not None:
            return index, value
    return None


def pattern_matches_at(data: bytes, offset: int, pattern: list[int | None]) -> bool:
    for index, expected in enumerate(pattern):
        if expected is not None and data[offset + index] != expected:
            return False
    return True


def section_by_rva(sections: list[Section], rva: int) -> Section | None:
    for section in sections:
        if section.virtual_address <= rva < section.virtual_address + section.span:
            return section
    return None


def format_pattern(data: bytes, offset: int, length: int) -> str:
    fixed = data[offset : offset + length]
    if length == 7:
        return " ".join(f"{byte:02X}" for byte in fixed[:3]) + " ?? ?? ?? ??"
    if length == 6:
        return " ".join(f"{byte:02X}" for byte in fixed[:2]) + " ?? ?? ?? ??"
    return " ".join(f"{byte:02X}" for byte in fixed)


def write_candidates(path: Path, candidates: list[Candidate]) -> None:
    with path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(
            [
                "score",
                "instr_rva",
                "file_offset",
                "kind",
                "length",
                "target_rva",
                "disp",
                "source_section",
                "target_section",
                "pattern",
                "context_pattern",
                "context_matches",
                "context_rip_relative_offset",
                "context_instruction_length",
                "rip_relative_offset",
                "instruction_length",
                "target_addend",
                "reasons",
            ]
        )
        for candidate in candidates:
            writer.writerow(
                [
                    candidate.score,
                    f"0x{candidate.instr_rva:X}",
                    f"0x{candidate.file_offset:X}",
                    candidate.kind,
                    candidate.length,
                    f"0x{candidate.target_rva:X}",
                    candidate.disp,
                    candidate.source_section,
                    candidate.target_section,
                    candidate.pattern,
                    candidate.context_pattern,
                    candidate.context_matches,
                    candidate.context_rip_relative_offset,
                    candidate.context_instruction_length,
                    rip_relative_disp_offset(candidate.length),
                    candidate.length,
                    0,
                    ";".join(candidate.reasons),
                ]
            )


def read_u16(data: bytes, offset: int) -> int:
    return struct.unpack_from("<H", data, offset)[0]


def read_u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def read_i32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<i", data, offset)[0]


if __name__ == "__main__":
    raise SystemExit(main())
