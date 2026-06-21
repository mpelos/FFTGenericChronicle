#!/usr/bin/env python3
"""Read-only static AOB scan for FFT Ivalice Chronicles executables.

This does not replace live tracing. It gives us a reproducible way to answer two offline
questions:

- do the stable low-.text anchors still exist in the installed executable?
- is a candidate pattern absent from the static file, suggesting it only exists in a
  runtime-decrypted/relocated region?
"""
from __future__ import annotations

import argparse
import struct
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


REPO = Path(__file__).resolve().parents[1]
DEFAULT_GAME_DIR = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles")
DEFAULT_OUTPUT = REPO / "work" / "static_code_pattern_scan.md"


@dataclass(frozen=True)
class AobPattern:
    name: str
    text: str
    note: str
    enhanced_expected_rvas: tuple[int, ...] = ()
    expected_static_status: str = "present"


PATTERNS: tuple[AobPattern, ...] = (
    AobPattern(
        "battle_base_ptr",
        "0F B7 41 30 66 89 42 0C",
        "stable unit-struct touchpoint; rcx is a battle unit and +0x30 is HP",
        (0x226D98,),
    ),
    AobPattern(
        "damage_multiplier",
        "0F B7 47 30 2B C2 85 C0 41 0F 4E CE 8A D1 E8 F2",
        "volatile damage application site; expected to be absent from static file on current build",
        (),
        "absent",
    ),
    AobPattern(
        "damage_mult_2",
        "2B C8 8D 04 11",
        "stable helper anchor near damage math evidence",
        (0x30A685,),
    ),
    AobPattern(
        "jp_multiplier",
        "03 C2 8B CF 41 3B C0",
        "stable JP math anchor",
        (0x283754,),
    ),
    AobPattern(
        "xp_multiplier",
        "0F B7 84 7B 1E 01 00 00",
        "stable XP math anchor",
        (0x283767,),
    ),
    AobPattern(
        "min_brave_faith",
        "41 0F B6 5A 2B",
        "short/unstable public CE pattern; useful only as a warning signal",
    ),
    AobPattern(
        "min_spd_jmp_mov",
        "0F B6 47 42 66 89 43 30",
        "stable movement stat anchor; confirms +0x42 Move",
        (0x36027F,),
    ),
)


@dataclass(frozen=True)
class Section:
    name: str
    virtual_address: int
    virtual_size: int
    raw_address: int
    raw_size: int
    characteristics: int

    def contains_raw(self, offset: int) -> bool:
        return self.raw_address <= offset < self.raw_address + self.raw_size

    def raw_to_rva(self, offset: int) -> int:
        return self.virtual_address + (offset - self.raw_address)


@dataclass(frozen=True)
class PeImage:
    path: Path
    sections: tuple[Section, ...]

    @staticmethod
    def load(path: Path) -> "PeImage":
        with path.open("rb") as f:
            mz = f.read(0x40)
            if len(mz) < 0x40 or mz[:2] != b"MZ":
                raise ValueError(f"{path} is not a PE/MZ executable")

            pe_offset = struct.unpack_from("<I", mz, 0x3C)[0]
            f.seek(pe_offset)
            if f.read(4) != b"PE\0\0":
                raise ValueError(f"{path} has no PE signature at 0x{pe_offset:X}")

            coff = f.read(20)
            if len(coff) != 20:
                raise ValueError(f"{path} has a truncated COFF header")
            section_count = struct.unpack_from("<H", coff, 2)[0]
            optional_header_size = struct.unpack_from("<H", coff, 16)[0]
            f.seek(optional_header_size, 1)

            sections: list[Section] = []
            for _ in range(section_count):
                raw = f.read(40)
                if len(raw) != 40:
                    raise ValueError(f"{path} has a truncated section table")
                name = raw[:8].split(b"\0", 1)[0].decode("ascii", errors="replace")
                virtual_size, virtual_address, raw_size, raw_address = struct.unpack_from("<IIII", raw, 8)
                characteristics = struct.unpack_from("<I", raw, 36)[0]
                sections.append(
                    Section(name, virtual_address, virtual_size, raw_address, raw_size, characteristics)
                )

        return PeImage(path, tuple(sections))

    def raw_to_rva(self, offset: int) -> tuple[int | None, str]:
        for section in self.sections:
            if section.contains_raw(offset):
                return section.raw_to_rva(offset), section.name
        return None, "headers"


@dataclass(frozen=True)
class Match:
    raw_offset: int
    rva: int | None
    section: str


def parse_aob(text: str) -> tuple[int | None, ...]:
    tokens = text.replace(",", " ").split()
    pattern: list[int | None] = []
    for token in tokens:
        if token in {"?", "??"}:
            pattern.append(None)
            continue
        if len(token) != 2:
            raise ValueError(f"invalid AOB token {token!r}")
        pattern.append(int(token, 16))
    if not pattern:
        raise ValueError("empty AOB")
    return tuple(pattern)


def find_aob(data: bytes, pattern: tuple[int | None, ...], max_matches: int) -> list[int]:
    if all(byte is not None for byte in pattern):
        needle = bytes(byte for byte in pattern if byte is not None)
        matches: list[int] = []
        start = 0
        while len(matches) < max_matches:
            found = data.find(needle, start)
            if found < 0:
                break
            matches.append(found)
            start = found + 1
        return matches

    matches = []
    end = len(data) - len(pattern) + 1
    for offset in range(max(0, end)):
        for index, expected in enumerate(pattern):
            if expected is not None and data[offset + index] != expected:
                break
        else:
            matches.append(offset)
            if len(matches) >= max_matches:
                break
    return matches


