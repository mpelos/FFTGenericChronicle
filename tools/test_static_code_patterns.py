#!/usr/bin/env python3
from __future__ import annotations

import tempfile
from pathlib import Path

from scan_static_code_patterns import (
    AobPattern,
    Match,
    Section,
    find_aob,
    format_hex_list,
    parse_aob,
    status_for,
)


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def test_parse_and_find() -> None:
    pattern = parse_aob("AA BB ?? DD")
    check(pattern == (0xAA, 0xBB, None, 0xDD), f"unexpected parsed pattern: {pattern}")
    data = bytes.fromhex("00 AA BB CC DD AA BB 11 DD")
    matches = find_aob(data, pattern, max_matches=10)
    check(matches == [1, 5], f"wildcard AOB matches expected [1, 5], got {matches}")

    exact = parse_aob("AA BB CC")
    exact_matches = find_aob(data, exact, max_matches=10)
    check(exact_matches == [1], f"exact AOB match expected [1], got {exact_matches}")


def test_section_mapping() -> None:
    section = Section(".text", virtual_address=0x1000, virtual_size=0x400, raw_address=0x400, raw_size=0x300, characteristics=0)
    check(section.contains_raw(0x420), "section should contain raw offset 0x420")
    check(section.raw_to_rva(0x420) == 0x1020, "raw 0x420 should map to RVA 0x1020")


def test_status_and_formatting() -> None:
    with tempfile.TemporaryDirectory() as temp_dir:
        enhanced = Path(temp_dir) / "FFT_enhanced.exe"
        enhanced.write_bytes(b"MZ")
        pattern = AobPattern("anchor", "AA", "note", enhanced_expected_rvas=(0x1234,))
        ok = status_for(enhanced, pattern, [Match(raw_offset=0x234, rva=0x1234, section=".text")])
        missing = status_for(enhanced, pattern, [Match(raw_offset=0x999, rva=0x9999, section=".text")])
        check(ok == "PASS", f"expected PASS, got {ok}")
        check(missing == "MISSING_EXPECTED", f"expected MISSING_EXPECTED, got {missing}")

        absent_pattern = AobPattern("volatile", "BB", "note", expected_static_status="absent")
        absent = status_for(enhanced, absent_pattern, [])
        unexpected = status_for(enhanced, absent_pattern, [Match(raw_offset=1, rva=2, section=".text")])
        check(absent == "PASS_ABSENT", f"expected PASS_ABSENT, got {absent}")
        check(unexpected == "UNEXPECTED_STATIC_MATCH", f"expected UNEXPECTED_STATIC_MATCH, got {unexpected}")

    check(format_hex_list([0x10, None, 0x2A]) == "0x10, n/a, 0x2A", "hex list formatting mismatch")


def main() -> int:
    test_parse_and_find()
    test_section_mapping()
    test_status_and_formatting()
    print("static code pattern scanner smoke test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
