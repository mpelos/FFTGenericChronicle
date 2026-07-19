#!/usr/bin/env python3
"""Build an audited Enhanced autosave that clears bounded live-unit status bits."""

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
EFFECTIVE_STATUS_OFFSET = 0x61
MASTER_STATUS_OFFSET = 0x1EF
SOURCE_STATUS_OFFSET = 0x57


@dataclass(frozen=True)
class StatusClear:
    label: str
    signature: bytes
    byte_index: int
    mask: int


@dataclass(frozen=True)
class AppliedClear:
    component: str
    clear: StatusClear
    record_offset: int

    @property
    def effective_offset(self) -> int:
        return self.record_offset + EFFECTIVE_STATUS_OFFSET + self.clear.byte_index

    @property
    def master_offset(self) -> int:
        return self.record_offset + MASTER_STATUS_OFFSET + self.clear.byte_index

    @property
    def source_offset(self) -> int:
        return self.record_offset + SOURCE_STATUS_OFFSET + self.clear.byte_index


def parse_clear(value: str) -> StatusClear:
    parts = value.split(":")
    if len(parts) != 4:
        raise argparse.ArgumentTypeError(
            "status clear must be LABEL:HEX_SIGNATURE:BYTE_INDEX:MASK"
        )
    label, signature_hex, byte_raw, mask_raw = parts
    try:
        signature = bytes.fromhex(signature_hex)
        byte_index = int(byte_raw, 0)
        mask = int(mask_raw, 0)
    except ValueError as error:
        raise argparse.ArgumentTypeError(str(error)) from error
    if not label or len(signature) < 8:
        raise argparse.ArgumentTypeError("status label is required and signature needs at least eight bytes")
    if not 0 <= byte_index < 5 or not 1 <= mask <= 0xFF:
        raise argparse.ArgumentTypeError("status byte must be 0..4 and mask must be 1..255")
    return StatusClear(label, signature, byte_index, mask)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a non-deployed, audited Enhanced autosave status-clear fixture."
    )
    parser.add_argument("--prefix", required=True)
    parser.add_argument("--source-save", type=Path, required=True)
    parser.add_argument("--label", required=True)
    parser.add_argument("--clear", action="append", type=parse_clear, required=True)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--ff16tools", type=Path, default=manualfmt.DEFAULT_FF16TOOLS)
    return parser.parse_args()


def stage(
    files: dict[str, bytes],
    clears: tuple[StatusClear, ...],
) -> tuple[dict[str, bytes], tuple[AppliedClear, ...]]:
    savefmt.validate_current_aliases(files)
    savefmt.validate_battle_entry(files)
    staged = dict(files)
    applied: list[AppliedClear] = []
    for name in fixturefmt.MAIN_LIVE_COMPONENTS:
        data = files[name]
        mutable = bytearray(data)
        for clear in clears:
            hits = fixturefmt.signature_hits(data, clear.signature)
            if len(hits) != 1:
                raise AssertionError(
                    f"{name} expected exactly one {clear.label} signature; found {len(hits)}"
                )
            item = AppliedClear(name, clear, hits[0])
            effective = mutable[item.effective_offset]
            master = mutable[item.master_offset]
            if effective & clear.mask == 0 or master & clear.mask == 0:
                raise AssertionError(
                    f"{name} {clear.label} status mask 0x{clear.mask:02X} is not present in "
                    f"effective/master (0x{effective:02X}/0x{master:02X})"
                )
            mutable[item.effective_offset] &= ~clear.mask
            mutable[item.master_offset] &= ~clear.mask
            applied.append(item)
        staged[name] = bytes(mutable)

    if staged["resume_en00_fturn.sav"] != staged["resume_enbtl_main.sav"]:
        raise AssertionError("status edits broke the current fturn/main alias equality")
    return staged, tuple(applied)


