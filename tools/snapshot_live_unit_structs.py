#!/usr/bin/env python3
"""Snapshot known live battle-unit structs for breakpoint-by-breakpoint diffs."""
from __future__ import annotations

import argparse
import ctypes
import json
import struct
from pathlib import Path

from scan_live_unit_pointers import CloseHandle, DEFAULT_LOG, OpenProcess, ReadProcessMemory, find_pid, resolve_units

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT = ROOT / "work" / "live_unit_struct_snapshot.md"


KNOWN_FIELDS = [
    ("charId", 0x00, "u8"),
    ("team", 0x04, "u8"),
    ("level", 0x29, "u8"),
    ("brave", 0x2B, "u8"),
    ("faith", 0x2D, "u8"),
    ("hp", 0x30, "u16"),
    ("maxHp", 0x32, "u16"),
    ("mp", 0x34, "u16"),
    ("maxMp", 0x36, "u16"),
    ("pa", 0x3E, "u8"),
    ("ma", 0x3F, "u8"),
    ("speed", 0x40, "u8"),
    ("ct", 0x41, "u8"),
    ("status", 0x61, "u8"),
]


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Snapshot live FFT battle-unit structs.")
    p.add_argument("--process-name", default="FFT_enhanced.exe")
    p.add_argument("--pid", type=int, default=0)
    p.add_argument("--log", type=Path, default=DEFAULT_LOG)
    p.add_argument("--unit", action="append", default=[], help="Named unit pointer, e.g. Cloud=0x1418562E0.")
    p.add_argument("--unit-id", action="append", default=[], help="Resolve from latest [UNIT] line, e.g. Cloud=0x32.")
    p.add_argument("--bytes", type=lambda s: int(s, 0), default=0x200)
    p.add_argument("--previous-json", type=Path, default=None)
    p.add_argument("--output", type=Path, default=DEFAULT_OUT)
    p.add_argument("--json-output", type=Path, default=None)
    return p.parse_args()


def main() -> int:
    args = parse_args()
    units = resolve_units(args)
    if not units:
        raise SystemExit("no units configured")

    pid = args.pid or find_pid(args.process_name)
    if not pid:
        raise SystemExit(f"process not found: {args.process_name}")

    handle = OpenProcess(0x0400 | 0x0010, False, pid)
    if not handle:
        raise SystemExit(f"OpenProcess failed for pid={pid}")
    try:
        snapshots = []
        for unit in units:
            data = read_memory(handle, unit.ptr, args.bytes)
            snapshots.append(
                {
                    "name": unit.name,
                    "ptr": unit.ptr,
                    "source": unit.source,
                    "bytes": list(data),
                    "fields": parse_fields(data),
                }
            )
    finally:
        CloseHandle(handle)

    previous = load_previous(args.previous_json)
    payload = {
        "pid": pid,
        "size": args.bytes,
        "units": snapshots,
        "diffs": build_diffs(previous, snapshots),
    }

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(render_report(payload), encoding="utf-8")
    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(json.dumps(payload, indent=2, sort_keys=True), encoding="utf-8")

    print(f"wrote {args.output}")
    if args.json_output:
        print(f"wrote {args.json_output}")
    return 0


def read_memory(handle: int, address: int, size: int) -> bytes:
    buffer = ctypes.create_string_buffer(size)
    bytes_read = ctypes.c_size_t()
    if not ReadProcessMemory(handle, ctypes.c_void_p(address), buffer, size, ctypes.byref(bytes_read)):
        raise OSError(f"ReadProcessMemory failed at 0x{address:X}")
    return bytes(buffer.raw[: bytes_read.value])


def parse_fields(data: bytes) -> dict[str, int]:
    result: dict[str, int] = {}
    for name, offset, encoding in KNOWN_FIELDS:
        if offset >= len(data):
            continue
        if encoding == "u8":
            result[name] = data[offset]
        elif encoding == "u16" and offset + 2 <= len(data):
            result[name] = struct.unpack_from("<H", data, offset)[0]
    return result


def load_previous(path: Path | None) -> dict[str, list[int]]:
    if not path or not path.exists():
        return {}
    payload = json.loads(path.read_text(encoding="utf-8"))
    return {unit["name"]: unit["bytes"] for unit in payload.get("units", [])}


def build_diffs(previous: dict[str, list[int]], snapshots: list[dict]) -> dict[str, list[dict]]:
    diffs: dict[str, list[dict]] = {}
    for snapshot in snapshots:
        old = previous.get(snapshot["name"])
        if old is None:
            continue
        new = snapshot["bytes"]
        unit_diffs = []
        for offset, (before, after) in enumerate(zip(old, new)):
            if before != after:
                unit_diffs.append({"offset": offset, "before": before, "after": after})
        diffs[snapshot["name"]] = unit_diffs
    return diffs


def render_report(payload: dict) -> str:
    lines: list[str] = []
    lines.append("# Live Unit Struct Snapshot")
    lines.append("")
    lines.append(f"- PID: `{payload['pid']}`")
    lines.append(f"- Bytes per unit: `0x{payload['size']:X}`")
    lines.append("")
    lines.append("## Units")
    for unit in payload["units"]:
        fields = ", ".join(f"{key}={value}" for key, value in unit["fields"].items())
        lines.append(f"- `{unit['name']}` ptr `0x{unit['ptr']:X}`: {fields}")
    lines.append("")
    lines.append("## Diffs")
    if not payload["diffs"]:
        lines.append("No previous snapshot was provided.")
    else:
        for unit_name, diffs in payload["diffs"].items():
            lines.append(f"### {unit_name}")
            if not diffs:
                lines.append("- No byte changes.")
                continue
            for item in diffs[:160]:
                lines.append(
                    f"- `+0x{item['offset']:03X}`: `0x{item['before']:02X}` -> `0x{item['after']:02X}` "
                    f"({item['before']} -> {item['after']})"
                )
            if len(diffs) > 160:
                lines.append(f"- ... {len(diffs) - 160} more byte changes omitted.")
    lines.append("")
    lines.append("## Hex")
    for unit in payload["units"]:
        lines.append(f"### {unit['name']}")
        data = bytes(unit["bytes"])
        for offset in range(0, len(data), 16):
            chunk = data[offset : offset + 16]
            hex_bytes = " ".join(f"{byte:02X}" for byte in chunk)
            lines.append(f"`+0x{offset:03X}` {hex_bytes}")
        lines.append("")
    return "\n".join(lines)


if __name__ == "__main__":
    raise SystemExit(main())
