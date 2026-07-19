#!/usr/bin/env python3
"""Build a one-bit, round-trip-audited FFT Enhanced manual-save ability fixture.

The live save is never modified. FF16Tools unpacks the source PNG into a temporary directory,
the selected learned-ability bit is enabled in one manual slot/unit, and FF16Tools repacks a new
PNG (including its payload checksum). The repacked PNG is unpacked again and every payload byte
outside the checksum plus the selected learned bit is required to remain identical.
"""

from __future__ import annotations

import argparse
import hashlib
import shutil
import struct
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zlib
from dataclasses import dataclass
from pathlib import Path


REPO = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = REPO / "work"
DEFAULT_FF16TOOLS = Path(
    r"D:/Projects/FFTModNewGame++/tools/FF16Tools.CLI-1.13.2-win-x64/win-x64/FF16Tools.CLI.exe"
)
DEFAULT_TABLEDATA = Path(r"C:/Reloaded-II/Mods/fftivc.utility.modloader/TableData")

OUTER_HEADER_SIZE = 0x10
MANUAL_SLOT_COUNT = 50
MANUAL_SLOT_STRIDE = 0x9CE4
SAVEWORK_SIZE = 0x9CDC
SLOT_TRAILER_SIZE = MANUAL_SLOT_STRIDE - SAVEWORK_SIZE
BATTLE_OFFSET = 0x518
UNIT_COUNT = 54
UNIT_SIZE = 0x258
ABILITY_FLAGS_OFFSET = 0x32
ABILITY_FLAG_BYTES_PER_JOB = 3
GENERIC_JOB_FIRST = 0x4A
GENERIC_JOB_LAST = 0x5D
UNIT_NICKNAME_OFFSET = 0xDC
UNIT_NICKNAME_SIZE = 16
CHARA_NAME_KEY_OFFSET = 0x230
CURRENT_COMBAT_SET_OFFSET = 0x125
COMBAT_SET_FIRST_OFFSET = 0x126
COMBAT_SET_SIZE = 0x58
COMBAT_SET_SKILLSETS_OFFSET = 0x4C
COMBAT_SET_JOB_OFFSET = 0x56


@dataclass(frozen=True)
class UnitContext:
    slot_index: int
    unit_index: int
    absolute_offset: int
    character: int
    stored_unit_index: int
    job_id: int
    secondary_command_id: int
    nickname: str
    chara_name_key: int
    current_combat_set: int
    combat_set_job_id: int | None
    accessible_command_ids: tuple[int, ...]

    @property
    def active(self) -> bool:
        return self.character != 0 and self.stored_unit_index == self.unit_index


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a non-deployed, one-bit learned-ability manual-save fixture."
    )
    parser.add_argument("--prefix", required=True, help="Unix timestamp filename prefix.")
    parser.add_argument("--source-save", type=Path, required=True, help="Immutable source .png save.")
    parser.add_argument("--slot-number", type=int, default=5, help="1-based manual slot. Default: 5.")
    parser.add_argument("--unit-index", type=int, default=6, help="0-based roster unit. Default: 6.")
    parser.add_argument("--ability-id", type=int, default=30, help="Ability to learn. Default: Death (30).")
    parser.add_argument(
        "--label",
        default="lt37-save05-death",
        help="Artifact label placed after the timestamp. Default: lt37-save05-death.",
    )
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--ff16tools", type=Path, default=DEFAULT_FF16TOOLS)
    parser.add_argument("--job-data", type=Path, default=DEFAULT_TABLEDATA / "JobData.xml")
    parser.add_argument(
        "--job-command-data", type=Path, default=DEFAULT_TABLEDATA / "JobCommandData.xml"
    )
    return parser.parse_args()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def sha256_bytes(data: bytes | bytearray) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def parse_job_commands(path: Path) -> dict[int, tuple[int, ...]]:
    root = ET.parse(path).getroot()
    commands: dict[int, tuple[int, ...]] = {}
    for node in root.findall("./Entries/JobCommand"):
        command_id = int(node.findtext("Id", "-1"))
        abilities = tuple(int(node.findtext(f"AbilityId{index}", "0")) for index in range(1, 17))
        commands[command_id] = abilities
    return commands


def parse_jobs(path: Path) -> dict[int, int]:
    root = ET.parse(path).getroot()
    jobs: dict[int, int] = {}
    for node in root.findall("./Entries/Job"):
        job_id = int(node.findtext("Id", "-1"))
        jobs[job_id] = int(node.findtext("JobCommandId", "0"))
    return jobs


