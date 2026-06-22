#!/usr/bin/env python3
"""Read-only live scanner for preview/forecast battle context.

This is aimed at UI forecast states such as "Cloud uses Braver on Beowulf,
153 damage, 100%, 125%" before the action is confirmed. It searches process
memory for known unit pointers plus forecast values encoded as common integer
and floating-point formats, then reports small clusters that contain both.
"""
from __future__ import annotations

import argparse
import json
import math
import struct
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

from scan_live_unit_pointers import (
    CloseHandle,
    DEFAULT_LOG,
    Hit,
    MEMORY_BASIC_INFORMATION,
    OpenProcess,
    ReadProcessMemory,
    UnitPtr,
    VirtualQueryEx,
    find_pid,
    is_scannable_region,
    parse_named_value,
    resolve_units,
)

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT = ROOT / "work" / "live_forecast_scan.md"


@dataclass(frozen=True)
class PatternSpec:
    name: str
    kind: str
    encoding: str
    data: bytes


@dataclass(frozen=True)
class PatternHit:
    pattern: PatternSpec
    address: int
    region_base: int
    region_size: int
    protect: int
    mem_type: int


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Scan live FFT_enhanced memory for preview forecast clusters.")
    p.add_argument("--process-name", default="FFT_enhanced.exe")
    p.add_argument("--pid", type=int, default=0)
    p.add_argument("--log", type=Path, default=DEFAULT_LOG)
    p.add_argument("--unit", action="append", default=[], help="Named unit pointer, e.g. Cloud=0x1418562E0.")
    p.add_argument("--unit-id", action="append", default=[], help="Resolve from latest [UNIT] line, e.g. Cloud=0x32.")
    p.add_argument("--value", action="append", default=[], help="Named forecast value, e.g. damage=153. Repeatable.")
    p.add_argument("--text", action="append", default=[], help="Text to search as ASCII and UTF-16LE, e.g. Braver.")
    p.add_argument("--ratio", action="append", default=[], help="Named floating ratio, e.g. damageMod=1.25.")
    p.add_argument("--include-byte-values", action="store_true", help="Also search values as single bytes. Noisy.")
    p.add_argument("--near-bytes", type=lambda s: int(s, 0), default=0x200)
    p.add_argument("--chunk-size", type=lambda s: int(s, 0), default=0x400000)
    p.add_argument("--max-region-size", type=lambda s: int(s, 0), default=0x20000000)
    p.add_argument("--max-hits-per-pattern", type=int, default=5000)
    p.add_argument("--max-groups", type=int, default=160)
    p.add_argument("--min-distinct-kinds", type=int, default=2)
    p.add_argument("--include-exec", action="store_true")
    p.add_argument("--include-image", action="store_true")
    p.add_argument("--output", type=Path, default=DEFAULT_OUT)
    p.add_argument("--json-output", type=Path, default=None)
    return p.parse_args()


def main() -> int:
    args = parse_args()
    units = resolve_units(args)
    patterns = build_patterns(units, args)
    if not patterns:
        raise SystemExit("no patterns configured; pass --unit-id/--unit and/or --value/--text")

    pid = args.pid or find_pid(args.process_name)
    if not pid:
        raise SystemExit(f"process not found: {args.process_name}")

    handle = OpenProcess(0x0400 | 0x0010, False, pid)
    if not handle:
        raise SystemExit(f"OpenProcess failed for pid={pid}")

    try:
        hits, stats = scan_process(handle, patterns, args)
    finally:
        CloseHandle(handle)

    groups = find_groups(hits, args.near_bytes, args.max_groups, args.min_distinct_kinds)
    report = render_report(pid, units, patterns, hits, groups, stats, args)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(render_json(pid, units, patterns, hits, groups, stats, args), encoding="utf-8")

    print(f"wrote {args.output}")
    if args.json_output:
        print(f"wrote {args.json_output}")
    print(f"patterns: {len(patterns)}; hits: {sum(len(v) for v in hits.values())}; groups: {len(groups)}")
    return 0


def build_patterns(units: list[UnitPtr], args: argparse.Namespace) -> list[PatternSpec]:
    patterns: list[PatternSpec] = []
    seen: set[tuple[str, bytes]] = set()

    def add(name: str, kind: str, encoding: str, data: bytes) -> None:
        if not data:
            return
        key = (f"{kind}:{name}:{encoding}", data)
        if key in seen:
            return
        seen.add(key)
        patterns.append(PatternSpec(name, kind, encoding, data))

    for unit in units:
        add(unit.name, "unit", "ptr64", struct.pack("<Q", unit.ptr))

    for spec in args.value:
        name, value = parse_named_value(spec, "value")
        add_numeric_patterns(add, name, value, args.include_byte_values)

    for spec in args.ratio:
        name, raw = parse_ratio(spec)
        add(name, "ratio", "f32", struct.pack("<f", raw))
        add(name, "ratio", "f64", struct.pack("<d", raw))

    for text in args.text:
        label = text.strip()
        if not label:
            continue
        add(label, "text", "ascii", label.encode("ascii", errors="ignore"))
        add(label, "text", "utf16le", label.encode("utf-16le", errors="ignore"))

    return patterns


