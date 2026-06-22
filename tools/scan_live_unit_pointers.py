#!/usr/bin/env python3
"""Read-only external pointer scanner for live FFT_enhanced battle context RE.

The intended use is a paused/idle live battle after a charged action has been scheduled but before
it resolves. The scanner reads committed process memory and reports places where known battle-unit
pointers appear close together, e.g. caster + target inside a pending-action object.
"""
from __future__ import annotations

import argparse
import ctypes
import json
import re
import struct
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_LOG = Path(
    r"D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt"
)
DEFAULT_OUT = ROOT / "work" / "live_unit_pointer_scan.md"

PROCESS_QUERY_INFORMATION = 0x0400
PROCESS_VM_READ = 0x0010
MEM_COMMIT = 0x1000
MEM_PRIVATE = 0x20000
MEM_MAPPED = 0x40000
MEM_IMAGE = 0x1000000
PAGE_NOACCESS = 0x01
PAGE_GUARD = 0x100
PAGE_EXECUTE = 0x10
PAGE_EXECUTE_READ = 0x20
PAGE_EXECUTE_READWRITE = 0x40
PAGE_EXECUTE_WRITECOPY = 0x80

UNIT_RE = re.compile(r"\[UNIT ptr=(0x[0-9A-F]+) id=(0x[0-9A-F]{2}) .*?\] (?P<stats>.+)")


class MEMORY_BASIC_INFORMATION(ctypes.Structure):
    _fields_ = [
        ("BaseAddress", ctypes.c_void_p),
        ("AllocationBase", ctypes.c_void_p),
        ("AllocationProtect", ctypes.c_ulong),
        ("PartitionId", ctypes.c_ushort),
        ("RegionSize", ctypes.c_size_t),
        ("State", ctypes.c_ulong),
        ("Protect", ctypes.c_ulong),
        ("Type", ctypes.c_ulong),
    ]


kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
OpenProcess = kernel32.OpenProcess
OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_bool, ctypes.c_ulong]
OpenProcess.restype = ctypes.c_void_p
CloseHandle = kernel32.CloseHandle
CloseHandle.argtypes = [ctypes.c_void_p]
VirtualQueryEx = kernel32.VirtualQueryEx
VirtualQueryEx.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.POINTER(MEMORY_BASIC_INFORMATION), ctypes.c_size_t]
VirtualQueryEx.restype = ctypes.c_size_t
ReadProcessMemory = kernel32.ReadProcessMemory
ReadProcessMemory.argtypes = [
    ctypes.c_void_p,
    ctypes.c_void_p,
    ctypes.c_void_p,
    ctypes.c_size_t,
    ctypes.POINTER(ctypes.c_size_t),
]
ReadProcessMemory.restype = ctypes.c_bool


@dataclass(frozen=True)
class UnitPtr:
    name: str
    ptr: int
    source: str


@dataclass(frozen=True)
class Hit:
    name: str
    ptr: int
    address: int
    region_base: int
    region_size: int
    protect: int
    mem_type: int


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Scan live FFT_enhanced memory for known battle-unit pointers.")
    p.add_argument("--process-name", default="FFT_enhanced.exe")
    p.add_argument("--pid", type=int, default=0)
    p.add_argument("--log", type=Path, default=DEFAULT_LOG)
    p.add_argument("--unit", action="append", default=[], help="Named unit pointer, e.g. Cloud=0x1418562E0.")
    p.add_argument(
        "--unit-id",
        action="append",
        default=[],
        help="Resolve from latest [UNIT] log line by id, e.g. Cloud=0x32. Repeatable.",
    )
    p.add_argument("--near-bytes", type=lambda s: int(s, 0), default=0x400)
    p.add_argument("--chunk-size", type=lambda s: int(s, 0), default=0x400000)
    p.add_argument("--max-region-size", type=lambda s: int(s, 0), default=0x20000000)
    p.add_argument("--max-hits-per-unit", type=int, default=20000)
    p.add_argument("--max-groups", type=int, default=80)
    p.add_argument("--include-exec", action="store_true")
    p.add_argument("--include-image", action="store_true")
    p.add_argument("--output", type=Path, default=DEFAULT_OUT)
    p.add_argument("--json-output", type=Path, default=None)
    p.add_argument(
        "--json-include-hits",
        action="store_true",
        help="Include raw pointer hit addresses in JSON output. This can be large, but is useful for live RE diffs.",
    )
    p.add_argument(
        "--json-max-hits-per-unit",
        type=int,
        default=0,
        help="Maximum raw hits per unit in JSON when --json-include-hits is set. 0 means all collected hits.",
    )
    return p.parse_args()


