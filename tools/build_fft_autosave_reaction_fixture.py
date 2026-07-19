#!/usr/bin/env python3
"""Build a round-trip-audited FFT Enhanced autosave Reaction fixture.

The source PNG and live autosave are never modified. FF16Tools extracts the independent
``resume_*.sav`` members, this tool updates only the current battle's persistent roster copies and
contiguous live-unit copies, and FF16Tools owns every inner CRC plus the enclosing PNG container.
The packed result is unpacked again and rejected unless every component delta is accounted for.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import shutil
import struct
import subprocess
import tempfile
import zlib
from dataclasses import dataclass
from pathlib import Path

import build_fft_manual_ability_fixture as savefmt


REPO = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = REPO / "work"
DEFAULT_ABILITY_BASELINE = REPO / "work" / "wotl_ability_action_baseline.csv"

INNER_HEADER_SIZE = 0x10
INNER_CHECKSUM_OFFSET = 0x04
INNER_CHECKSUM_SIZE = 0x04
BATTLE_ENTRY_OFFSET = 0x16C

ROSTER_NAME_OFFSET = 0xDC
ROSTER_NAME_SIZE = 0x10
ROSTER_REACTION_OFFSET = 0x08
ROSTER_CHARA_NAME_KEY_OFFSET = 0x230
ROSTER_MIN_SIZE = 0x258

LIVE_NAME_OFFSET = 0x14C
LIVE_REACTION_OFFSET = 0x14
LIVE_LEVEL_OFFSET = 0x29
LIVE_BRAVE_OFFSET = 0x2B
LIVE_HP_OFFSET = 0x30
LIVE_MAX_HP_OFFSET = 0x32
LIVE_REACTION_SET_OFFSET = 0x94
LIVE_REACTION_SET_SIZE = 0x04
LIVE_MIN_SIZE = 0x200

REACTION_MIN = 422
REACTION_MAX = 453

CURRENT_COMPONENTS = (
    "resume_en00_world.sav",
    "resume_en00_main.sav",
    "resume_en00_fturn.sav",
    "resume_en00_attack.sav",
    "resume_enbtl_world.sav",
    "resume_enbtl_main.sav",
    "resume_enbtl_attack.sav",
    "resume_enwm_main.sav",
)

CURRENT_ALIAS_GROUPS = (
    ("resume_en00_world.sav", "resume_enbtl_world.sav", "resume_enwm_main.sav"),
    ("resume_en00_fturn.sav", "resume_enbtl_main.sav"),
    ("resume_en00_attack.sav", "resume_enbtl_attack.sav"),
)

CURRENT_BATTLE_COMPONENTS = (
    "resume_en00_main.sav",
    "resume_en00_fturn.sav",
    "resume_en00_attack.sav",
    "resume_enbtl_main.sav",
    "resume_enbtl_attack.sav",
)

EXPECTED_LIVE_COPY_COUNTS = {
    "resume_en00_main.sav": 2,
    "resume_en00_fturn.sav": 2,
    "resume_enbtl_main.sav": 2,
}


@dataclass(frozen=True)
class Identity:
    nickname: str
    character: int
    job: int
    roster_index: int
    chara_name_key: int
    level: int
    brave: int
    max_hp: int


@dataclass(frozen=True)
class Target:
    kind: str
    record_offset: int
    reaction_offset: int


@dataclass(frozen=True)
class ComponentAudit:
    name: str
    source_sha256: str
    fixture_sha256: str
    source_checksum: int
    fixture_checksum: int
    targets: tuple[Target, ...]
    changed_offsets: tuple[int, ...]


def parse_int(value: str) -> int:
    return int(value, 0)


def reaction_set_bit(reaction_id: int) -> tuple[int, int]:
    """Return the live-unit byte offset and mask for one Reaction id."""
    if not REACTION_MIN <= reaction_id <= REACTION_MAX:
        raise ValueError(
            f"Reaction id must be in [{REACTION_MIN}, {REACTION_MAX}]: {reaction_id}"
        )
    relative = reaction_id - REACTION_MIN
    return LIVE_REACTION_SET_OFFSET + relative // 8, 1 << (7 - relative % 8)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a non-deployed, audited Enhanced autosave equipped-Reaction fixture."
    )
    parser.add_argument("--prefix", required=True, help="Unix timestamp filename prefix.")
    parser.add_argument("--source-save", type=Path, required=True, help="Immutable autoenhanced PNG.")
    parser.add_argument("--reaction-id", type=int, default=443, help="Reaction to equip. Default: 443.")
    parser.add_argument(
        "--expected-reaction-id",
        type=int,
        required=True,
        help="Required source Reaction id; protects against the wrong autosave/unit.",
    )
    parser.add_argument("--nickname", default="Rion")
    parser.add_argument("--expected-character", type=parse_int, default=0x80)
    parser.add_argument("--expected-job", type=parse_int, default=0x59)
    parser.add_argument("--expected-roster-index", type=int, default=3)
    parser.add_argument("--expected-chara-name-key", type=int, default=357)
    parser.add_argument("--expected-level", type=int, default=71)
    parser.add_argument("--expected-brave", type=int, default=97)
    parser.add_argument("--expected-max-hp", type=int, default=277)
    parser.add_argument(
        "--allow-unlearned-reaction",
        action="store_true",
        help="Required for a reserved Reaction absent from learned generic-job RSM lists.",
    )
    parser.add_argument("--label", default="lt40-auto-rion-hex-ward")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--ff16tools", type=Path, default=savefmt.DEFAULT_FF16TOOLS)
    parser.add_argument("--ability-baseline", type=Path, default=DEFAULT_ABILITY_BASELINE)
    return parser.parse_args()


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def load_reaction_names(path: Path) -> dict[int, str]:
    result: dict[int, str] = {}
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            if row.get("ability_type") != "Reaction":
                continue
            result[int(row["id_dec"])] = (
                row.get("name_ivc") or row.get("name_wotl") or "<unnamed>"
            )
    if not result:
        raise AssertionError(f"no Reaction rows found in {path}")
    return result


def validate_inner_checksum(name: str, data: bytes) -> int:
    if len(data) < INNER_HEADER_SIZE:
        raise AssertionError(f"{name} is shorter than the 0x10-byte inner header")
    stored = struct.unpack_from("<I", data, INNER_CHECKSUM_OFFSET)[0]
    computed = zlib.crc32(data[INNER_HEADER_SIZE:]) & 0xFFFFFFFF
    if stored != computed:
        raise AssertionError(
            f"{name} checksum mismatch: stored 0x{stored:08X}, computed 0x{computed:08X}"
        )
    return stored


def fixed_ascii(data: bytes, offset: int, size: int) -> str:
    return data[offset : offset + size].split(b"\0", 1)[0].decode("ascii", errors="strict")


def nickname_hits(data: bytes, nickname: str) -> list[int]:
    needle = nickname.encode("ascii") + b"\0"
    result: list[int] = []
    start = 0
    while True:
        found = data.find(needle, start)
        if found < 0:
            return result
        result.append(found)
        start = found + 1


def find_roster_targets(
    data: bytes,
    identity: Identity,
    expected_reaction_id: int,
) -> tuple[Target, ...]:
    result: list[Target] = []
    for name_offset in nickname_hits(data, identity.nickname):
        record = name_offset - ROSTER_NAME_OFFSET
        if record < 0 or record + ROSTER_MIN_SIZE > len(data):
            continue
        if fixed_ascii(data, record + ROSTER_NAME_OFFSET, ROSTER_NAME_SIZE) != identity.nickname:
            continue
        if data[record] != identity.character:
            continue
        if data[record + 1] != identity.roster_index:
            continue
        if data[record + 2] != identity.job:
            continue
        if struct.unpack_from("<H", data, record + ROSTER_CHARA_NAME_KEY_OFFSET)[0] != identity.chara_name_key:
            continue
        reaction_offset = record + ROSTER_REACTION_OFFSET
        if struct.unpack_from("<H", data, reaction_offset)[0] != expected_reaction_id:
            continue
        result.append(Target("roster", record, reaction_offset))
    return tuple(result)


def find_live_targets(
    data: bytes,
    identity: Identity,
    expected_reaction_id: int,
) -> tuple[Target, ...]:
    result: list[Target] = []
    for name_offset in nickname_hits(data, identity.nickname):
        record = name_offset - LIVE_NAME_OFFSET
        if record < 0 or record + LIVE_MIN_SIZE > len(data):
            continue
        if data[record] != identity.character or data[record + 3] != identity.job:
            continue
        if data[record + LIVE_LEVEL_OFFSET] != identity.level:
            continue
        if data[record + LIVE_BRAVE_OFFSET] != identity.brave:
            continue
        if struct.unpack_from("<H", data, record + LIVE_MAX_HP_OFFSET)[0] != identity.max_hp:
            continue
        current_hp = struct.unpack_from("<H", data, record + LIVE_HP_OFFSET)[0]
        if current_hp > identity.max_hp:
            continue
        reaction_offset = record + LIVE_REACTION_OFFSET
        if struct.unpack_from("<H", data, reaction_offset)[0] != expected_reaction_id:
            continue
        result.append(Target("live", record, reaction_offset))
    return tuple(result)


def validate_current_aliases(files: dict[str, bytes]) -> None:
    for group in CURRENT_ALIAS_GROUPS:
        missing = [name for name in group if name not in files]
        if missing:
            raise AssertionError(f"current autosave alias group is incomplete: {missing}")
        hashes = {sha256_bytes(files[name]) for name in group}
        if len(hashes) != 1:
            raise AssertionError(f"current autosave aliases disagree: {group}")


def validate_battle_entry(files: dict[str, bytes]) -> int:
    values: dict[str, int] = {}
    for name in CURRENT_BATTLE_COMPONENTS:
        data = files[name]
        values[name] = struct.unpack_from("<H", data, BATTLE_ENTRY_OFFSET)[0]
    distinct = set(values.values())
    if len(distinct) != 1:
        raise AssertionError(f"current battle components disagree on entry id: {values}")
    battle_entry = distinct.pop()
    if not 0 <= battle_entry <= 511:
        raise AssertionError(f"current battle entry is outside [0, 511]: {battle_entry}")
    return battle_entry


def stage_current_components(
    files: dict[str, bytes],
    identity: Identity,
    expected_reaction_id: int,
    reaction_id: int,
) -> tuple[dict[str, bytes], dict[str, tuple[Target, ...]], int]:
    missing = [name for name in CURRENT_COMPONENTS if name not in files]
    if missing:
        raise AssertionError(f"autosave is missing current components: {missing}")
    validate_current_aliases(files)
    battle_entry = validate_battle_entry(files)

    staged = dict(files)
    targets_by_name: dict[str, tuple[Target, ...]] = {}
    total_live = 0
    for name in CURRENT_COMPONENTS:
        data = files[name]
        roster = find_roster_targets(data, identity, expected_reaction_id)
        if len(roster) != 1:
            raise AssertionError(
                f"{name} expected exactly one roster owner for {identity.nickname}; found {len(roster)}"
            )
        live = find_live_targets(data, identity, expected_reaction_id)
        required_live = EXPECTED_LIVE_COPY_COUNTS.get(name)
        if required_live is not None and len(live) != required_live:
            raise AssertionError(
                f"{name} expected {required_live} contiguous live copies; found {len(live)}"
            )
        targets = roster + live
        total_live += len(live)
        mutable = bytearray(data)
        for target in targets:
            struct.pack_into("<H", mutable, target.reaction_offset, reaction_id)
            if target.kind == "live":
                old_relative, old_mask = reaction_set_bit(expected_reaction_id)
                new_relative, new_mask = reaction_set_bit(reaction_id)
                old_offset = target.record_offset + old_relative
                new_offset = target.record_offset + new_relative
                if mutable[old_offset] & old_mask == 0:
                    raise AssertionError(
                        f"{name} live target at 0x{target.record_offset:X} does not carry "
                        f"Reaction {expected_reaction_id} in its reaction-set bitfield"
                    )
                mutable[old_offset] &= ~old_mask
                mutable[new_offset] |= new_mask
        staged[name] = bytes(mutable)
        targets_by_name[name] = targets

    if total_live != sum(EXPECTED_LIVE_COPY_COUNTS.values()):
        raise AssertionError(
            f"unexpected total contiguous live copies: {total_live} != {sum(EXPECTED_LIVE_COPY_COUNTS.values())}"
        )
    return staged, targets_by_name, battle_entry


def unpack_save(ff16tools: Path, source: Path, output_dir: Path) -> dict[str, bytes]:
    subprocess.run(
        [str(ff16tools), "unpack-save", "-i", str(source), "-o", str(output_dir)],
        check=True,
    )
    files = {
        path.name: path.read_bytes()
        for path in output_dir.iterdir()
        if path.is_file()
    }
    if not files or "fftsave.bin" in files:
        raise AssertionError(f"{source} did not unpack as an Enhanced autosave container")
    return files


def pack_save(ff16tools: Path, source_dir: Path, output: Path) -> None:
    subprocess.run(
        [str(ff16tools), "pack-save", "-i", str(source_dir), "-o", str(output), "-g", "fft", "-s"],
        check=True,
    )
    if not output.is_file() or output.stat().st_size == 0:
        raise AssertionError(f"FF16Tools did not emit a non-empty save: {output}")


def audit_roundtrip(
    source: dict[str, bytes],
    roundtrip: dict[str, bytes],
    targets_by_name: dict[str, tuple[Target, ...]],
    expected_reaction_id: int,
    reaction_id: int,
) -> tuple[ComponentAudit, ...]:
    if set(source) != set(roundtrip):
        raise AssertionError(
            f"autosave member set changed: removed={sorted(set(source) - set(roundtrip))}, "
            f"added={sorted(set(roundtrip) - set(source))}"
        )

    audits: list[ComponentAudit] = []
    for name in sorted(source):
        before = source[name]
        after = roundtrip[name]
        if len(before) != len(after):
            raise AssertionError(f"{name} length changed: {len(before)} -> {len(after)}")
        source_checksum = validate_inner_checksum(name, before)
        fixture_checksum = validate_inner_checksum(name, after)
        targets = targets_by_name.get(name, ())
        allowed = set(range(INNER_CHECKSUM_OFFSET, INNER_CHECKSUM_OFFSET + INNER_CHECKSUM_SIZE))
        for target in targets:
            allowed.update((target.reaction_offset, target.reaction_offset + 1))
            if struct.unpack_from("<H", before, target.reaction_offset)[0] != expected_reaction_id:
                raise AssertionError(f"{name} source target no longer contains {expected_reaction_id}")
            if struct.unpack_from("<H", after, target.reaction_offset)[0] != reaction_id:
                raise AssertionError(f"{name} fixture target does not contain {reaction_id}")
            if target.kind == "live":
                old_relative, old_mask = reaction_set_bit(expected_reaction_id)
                new_relative, new_mask = reaction_set_bit(reaction_id)
                old_offset = target.record_offset + old_relative
                new_offset = target.record_offset + new_relative
                before_set = bytearray(
                    before[
                        target.record_offset + LIVE_REACTION_SET_OFFSET :
                        target.record_offset + LIVE_REACTION_SET_OFFSET + LIVE_REACTION_SET_SIZE
                    ]
                )
                after_set = after[
                    target.record_offset + LIVE_REACTION_SET_OFFSET :
                    target.record_offset + LIVE_REACTION_SET_OFFSET + LIVE_REACTION_SET_SIZE
                ]
                if before[old_offset] & old_mask == 0:
                    raise AssertionError(
                        f"{name} source live target at 0x{target.record_offset:X} lacks "
                        f"Reaction {expected_reaction_id} in its reaction-set bitfield"
                    )
                before_set[old_relative - LIVE_REACTION_SET_OFFSET] &= ~old_mask
                before_set[new_relative - LIVE_REACTION_SET_OFFSET] |= new_mask
                if after_set != bytes(before_set):
                    raise AssertionError(
                        f"{name} fixture live target at 0x{target.record_offset:X} has an "
                        "inconsistent reaction-set bitfield"
                    )
                allowed.update((old_offset, new_offset))
        changed = tuple(
            index
            for index, pair in enumerate(zip(before, after, strict=True))
            if pair[0] != pair[1]
        )
        unexpected = [offset for offset in changed if offset not in allowed]
        if unexpected:
            raise AssertionError(f"{name} has unexpected changed bytes: {unexpected[:32]}")
        if targets and not changed:
            raise AssertionError(f"{name} owns targets but has no round-trip delta")
        if not targets and changed:
            raise AssertionError(f"stale/non-current member changed unexpectedly: {name}")
        audits.append(
            ComponentAudit(
                name=name,
                source_sha256=sha256_bytes(before),
                fixture_sha256=sha256_bytes(after),
                source_checksum=source_checksum,
                fixture_checksum=fixture_checksum,
                targets=targets,
                changed_offsets=changed,
            )
        )
    validate_current_aliases(roundtrip)
    return tuple(audits)


def build_manifest(
    *,
    source_save: Path,
    fixture_png: Path,
    source_sha256: str,
    identity: Identity,
    expected_reaction_id: int,
    reaction_id: int,
    reaction_names: dict[int, str],
    battle_entry: int,
    audits: tuple[ComponentAudit, ...],
) -> str:
    changed = [audit for audit in audits if audit.targets]
    lines = [
        "# FFT Enhanced autosave equipped-Reaction fixture",
        "",
        "## Scope",
        "",
        "This non-deployed fixture changes the selected owner in the current battle aliases only.",
        "Stale `en01`, `en02`, and `enma` members remain byte-identical to the source container.",
        "FF16Tools recomputes each changed member CRC and owns the enclosing PNG serialization.",
        "",
        "## Source and identity",
        "",
        f"- Immutable source autosave: `{source_save}`",
        f"- Source autosave SHA-256: `{source_sha256}`",
        f"- Battle entry id: `{battle_entry}`",
        f"- Owner: `{identity.nickname}`; character `0x{identity.character:02X}`; job `0x{identity.job:02X}`",
        f"- Roster index: `{identity.roster_index}`; CharaNameKey: `{identity.chara_name_key}`",
        f"- Live fingerprint: level `{identity.level}`, Brave `{identity.brave}`, max HP `{identity.max_hp}`",
        f"- Reaction: `{expected_reaction_id}` ({reaction_names[expected_reaction_id]}) -> "
        f"`{reaction_id}` ({reaction_names[reaction_id]})",
        f"- Live reaction-set transition: `unit+0x{reaction_set_bit(expected_reaction_id)[0]:X} "
        f"& 0x{reaction_set_bit(expected_reaction_id)[1]:02X}` -> "
        f"`unit+0x{reaction_set_bit(reaction_id)[0]:X} & "
        f"0x{reaction_set_bit(reaction_id)[1]:02X}`; unrelated bits are preserved",
        "- Learned-state policy: explicit test-only bypass for reserved Reaction 443",
        "",
        "## Current-member proof",
        "",
        "The source preserves the current alias equalities `en00_world == enbtl_world == enwm_main`,",
        "`en00_fturn == enbtl_main`, and `en00_attack == enbtl_attack`. They remain equal after",
        "the audited edit. Current battle-state members agree on the same entry id.",
        "",
        "| Member | Targets | Source CRC | Fixture CRC | Changed offsets |",
        "| --- | --- | ---: | ---: | --- |",
    ]
    for audit in changed:
        rendered_targets: list[str] = []
        for target in audit.targets:
            rendered = f"{target.kind}@0x{target.reaction_offset:X}"
            if target.kind == "live":
                set_offsets = sorted(
                    {
                        target.record_offset + reaction_set_bit(expected_reaction_id)[0],
                        target.record_offset + reaction_set_bit(reaction_id)[0],
                    }
                )
                rendered += "/set@" + "+".join(f"0x{offset:X}" for offset in set_offsets)
            rendered_targets.append(rendered)
        targets = ", ".join(rendered_targets)
        offsets = ", ".join(f"0x{offset:X}" for offset in audit.changed_offsets)
        lines.append(
            f"| `{audit.name}` | {targets} | `0x{audit.source_checksum:08X}` | "
            f"`0x{audit.fixture_checksum:08X}` | {offsets} |"
        )
    lines.extend(
        [
            "",
            "Every member not listed in the table is byte-identical after pack/unpack. Every listed",
            "member changes only its four-byte CRC field, the enumerated Reaction words, and the",
            "enumerated live reaction-set bytes. The packed PNG was unpacked again before these",
            "claims were emitted.",
            "",
            "## Artifact",
            "",
            f"- Fixture PNG: `{fixture_png.name}`",
            f"- Fixture PNG SHA-256: `{savefmt.sha256(fixture_png)}`",
            "",
            "The fixture remains non-deployed. Restore it only with the stopped-process autosave protocol.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    args = parse_args()
    prefix = str(args.prefix).strip()
    if not prefix.isdigit() or len(prefix) < 10:
        raise SystemExit("--prefix must be a Unix timestamp with at least ten digits")
    if args.reaction_id == args.expected_reaction_id:
        raise SystemExit("--reaction-id must differ from --expected-reaction-id")
    if args.reaction_id == 443 and not args.allow_unlearned_reaction:
        raise SystemExit("Reaction 443 requires explicit --allow-unlearned-reaction")

    source_save = args.source_save.resolve()
    ff16tools = args.ff16tools.resolve()
    ability_baseline = args.ability_baseline.resolve()
    for required in (source_save, ff16tools, ability_baseline):
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
    manifest_path = output_dir / f"{prefix}-{label}-fixture-manifest.md"
    existing = [path for path in (fixture_png, manifest_path) if path.exists()]
    if existing:
        raise SystemExit("refusing to overwrite fixture artifacts: " + ", ".join(map(str, existing)))

    identity = Identity(
        nickname=args.nickname,
        character=args.expected_character,
        job=args.expected_job,
        roster_index=args.expected_roster_index,
        chara_name_key=args.expected_chara_name_key,
        level=args.expected_level,
        brave=args.expected_brave,
        max_hp=args.expected_max_hp,
    )
    source_sha256 = savefmt.sha256(source_save)

    with tempfile.TemporaryDirectory(prefix="gc_fft_autosave_reaction_", ignore_cleanup_errors=True) as tmp:
        temp = Path(tmp)
        source_dir = temp / "source"
        source_dir.mkdir()
        source_files = unpack_save(ff16tools, source_save, source_dir)
        for name, data in source_files.items():
            validate_inner_checksum(name, data)

        staged, targets_by_name, battle_entry = stage_current_components(
            source_files,
            identity,
            args.expected_reaction_id,
            args.reaction_id,
        )
        for name, data in staged.items():
            (source_dir / name).write_bytes(data)

        packed_temp = temp / "fixture.png"
        pack_save(ff16tools, source_dir, packed_temp)
        roundtrip_dir = temp / "roundtrip"
        roundtrip_dir.mkdir()
        roundtrip_files = unpack_save(ff16tools, packed_temp, roundtrip_dir)
        audits = audit_roundtrip(
            source_files,
            roundtrip_files,
            targets_by_name,
            args.expected_reaction_id,
            args.reaction_id,
        )

        shutil.copyfile(packed_temp, fixture_png)
        manifest = build_manifest(
            source_save=source_save,
            fixture_png=fixture_png,
            source_sha256=source_sha256,
            identity=identity,
            expected_reaction_id=args.expected_reaction_id,
            reaction_id=args.reaction_id,
            reaction_names=reaction_names,
            battle_entry=battle_entry,
            audits=audits,
        )
        manifest_path.write_text(manifest, encoding="utf-8")

    print(fixture_png)
    print(manifest_path)
    print("autosave equipped-Reaction fixture validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
