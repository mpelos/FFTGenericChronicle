#!/usr/bin/env python3
"""Build a round-trip-audited Enhanced autosave with bounded live-unit CT edits."""

from __future__ import annotations

import argparse
import hashlib
import shutil
import tempfile
from dataclasses import dataclass
from pathlib import Path

import build_fft_autosave_reaction_fixture as savefmt
import build_fft_manual_ability_fixture as manualfmt


REPO = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = REPO / "work"
CT_OFFSET = 0x41
INNER_CHECKSUM_OFFSETS = frozenset(range(0x04, 0x08))
MAIN_LIVE_COMPONENTS = (
    "resume_en00_main.sav",
    "resume_en00_fturn.sav",
    "resume_enbtl_main.sav",
)
ATTACK_LIVE_COMPONENTS = (
    "resume_en00_attack.sav",
    "resume_enbtl_attack.sav",
)
SCOPE_COMPONENTS = {
    "main": MAIN_LIVE_COMPONENTS,
    "snapshot": ("resume_en00_main.sav",),
    "turn": ("resume_en00_fturn.sav", "resume_enbtl_main.sav"),
    "attack": ATTACK_LIVE_COMPONENTS,
}


@dataclass(frozen=True)
class CtEdit:
    scope: str
    label: str
    signature: bytes
    expected_ct: int
    new_ct: int


@dataclass(frozen=True)
class AppliedEdit:
    component: str
    edit: CtEdit
    record_offset: int

    @property
    def ct_offset(self) -> int:
        return self.record_offset + CT_OFFSET


def parse_edit(value: str) -> CtEdit:
    parts = value.split(":")
    if len(parts) == 4:
        scope = "main"
        label, signature_hex, expected_raw, new_raw = parts
    elif len(parts) == 5:
        scope, label, signature_hex, expected_raw, new_raw = parts
    else:
        raise argparse.ArgumentTypeError(
            "CT edit must be [main|snapshot|turn|attack:]LABEL:HEX_SIGNATURE:EXPECTED_CT:NEW_CT"
        )
    if scope not in SCOPE_COMPONENTS:
        raise argparse.ArgumentTypeError(
            "CT edit scope must be main, snapshot, turn, or attack"
        )
    if not label or not signature_hex:
        raise argparse.ArgumentTypeError("CT edit label and signature cannot be empty")
    try:
        signature = bytes.fromhex(signature_hex)
        expected_ct = int(expected_raw, 0)
        new_ct = int(new_raw, 0)
    except ValueError as error:
        raise argparse.ArgumentTypeError(str(error)) from error
    if len(signature) < 8:
        raise argparse.ArgumentTypeError("CT edit signature must contain at least eight bytes")
    if not 0 <= expected_ct <= 0xFF or not 0 <= new_ct <= 0xFF:
        raise argparse.ArgumentTypeError("CT values must be within 0..255")
    return CtEdit(scope, label, signature, expected_ct, new_ct)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a non-deployed, audited Enhanced autosave CT-order fixture."
    )
    parser.add_argument("--prefix", required=True, help="Unix timestamp filename prefix.")
    parser.add_argument("--source-save", type=Path, required=True, help="Immutable autosave PNG.")
    parser.add_argument("--label", required=True, help="Filename label after the timestamp.")
    parser.add_argument(
        "--edit",
        action="append",
        type=parse_edit,
        required=True,
        help="Repeatable [main|attack:]LABEL:HEX_SIGNATURE:EXPECTED_CT:NEW_CT edit.",
    )
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--ff16tools", type=Path, default=manualfmt.DEFAULT_FF16TOOLS)
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def signature_hits(data: bytes, signature: bytes) -> tuple[int, ...]:
    hits: list[int] = []
    start = 0
    while True:
        offset = data.find(signature, start)
        if offset < 0:
            return tuple(hits)
        hits.append(offset)
        start = offset + 1


def stage(files: dict[str, bytes], edits: tuple[CtEdit, ...]) -> tuple[dict[str, bytes], tuple[AppliedEdit, ...]]:
    savefmt.validate_current_aliases(files)
    savefmt.validate_battle_entry(files)
    selected_components = tuple(
        dict.fromkeys(name for edit in edits for name in SCOPE_COMPONENTS[edit.scope])
    )
    missing = [name for name in selected_components if name not in files]
    if missing:
        raise AssertionError(f"autosave is missing live components: {missing}")

    staged = dict(files)
    applied: list[AppliedEdit] = []
    for name in selected_components:
        mutable = bytearray(files[name])
        for edit in (candidate for candidate in edits if name in SCOPE_COMPONENTS[candidate.scope]):
            hits = signature_hits(files[name], edit.signature)
            if len(hits) != 1:
                raise AssertionError(
                    f"{name} expected exactly one {edit.label} signature; found {len(hits)}"
                )
            record_offset = hits[0]
            ct_offset = record_offset + CT_OFFSET
            if ct_offset >= len(mutable):
                raise AssertionError(f"{name} {edit.label} CT offset is outside the component")
            actual = mutable[ct_offset]
            if actual != edit.expected_ct:
                raise AssertionError(
                    f"{name} {edit.label} CT mismatch: expected {edit.expected_ct}, found {actual}"
                )
            mutable[ct_offset] = edit.new_ct
            applied.append(AppliedEdit(name, edit, record_offset))
        staged[name] = bytes(mutable)

    if staged["resume_en00_fturn.sav"] != staged["resume_enbtl_main.sav"]:
        raise AssertionError("CT edits broke the current fturn/main alias equality")
    if staged["resume_en00_attack.sav"] != staged["resume_enbtl_attack.sav"]:
        raise AssertionError("CT edits broke the current attack alias equality")
    return staged, tuple(applied)