def main() -> int:
    args = parse_args()
    units = resolve_units(args)
    if len(units) < 1:
        raise SystemExit("no unit pointers resolved; pass --unit Name=0xPTR or --unit-id Name=0xID")

    pid = args.pid or find_pid(args.process_name)
    if not pid:
        raise SystemExit(f"process not found: {args.process_name}")

    handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, False, pid)
    if not handle:
        raise SystemExit(f"OpenProcess failed for pid={pid}: win32={ctypes.get_last_error()}")

    try:
        hits, stats = scan_process(handle, units, args)
    finally:
        CloseHandle(handle)

    groups = find_nearby_groups(hits, args.near_bytes, args.max_groups)
    report = render_report(pid, units, hits, groups, stats, args)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(render_json(pid, units, hits, groups, stats, args), encoding="utf-8")
    print(f"wrote {args.output}")
    if args.json_output:
        print(f"wrote {args.json_output}")
    print(f"hits: {sum(len(v) for v in hits.values())}; nearby groups: {len(groups)}")
    return 0


def resolve_units(args: argparse.Namespace) -> list[UnitPtr]:
    units: list[UnitPtr] = []
    for spec in args.unit:
        name, ptr = parse_named_value(spec, "unit")
        units.append(UnitPtr(name, ptr, "cli"))

    if args.unit_id:
        latest = latest_units_by_id(args.log)
        for spec in args.unit_id:
            name, unit_id = parse_named_value(spec, "unit-id")
            key = f"0X{unit_id:02X}"
            ptr = latest.get(key)
            if ptr is None:
                print(f"warning: no latest [UNIT] line for {name} id={key} in {args.log}", file=sys.stderr)
                continue
            units.append(UnitPtr(name, ptr, f"log-id:{key}"))

    dedup: dict[str, UnitPtr] = {}
    for unit in units:
        dedup[unit.name.lower()] = unit
    return list(dedup.values())


def parse_named_value(spec: str, label: str) -> tuple[str, int]:
    name, sep, raw = spec.partition("=")
    if not sep or not name.strip() or not raw.strip():
        raise SystemExit(f"invalid --{label} {spec!r}; expected Name=0xVALUE")
    return name.strip(), int(raw.strip(), 0)


def latest_units_by_id(path: Path) -> dict[str, int]:
    latest: dict[str, int] = {}
    if not path.exists():
        return latest
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        if m := UNIT_RE.search(line):
            latest[m.group(2).upper()] = int(m.group(1), 16)
    return latest


def find_pid(process_name: str) -> int:
    # tasklist is available on Windows and avoids an extra dependency such as psutil.
    result = subprocess.run(
        ["tasklist", "/FI", f"IMAGENAME eq {process_name}", "/FO", "CSV", "/NH"],
        check=False,
        capture_output=True,
        text=True,
    )
    for line in result.stdout.splitlines():
        parts = [part.strip().strip('"') for part in line.split(",")]
        if len(parts) >= 2 and parts[0].lower() == process_name.lower():
            try:
                return int(parts[1])
            except ValueError:
                continue
    return 0


