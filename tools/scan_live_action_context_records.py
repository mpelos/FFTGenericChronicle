#!/usr/bin/env python3
"""Find compact live memory records containing action actor/target unit pointers.

This is a narrower companion to scan_live_forecast_values.py. Forecast values
are useful on the preview UI, but charged actions may store only unit pointers
in their real pending-action record. This scanner looks for nearby unit-pointer
clusters and ranks windows that contain a requested actor plus target.
"""
from __future__ import annotations

import argparse
import json
from bisect import bisect_left, bisect_right
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from scan_live_unit_pointers import (
    CloseHandle,
    DEFAULT_LOG,
    Hit,
    OpenProcess,
    UnitPtr,
    find_pid,
    resolve_units,
    scan_process,
)

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT = ROOT / "work" / "live_action_context_records.md"


@dataclass(frozen=True)
class Candidate:
    start: int
    end: int
    span: int
    score: float
    hits: tuple[Hit, ...]
    repeat_count: int = 1

    @property
    def names(self) -> tuple[str, ...]:
        seen: list[str] = []
        for hit in self.hits:
            if hit.name not in seen:
                seen.append(hit.name)
        return tuple(seen)


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Scan live FFT_enhanced memory for compact actor/target pointer records.")
    p.add_argument("--process-name", default="FFT_enhanced.exe")
    p.add_argument("--pid", type=int, default=0)
    p.add_argument("--log", type=Path, default=DEFAULT_LOG)
    p.add_argument("--unit", action="append", default=[], help="Named unit pointer, e.g. Cloud=0x1418562E0.")
    p.add_argument("--unit-id", action="append", default=[], help="Resolve from latest [UNIT] line, e.g. Cloud=0x32.")
    p.add_argument("--actor", required=True, help="Actor/caster unit name to require in candidate windows.")
    p.add_argument("--target", required=True, help="Target unit name to require in candidate windows.")
    p.add_argument("--also-require", action="append", default=[], help="Additional unit name required in candidate windows.")
    p.add_argument("--near-bytes", type=lambda s: int(s, 0), default=0x400)
    p.add_argument("--max-span", type=lambda s: int(s, 0), default=0x800)
    p.add_argument("--chunk-size", type=lambda s: int(s, 0), default=0x400000)
    p.add_argument("--max-region-size", type=lambda s: int(s, 0), default=0x20000000)
    p.add_argument("--max-hits-per-unit", type=int, default=30000)
    p.add_argument("--max-candidates", type=int, default=120)
    p.add_argument("--max-hits-per-candidate", type=int, default=16)
    p.add_argument(
        "--max-window-hits",
        type=int,
        default=96,
        help="Skip dense windows with more hits than this; they are usually rotating unit arrays.",
    )
    p.add_argument("--include-exec", action="store_true")
    p.add_argument("--include-image", action="store_true")
    p.add_argument("--output", type=Path, default=DEFAULT_OUT)
    p.add_argument("--json-output", type=Path, default=None)
    return p.parse_args()


def main() -> int:
    args = parse_args()
    units = resolve_units(args)
    require = normalize_required(args)
    validate_required_units(units, require)

    pid = args.pid or find_pid(args.process_name)
    if not pid:
        raise SystemExit(f"process not found: {args.process_name}")

    handle = OpenProcess(0x0400 | 0x0010, False, pid)
    if not handle:
        raise SystemExit(f"OpenProcess failed for pid={pid}")

    try:
        hits_by_name, stats = scan_process(handle, units, args)
    finally:
        CloseHandle(handle)

    candidates = build_candidates(hits_by_name, require, args)
    report = render_report(pid, units, hits_by_name, candidates, require, stats, args)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(render_json(pid, units, hits_by_name, candidates, require, stats, args), encoding="utf-8")

    print(f"wrote {args.output}")
    if args.json_output:
        print(f"wrote {args.json_output}")
    print(f"hits: {sum(len(v) for v in hits_by_name.values())}; candidates: {len(candidates)}")
    return 0


def normalize_required(args: argparse.Namespace) -> tuple[str, ...]:
    ordered = [args.actor, args.target, *args.also_require]
    result: list[str] = []
    for name in ordered:
        key = name.strip().lower()
        if key and key not in result:
            result.append(key)
    return tuple(result)


