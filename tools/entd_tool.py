#!/usr/bin/env python3
"""
Small ENTD helper for FINAL FANTASY TACTICS - The Ivalice Chronicles.

The remaster keeps classic ENTD unit records in fftpack/battle_entd*_ent.bin.
Each file has 128 entries. Each entry has 16 unit slots. Each slot is 0x28 bytes.

Known offsets are based on public FFT ENTD documentation plus local v1.5.0 files.
This tool intentionally writes only copied output files; it never edits the source in place.
"""

from __future__ import annotations

import argparse
import csv
from dataclasses import dataclass
from pathlib import Path
from struct import unpack_from


ENTRY_COUNT_PER_FILE = 128
ENTRY_SIZE = 0x280
SLOT_COUNT = 16
SLOT_SIZE = 0x28
LEVEL_OFFSET = 0x03


@dataclass(frozen=True)
class UnitRecord:
    global_entry: int
    file_no: int
    local_entry: int
    slot: int
    offset: int
    sprite: int
    flags: int
    name_id: int
    level: int
    birthday_month: int
    birthday_day: int
    bravery: int
    faith: int
    job_unlock: int
    job_level: int
    main_job: int
    secondary_skillset: int
    reaction: int
    support: int
    movement: int
    equipment: tuple[int, int, int, int, int]
    raw_tail: str

    @property
    def is_presentish(self) -> bool:
        return any(
            [
                self.sprite,
                self.flags,
                self.level,
                self.main_job,
                self.job_level,
                self.bravery,
                self.faith,
            ]
        )

    @property
    def sex_or_type(self) -> str:
        parts: list[str] = []
        if self.flags & 0x80:
            parts.append("male")
        if self.flags & 0x40:
            parts.append("female")
        if self.flags & 0x20:
            parts.append("monster")
        return "+".join(parts) if parts else "-"


def file_no_for_entry(global_entry: int) -> int:
    if global_entry < 0 or global_entry >= ENTRY_COUNT_PER_FILE * 4:
        raise ValueError("global entry must be in range 0..511")
    return global_entry // ENTRY_COUNT_PER_FILE + 1


def local_entry_for_entry(global_entry: int) -> int:
    return global_entry % ENTRY_COUNT_PER_FILE


def expected_file_name(global_entry: int) -> str:
    return f"battle_entd{file_no_for_entry(global_entry)}_ent.bin"


def read_entry(path: Path, global_entry: int) -> list[UnitRecord]:
    data = path.read_bytes()
    if len(data) != ENTRY_COUNT_PER_FILE * ENTRY_SIZE:
        raise ValueError(f"{path} is {len(data)} bytes; expected {ENTRY_COUNT_PER_FILE * ENTRY_SIZE}")

    file_no = file_no_for_entry(global_entry)
    local_entry = local_entry_for_entry(global_entry)
    entry_offset = local_entry * ENTRY_SIZE
    units: list[UnitRecord] = []

    for slot in range(SLOT_COUNT):
        offset = entry_offset + slot * SLOT_SIZE
        rec = data[offset : offset + SLOT_SIZE]
        equipment = tuple(rec[0x12:0x17])
        units.append(
            UnitRecord(
                global_entry=global_entry,
                file_no=file_no,
                local_entry=local_entry,
                slot=slot,
                offset=offset,
                sprite=rec[0x00],
                flags=rec[0x01],
                name_id=rec[0x02],
                level=rec[0x03],
                birthday_month=rec[0x04],
                birthday_day=rec[0x05],
                bravery=rec[0x06],
                faith=rec[0x07],
                job_unlock=rec[0x08],
                job_level=rec[0x09],
                main_job=rec[0x0A],
                secondary_skillset=rec[0x0B],
                reaction=unpack_from("<H", rec, 0x0C)[0],
                support=unpack_from("<H", rec, 0x0E)[0],
                movement=unpack_from("<H", rec, 0x10)[0],
                equipment=equipment,  # head/body/accessory/right/left, assumed from override layout order
                raw_tail=rec[0x17:].hex(" "),
            )
        )

    return units


def print_units(units: list[UnitRecord], include_empty: bool) -> None:
    shown = [u for u in units if include_empty or u.is_presentish]
    print(
        "entry file local slot offset sprite flags type name level brave faith "
        "job_unlock job_level main_job secondary reaction support movement equipment raw_tail"
    )
    for u in shown:
        print(
            f"{u.global_entry:03d} entd{u.file_no} {u.local_entry:03d} {u.slot:02d} "
            f"0x{u.offset:05X} {u.sprite:3d} 0x{u.flags:02X} {u.sex_or_type:13s} "
            f"{u.name_id:3d} {u.level:3d} {u.bravery:3d} {u.faith:3d} "
            f"{u.job_unlock:3d} {u.job_level:3d} {u.main_job:3d} {u.secondary_skillset:3d} "
            f"{u.reaction:5d} {u.support:5d} {u.movement:5d} "
            f"{','.join(str(x) for x in u.equipment):15s} {u.raw_tail}"
        )