def audit_roundtrip(
    source: dict[str, bytes],
    roundtrip: dict[str, bytes],
    applied: tuple[AppliedEdit, ...],
) -> None:
    if set(source) != set(roundtrip):
        raise AssertionError("packed autosave member set changed")
    expected_by_component: dict[str, set[int]] = {}
    for item in applied:
        expected_by_component.setdefault(item.component, set()).add(item.ct_offset)

    for name in sorted(source):
        before = source[name]
        after = roundtrip[name]
        if len(before) != len(after):
            raise AssertionError(f"{name} length changed after pack/unpack")
        changed = {index for index, pair in enumerate(zip(before, after)) if pair[0] != pair[1]}
        expected = expected_by_component.get(name, set())
        if expected:
            allowed = expected | set(INNER_CHECKSUM_OFFSETS)
            if not expected.issubset(changed) or not changed.issubset(allowed):
                raise AssertionError(
                    f"{name} changed unexpected offsets: expected CT {sorted(expected)}, got {sorted(changed)}"
                )
            for item in applied:
                if item.component == name and after[item.ct_offset] != item.edit.new_ct:
                    raise AssertionError(f"{name} did not retain {item.edit.label} CT")
        elif changed:
            raise AssertionError(f"unchanged member {name} changed at {sorted(changed)}")

    savefmt.validate_current_aliases(roundtrip)
    savefmt.validate_battle_entry(roundtrip)


def build_manifest(
    source_save: Path,
    fixture: Path,
    edits: tuple[CtEdit, ...],
    applied: tuple[AppliedEdit, ...],
) -> str:
    lines = [
        "# FFT Enhanced autosave CT-order fixture",
        "",
        "## Scope",
        "",
        "This non-deployed fixture changes only `unit+0x41` CT bytes in the current live battle",
        "components. FF16Tools owns every updated member CRC and the enclosing PNG serialization.",
        "",
        "## Source",
        "",
        f"- Source autosave: `{source_save}`",
        f"- Source SHA-256: `{sha256(source_save)}`",
        "",
        "## Requested CT edits",
        "",
    ]
    for edit in edits:
        lines.append(
            f"- `{edit.label}` scope `{edit.scope}`, signature `{edit.signature.hex().upper()}`: "
            f"CT `{edit.expected_ct}` -> `{edit.new_ct}`"
        )
    lines.extend(
        [
            "",
            "## Round-trip proof",
            "",
            "| Member | Unit | Record | CT byte | Transition |",
            "| --- | --- | ---: | ---: | ---: |",
        ]
    )
    for item in applied:
        lines.append(
            f"| `{item.component}` | `{item.edit.label}` | `0x{item.record_offset:X}` | "
            f"`0x{item.ct_offset:X}` | `{item.edit.expected_ct}` -> `{item.edit.new_ct}` |"
        )
    lines.extend(
        [
            "",
            "Every unlisted member is byte-identical after pack/unpack. Listed members change only",
            "the enumerated CT bytes and their four-byte inner CRC fields. Current alias equalities and",
            "the battle-entry id remain valid.",
            "",
            "## Artifact",
            "",
            f"- Fixture PNG: `{fixture.name}`",
            f"- Fixture SHA-256: `{sha256(fixture)}`",
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
    if not source_save.is_file():
        raise SystemExit(f"source autosave not found: {source_save}")
    if not ff16tools.is_file():
        raise SystemExit(f"FF16Tools not found: {ff16tools}")
    if len({edit.label for edit in edits}) != len(edits):
        raise SystemExit("CT edit labels must be unique")

    output_dir.mkdir(parents=True, exist_ok=True)
    fixture = output_dir / f"{args.prefix}-{args.label}-fixture.png"
    manifest = output_dir / f"{args.prefix}-{args.label}-fixture-manifest.md"
    existing = [path for path in (fixture, manifest) if path.exists()]
    if existing:
        raise SystemExit("refusing to overwrite fixture artifacts: " + ", ".join(map(str, existing)))

    with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_ct_", ignore_cleanup_errors=True) as tmp:
        root = Path(tmp)
        source_dir = root / "source"
        source_dir.mkdir()
        source_files = savefmt.unpack_save(ff16tools, source_save, source_dir)
        for name, data in source_files.items():
            savefmt.validate_inner_checksum(name, data)

        staged, applied = stage(source_files, edits)
        for name, data in staged.items():
            (source_dir / name).write_bytes(data)

        packed = root / "fixture.png"
        savefmt.pack_save(ff16tools, source_dir, packed)
        roundtrip_dir = root / "roundtrip"
        roundtrip_dir.mkdir()
        roundtrip = savefmt.unpack_save(ff16tools, packed, roundtrip_dir)
        audit_roundtrip(source_files, roundtrip, applied)

        shutil.copyfile(packed, fixture)
        manifest.write_text(build_manifest(source_save, fixture, edits, applied), encoding="utf-8")

    print(fixture)
    print(manifest)
    print("autosave CT-order fixture validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