def validate_required_units(units: list[UnitPtr], require: tuple[str, ...]) -> None:
    available = {unit.name.lower() for unit in units}
    missing = [name for name in require if name not in available]
    if missing:
        raise SystemExit(f"required unit(s) missing from scan input: {', '.join(missing)}")


def build_candidates(
    hits_by_name: dict[str, list[Hit]],
    require: tuple[str, ...],
    args: argparse.Namespace,
) -> list[Candidate]:
    all_hits = sorted((hit for hits in hits_by_name.values() for hit in hits), key=lambda hit: hit.address)
    by_start: dict[tuple[int, int, tuple[tuple[str, int], ...]], Candidate] = {}
    if not all_hits:
        return []

    addresses = [hit.address for hit in all_hits]
    required = set(require)
    for hit in all_hits:
        if hit.name.lower() not in required:
            continue
        left = bisect_left(addresses, hit.address - args.near_bytes)
        right = bisect_right(addresses, hit.address + args.near_bytes)
        window = tuple(h for h in all_hits[left:right] if h.region_base == hit.region_base)
        if not window or len(window) > args.max_window_hits:
            continue
        names = {h.name.lower() for h in window}
        if not required <= names:
            continue

        compact = compact_to_required_span(window, required, args.max_span)
        if not compact:
            continue
        trimmed = trim_window(compact, require, args.max_hits_per_candidate)
        key = (
            trimmed[0].address,
            trimmed[-1].address,
            tuple((h.name, h.address) for h in trimmed),
        )
        if key in by_start:
            continue
        by_start[key] = Candidate(
            start=trimmed[0].address,
            end=trimmed[-1].address,
            span=trimmed[-1].address - trimmed[0].address,
            score=score_candidate(trimmed, require),
            hits=trimmed,
        )

    collapsed = collapse_repeated_shapes(by_start.values())
    return sorted(collapsed, key=lambda c: (c.score, -c.repeat_count, -c.span), reverse=True)[: args.max_candidates]


def compact_to_required_span(window: tuple[Hit, ...], required: set[str], max_span: int) -> tuple[Hit, ...] | None:
    required_indexes = [idx for idx, hit in enumerate(window) if hit.name.lower() in required]
    if not required_indexes:
        return None

    best: tuple[Hit, ...] | None = None
    for start_index in required_indexes:
        names: set[str] = set()
        for end_index in range(start_index, len(window)):
            names.add(window[end_index].name.lower())
            span = window[end_index].address - window[start_index].address
            if span > max_span:
                break
            if required <= names:
                candidate = window[start_index : end_index + 1]
                if best is None or (candidate[-1].address - candidate[0].address) < (best[-1].address - best[0].address):
                    best = candidate
                break
    return best


def trim_window(window: tuple[Hit, ...], require: tuple[str, ...], max_hits: int) -> tuple[Hit, ...]:
    if len(window) <= max_hits:
        return window

    required_indexes = [idx for idx, hit in enumerate(window) if hit.name.lower() in require]
    if not required_indexes:
        return window[:max_hits]
    center = (required_indexes[0] + required_indexes[-1]) // 2
    half = max_hits // 2
    start = max(0, center - half)
    end = min(len(window), start + max_hits)
    start = max(0, end - max_hits)
    return window[start:end]


def score_candidate(hits: tuple[Hit, ...], require: tuple[str, ...]) -> float:
    names = {hit.name.lower() for hit in hits}
    span = hits[-1].address - hits[0].address
    required_hits = sum(1 for hit in hits if hit.name.lower() in require)
    extra_names = max(0, len(names) - len(require))
    score = 1000.0
    score += 120.0 * len(names & set(require))
    score += 15.0 * required_hits
    score -= span / 8.0
    score -= max(0, len(hits) - len(require)) * 18.0
    score -= 130.0 * extra_names
    if len(names) > 4:
        score -= 120.0
    return score


