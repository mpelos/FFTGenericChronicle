#!/usr/bin/env python3
"""Build a round-trip-audited FFT Enhanced manual-save Reaction fixture.

The live save is never modified. FF16Tools unpacks the source PNG into a temporary directory,
the selected roster unit's equipped Reaction word is replaced, and FF16Tools repacks a new PNG
with the correct payload checksum. A second unpack proves that only the checksum and Reaction
word changed.
"""

from __future__ import annotations

import argparse
import csv
import shutil
import struct
import tempfile
import xml.etree.ElementTree as ET
import zlib
from pathlib import Path

import build_fft_manual_ability_fixture as savefmt


REPO = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = REPO / "work"
DEFAULT_ABILITY_BASELINE = REPO / "work" / "wotl_ability_action_baseline.csv"
EQUIPPED_REACTION_OFFSET = 0x08
RSM_FIRST_BIT = 0x80


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a non-deployed, audited manual-save equipped-Reaction fixture."
    )
    parser.add_argument("--prefix", required=True, help="Unix timestamp filename prefix.")
    parser.add_argument("--source-save", type=Path, required=True, help="Immutable source .png save.")
    parser.add_argument("--slot-number", type=int, default=5, help="1-based manual slot. Default: 5.")
    parser.add_argument("--unit-index", type=int, default=6, help="0-based roster unit. Default: 6.")
    parser.add_argument("--reaction-id", type=int, default=442, help="Reaction to equip. Default: Counter (442).")
    parser.add_argument(
        "--expected-reaction-id",
        type=int,
        required=True,
        help="Required source Reaction id; protects against editing the wrong unit/save.",
    )
    parser.add_argument(
        "--label",
        default="lt23-save05-counter",
        help="Artifact label placed after the timestamp. Default: lt23-save05-counter.",
    )
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--ff16tools", type=Path, default=savefmt.DEFAULT_FF16TOOLS)
    parser.add_argument("--job-data", type=Path, default=savefmt.DEFAULT_TABLEDATA / "JobData.xml")
    parser.add_argument(
        "--job-command-data", type=Path, default=savefmt.DEFAULT_TABLEDATA / "JobCommandData.xml"
    )
    parser.add_argument("--ability-baseline", type=Path, default=DEFAULT_ABILITY_BASELINE)
    parser.add_argument(
        "--allow-unlearned-reaction",
        action="store_true",
        help="Allow a reserved/unnamed Reaction that is absent from generic-job RSM lists. Test fixtures only.",
    )
    return parser.parse_args()


def load_reaction_names(path: Path) -> dict[int, str]:
    result: dict[int, str] = {}
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            if row.get("ability_type") != "Reaction":
                continue
            result[int(row["id_dec"])] = row.get("name_ivc") or row.get("name_wotl") or "<unnamed>"
    if not result:
        raise AssertionError(f"no Reaction rows found in {path}")
    return result


def parse_job_rsm(path: Path) -> dict[int, tuple[int, ...]]:
    root = ET.parse(path).getroot()
    commands: dict[int, tuple[int, ...]] = {}
    for node in root.findall("./Entries/JobCommand"):
        command_id = int(node.findtext("Id", "-1"))
        commands[command_id] = tuple(
            int(node.findtext(f"ReactionSupportMovementId{index}", "0"))
            for index in range(1, 7)
        )
    return commands


def learned_rsm_location(
    reaction_id: int,
    jobs: dict[int, int],
    commands: dict[int, tuple[int, ...]],
) -> tuple[int, int, int]:
    matches: list[tuple[int, int]] = []
    for job_id in range(savefmt.GENERIC_JOB_FIRST, savefmt.GENERIC_JOB_LAST + 1):
        command_id = jobs[job_id]
        abilities = commands.get(command_id, ())
        if reaction_id in abilities:
            matches.append((job_id, abilities.index(reaction_id)))
    if len(matches) != 1:
        raise AssertionError(
            f"expected Reaction {reaction_id} in exactly one generic-job RSM list; matches={matches}"
        )
    job_id, rsm_index = matches[0]
    return job_id, rsm_index, RSM_FIRST_BIT >> rsm_index