def parse_ratio(spec: str) -> tuple[str, float]:
    name, sep, raw = spec.partition("=")
    if not sep or not name.strip() or not raw.strip():
        raise SystemExit(f"invalid --ratio {spec!r}; expected Name=1.25")
    value = float(raw.strip())
    if not math.isfinite(value):
        raise SystemExit(f"invalid --ratio {spec!r}; value must be finite")
    return name.strip(), value


def add_numeric_patterns(add, name: str, value: int, include_byte: bool) -> None:
    if include_byte and 0 <= value <= 0xFF:
        add(name, "value", "u8", struct.pack("<B", value))
    if 0 <= value <= 0xFFFF:
        add(name, "value", "u16", struct.pack("<H", value))
    if -0x8000 <= value <= 0x7FFF:
        add(name, "value", "s16", struct.pack("<h", value))
    if 0 <= value <= 0xFFFFFFFF:
        add(name, "value", "u32", struct.pack("<I", value))
    if -0x80000000 <= value <= 0x7FFFFFFF:
        add(name, "value", "s32", struct.pack("<i", value))
    add(name, "value", "f32", struct.pack("<f", float(value)))
    add(name, "value", "f64", struct.pack("<d", float(value)))


def scan_process(
    handle: int,
    patterns: list[PatternSpec],
    args: argparse.Namespace,
) -> tuple[dict[str, list[PatternHit]], dict[str, int]]:
    hits: dict[str, list[PatternHit]] = defaultdict(list)
    stats = defaultdict(int)
    address = 0
    mbi = MEMORY_BASIC_INFORMATION()
    mbi_size = struct.calcsize("P")
    # ctypes.sizeof is accessed through the imported structure type.
    import ctypes

    mbi_size = ctypes.sizeof(MEMORY_BASIC_INFORMATION)
    while VirtualQueryEx(handle, ctypes.c_void_p(address), ctypes.byref(mbi), mbi_size):
        base = int(mbi.BaseAddress or 0)
        size = int(mbi.RegionSize or 0)
        next_address = base + max(size, 0x1000)
        if is_scannable_region(mbi, args):
            stats["regions_scanned"] += 1
            stats["bytes_scanned"] += size
            scan_region(handle, base, size, int(mbi.Protect), int(mbi.Type), patterns, hits, args)
        else:
            stats["regions_skipped"] += 1
        if next_address <= address:
            break
        address = next_address
    return hits, dict(stats)


def scan_region(
    handle: int,
    base: int,
    size: int,
    protect: int,
    mem_type: int,
    patterns: list[PatternSpec],
    hits: dict[str, list[PatternHit]],
    args: argparse.Namespace,
) -> None:
    chunk_size = max(0x1000, args.chunk_size)
    overlap = max((len(pattern.data) for pattern in patterns), default=1) - 1
    offset = 0
    while offset < size:
        read_size = min(chunk_size, size - offset)
        data = read_memory(handle, base + offset, read_size)
        if data:
            for pattern in patterns:
                key = pattern_key(pattern)
                if len(hits[key]) >= args.max_hits_per_pattern:
                    continue
                start = 0
                while True:
                    found = data.find(pattern.data, start)
                    if found < 0:
                        break
                    hits[key].append(PatternHit(pattern, base + offset + found, base, size, protect, mem_type))
                    if len(hits[key]) >= args.max_hits_per_pattern:
                        break
                    start = found + 1
        if read_size <= overlap:
            break
        offset += read_size - overlap


def read_memory(handle: int, address: int, size: int) -> bytes:
    import ctypes

    buffer = ctypes.create_string_buffer(size)
    bytes_read = ctypes.c_size_t()
    if not ReadProcessMemory(handle, ctypes.c_void_p(address), buffer, size, ctypes.byref(bytes_read)):
        return b""
    return buffer.raw[: bytes_read.value]


def find_groups(
    hits: dict[str, list[PatternHit]],
    near_bytes: int,
    max_groups: int,
    min_distinct_kinds: int,
) -> list[list[PatternHit]]:
    all_hits = sorted((hit for values in hits.values() for hit in values), key=lambda hit: hit.address)
    groups: list[list[PatternHit]] = []
    left = 0
    for right, hit in enumerate(all_hits):
        while all_hits[left].address < hit.address - near_bytes:
            left += 1
        window = all_hits[left : right + 1]
        kinds = {candidate.pattern.kind for candidate in window}
        if len(kinds) < min_distinct_kinds:
            continue
        if not any(candidate.pattern.kind == "unit" for candidate in window):
            continue
        if not any(candidate.pattern.kind in {"value", "ratio", "text"} for candidate in window):
            continue
        groups.append(window)
        if len(groups) >= max_groups * 3:
            break
    return compact_groups(groups)[:max_groups]