def collapse_repeated_shapes(candidates: Iterable[Candidate]) -> list[Candidate]:
    by_shape: dict[tuple[tuple[str, int], ...], tuple[Candidate, int]] = {}
    for candidate in candidates:
        shape = tuple((hit.name, hit.address - candidate.start) for hit in candidate.hits)
        current = by_shape.get(shape)
        if current is None or candidate.score > current[0].score:
            by_shape[shape] = (candidate, (current[1] + 1) if current else 1)
        else:
            by_shape[shape] = (current[0], current[1] + 1)

    collapsed: list[Candidate] = []
    for candidate, count in by_shape.values():
        repeat_penalty = min(300.0, max(0, count - 1) * 6.0)
        collapsed.append(
            Candidate(
                start=candidate.start,
                end=candidate.end,
                span=candidate.span,
                score=candidate.score - repeat_penalty,
                hits=candidate.hits,
                repeat_count=count,
            )
        )
    return collapsed


def render_report(
    pid: int,
    units: list[UnitPtr],
    hits_by_name: dict[str, list[Hit]],
    candidates: list[Candidate],
    require: tuple[str, ...],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    lines: list[str] = []
    lines.append("# Live Action Context Records")
    lines.append("")
    lines.append(f"- PID: `{pid}`")
    lines.append(f"- Required names: `{', '.join(require)}`")
    lines.append(f"- Near window: `0x{args.near_bytes:X}`")
    lines.append(f"- Max span: `0x{args.max_span:X}`")
    lines.append(f"- Regions scanned: `{stats.get('regions_scanned', 0)}`")
    lines.append(f"- Bytes scanned: `{stats.get('bytes_scanned', 0):,}`")
    lines.append("")
    lines.append("## Units")
    for unit in units:
        lines.append(f"- `{unit.name}` = `0x{unit.ptr:X}` ({unit.source})")
    lines.append("")
    lines.append("## Hit Counts")
    for name in sorted(hits_by_name):
        lines.append(f"- `{name}` hits `{len(hits_by_name[name])}`")
    lines.append("")
    lines.append("## Candidate Records")
    if not candidates:
        lines.append("No compact records contained all required units.")
        return "\n".join(lines)

    lines.append("| Start | Span | Score | Repeats | Names | Hits | Region |")
    lines.append("| --- | ---: | ---: | ---: | --- | --- | --- |")
    for candidate in candidates:
        hits = ", ".join(f"{hit.name}@+0x{hit.address - candidate.start:X}" for hit in candidate.hits)
        first = candidate.hits[0]
        region = (
            f"base=0x{first.region_base:X} size=0x{first.region_size:X} "
            f"protect=0x{first.protect:X} type=0x{first.mem_type:X}"
        )
        lines.append(
            f"| `0x{candidate.start:X}` | `0x{candidate.span:X}` | `{candidate.score:.1f}` | `{candidate.repeat_count}` | "
            f"`{', '.join(candidate.names)}` | `{hits}` | `{region}` |"
        )
    lines.append("")
    return "\n".join(lines)


def render_json(
    pid: int,
    units: list[UnitPtr],
    hits_by_name: dict[str, list[Hit]],
    candidates: list[Candidate],
    require: tuple[str, ...],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    payload = {
        "pid": pid,
        "required": list(require),
        "nearBytes": args.near_bytes,
        "maxSpan": args.max_span,
        "stats": stats,
        "units": [{"name": unit.name, "ptr": unit.ptr, "source": unit.source} for unit in units],
        "hitCounts": {name: len(hits) for name, hits in hits_by_name.items()},
        "candidates": [
            {
                "start": candidate.start,
                "end": candidate.end,
                "span": candidate.span,
                "score": candidate.score,
                "repeatCount": candidate.repeat_count,
                "names": list(candidate.names),
                "regionBase": candidate.hits[0].region_base,
                "regionSize": candidate.hits[0].region_size,
                "protect": candidate.hits[0].protect,
                "memType": candidate.hits[0].mem_type,
                "hits": [
                    {
                        "name": hit.name,
                        "ptr": hit.ptr,
                        "address": hit.address,
                        "relative": hit.address - candidate.start,
                    }
                    for hit in candidate.hits
                ],
            }
            for candidate in candidates
        ],
    }
    return json.dumps(payload, indent=2)


if __name__ == "__main__":
    raise SystemExit(main())