def scan_process(handle: int, units: list[UnitPtr], args: argparse.Namespace) -> tuple[dict[str, list[Hit]], dict[str, int]]:
    patterns = {unit.name: struct.pack("<Q", unit.ptr) for unit in units}
    hit_counts = defaultdict(int)
    hits: dict[str, list[Hit]] = defaultdict(list)
    stats = defaultdict(int)
    address = 0
    mbi = MEMORY_BASIC_INFORMATION()
    mbi_size = ctypes.sizeof(MEMORY_BASIC_INFORMATION)

    while VirtualQueryEx(handle, ctypes.c_void_p(address), ctypes.byref(mbi), mbi_size):
        base = int(mbi.BaseAddress or 0)
        size = int(mbi.RegionSize or 0)
        next_address = base + max(size, 0x1000)
        if is_scannable_region(mbi, args):
            stats["regions_scanned"] += 1
            stats["bytes_scanned"] += size
            scan_region(handle, base, size, mbi.Protect, mbi.Type, patterns, hit_counts, hits, args)
        else:
            stats["regions_skipped"] += 1
        if next_address <= address:
            break
        address = next_address

    return hits, dict(stats)


def is_scannable_region(mbi: MEMORY_BASIC_INFORMATION, args: argparse.Namespace) -> bool:
    if mbi.State != MEM_COMMIT:
        return False
    if mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD):
        return False
    if not args.include_exec and (mbi.Protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)):
        return False
    if not args.include_image and mbi.Type == MEM_IMAGE:
        return False
    if int(mbi.RegionSize or 0) <= 0 or int(mbi.RegionSize or 0) > args.max_region_size:
        return False
    return mbi.Type in {MEM_PRIVATE, MEM_MAPPED, MEM_IMAGE}


def scan_region(
    handle: int,
    base: int,
    size: int,
    protect: int,
    mem_type: int,
    patterns: dict[str, bytes],
    hit_counts: dict[str, int],
    hits: dict[str, list[Hit]],
    args: argparse.Namespace,
) -> None:
    chunk_size = max(0x1000, args.chunk_size)
    overlap = 7
    offset = 0
    while offset < size:
        read_size = min(chunk_size, size - offset)
        data = read_memory(handle, base + offset, read_size)
        if data:
            for name, pattern in patterns.items():
                if hit_counts[name] >= args.max_hits_per_unit:
                    continue
                start = 0
                while True:
                    found = data.find(pattern, start)
                    if found < 0:
                        break
                    hits[name].append(Hit(name, struct.unpack("<Q", pattern)[0], base + offset + found, base, size, protect, mem_type))
                    hit_counts[name] += 1
                    if hit_counts[name] >= args.max_hits_per_unit:
                        break
                    start = found + 1
        if read_size <= overlap:
            break
        offset += read_size - overlap


def read_memory(handle: int, address: int, size: int) -> bytes:
    buffer = ctypes.create_string_buffer(size)
    bytes_read = ctypes.c_size_t()
    if not ReadProcessMemory(handle, ctypes.c_void_p(address), buffer, size, ctypes.byref(bytes_read)):
        return b""
    return buffer.raw[: bytes_read.value]


def find_nearby_groups(hits: dict[str, list[Hit]], near_bytes: int, max_groups: int) -> list[list[Hit]]:
    all_hits = sorted((hit for unit_hits in hits.values() for hit in unit_hits), key=lambda hit: hit.address)
    groups: list[list[Hit]] = []
    left = 0
    for right, hit in enumerate(all_hits):
        while all_hits[left].address < hit.address - near_bytes:
            left += 1
        window = all_hits[left : right + 1]
        names = {candidate.name for candidate in window}
        if len(names) >= 2:
            groups.append(window)
            if len(groups) >= max_groups:
                break
    return compact_groups(groups)


