#!/usr/bin/env python3
"""Build an audited Enhanced autosave with bounded live secondary-command edits."""

from __future__ import annotations

import argparse
import shutil
import tempfile
from dataclasses import dataclass
from pathlib import Path

import build_fft_autosave_ct_fixture as fixturefmt
import build_fft_autosave_reaction_fixture as savefmt
import build_fft_manual_ability_fixture as manualfmt


REPO = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = REPO / "work"
SECONDARY_COMMAND_OFFSET = 0x13
SECONDARY_COMMAND_LIVE_COMPONENTS = (
    "resume_en00_main.sav",
    "resume_en00_fturn.sav",
    "resume_en01_main.sav",
    "resume_en01_fturn.sav",
    "resume_enbtl_main.sav",
)


@dataclass(frozen=True)
class CommandEdit:
    label: str
    signature: bytes
    expected_command: int
    new_command: int


@dataclass(frozen=True)
class AppliedEdit:
    component: str
    edit: CommandEdit
    record_offset: int

    @property
    def command_offset(self) -> int:
        return self.record_offset + SECONDARY_COMMAND_OFFSET


@dataclass(frozen=True)
class UnitByteEdit:
    label: str
    signature: bytes
    relative_offset: int
    expected_value: int
    new_value: int


@dataclass(frozen=True)
class AppliedUnitByteEdit:
    component: str
    edit: UnitByteEdit
    record_offset: int

    @property
    def byte_offset(self) -> int:
        return self.record_offset + self.edit.relative_offset


def parse_edit(value: str) -> CommandEdit:
    parts = value.split(":")
    if len(parts) != 4:
        raise argparse.ArgumentTypeError(
            "command edit must be LABEL:HEX_SIGNATURE:EXPECTED_COMMAND:NEW_COMMAND"
        )
    label, signature_hex, expected_raw, new_raw = parts
    try:
        signature = bytes.fromhex(signature_hex)
        expected = int(expected_raw, 0)
        new = int(new_raw, 0)
    except ValueError as error:
        raise argparse.ArgumentTypeError(str(error)) from error
    if not label or len(signature) < 8:
        raise argparse.ArgumentTypeError("label is required and signature needs at least eight bytes")
    if not 0 <= expected <= 0xFF or not 0 <= new <= 0xFF:
        raise argparse.ArgumentTypeError("command ids must be within 0..255")
    return CommandEdit(label, signature, expected, new)


def parse_unit_byte(value: str) -> UnitByteEdit:
    parts = value.split(":")
    if len(parts) != 5:
        raise argparse.ArgumentTypeError(
            "unit byte edit must be LABEL:HEX_SIGNATURE:RELATIVE_OFFSET:EXPECTED_VALUE:NEW_VALUE"
        )
    label, signature_hex, offset_raw, expected_raw, new_raw = parts
    try:
        signature = bytes.fromhex(signature_hex)
        relative_offset = int(offset_raw, 0)
        expected = int(expected_raw, 0)
        new = int(new_raw, 0)
    except ValueError as error:
        raise argparse.ArgumentTypeError(str(error)) from error
    if not label or len(signature) < 8:
        raise argparse.ArgumentTypeError("label is required and signature needs at least eight bytes")
    if not 0 <= relative_offset < 0x258:
        raise argparse.ArgumentTypeError("relative unit offset must be within 0..0x257")
    if not 0 <= expected <= 0xFF or not 0 <= new <= 0xFF:
        raise argparse.ArgumentTypeError("unit byte values must be within 0..255")
    return UnitByteEdit(label, signature, relative_offset, expected, new)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a non-deployed, audited Enhanced autosave secondary-command fixture."
    )
    parser.add_argument("--prefix", required=True)
    parser.add_argument("--source-save", type=Path, required=True)
    parser.add_argument("--label", required=True)
    parser.add_argument("--edit", action="append", type=parse_edit, required=True)
    parser.add_argument("--unit-byte", action="append", type=parse_unit_byte, default=[])
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--ff16tools", type=Path, default=manualfmt.DEFAULT_FF16TOOLS)
    return parser.parse_args()


