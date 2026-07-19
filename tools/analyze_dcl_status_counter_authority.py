#!/usr/bin/env python3
"""Audit native StatusEffectData counters against DCL per-source duration ownership."""
from __future__ import annotations

import argparse
import hashlib
import re
import struct
import time
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

import pefile

import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE
DEFAULT_XML = (
    ROOT / "work/external/fftivc.utility.modloader/fftivc.utility.modloader"
    / "TableData/StatusEffectData.xml"
)
ENUM_SOURCE = (
    ROOT / "work/external/fftivc.utility.modloader/fftivc.utility.modloader.Interfaces"
    / "Tables/Structures/STATUS_EFFECT_DATA.cs"
)
MOD_SOURCE = ROOT / "codemod/fftivc.generic.chronicle.codemod/Mod.cs"


DCL_TO_NATIVE = {
    "Crystal": "Crystal",
    "Dead": "KO",
    "Undead": "Undead",
    "Petrify": "Stone",
    "Invite": "Traitor",
    "Darkness": "Blindness",
    "Confusion": "Confusion",
    "Silence": "Silence",
    "BloodSuck": "Vampire",
    "Oil": "Oil",
    "Float": "Float",
    "Reraise": "Reraise",
    "Transparent": "Invisibility",
    "Berserk": "Berserk",
    "Frog": "Toad",
    "Poison": "Poison",
    "Regen": "Regen",
    "Protect": "Protect",
    "Shell": "Shell",
    "Haste": "Haste",
    "Slow": "Slow",
    "Stop": "Stop",
    "Faith": "Faith",
    "Innocent": "Atheist",
    "Charm": "Charmed",
    "Sleep": "Sleep",
    "DontMove": "Immobilize",
    "DontAct": "Disable",
    "Reflect": "Reflect",
    "DeathSentence": "Doom",
}


@dataclass(frozen=True)
class NativeStatusRow:
    table_index: int
    enum_value: int
    name: str
    order: int
    counter: int
    check_flags: tuple[str, ...]
    cancel_flags: tuple[str, ...]
    no_stack_flags: tuple[str, ...]
    encoded: bytes


def _enum_block(source: str, name: str) -> str:
    match = re.search(rf"public enum {re.escape(name)}[^{{]*\{{(.*?)\}}", source, re.S)
    if not match:
        raise ValueError(f"enum {name} not found")
    return match.group(1)


def parse_status_enum(source: str) -> dict[str, int]:
    block = _enum_block(source, "StatusEffectType")
    values = {
        name: int(value)
        for name, value in re.findall(r"^\s*(\w+)\s*=\s*(\d+)\s*,?", block, re.M)
    }
    if sorted(values.values()) != list(range(1, 41)):
        raise ValueError("StatusEffectType must cover the exact one-based range 1..40")
    return values


def parse_check_enum(source: str) -> dict[str, int]:
    block = _enum_block(source, "StatusCheckFlags")
    values = {
        name: 1 << int(bit)
        for name, bit in re.findall(r"^\s*(\w+)\s*=\s*1\s*<<\s*(\d+)\s*,?", block, re.M)
    }
    if len(values) != 16:
        raise ValueError(f"expected 16 StatusCheckFlags, found {len(values)}")
    return values


def _tokens(node: ET.Element, child_name: str) -> tuple[str, ...]:
    parent = node.find(child_name)
    if parent is None:
        return ()
    return tuple(
        child.text.strip()
        for child in parent.findall("StatusEffectType")
        if child.text and child.text.strip()
    )


def _check_tokens(raw: str | None) -> tuple[str, ...]:
    if raw is None or raw.strip() in ("", "0"):
        return ()
    return tuple(token.strip() for token in raw.split(",") if token.strip())


