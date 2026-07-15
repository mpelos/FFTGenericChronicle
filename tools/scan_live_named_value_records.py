#!/usr/bin/env python3
"""Read-only scanner for compact live records made of named numeric tokens.

The forecast scanner groups by broad kinds such as "value" and "unit". That is
useful for UI previews, but pending action records can be tiny numeric structs:
ability id, actor id, target id, charge, damage, flags. This scanner groups by
the *names* of the values instead, so a window containing braverId+damage+charge
is reported even though all three are just integers.
"""
from __future__ import annotations

import argparse
import json
import struct
from bisect import bisect_left, bisect_right
from dataclasses import dataclass
from pathlib import Path

from scan_live_forecast_values import PatternHit, PatternSpec, pattern_key, scan_process
from scan_live_unit_pointers import CloseHandle, OpenProcess, find_pid, parse_named_value

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT = ROOT / "work" / "live_named_value_records.md"


@dataclass(frozen=True)
class Candidate:
    start: int
    end: int
    span: int
    score: float
    hits: tuple[PatternHit, ...]

    @property
    def names(self) -> tuple[str, ...]:
        result: list[str] = []
        for hit in self.hits:
            if hit.pattern.name not in result:
                result.append(hit.pattern.name)
        return tuple(result)


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Scan live FFT_enhanced memory for compact named numeric records.")
    p.add_argument("--process-name", default="FFT_enhanced.exe")
    p.add_argument("--pid", type=int, default=0)
    p.add_argument("--value", action="append", default=[], help="Named value, e.g. braverId=257. Repeatable.")
    p.add_argument("--byte-value", action="append", default=[], help="Named byte-only value, e.g. cloudId=0x32.")
    p.add_argument("--text", action="append", default=[], help="ASCII/UTF-16LE text token, e.g. Braver.")
    p.add_argument("--require", action="append", default=[], help="Value/text name required in candidate windows.")
    p.add_argument("--near-bytes", type=lambda s: int(s, 0), default=0x180)
    p.add_argument("--max-span", type=lambda s: int(s, 0), default=0x300)
    p.add_argument("--chunk-size", type=lambda s: int(s, 0), default=0x400000)
    p.add_argument("--max-region-size", type=lambda s: int(s, 0), default=0x20000000)
    p.add_argument("--max-hits-per-pattern", type=int, default=5000)
    p.add_argument("--max-window-hits", type=int, default=64)
    p.add_argument("--max-candidates", type=int, default=120)
    p.add_argument("--include-exec", action="store_true")
    p.add_argument("--include-image", action="store_true")
    p.add_argument("--output", type=Path, default=DEFAULT_OUT)
    p.add_argument("--json-output", type=Path, default=None)
    return p.parse_args()


def main() -> int:
    args = parse_args()
    patterns = build_patterns(args)
    if not patterns:
        raise SystemExit("no patterns configured")

    required = tuple(name.strip() for name in (args.require or [p.name for p in patterns]) if name.strip())
    if not required:
        raise SystemExit("no required names configured")

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

    candidates = build_candidates(hits, required, args)
    report = render_report(pid, patterns, hits, candidates, required, stats, args)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(render_json(pid, patterns, hits, candidates, required, stats, args), encoding="utf-8")

    print(f"wrote {args.output}")
    if args.json_output:
        print(f"wrote {args.json_output}")
    print(f"patterns: {len(patterns)}; hits: {sum(len(v) for v in hits.values())}; candidates: {len(candidates)}")
    return 0


def build_patterns(args: argparse.Namespace) -> list[PatternSpec]:
    patterns: list[PatternSpec] = []
    seen: set[tuple[str, str, bytes]] = set()

    def add(name: str, kind: str, encoding: str, data: bytes) -> None:
        key = (name, encoding, data)
        if data and key not in seen:
            seen.add(key)
            patterns.append(PatternSpec(name, kind, encoding, data))

    for spec in args.value:
        name, value = parse_named_value(spec, "value")
        if 0 <= value <= 0xFFFF:
            add(name, "value", "u16", struct.pack("<H", value))
        if -0x8000 <= value <= 0x7FFF:
            add(name, "value", "s16", struct.pack("<h", value))
        if 0 <= value <= 0xFFFFFFFF:
            add(name, "value", "u32", struct.pack("<I", value))
        if -0x80000000 <= value <= 0x7FFFFFFF:
            add(name, "value", "s32", struct.pack("<i", value))

    for spec in args.byte_value:
        name, value = parse_named_value(spec, "byte-value")
        if not 0 <= value <= 0xFF:
            raise SystemExit(f"--byte-value {spec!r} is outside byte range")
        add(name, "value", "u8", struct.pack("<B", value))

    for text in args.text:
        label = text.strip()
        if not label:
            continue
        add(label, "text", "ascii", label.encode("ascii", errors="ignore"))
        add(label, "text", "utf16le", label.encode("utf-16le", errors="ignore"))

    return patterns