def stage(
    files: dict[str, bytes],
    edits: tuple[CommandEdit, ...],
    unit_byte_edits: tuple[UnitByteEdit, ...],
) -> tuple[dict[str, bytes], tuple[AppliedEdit, ...], tuple[AppliedUnitByteEdit, ...]]:
    savefmt.validate_current_aliases(files)
    savefmt.validate_battle_entry(files)
    staged = dict(files)
    applied: list[AppliedEdit] = []
    applied_unit_bytes: list[AppliedUnitByteEdit] = []
    for name in SECONDARY_COMMAND_LIVE_COMPONENTS:
        data = files[name]
        mutable = bytearray(data)
        for edit in edits:
            hits = fixturefmt.signature_hits(data, edit.signature)
            if len(hits) != 1:
                raise AssertionError(
                    f"{name} expected exactly one {edit.label} signature; found {len(hits)}"
                )
            item = AppliedEdit(name, edit, hits[0])
            actual = mutable[item.command_offset]
            if actual != edit.expected_command:
                raise AssertionError(
                    f"{name} {edit.label} command mismatch: "
                    f"expected {edit.expected_command}, found {actual}"
                )
            mutable[item.command_offset] = edit.new_command
            applied.append(item)
        for edit in unit_byte_edits:
            hits = fixturefmt.signature_hits(data, edit.signature)
            if len(hits) != 1:
                raise AssertionError(
                    f"{name} expected exactly one {edit.label} signature; found {len(hits)}"
                )
            item = AppliedUnitByteEdit(name, edit, hits[0])
            actual = mutable[item.byte_offset]
            if actual != edit.expected_value:
                raise AssertionError(
                    f"{name} {edit.label} byte mismatch at unit+0x{edit.relative_offset:X}: "
                    f"expected {edit.expected_value}, found {actual}"
                )
            mutable[item.byte_offset] = edit.new_value
            applied_unit_bytes.append(item)
        staged[name] = bytes(mutable)

    if staged["resume_en00_fturn.sav"] != staged["resume_enbtl_main.sav"]:
        raise AssertionError("command edits broke the current fturn/main alias equality")
    return staged, tuple(applied), tuple(applied_unit_bytes)


def audit(
    source: dict[str, bytes],
    roundtrip: dict[str, bytes],
    applied: tuple[AppliedEdit, ...],
    applied_unit_bytes: tuple[AppliedUnitByteEdit, ...],
) -> None:
    if set(source) != set(roundtrip):
        raise AssertionError("packed autosave member set changed")
    expected_by_component: dict[str, set[int]] = {}
    for item in applied:
        expected_by_component.setdefault(item.component, set()).add(item.command_offset)
    for item in applied_unit_bytes:
        expected_by_component.setdefault(item.component, set()).add(item.byte_offset)

    for name in sorted(source):
        before = source[name]
        after = roundtrip[name]
        if len(before) != len(after):
            raise AssertionError(f"{name} length changed after pack/unpack")
        changed = {index for index, pair in enumerate(zip(before, after)) if pair[0] != pair[1]}
        expected = expected_by_component.get(name, set())
        if expected:
            allowed = expected | set(fixturefmt.INNER_CHECKSUM_OFFSETS)
            if not expected.issubset(changed) or not changed.issubset(allowed):
                raise AssertionError(
                    f"{name} changed unexpected offsets: expected command {sorted(expected)}, "
                    f"got {sorted(changed)}"
                )
        elif changed:
            raise AssertionError(f"unchanged member {name} changed at {sorted(changed)}")

    for item in applied:
        if roundtrip[item.component][item.command_offset] != item.edit.new_command:
            raise AssertionError(f"{item.component} did not retain {item.edit.label} command")
    for item in applied_unit_bytes:
        if roundtrip[item.component][item.byte_offset] != item.edit.new_value:
            raise AssertionError(f"{item.component} did not retain {item.edit.label} unit byte")
    savefmt.validate_current_aliases(roundtrip)
    savefmt.validate_battle_entry(roundtrip)