def _status_mask(tokens: tuple[str, ...], status_values: dict[str, int]) -> bytes:
    result = bytearray(5)
    for token in tokens:
        value = status_values[token]
        bit = value - 1
        result[bit // 8] |= 0x80 >> (bit % 8)
    return bytes(result)


def load_rows(xml_path: Path = DEFAULT_XML, enum_path: Path = ENUM_SOURCE) -> list[NativeStatusRow]:
    source = enum_path.read_text(encoding="utf-8-sig")
    status_values = parse_status_enum(source)
    check_values = parse_check_enum(source)
    names_by_value = {value: name for name, value in status_values.items()}

    root = ET.parse(xml_path).getroot()
    nodes = root.findall("./Entries/StatusEffect")
    ids = [int(node.findtext("Id", "-1")) for node in nodes]
    if ids != list(range(40)):
        raise ValueError(f"StatusEffectData ids must be the exact zero-based range 0..39, got {ids}")

    rows: list[NativeStatusRow] = []
    for node, table_index in zip(nodes, ids, strict=True):
        enum_value = table_index + 1
        name = names_by_value[enum_value]
        unused0 = int(node.findtext("Unused_0x00", "0"))
        unused1 = int(node.findtext("Unused_0x01", "0"))
        order = int(node.findtext("Order", "0"))
        counter = int(node.findtext("Counter", "0"))
        checks = _check_tokens(node.findtext("CheckFlags"))
        check_mask = 0
        for token in checks:
            check_mask |= check_values[token]
        cancels = _tokens(node, "CancelFlags")
        no_stacks = _tokens(node, "NoStackFlags")
        encoded = (
            struct.pack("<BBBBH", unused0, unused1, order, counter, check_mask)
            + _status_mask(cancels, status_values)
            + _status_mask(no_stacks, status_values)
        )
        if len(encoded) != 16:
            raise AssertionError("STATUS_EFFECT_DATA must remain 16 bytes")
        rows.append(NativeStatusRow(
            table_index=table_index,
            enum_value=enum_value,
            name=name,
            order=order,
            counter=counter,
            check_flags=checks,
            cancel_flags=cancels,
            no_stack_flags=no_stacks,
            encoded=encoded,
        ))

    missing = sorted(set(DCL_TO_NATIVE.values()) - set(status_values))
    if missing:
        raise ValueError(f"DCL status names absent from native enum: {missing}")
    return rows


def encode_table(rows: list[NativeStatusRow]) -> bytes:
    return b"".join(row.encoded for row in rows)


def find_exact_table(exe: Path, table: bytes) -> tuple[int, int, str]:
    raw = exe.read_bytes()
    offsets: list[int] = []
    cursor = 0
    while True:
        found = raw.find(table, cursor)
        if found < 0:
            break
        offsets.append(found)
        cursor = found + 1
    if len(offsets) != 1:
        raise ValueError(f"expected one exact 640-byte StatusEffectData table, found {len(offsets)}")
    pe = pefile.PE(data=raw, fast_load=True)
    return offsets[0], pe.get_rva_from_offset(offsets[0]), hashlib.sha256(raw).hexdigest().upper()


def duration_authority(rows: list[NativeStatusRow]) -> tuple[list[NativeStatusRow], list[str]]:
    timed = [row for row in rows if row.counter > 0]
    mod_source = MOD_SOURCE.read_text(encoding="utf-8")
    required_source = (
        "DurationTargetTurns",
        "_dclStatusDurations",
        "outcome=native-clear",
        "DclStatusDurationTracker.Advance",
    )
    missing = [token for token in required_source if token not in mod_source]
    return timed, missing


def render_report(exe: Path, rows: list[NativeStatusRow], output: Path) -> tuple[str, bool]:
    table = encode_table(rows)
    file_offset, rva, exe_hash = find_exact_table(exe, table)
    timed, source_missing = duration_authority(rows)
    by_name = {row.name: row for row in rows}
    mapped_timed = sorted(
        {
            native
            for native in DCL_TO_NATIVE.values()
            if by_name[native].counter > 0
        }
    )
    overall = not source_missing and len(rows) == 40 and len(table) == 640

    lines = [
        "# DCL native status-counter authority analysis",
        "",
        "Generated by `tools/analyze_dcl_status_counter_authority.py`.",
        "",
        "## Current-build identity",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{exe_hash}`",
        f"- Exact 640-byte table: file offset `0x{file_offset:X}`, RVA `0x{rva:X}`.",
        f"- Output: `{output}`",
        "",
        "The modloader XML serializes byte-for-byte to the unique table embedded in the current",
        "Enhanced executable. `StatusEffectData.Id` is a zero-based table index, while",
        "`StatusEffectType` is one-based. The authoritative mapping is therefore",
        "`table_index = enum_value - 1`. Template comments that label a row with the same numeric",
        "enum value are displaced by one; row 24 is Poison and row 39 is Doom.",
        "",
        "## Native rows",
        "",
        "| Table index | Enum | Status | Order | Counter | Check flags | Cancels | No-stack |",
        "| ---: | ---: | --- | ---: | ---: | --- | --- | --- |",
    ]
    for row in rows:
        lines.append(
            f"| {row.table_index} | {row.enum_value} | {row.name} | {row.order} | {row.counter} | "
            f"{', '.join(row.check_flags) or '-'} | {', '.join(row.cancel_flags) or '-'} | "
            f"{', '.join(row.no_stack_flags) or '-'} |"
        )
    lines.extend([
        "",
        "## Duration authority consequence",
        "",
        f"Native nonzero counters exist on {len(timed)} rows: "
        + ", ".join(f"{row.name}={row.counter}" for row in timed) + ".",
        "",
        "The DCL duration tracker owns a second clock keyed by target pointer and status bit. Its",
        "poller explicitly abandons ownership with `outcome=native-clear` whenever the engine clears",
        "the bit first. Therefore a DCL `DurationTargetTurns` value is currently an upper bound, not",
        "exclusive duration authority, for every status whose native Counter remains nonzero.",
        "",
        "DCL-authored statuses affected by this dual clock: " + ", ".join(mapped_timed) + ".",
        "",
        "Source-specific Stun/Knockdown versus magical Disable/Immobilize cannot be complete until",
        "their shared native counters are neutralized and every producer of those bits has a paired",
        "DCL duration rule. Counter neutralization must fail closed on incomplete producer coverage;",
        "otherwise a native action can create a permanent status. Doom is a separate lifecycle",
        "exception: its native counter ends in KO rather than ordinary expiry and must not be replaced",
        "by the generic DCL clear-on-expiry path.",
        "",
        "## Source contract",
        "",
        *[f"- `{token}`: {'MISSING' if token in source_missing else 'PASS'}" for token in (
            "DurationTargetTurns", "_dclStatusDurations", "outcome=native-clear", "DclStatusDurationTracker.Advance"
        )],
        "",
        f"Overall static gate: **{'PASS' if overall else 'FAIL'}**.",
        "",
    ])
    return "\n".join(lines), overall


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--xml", type=Path, default=DEFAULT_XML)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-status-counter-authority-analysis.md"
    try:
        rows = load_rows(args.xml)
        report, ok = render_report(args.exe, rows, output)
    except (OSError, KeyError, ValueError, ET.ParseError) as error:
        print(f"ERROR: {error}")
        print("DCL status-counter authority analysis FAIL")
        return 1
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output.resolve()}")
    print("DCL status-counter authority analysis PASS" if ok else "DCL status-counter authority analysis FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())