def export_csv(units: list[UnitRecord], output: Path, include_empty: bool) -> None:
    shown = [u for u in units if include_empty or u.is_presentish]
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "global_entry",
                "file_no",
                "local_entry",
                "slot",
                "offset_hex",
                "sprite",
                "flags_hex",
                "type",
                "name_id",
                "level",
                "bravery",
                "faith",
                "job_unlock",
                "job_level",
                "main_job",
                "secondary_skillset",
                "reaction",
                "support",
                "movement",
                "equipment",
                "raw_tail",
            ],
        )
        writer.writeheader()
        for u in shown:
            writer.writerow(
                {
                    "global_entry": u.global_entry,
                    "file_no": u.file_no,
                    "local_entry": u.local_entry,
                    "slot": u.slot,
                    "offset_hex": f"0x{u.offset:05X}",
                    "sprite": u.sprite,
                    "flags_hex": f"0x{u.flags:02X}",
                    "type": u.sex_or_type,
                    "name_id": u.name_id,
                    "level": u.level,
                    "bravery": u.bravery,
                    "faith": u.faith,
                    "job_unlock": u.job_unlock,
                    "job_level": u.job_level,
                    "main_job": u.main_job,
                    "secondary_skillset": u.secondary_skillset,
                    "reaction": u.reaction,
                    "support": u.support,
                    "movement": u.movement,
                    "equipment": ",".join(str(x) for x in u.equipment),
                    "raw_tail": u.raw_tail,
                }
            )


def patch_levels(input_path: Path, output_path: Path, global_entry: int, slots: list[int], level: int) -> None:
    if not (1 <= level <= 255):
        raise ValueError("level byte must be in range 1..255")
    for slot in slots:
        if slot < 0 or slot >= SLOT_COUNT:
            raise ValueError("slots must be in range 0..15")

    data = bytearray(input_path.read_bytes())
    if len(data) != ENTRY_COUNT_PER_FILE * ENTRY_SIZE:
        raise ValueError(f"{input_path} is {len(data)} bytes; expected {ENTRY_COUNT_PER_FILE * ENTRY_SIZE}")

    local_entry = local_entry_for_entry(global_entry)
    for slot in slots:
        absolute = local_entry * ENTRY_SIZE + slot * SLOT_SIZE + LEVEL_OFFSET
        old = data[absolute]
        data[absolute] = level
        print(f"patched entry {global_entry} slot {slot}: level {old} -> {level} at 0x{absolute:05X}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_bytes(data)
    print(f"wrote {output_path}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspect and patch FFT TIC ENTD files.")
    sub = parser.add_subparsers(dest="command", required=True)

    dump = sub.add_parser("dump-entry", help="Dump one global ENTD entry.")
    dump.add_argument("--input", type=Path, required=True)
    dump.add_argument("--entry", type=int, required=True)
    dump.add_argument("--include-empty", action="store_true")
    dump.add_argument("--csv", type=Path)

    patch = sub.add_parser("patch-levels", help="Copy an ENTD file and patch level bytes.")
    patch.add_argument("--input", type=Path, required=True)
    patch.add_argument("--output", type=Path, required=True)
    patch.add_argument("--entry", type=int, required=True)
    patch.add_argument("--slots", type=int, nargs="+", required=True)
    patch.add_argument("--level", type=int, default=100)

    args = parser.parse_args()

    if args.command == "dump-entry":
        expected = expected_file_name(args.entry)
        if args.input.name.lower() != expected:
            print(f"warning: entry {args.entry} normally lives in {expected}, got {args.input.name}")
        units = read_entry(args.input, args.entry)
        print_units(units, args.include_empty)
        if args.csv:
            export_csv(units, args.csv, args.include_empty)
            print(f"wrote {args.csv}")
        return 0

    if args.command == "patch-levels":
        expected = expected_file_name(args.entry)
        if args.input.name.lower() != expected:
            print(f"warning: entry {args.entry} normally lives in {expected}, got {args.input.name}")
        patch_levels(args.input, args.output, args.entry, args.slots, args.level)
        return 0

    raise AssertionError(args.command)


if __name__ == "__main__":
    raise SystemExit(main())