def audit(
    source: dict[str, bytes],
    roundtrip: dict[str, bytes],
    applied: tuple[AppliedClear, ...],
) -> None:
    if set(source) != set(roundtrip):
        raise AssertionError("packed autosave member set changed")
    expected_by_component: dict[str, set[int]] = {}
    for item in applied:
        expected_by_component.setdefault(item.component, set()).update(
            (item.effective_offset, item.master_offset)
        )

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
                    f"{name} changed unexpected offsets: expected status {sorted(expected)}, got {sorted(changed)}"
                )
        elif changed:
            raise AssertionError(f"unchanged member {name} changed at {sorted(changed)}")

    for item in applied:
        data = roundtrip[item.component]
        if data[item.effective_offset] & item.clear.mask or data[item.master_offset] & item.clear.mask:
            raise AssertionError(f"{item.component} retained {item.clear.label}")
        if data[item.source_offset] != source[item.component][item.source_offset]:
            raise AssertionError(f"{item.component} changed the status source byte for {item.clear.label}")
    savefmt.validate_current_aliases(roundtrip)
    savefmt.validate_battle_entry(roundtrip)


def manifest_text(
    source_save: Path,
    fixture: Path,
    clears: tuple[StatusClear, ...],
    applied: tuple[AppliedClear, ...],
) -> str:
    lines = [
        "# FFT Enhanced autosave status-clear fixture",
        "",
        "## Scope",
        "",
        "This non-deployed fixture clears only the requested bits from the effective and durable",
        "status arrays of exact live-unit signatures. Status-source bytes are preserved.",
        "",
        "## Source",
        "",
        f"- Source autosave: `{source_save}`",
        f"- Source SHA-256: `{fixturefmt.sha256(source_save)}`",
        "",
        "## Requested clears",
        "",
    ]
    for clear in clears:
        lines.append(
            f"- `{clear.label}` signature `{clear.signature.hex().upper()}`: status byte "
            f"`{clear.byte_index}`, mask `0x{clear.mask:02X}`"
        )
    lines.extend(
        [
            "",
            "## Round-trip proof",
            "",
            "| Member | Unit | Record | Effective | Master | Source preserved |",
            "| --- | --- | ---: | ---: | ---: | ---: |",
        ]
    )
    for item in applied:
        lines.append(
            f"| `{item.component}` | `{item.clear.label}` | `0x{item.record_offset:X}` | "
            f"`0x{item.effective_offset:X}` | `0x{item.master_offset:X}` | "
            f"`0x{item.source_offset:X}` |"
        )
    lines.extend(
        [
            "",
            "All unlisted members are byte-identical after pack/unpack. Listed members change only",
            "the enumerated effective/master bytes and their recomputed inner CRC fields.",
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
    clears = tuple(args.clear)
    if not source_save.is_file() or not ff16tools.is_file():
        raise SystemExit("source autosave or FF16Tools is missing")

    output_dir.mkdir(parents=True, exist_ok=True)
    fixture = output_dir / f"{args.prefix}-{args.label}-fixture.png"
    manifest = output_dir / f"{args.prefix}-{args.label}-fixture-manifest.md"
    if fixture.exists() or manifest.exists():
        raise SystemExit("refusing to overwrite fixture artifacts")

    with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_status_", ignore_cleanup_errors=True) as tmp:
        root = Path(tmp)
        source_dir = root / "source"
        source_dir.mkdir()
        source_files = savefmt.unpack_save(ff16tools, source_save, source_dir)
        for name, data in source_files.items():
            savefmt.validate_inner_checksum(name, data)
        staged, applied = stage(source_files, clears)
        for name, data in staged.items():
            (source_dir / name).write_bytes(data)

        packed = root / "fixture.png"
        savefmt.pack_save(ff16tools, source_dir, packed)
        roundtrip_dir = root / "roundtrip"
        roundtrip_dir.mkdir()
        roundtrip = savefmt.unpack_save(ff16tools, packed, roundtrip_dir)
        audit(source_files, roundtrip, applied)
        shutil.copyfile(packed, fixture)
        manifest.write_text(manifest_text(source_save, fixture, clears, applied), encoding="utf-8")

    print(fixture)
    print(manifest)
    print("autosave status-clear fixture validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