def build_candidates(
    hits_by_key: dict[str, list[PatternHit]],
    required: tuple[str, ...],
    args: argparse.Namespace,
) -> list[Candidate]:
    required_names = {name.lower() for name in required}
    all_hits = sorted((hit for values in hits_by_key.values() for hit in values), key=lambda item: item.address)
    addresses = [hit.address for hit in all_hits]
    candidates: dict[tuple[int, int, tuple[tuple[str, str, int], ...]], Candidate] = {}

    for hit in all_hits:
        if hit.pattern.name.lower() not in required_names:
            continue
        left = bisect_left(addresses, hit.address - args.near_bytes)
        right = bisect_right(addresses, hit.address + args.near_bytes)
        window = tuple(item for item in all_hits[left:right] if item.region_base == hit.region_base)
        if not window or len(window) > args.max_window_hits:
            continue
        names = {item.pattern.name.lower() for item in window}
        if not required_names <= names:
            continue
        compact = compact_to_required_span(window, required_names, args.max_span)
        if not compact:
            continue
        key = (
            compact[0].address,
            compact[-1].address,
            tuple((item.pattern.name, item.pattern.encoding, item.address) for item in compact),
        )
        candidates[key] = Candidate(
            start=compact[0].address,
            end=compact[-1].address,
            span=compact[-1].address - compact[0].address,
            score=score_candidate(compact, required_names),
            hits=compact,
        )

    return sorted(candidates.values(), key=lambda item: item.score, reverse=True)[: args.max_candidates]


def compact_to_required_span(
    window: tuple[PatternHit, ...],
    required_names: set[str],
    max_span: int,
) -> tuple[PatternHit, ...] | None:
    best: tuple[PatternHit, ...] | None = None
    for start_index, start_hit in enumerate(window):
        if start_hit.pattern.name.lower() not in required_names:
            continue
        names: set[str] = set()
        for end_index in range(start_index, len(window)):
            names.add(window[end_index].pattern.name.lower())
            if window[end_index].address - start_hit.address > max_span:
                break
            if required_names <= names:
                candidate = window[start_index : end_index + 1]
                if best is None or span(candidate) < span(best):
                    best = candidate
                break
    return best


def span(hits: tuple[PatternHit, ...]) -> int:
    return hits[-1].address - hits[0].address


def score_candidate(hits: tuple[PatternHit, ...], required_names: set[str]) -> float:
    names = {hit.pattern.name.lower() for hit in hits}
    score = 1000.0 + 120.0 * len(names & required_names)
    score -= span(hits) / 4.0
    score -= max(0, len(hits) - len(required_names)) * 12.0
    score -= max(0, len(names) - len(required_names)) * 80.0
    return score


def render_report(
    pid: int,
    patterns: list[PatternSpec],
    hits: dict[str, list[PatternHit]],
    candidates: list[Candidate],
    required: tuple[str, ...],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    lines: list[str] = []
    lines.append("# Live Named Value Records")
    lines.append("")
    lines.append(f"- PID: `{pid}`")
    lines.append(f"- Required: `{', '.join(required)}`")
    lines.append(f"- Near window: `0x{args.near_bytes:X}`")
    lines.append(f"- Max span: `0x{args.max_span:X}`")
    lines.append(f"- Regions scanned: `{stats.get('regions_scanned', 0)}`")
    lines.append(f"- Bytes scanned: `{stats.get('bytes_scanned', 0):,}`")
    lines.append("")
    lines.append("## Pattern Hits")
    for pattern in patterns:
        lines.append(f"- `{pattern_key(pattern)}` hits `{len(hits.get(pattern_key(pattern), []))}`")
    lines.append("")
    lines.append("## Candidate Records")
    if not candidates:
        lines.append("No compact named-value records found.")
    else:
        lines.append("| Start | Span | Score | Names | Hits | Region |")
        lines.append("| --- | ---: | ---: | --- | --- | --- |")
        for candidate in candidates:
            hit_text = ", ".join(
                f"{pattern_key(hit.pattern)}@0x{hit.address:X}" for hit in sorted(candidate.hits, key=lambda item: item.address)
            )
            region = candidate.hits[0]
            lines.append(
                f"| `0x{candidate.start:X}` | `0x{candidate.span:X}` | `{candidate.score:.1f}` | "
                f"`{', '.join(candidate.names)}` | `{hit_text}` | "
                f"`base=0x{region.region_base:X} size=0x{region.region_size:X} protect=0x{region.protect:X} type=0x{region.mem_type:X}` |"
            )
    lines.append("")
    return "\n".join(lines)


def render_json(
    pid: int,
    patterns: list[PatternSpec],
    hits: dict[str, list[PatternHit]],
    candidates: list[Candidate],
    required: tuple[str, ...],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    payload = {
        "pid": pid,
        "required": list(required),
        "nearBytes": args.near_bytes,
        "maxSpan": args.max_span,
        "stats": stats,
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
        "candidates": [
            {
                "start": candidate.start,
                "end": candidate.end,
                "span": candidate.span,
                "score": candidate.score,
                "names": list(candidate.names),
                "regionBase": candidate.hits[0].region_base,
                "regionSize": candidate.hits[0].region_size,
                "protect": candidate.hits[0].protect,
                "memType": candidate.hits[0].mem_type,
                "hits": [
                    {
                        "key": pattern_key(hit.pattern),
                        "name": hit.pattern.name,
                        "kind": hit.pattern.kind,
                        "encoding": hit.pattern.encoding,
                        "address": hit.address,
                    }
                    for hit in sorted(candidate.hits, key=lambda item: item.address)
                ],
            }
            for candidate in candidates
        ],
    }
    return json.dumps(payload, indent=2, sort_keys=True)


if __name__ == "__main__":
    raise SystemExit(main())