def compact_groups(groups: list[list[Hit]]) -> list[list[Hit]]:
    compact: list[list[Hit]] = []
    seen: set[tuple[int, int, tuple[str, ...]]] = set()
    for group in groups:
        start = min(hit.address for hit in group)
        end = max(hit.address for hit in group)
        names = tuple(sorted({hit.name for hit in group}))
        key = (start // 0x20, end // 0x20, names)
        if key in seen:
            continue
        seen.add(key)
        compact.append(group)
    return compact


def render_report(
    pid: int,
    units: list[UnitPtr],
    hits: dict[str, list[Hit]],
    groups: list[list[Hit]],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    lines: list[str] = []
    lines.append("# Live Unit Pointer Scan")
    lines.append("")
    lines.append(f"- PID: `{pid}`")
    lines.append(f"- Near window: `0x{args.near_bytes:X}`")
    lines.append(f"- Regions scanned: `{stats.get('regions_scanned', 0)}`")
    lines.append(f"- Regions skipped: `{stats.get('regions_skipped', 0)}`")
    lines.append(f"- Bytes scanned: `{stats.get('bytes_scanned', 0):,}`")
    lines.append("")
    lines.append("## Units")
    for unit in units:
        lines.append(f"- `{unit.name}` = `0x{unit.ptr:X}` ({unit.source}), hits `{len(hits.get(unit.name, []))}`")
    lines.append("")
    lines.append("## Nearby Groups")
    if not groups:
        lines.append("No groups with two or more requested unit pointers inside the near window.")
    else:
        lines.append("| Start | Span | Names | Hits | Region |")
        lines.append("| --- | ---: | --- | --- | --- |")
        for group in groups:
            start = min(hit.address for hit in group)
            end = max(hit.address for hit in group)
            names = ", ".join(sorted({hit.name for hit in group}))
            hit_text = ", ".join(f"{hit.name}@0x{hit.address:X}" for hit in group[:12])
            region = group[0]
            lines.append(
                f"| `0x{start:X}` | `0x{end - start:X}` | `{names}` | `{hit_text}` | "
                f"`base=0x{region.region_base:X} size=0x{region.region_size:X} protect=0x{region.protect:X} type=0x{region.mem_type:X}` |"
            )
    lines.append("")
    lines.append("## First Hits")
    for name, unit_hits in hits.items():
        lines.append(f"### {name}")
        for hit in unit_hits[:20]:
            lines.append(
                f"- `0x{hit.address:X}` in region `0x{hit.region_base:X}+0x{hit.region_size:X}` "
                f"protect `0x{hit.protect:X}` type `0x{hit.mem_type:X}`"
            )
        if len(unit_hits) > 20:
            lines.append(f"- ... {len(unit_hits) - 20} more")
    lines.append("")
    return "\n".join(lines)


def render_json(
    pid: int,
    units: list[UnitPtr],
    hits: dict[str, list[Hit]],
    groups: list[list[Hit]],
    stats: dict[str, int],
    args: argparse.Namespace,
) -> str:
    payload = {
        "pid": pid,
        "nearBytes": args.near_bytes,
        "stats": stats,
        "units": [
            {"name": unit.name, "ptr": unit.ptr, "source": unit.source, "hits": len(hits.get(unit.name, []))}
            for unit in units
        ],
        "groups": [
            {
                "start": min(hit.address for hit in group),
                "end": max(hit.address for hit in group),
                "span": max(hit.address for hit in group) - min(hit.address for hit in group),
                "names": sorted({hit.name for hit in group}),
                "regionBase": group[0].region_base,
                "regionSize": group[0].region_size,
                "protect": group[0].protect,
                "memType": group[0].mem_type,
                "hits": [
                    {"name": hit.name, "address": hit.address, "ptr": hit.ptr}
                    for hit in group
                ],
            }
            for group in groups
        ],
    }
    if args.json_include_hits:
        limit = int(args.json_max_hits_per_unit or 0)
        payload["rawHits"] = {
            name: [
                {
                    "address": hit.address,
                    "ptr": hit.ptr,
                    "regionBase": hit.region_base,
                    "regionSize": hit.region_size,
                    "protect": hit.protect,
                    "memType": hit.mem_type,
                }
                for hit in (unit_hits[:limit] if limit > 0 else unit_hits)
            ]
            for name, unit_hits in hits.items()
        }
        payload["rawHitsTruncated"] = {
            name: max(0, len(unit_hits) - limit) if limit > 0 else 0
            for name, unit_hits in hits.items()
        }
    return json.dumps(payload, indent=2, sort_keys=True)


if __name__ == "__main__":
    raise SystemExit(main())
