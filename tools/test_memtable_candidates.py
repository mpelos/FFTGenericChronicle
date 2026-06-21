#!/usr/bin/env python3
"""Smoke tests for the offline MEMTABLE candidate scanner."""
from __future__ import annotations

import struct
from pathlib import Path

from build_memtable_probe_settings import build_settings, filter_candidate_rows
from find_memtable_candidates import (
    IMAGE_SCN_CNT_CODE,
    IMAGE_SCN_MEM_READ,
    Section,
    annotate_context_patterns,
    count_pattern_matches,
    find_candidates,
    format_aob_pattern,
    source_scan_sections,
)


def main() -> int:
    check(source_scan_sections(None, False) == {".text", ".xcode"}, "default source scan sections changed")
    check(source_scan_sections([".edata"], False) == {".edata"}, "explicit source section should win")
    check(source_scan_sections(None, True) is None, "include-all should scan all executable sections")

    data = bytearray(b"\x90" * 0x80)
    data[0x08:0x0C] = struct.pack("<I", 0x258)
    data[0x10:0x17] = b"\x48\x8D\x05" + struct.pack("<i", 0x19)

    sections = [
        Section(".xcode", 0x1000, 0x28, 0x00, 0x28, IMAGE_SCN_CNT_CODE | IMAGE_SCN_MEM_READ),
        Section(".rodata", 0x1030, 0x10, 0x30, 0x10, IMAGE_SCN_MEM_READ),
    ]

    candidates = find_candidates(bytes(data), sections, nearby_window=16, stride=0x258, source_scan_names={".xcode"})
    check(len(candidates) == 1, f"expected one candidate, got {len(candidates)}")
    candidate = candidates[0]
    check(candidate.instr_rva == 0x1010, "instruction RVA should resolve from source section")
    check(candidate.target_rva == 0x1030, "RIP target should resolve into .rodata")
    check(candidate.pattern == "48 8D 05 ?? ?? ?? ??", "minimal AOB should wildcard disp32")
    check(candidate.score >= 11, "candidate should score source/target/lea/alignment/stride")

    annotated = annotate_context_patterns(bytes(data), sections, candidates, 2, 2, {".xcode"})
    check(annotated[0].context_matches == 1, "contextual AOB should be unique in synthetic xcode")
    check("48 8D 05 ?? ?? ?? ??" in annotated[0].context_pattern, "contextual AOB should wildcard disp32")
    check(annotated[0].context_rip_relative_offset == 5, "contextual RIP offset should include prefix bytes")
    check(annotated[0].context_instruction_length == 9, "contextual instruction length should include prefix bytes")

    pattern = [0xAA, None, 0xBB]
    pattern_data = bytes([0xAA, 0x00, 0xBB, 0xAA, 0x01, 0xBC, 0xAA, 0xFF, 0xBB])
    pattern_sections = [Section(".xcode", 0x2000, len(pattern_data), 0, len(pattern_data), IMAGE_SCN_CNT_CODE)]
    check(count_pattern_matches(pattern_data, pattern_sections, pattern) == 2, "wildcard match count failed")
    check(format_aob_pattern(pattern) == "AA ?? BB", "AOB formatting failed")

    candidate_rows = [
        {
            "score": "11",
            "context_matches": "1",
            "instr_rva": "0x1010",
            "target_rva": "0x1030",
            "source_section": ".xcode",
            "target_section": ".rodata",
            "pattern": "48 8D 05 ?? ?? ?? ??",
            "context_pattern": annotated[0].context_pattern,
            "context_rip_relative_offset": str(annotated[0].context_rip_relative_offset),
            "context_instruction_length": str(annotated[0].context_instruction_length),
            "rip_relative_offset": "3",
            "instruction_length": "7",
            "target_addend": "0",
            "reasons": "source-.xcode;target-.rodata;near-stride-0x258",
        },
        {
            "score": "11",
            "context_matches": "2",
            "target_section": ".rodata",
        },
    ]
    filtered = filter_candidate_rows(candidate_rows, min_score=10, require_unique_context=True, target_sections=[".rodata"])
    check(len(filtered) == 1, "settings builder should keep only unique-context rows")
    settings = build_settings(filtered, use_short_pattern=False, count=55, stride=600, source_path=Path("candidates.csv"))
    probes = settings["MemoryTableProbes"]
    check(len(probes) == 1, "settings builder should emit one probe")
    probe = probes[0]
    check(probe["Enabled"] is False, "generated probe must be disabled by default")
    check(probe["Pattern"] == annotated[0].context_pattern, "generated probe should use contextual pattern")
    check(probe["RipRelativeOffset"] == 5, "generated probe should use contextual RIP offset")
    check(probe["InstructionLength"] == 9, "generated probe should use contextual instruction length")

    print("memtable candidate scanner smoke tests passed")
    return 0


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


if __name__ == "__main__":
    raise SystemExit(main())