def assert_payload_delta(
    source: bytes,
    roundtrip: bytes,
    reaction_offset: int,
    expected_reaction_id: int,
    reaction_id: int,
) -> list[int]:
    if len(source) != len(roundtrip):
        raise AssertionError(f"payload length changed: {len(source)} -> {len(roundtrip)}")
    changed_offsets = [
        index
        for index, pair in enumerate(zip(source, roundtrip, strict=True))
        if pair[0] != pair[1]
    ]
    allowed = {4, 5, 6, 7, reaction_offset, reaction_offset + 1}
    unexpected = [offset for offset in changed_offsets if offset not in allowed]
    if unexpected:
        raise AssertionError(f"unexpected payload bytes changed: {unexpected[:32]}")
    before = struct.unpack_from("<H", source, reaction_offset)[0]
    after = struct.unpack_from("<H", roundtrip, reaction_offset)[0]
    if before != expected_reaction_id:
        raise AssertionError(
            f"source Reaction mismatch at 0x{reaction_offset:X}: {before} != {expected_reaction_id}"
        )
    if after != reaction_id:
        raise AssertionError(
            f"round-trip Reaction mismatch at 0x{reaction_offset:X}: {after} != {reaction_id}"
        )
    stored_crc = struct.unpack_from("<I", roundtrip, 4)[0]
    computed_crc = zlib.crc32(roundtrip[savefmt.OUTER_HEADER_SIZE :]) & 0xFFFFFFFF
    if stored_crc != computed_crc:
        raise AssertionError(
            f"FF16Tools checksum mismatch: stored 0x{stored_crc:08X}, computed 0x{computed_crc:08X}"
        )
    return changed_offsets