def scan_executable(path: Path, patterns: Iterable[AobPattern], max_matches: int) -> dict[str, list[Match]]:
    image = PeImage.load(path)
    data = path.read_bytes()
    results: dict[str, list[Match]] = {}
    for pattern in patterns:
        matches: list[Match] = []
        for raw_offset in find_aob(data, parse_aob(pattern.text), max_matches):
            rva, section = image.raw_to_rva(raw_offset)
            matches.append(Match(raw_offset, rva, section))
        results[pattern.name] = matches
    return results


def status_for(path: Path, pattern: AobPattern, matches: list[Match]) -> str:
    is_enhanced = path.name.lower() == "fft_enhanced.exe"
    rvas = {match.rva for match in matches if match.rva is not None}

    if is_enhanced and pattern.enhanced_expected_rvas:
        missing = [rva for rva in pattern.enhanced_expected_rvas if rva not in rvas]
        return "PASS" if not missing else "MISSING_EXPECTED"

    if pattern.expected_static_status == "absent":
        return "PASS_ABSENT" if not matches else "UNEXPECTED_STATIC_MATCH"

    return "FOUND" if matches else "NOTFOUND"


def format_hex_list(values: Iterable[int | None]) -> str:
    rendered = ["n/a" if value is None else f"0x{value:X}" for value in values]
    return ", ".join(rendered) if rendered else "-"


def render_report(exe_paths: list[Path], all_results: dict[Path, dict[str, list[Match]]]) -> str:
    lines: list[str] = [
        "# Static Code Pattern Scan",
        "",
        "Generated by `tools/scan_static_code_patterns.py`. This is a read-only static scan; it",
        "does not prove runtime-decrypted/Denuvo regions and does not replace live tracing.",
        "",
    ]

    for path in exe_paths:
        lines.append(f"## {path.name}")
        if path not in all_results:
            lines.append("")
            lines.append("Executable not found.")
            lines.append("")
            continue

        lines.append("")
        lines.append(f"- Path: `{path}`")
        lines.append(f"- Size: {path.stat().st_size} bytes")
        lines.append("")
        lines.append("| Pattern | Matches | RVAs | Raw Offsets | Sections | Status | Note |")
        lines.append("| --- | ---: | --- | --- | --- | --- | --- |")
        results = all_results[path]
        for pattern in PATTERNS:
            matches = results.get(pattern.name, [])
            rvas = [match.rva for match in matches]
            raw_offsets = [match.raw_offset for match in matches]
            sections = ", ".join(sorted({match.section for match in matches})) if matches else "-"
            status = status_for(path, pattern, matches)
            lines.append(
                f"| `{pattern.name}` | {len(matches)} | `{format_hex_list(rvas)}` | "
                f"`{format_hex_list(raw_offsets)}` | `{sections}` | {status} | {pattern.note} |"
            )
        lines.append("")

    lines.extend(
        [
            "## Read",
            "",
            "- On `FFT_enhanced.exe`, the low-.text anchors should map to the live RVAs already",
            "  documented in `docs/modding/04-re-strategy.md`: `battle_base_ptr`,",
            "  `damage_mult_2`, `jp_multiplier`, `xp_multiplier`, and `min_spd_jmp_mov`.",
            "- `damage_multiplier` being absent from the static file is expected on the current",
            "  build and supports the existing conclusion: the direct damage application site must",
            "  be found by live/runtime tracing, not static AOB matching.",
            "- Extra matches are not automatically bad. They are candidate read sites, not proof of",
            "  semantics; only the expected enhanced RVAs are treated as stable anchors.",
            "",
        ]
    )
    return "\n".join(lines)


def default_exes() -> list[Path]:
    return [DEFAULT_GAME_DIR / "FFT_enhanced.exe", DEFAULT_GAME_DIR / "FFT_classic.exe"]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Static read-only scan of known FFT IVC AOBs.")
    parser.add_argument("--exe", action="append", type=Path, help="Executable to scan. Defaults to installed enhanced/classic exes.")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT, help=f"Markdown report path. Default: {DEFAULT_OUTPUT}")
    parser.add_argument("--max-matches", type=int, default=32, help="Maximum matches per pattern.")
    parser.add_argument("--strict-enhanced", action="store_true", help="Exit nonzero if an enhanced expected RVA is missing.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    exe_paths = [path.resolve() for path in (args.exe or default_exes())]
    all_results: dict[Path, dict[str, list[Match]]] = {}
    failures: list[str] = []

    for path in exe_paths:
        if not path.exists():
            continue
        results = scan_executable(path, PATTERNS, max(1, args.max_matches))
        all_results[path] = results
        if args.strict_enhanced and path.name.lower() == "fft_enhanced.exe":
            for pattern in PATTERNS:
                if status_for(path, pattern, results.get(pattern.name, [])) == "MISSING_EXPECTED":
                    failures.append(f"{path.name}: missing expected RVA for {pattern.name}")

    report = render_report(exe_paths, all_results)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {args.output}")

    if failures:
        for failure in failures:
            print(f"error: {failure}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