def manifest_text(
    source_save: Path,
    fixture: Path,
    edits: tuple[CommandEdit, ...],
    applied: tuple[AppliedEdit, ...],
    unit_byte_edits: tuple[UnitByteEdit, ...],
    applied_unit_bytes: tuple[AppliedUnitByteEdit, ...],
) -> str:
    lines = [
        "# FFT Enhanced autosave secondary-command fixture",
        "",
        "## Scope",
        "",
        "This non-deployed fixture changes only `unit+0x13`, the live secondary-command id,",
        "for exact unit signatures in every current-battle command-bearing copy.",
        "",
        "## Source",
        "",
        f"- Source autosave: `{source_save}`",
        f"- Source SHA-256: `{fixturefmt.sha256(source_save)}`",
        "",
        "## Requested edits",
        "",
    ]
    for edit in edits:
        lines.append(
            f"- `{edit.label}` signature `{edit.signature.hex().upper()}`: secondary command "
            f"`{edit.expected_command}` -> `{edit.new_command}`"
        )
    for edit in unit_byte_edits:
        lines.append(
            f"- `{edit.label}` signature `{edit.signature.hex().upper()}`: unit+"
            f"`0x{edit.relative_offset:X}` `{edit.expected_value}` -> `{edit.new_value}`"
        )
    lines.extend(
        [
            "",
            "## Round-trip proof",
            "",
            "| Member | Unit | Record | Command byte | Transition |",
            "| --- | --- | ---: | ---: | ---: |",
        ]
    )
    for item in applied:
        lines.append(
            f"| `{item.component}` | `{item.edit.label}` | `0x{item.record_offset:X}` | "
            f"`0x{item.command_offset:X}` | `{item.edit.expected_command}` -> "
            f"`{item.edit.new_command}` |"
        )
    for item in applied_unit_bytes:
        lines.append(
            f"| `{item.component}` | `{item.edit.label}` | `0x{item.record_offset:X}` | "
            f"`0x{item.byte_offset:X}` | `{item.edit.expected_value}` -> "
            f"`{item.edit.new_value}` |"
        )
    lines.extend(
        [
            "",
            "All unlisted members are byte-identical after pack/unpack. Listed members change only",
            "the enumerated command byte and their recomputed inner CRC fields.",
            "",
            "## Artifact",
            "",
            f"- Fixture PNG: `{fixture.name}`",
            f"- Fixture SHA-256: `{fixturefmt.sha256(fixture)}`",
            "",
            "The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    args = parse_args()
    source_save = args.source_save.resolve()
    output_dir = args.output_dir.resolve()
    ff16tools = args.ff16tools.resolve()
    edits = tuple(args.edit)
    unit_byte_edits = tuple(args.unit_byte)
    if not source_save.is_file() or not ff16tools.is_file():
        raise SystemExit("source autosave or FF16Tools is missing")
    if len({edit.label for edit in edits}) != len(edits):
        raise SystemExit("command edit labels must be unique")

    output_dir.mkdir(parents=True, exist_ok=True)
    fixture = output_dir / f"{args.prefix}-{args.label}-fixture.png"
    manifest = output_dir / f"{args.prefix}-{args.label}-fixture-manifest.md"
    if fixture.exists() or manifest.exists():
        raise SystemExit("refusing to overwrite fixture artifacts")

    with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_command_", ignore_cleanup_errors=True) as tmp:
        root = Path(tmp)
        source_dir = root / "source"
        source_dir.mkdir()
        source_files = savefmt.unpack_save(ff16tools, source_save, source_dir)
        for name, data in source_files.items():
            savefmt.validate_inner_checksum(name, data)
        staged, applied, applied_unit_bytes = stage(source_files, edits, unit_byte_edits)
        for name, data in staged.items():
            (source_dir / name).write_bytes(data)

        packed = root / "fixture.png"
        savefmt.pack_save(ff16tools, source_dir, packed)
        roundtrip_dir = root / "roundtrip"
        roundtrip_dir.mkdir()
        roundtrip = savefmt.unpack_save(ff16tools, packed, roundtrip_dir)
        audit(source_files, roundtrip, applied, applied_unit_bytes)
        shutil.copyfile(packed, fixture)
        manifest.write_text(
            manifest_text(
                source_save,
                fixture,
                edits,
                applied,
                unit_byte_edits,
                applied_unit_bytes,
            ),
            encoding="utf-8",
        )

    print(fixture)
    print(manifest)
    print("autosave secondary-command fixture validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