def main() -> int:
    args = parse_args()
    prefix = str(args.prefix).strip()
    if not prefix.isdigit() or len(prefix) < 10:
        raise SystemExit("--prefix must be a Unix timestamp with at least ten digits")
    if not 1 <= args.slot_number <= savefmt.MANUAL_SLOT_COUNT:
        raise SystemExit(f"--slot-number must be in [1, {savefmt.MANUAL_SLOT_COUNT}]")
    if not 0 <= args.unit_index < savefmt.UNIT_COUNT:
        raise SystemExit(f"--unit-index must be in [0, {savefmt.UNIT_COUNT - 1}]")
    if args.reaction_id == args.expected_reaction_id:
        raise SystemExit("--reaction-id must differ from --expected-reaction-id")

    source_save = args.source_save.resolve()
    ff16tools = args.ff16tools.resolve()
    job_data = args.job_data.resolve()
    job_command_data = args.job_command_data.resolve()
    ability_baseline = args.ability_baseline.resolve()
    for required in (source_save, ff16tools, job_data, job_command_data, ability_baseline):
        if not required.is_file():
            raise SystemExit(f"required file not found: {required}")

    reaction_names = load_reaction_names(ability_baseline)
    for reaction_id in (args.expected_reaction_id, args.reaction_id):
        if reaction_id not in reaction_names:
            raise SystemExit(f"ability {reaction_id} is not a Reaction in {ability_baseline}")

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

    jobs = savefmt.parse_jobs(job_data)
    job_rsm = parse_job_rsm(job_command_data)
    learned = None if args.allow_unlearned_reaction else learned_rsm_location(
        args.reaction_id, jobs, job_rsm
    )
    with tempfile.TemporaryDirectory(prefix="gc_fft_manual_reaction_", ignore_cleanup_errors=True) as tmp:
        temp = Path(tmp)
        source_dir = temp / "source"
        source_dir.mkdir()
        source_payload_path = savefmt.unpack_save(ff16tools, source_save, source_dir)
        source_payload = source_payload_path.read_bytes()
        if len(source_payload) != savefmt.expected_manual_size():
            raise AssertionError(
                f"manual payload size mismatch: {len(source_payload)} != {savefmt.expected_manual_size()}"
            )

        unit = savefmt.read_unit_context(source_payload, args.slot_number - 1, args.unit_index, jobs)
        if not unit.active:
            raise AssertionError(
                f"unit {args.unit_index} is not active: character=0x{unit.character:02X}, "
                f"stored index=0x{unit.stored_unit_index:02X}"
            )
        reaction_offset = unit.absolute_offset + EQUIPPED_REACTION_OFFSET
        source_reaction_id = struct.unpack_from("<H", source_payload, reaction_offset)[0]
        if source_reaction_id != args.expected_reaction_id:
            raise AssertionError(
                f"unit {args.unit_index} source Reaction is {source_reaction_id}, "
                f"expected {args.expected_reaction_id}"
            )
        learned_offset = None
        if learned is not None:
            learned_job_id, learned_rsm_index, learned_mask = learned
            learned_offset = (
                unit.absolute_offset
                + savefmt.ABILITY_FLAGS_OFFSET
                + (learned_job_id - savefmt.GENERIC_JOB_FIRST) * savefmt.ABILITY_FLAG_BYTES_PER_JOB
                + 2
            )
            if not source_payload[learned_offset] & learned_mask:
                raise AssertionError(
                    f"unit {args.unit_index} has not learned Reaction {args.reaction_id}: "
                    f"job=0x{learned_job_id:02X} rsmIndex={learned_rsm_index + 1} "
                    f"byte=0x{source_payload[learned_offset]:02X} mask=0x{learned_mask:02X}"
                )

        staged = bytearray(source_payload)
        struct.pack_into("<H", staged, reaction_offset, args.reaction_id)
        source_payload_path.write_bytes(staged)
        packed_temp = temp / "fixture.png"
        savefmt.pack_save(ff16tools, source_dir, packed_temp)

        roundtrip_dir = temp / "roundtrip"
        roundtrip_dir.mkdir()
        roundtrip_path = savefmt.unpack_save(ff16tools, packed_temp, roundtrip_dir)
        roundtrip_payload = roundtrip_path.read_bytes()
        changed_offsets = assert_payload_delta(
            source_payload,
            roundtrip_payload,
            reaction_offset,
            args.expected_reaction_id,
            args.reaction_id,
        )

        shutil.copyfile(packed_temp, fixture_png)
        fixture_payload.write_bytes(roundtrip_payload)
        stored_crc = struct.unpack_from("<I", roundtrip_payload, 4)[0]
        manifest = "\n".join(
            [
                "# FFT Enhanced manual-save equipped-Reaction fixture",
                "",
                "## Scope",
                "",
                "This fixture is generated outside the live save directory. It replaces exactly one",
                "equipped-Reaction word on one unit; FF16Tools owns the enclosing PNG and payload checksum.",
                "",
                "## Sources",
                "",
                f"- Immutable source save: `{source_save}`",
                f"- Source save SHA-256: `{savefmt.sha256(source_save)}`",
                f"- Source unpacked payload SHA-256: `{savefmt.sha256_bytes(source_payload)}`",
                f"- Ability baseline: `{ability_baseline}`",
                f"- Ability baseline SHA-256: `{savefmt.sha256(ability_baseline)}`",
                "",
                "## Selected unit and Reaction",
                "",
                f"- Manual slot: `{unit.slot_index + 1}` (zero-based index `{unit.slot_index}`)",
                f"- Unit slot: `{unit.unit_index}`; active: `{str(unit.active).lower()}`",
                f"- Character byte: `0x{unit.character:02X}`; job: `0x{unit.job_id:02X}`",
                f"- Nickname: `{unit.nickname or '<empty>'}`; CharaNameKey: `{unit.chara_name_key}`",
                f"- Source Reaction: `{args.expected_reaction_id}` ({reaction_names[args.expected_reaction_id]})",
                f"- Fixture Reaction: `{args.reaction_id}` ({reaction_names[args.reaction_id]})",
                (f"- Learned in job: `0x{learned_job_id:02X}`; R/S/M slot: `{learned_rsm_index + 1}`"
                 if learned is not None else "- Learned-state policy: bypassed for reserved/unnamed live-test carrier"),
                (f"- Learned-byte proof: absolute `0x{learned_offset:X}` = "
                 f"`0x{source_payload[learned_offset]:02X}` includes mask `0x{learned_mask:02X}`"
                 if learned is not None else "- Learned-byte proof: not applicable; `--allow-unlearned-reaction` was explicit"),
                "",
                "## Exact payload delta",
                "",
                f"- Equipped-Reaction word absolute offset: `0x{reaction_offset:X}`",
                f"- Unit-relative offset: `+0x{EQUIPPED_REACTION_OFFSET:02X}`",
                f"- Word: `{args.expected_reaction_id}` -> `{args.reaction_id}`",
                f"- Repacked checksum: `0x{stored_crc:08X}` over payload bytes `[0x10:]`",
                f"- All changed offsets: `{', '.join(f'0x{offset:X}' for offset in changed_offsets)}`",
                "",
                "Every payload byte outside the four-byte checksum field and the selected Reaction word is",
                "identical to the immutable source. Re-unpacking the packed PNG reproduced the audited payload",
                "exactly; no live save file was read for writing or replaced.",
                "",
                "## Artifacts",
                "",
                f"- Fixture PNG: `{fixture_png.name}`",
                f"- Fixture PNG SHA-256: `{savefmt.sha256(fixture_png)}`",
                f"- Re-unpacked fixture payload: `{fixture_payload.name}`",
                f"- Fixture payload SHA-256: `{savefmt.sha256(fixture_payload)}`",
                "",
                "The fixture remains non-deployed. Copy it into the live manual-save path only behind a",
                "separate backup/restore protocol while `FFT_enhanced.exe` is stopped.",
                "",
            ]
        )
        manifest_path.write_text(manifest, encoding="utf-8")

    print(fixture_png)
    print(fixture_payload)
    print(manifest_path)
    print("manual-save equipped-Reaction fixture validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