def command_to_ability_flag_slots(jobs: dict[int, int]) -> dict[int, tuple[int, ...]]:
    slots: dict[int, list[int]] = {}
    for job_id in range(GENERIC_JOB_FIRST, GENERIC_JOB_LAST + 1):
        command_id = jobs.get(job_id)
        if command_id is None:
            raise AssertionError(f"generic job 0x{job_id:02X} is absent from JobData")
        slots.setdefault(command_id, []).append(job_id - GENERIC_JOB_FIRST)
    return {command_id: tuple(values) for command_id, values in slots.items()}


def decode_ascii_fixed(data: bytes | bytearray) -> str:
    return bytes(data).split(b"\0", 1)[0].decode("ascii", errors="replace")


def read_unit_context(payload: bytes | bytearray, slot_index: int, unit_index: int, jobs: dict[int, int]) -> UnitContext:
    slot_base = OUTER_HEADER_SIZE + slot_index * MANUAL_SLOT_STRIDE
    unit_offset = slot_base + BATTLE_OFFSET + unit_index * UNIT_SIZE
    character, stored_unit_index, job_id = payload[unit_offset : unit_offset + 3]
    secondary_command_id = payload[unit_offset + 0x07]
    nickname = decode_ascii_fixed(
        payload[unit_offset + UNIT_NICKNAME_OFFSET : unit_offset + UNIT_NICKNAME_OFFSET + UNIT_NICKNAME_SIZE]
    )
    chara_name_key = struct.unpack_from("<H", payload, unit_offset + CHARA_NAME_KEY_OFFSET)[0]
    current_combat_set = payload[unit_offset + CURRENT_COMBAT_SET_OFFSET]

    combat_set_job_id: int | None = None
    if current_combat_set == 0xFF:
        primary_command_id = jobs.get(job_id)
        commands = [primary_command_id, secondary_command_id]
    elif current_combat_set < 3:
        combat_set_offset = (
            unit_offset + COMBAT_SET_FIRST_OFFSET + current_combat_set * COMBAT_SET_SIZE
        )
        skillset0, skillset1 = struct.unpack_from(
            "<hh", payload, combat_set_offset + COMBAT_SET_SKILLSETS_OFFSET
        )
        combat_set_job_id = payload[combat_set_offset + COMBAT_SET_JOB_OFFSET]
        commands = [skillset0, skillset1]
    else:
        raise AssertionError(
            f"unit {unit_index} has invalid current combat set 0x{current_combat_set:02X}"
        )

    accessible = tuple(dict.fromkeys(int(value) for value in commands if value not in (None, 0, -1, 0xFF)))
    return UnitContext(
        slot_index=slot_index,
        unit_index=unit_index,
        absolute_offset=unit_offset,
        character=character,
        stored_unit_index=stored_unit_index,
        job_id=job_id,
        secondary_command_id=secondary_command_id,
        nickname=nickname,
        chara_name_key=chara_name_key,
        current_combat_set=current_combat_set,
        combat_set_job_id=combat_set_job_id,
        accessible_command_ids=accessible,
    )


def expected_manual_size() -> int:
    return OUTER_HEADER_SIZE + MANUAL_SLOT_COUNT * MANUAL_SLOT_STRIDE


def unpack_save(ff16tools: Path, source: Path, output_dir: Path) -> Path:
    subprocess.run(
        [str(ff16tools), "unpack-save", "-i", str(source), "-o", str(output_dir)],
        check=True,
    )
    payload_path = output_dir / "fftsave.bin"
    if not payload_path.is_file():
        raise AssertionError(f"FF16Tools did not emit {payload_path}")
    return payload_path


def pack_save(ff16tools: Path, source_dir: Path, output: Path) -> None:
    subprocess.run(
        [
            str(ff16tools),
            "pack-save",
            "-i",
            str(source_dir),
            "-o",
            str(output),
            "-g",
            "fft",
            "-s",
        ],
        check=True,
    )
    if not output.is_file() or output.stat().st_size == 0:
        raise AssertionError(f"FF16Tools did not emit a non-empty save: {output}")


def assert_payload_delta(
    source: bytes,
    roundtrip: bytes,
    target_offset: int,
    target_mask: int,
) -> list[int]:
    if len(source) != len(roundtrip):
        raise AssertionError(f"payload length changed: {len(source)} -> {len(roundtrip)}")
    changed_offsets = [index for index, pair in enumerate(zip(source, roundtrip, strict=True)) if pair[0] != pair[1]]
    allowed = {4, 5, 6, 7, target_offset}
    unexpected = [offset for offset in changed_offsets if offset not in allowed]
    if unexpected:
        raise AssertionError(f"unexpected payload bytes changed: {unexpected[:32]}")
    if source[target_offset] & target_mask:
        raise AssertionError("source learned bit was already enabled")
    if roundtrip[target_offset] != (source[target_offset] | target_mask):
        raise AssertionError(
            f"learned byte mismatch at 0x{target_offset:X}: "
            f"0x{source[target_offset]:02X} -> 0x{roundtrip[target_offset]:02X}"
        )
    stored_crc = struct.unpack_from("<I", roundtrip, 4)[0]
    computed_crc = zlib.crc32(roundtrip[OUTER_HEADER_SIZE:]) & 0xFFFFFFFF
    if stored_crc != computed_crc:
        raise AssertionError(
            f"FF16Tools checksum mismatch: stored 0x{stored_crc:08X}, computed 0x{computed_crc:08X}"
        )
    return changed_offsets