def compact_groups(groups: list[list[PatternHit]]) -> list[list[PatternHit]]:
    compact: list[list[PatternHit]] = []
    seen: set[tuple[int, int, tuple[str, ...]]] = set()
    for group in sorted(groups, key=score_group, reverse=True):
        start = min(hit.address for hit in group)
        end = max(hit.address for hit in group)
        names = tuple(sorted({pattern_key(hit.pattern) for hit in group}))
        key = (start // 0x20, end // 0x20, names)
        if key in seen:
            continue
        seen.add(key)
        compact.append(group)
    return compact


def score_group(group: list[PatternHit]) -> float:
    kinds = {hit.pattern.kind for hit in group}
    names = {hit.pattern.name.lower() for hit in group}
    span = max(hit.address for hit in group) - min(hit.address for hit in group)
    score = 0.0
    score += 500.0 if "unit" in kinds and "value" in kinds else 0.0
    score += 200.0 if "text" in kinds else 0.0
    score += 100.0 * len(kinds)
    score += 25.0 * len(names)
    score -= span / 16.0
    score -= max(0, len(group) - 12) * 5.0
    return score


def render_report(
    pid: int,
    units: list[UnitPtr],
    patterns: list[PatternSpec],
    hits: dict[str, list[PatternHit]],
    groups: list[list[PatternHit]],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    lines: list[str] = []
    lines.append("# Live Forecast Scan")
    lines.append("")
    lines.append(f"- PID: `{pid}`")
    lines.append(f"- Near window: `0x{args.near_bytes:X}`")
    lines.append(f"- Regions scanned: `{stats.get('regions_scanned', 0)}`")
    lines.append(f"- Regions skipped: `{stats.get('regions_skipped', 0)}`")
    lines.append(f"- Bytes scanned: `{stats.get('bytes_scanned', 0):,}`")
    lines.append("")
    lines.append("## Units")
    for unit in units:
        lines.append(f"- `{unit.name}` = `0x{unit.ptr:X}` ({unit.source})")
    lines.append("")
    lines.append("## Pattern Hits")
    for pattern in patterns:
        lines.append(f"- `{pattern_key(pattern)}` hits `{len(hits.get(pattern_key(pattern), []))}`")
    lines.append("")
    lines.append("## Candidate Clusters")
    if not groups:
        lines.append("No unit/value clusters found.")
    else:
        lines.append("| Start | Span | Score | Kinds | Hits | Region |")
        lines.append("| --- | ---: | ---: | --- | --- | --- |")
        for group in groups:
            start = min(hit.address for hit in group)
            end = max(hit.address for hit in group)
            kinds = ", ".join(sorted({hit.pattern.kind for hit in group}))
            hit_text = ", ".join(
                f"{pattern_key(hit.pattern)}@0x{hit.address:X}" for hit in sorted(group, key=lambda item: item.address)[:16]
            )
            region = group[0]
            lines.append(
                f"| `0x{start:X}` | `0x{end - start:X}` | `{score_group(group):.1f}` | `{kinds}` | "
                f"`{hit_text}` | `base=0x{region.region_base:X} size=0x{region.region_size:X} "
                f"protect=0x{region.protect:X} type=0x{region.mem_type:X}` |"
            )
    lines.append("")
    return "\n".join(lines)


def render_json(
    pid: int,
    units: list[UnitPtr],
    patterns: list[PatternSpec],
    hits: dict[str, list[PatternHit]],
    groups: list[list[PatternHit]],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    payload = {
        "pid": pid,
        "nearBytes": args.near_bytes,
        "stats": stats,
        "units": [{"name": unit.name, "ptr": unit.ptr, "source": unit.source} for unit in units],
        "patterns": [
            {
                "key": pattern_key(pattern),
                "name": pattern.name,
                "kind": pattern.kind,
                "encoding": pattern.encoding,
                "bytes": pattern.data.hex(),
                "hits": len(hits.get(pattern_key(pattern), [])),
            }
            for pattern in patterns
        ],
        "groups": [
            {
                "start": min(hit.address for hit in group),
                "end": max(hit.address for hit in group),
                "span": max(hit.address for hit in group) - min(hit.address for hit in group),
                "score": score_group(group),
                "kinds": sorted({hit.pattern.kind for hit in group}),
                "regionBase": group[0].region_base,
                "regionSize": group[0].region_size,
                "protect": group[0].protect,
                "memType": group[0].mem_type,
                "hits": [
                    {
                        "key": pattern_key(hit.pattern),
                        "name": hit.pattern.name,
                        "kind": hit.pattern.kind,
                        "encoding": hit.pattern.encoding,
                        "address": hit.address,
                    }
                    for hit in sorted(group, key=lambda item: item.address)
                ],
            }
            for group in groups
        ],
    }
    return json.dumps(payload, indent=2, sort_keys=True)


def pattern_key(pattern: PatternSpec) -> str:
    return f"{pattern.kind}:{pattern.name}:{pattern.encoding}"


if __name__ == "__main__":
    raise SystemExit(main())