def build_manifest(
    *,
    source_save: Path,
    fixture_png: Path,
    fixture_payload: Path,
    source_payload: bytes,
    roundtrip_payload: bytes,
    unit: UnitContext,
    ability_id: int,
    command_id: int,
    ability_index: int,
    ability_flag_slot: int,
    target_offset: int,
    target_mask: int,
    changed_offsets: list[int],
    job_data: Path,
    job_command_data: Path,
) -> str:
    before_byte = source_payload[target_offset]
    after_byte = roundtrip_payload[target_offset]
    stored_crc = struct.unpack_from("<I", roundtrip_payload, 4)[0]
    return "\n".join(
        [
            "# FFT Enhanced manual-save learned-ability fixture",
            "",
            "## Scope",
            "",
            "This fixture is generated outside the live save directory. It enables exactly one learned",
            "active-ability bit on one unit; FF16Tools owns the enclosing PNG and payload checksum.",
            "",
            "## Sources",
            "",
            f"- Immutable source save: `{source_save.resolve()}`",
            f"- Source save SHA-256: `{sha256(source_save)}`",
            f"- Source unpacked payload SHA-256: `{sha256_bytes(source_payload)}`",
            f"- JobData: `{job_data.resolve()}`",
            f"- JobData SHA-256: `{sha256(job_data)}`",
            f"- JobCommandData: `{job_command_data.resolve()}`",
            f"- JobCommandData SHA-256: `{sha256(job_command_data)}`",
            "",
            "## Selected unit and ability",
            "",
            f"- Manual slot: `{unit.slot_index + 1}` (zero-based index `{unit.slot_index}`)",
            f"- Unit slot: `{unit.unit_index}`; active: `{str(unit.active).lower()}`",
            f"- Character byte: `0x{unit.character:02X}`; job: `0x{unit.job_id:02X}`",
            f"- Nickname: `{unit.nickname or '<empty>'}`; CharaNameKey: `{unit.chara_name_key}`",
            f"- Current combat set: `0x{unit.current_combat_set:02X}`",
            f"- Accessible command ids: `{', '.join(map(str, unit.accessible_command_ids))}`",
            f"- Ability id: `{ability_id}`; selected command id: `{command_id}`",
            f"- Active-ability index: `{ability_index}`; ability-flag job slot: `{ability_flag_slot}`",
            "",
            "## Exact payload delta",
            "",
            f"- Learned byte absolute offset: `0x{target_offset:X}`",
            f"- Learned mask: `0x{target_mask:02X}`",
            f"- Learned byte: `0x{before_byte:02X}` -> `0x{after_byte:02X}`",
            f"- Repacked checksum: `0x{stored_crc:08X}` over payload bytes `[0x10:]`",
            f"- All changed offsets: `{', '.join(f'0x{offset:X}' for offset in changed_offsets)}`",
            "",
            "Every payload byte outside the four-byte checksum field and the selected learned byte is",
            "identical to the immutable source. Re-unpacking the packed PNG reproduced the audited payload",
            "exactly; no live save file was read for writing or replaced.",
            "",
            "## Artifacts",
            "",
            f"- Fixture PNG: `{fixture_png.name}`",
            f"- Fixture PNG SHA-256: `{sha256(fixture_png)}`",
            f"- Re-unpacked fixture payload: `{fixture_payload.name}`",
            f"- Fixture payload SHA-256: `{sha256(fixture_payload)}`",
            "",
            "The fixture remains non-deployed. Copy it into the live manual-save path only behind a",
            "separate backup/restore protocol while `FFT_enhanced.exe` is stopped.",
            "",
        ]
    )


def main() -> int:
    args = parse_args()
    prefix = str(args.prefix).strip()
    if not prefix.isdigit() or len(prefix) < 10:
        raise SystemExit("--prefix must be a Unix timestamp with at least ten digits")
    if not 1 <= args.slot_number <= MANUAL_SLOT_COUNT:
        raise SystemExit(f"--slot-number must be in [1, {MANUAL_SLOT_COUNT}]")
    if not 0 <= args.unit_index < UNIT_COUNT:
        raise SystemExit(f"--unit-index must be in [0, {UNIT_COUNT - 1}]")

    source_save = args.source_save.resolve()
    ff16tools = args.ff16tools.resolve()
    job_data = args.job_data.resolve()
    job_command_data = args.job_command_data.resolve()
    for required in (source_save, ff16tools, job_data, job_command_data):
        if not required.is_file():
            raise SystemExit(f"required file not found: {required}")

    label = args.label.strip()
    if not label or any(char in label for char in r'<>:"/\\|?*'):
        raise SystemExit("--label must be a non-empty filename-safe value")
    output_dir = args.output_dir.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    fixture_png = output_dir / f"{prefix}-{label}-fixture.png"
    fixture_payload = output_dir / f"{prefix}-{label}-fixture.bin"
    manifest_path = output_dir / f"{prefix}-{label}-fixture-manifest.md"
    existing = [path for path in (fixture_png, fixture_payload, manifest_path) if path.exists()]
    if existing:
        raise SystemExit("refusing to overwrite fixture artifacts: " + ", ".join(map(str, existing)))

    jobs = parse_jobs(job_data)
    commands = parse_job_commands(job_command_data)
    command_slots = command_to_ability_flag_slots(jobs)
    matching_commands = {
        command_id: abilities.index(args.ability_id)
        for command_id, abilities in commands.items()
        if args.ability_id in abilities
    }
    if not matching_commands:
        raise AssertionError(f"ability {args.ability_id} is absent from JobCommandData")

    with tempfile.TemporaryDirectory(prefix="gc_fft_manual_fixture_", ignore_cleanup_errors=True) as tmp:
        temp = Path(tmp)
        source_dir = temp / "source"
        source_dir.mkdir()
        source_payload_path = unpack_save(ff16tools, source_save, source_dir)
        source_payload = source_payload_path.read_bytes()
        if len(source_payload) != expected_manual_size():
            raise AssertionError(
                f"manual payload size mismatch: {len(source_payload)} != {expected_manual_size()}"
            )

        slot_index = args.slot_number - 1
        unit = read_unit_context(source_payload, slot_index, args.unit_index, jobs)
        if not unit.active:
            raise AssertionError(
                f"unit {args.unit_index} is not active: character=0x{unit.character:02X}, "
                f"stored index=0x{unit.stored_unit_index:02X}"
            )
        accessible_matches = [
            command_id for command_id in unit.accessible_command_ids if command_id in matching_commands
        ]
        if len(accessible_matches) != 1:
            raise AssertionError(
                f"expected one accessible command containing ability {args.ability_id}; "
                f"accessible={unit.accessible_command_ids}, matching={matching_commands}"
            )
        command_id = accessible_matches[0]
        slots = command_slots.get(command_id, ())
        if len(slots) != 1:
            raise AssertionError(
                f"command {command_id} does not map to exactly one generic ability-flag slot: {slots}"
            )
        ability_flag_slot = slots[0]
        ability_index = matching_commands[command_id]
        target_offset = (
            unit.absolute_offset
            + ABILITY_FLAGS_OFFSET
            + ability_flag_slot * ABILITY_FLAG_BYTES_PER_JOB
            + (ability_index >> 3)
        )
        target_mask = 1 << (7 - (ability_index & 7))
        if source_payload[target_offset] & target_mask:
            raise AssertionError(
                f"ability {args.ability_id} is already learned by unit {args.unit_index}"
            )

        staged = bytearray(source_payload)
        staged[target_offset] |= target_mask
        source_payload_path.write_bytes(staged)
        packed_temp = temp / "fixture.png"
        pack_save(ff16tools, source_dir, packed_temp)

        roundtrip_dir = temp / "roundtrip"
        roundtrip_dir.mkdir()
        roundtrip_path = unpack_save(ff16tools, packed_temp, roundtrip_dir)
        roundtrip_payload = roundtrip_path.read_bytes()
        changed_offsets = assert_payload_delta(
            source_payload, roundtrip_payload, target_offset, target_mask
        )

        shutil.copyfile(packed_temp, fixture_png)
        fixture_payload.write_bytes(roundtrip_payload)
        manifest = build_manifest(
            source_save=source_save,
            fixture_png=fixture_png,
            fixture_payload=fixture_payload,
            source_payload=source_payload,
            roundtrip_payload=roundtrip_payload,
            unit=unit,
            ability_id=args.ability_id,
            command_id=command_id,
            ability_index=ability_index,
            ability_flag_slot=ability_flag_slot,
            target_offset=target_offset,
            target_mask=target_mask,
            changed_offsets=changed_offsets,
            job_data=job_data,
            job_command_data=job_command_data,
        )
        manifest_path.write_text(manifest, encoding="utf-8")

    print(fixture_png)
    print(fixture_payload)
    print(manifest_path)
    print("manual-save learned-ability fixture validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
