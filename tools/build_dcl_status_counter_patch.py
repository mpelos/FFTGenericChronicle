#!/usr/bin/env python3
"""Build a minimal StatusEffectData patch that transfers selected durations to DCL."""
from __future__ import annotations

import argparse
import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any

import analyze_dcl_status_counter_authority as authority
import validate_dcl_status_duration_pair as duration_pair


ROOT = Path(__file__).resolve().parents[1]
FORBIDDEN = {"Doom", "Empty_32"}


def parse_names(raw: str) -> tuple[str, ...]:
    names = tuple(dict.fromkeys(token.strip() for token in raw.split(",") if token.strip()))
    if not names:
        raise ValueError("at least one native status name is required")
    return names


def load_settings(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as error:
        raise ValueError(f"cannot read runtime settings {path}: {error}") from error
    if not isinstance(value, dict):
        raise ValueError("runtime settings must be a JSON object")
    return value


def build_patch(
    names: tuple[str, ...],
    output: Path,
    settings: dict[str, Any] | None,
) -> list[authority.NativeStatusRow]:
    rows = authority.load_rows()
    by_name = {row.name: row for row in rows}
    unknown = sorted(set(names) - set(by_name))
    if unknown:
        raise ValueError(f"unknown native status names: {unknown}")
    forbidden = sorted(set(names) & FORBIDDEN)
    if forbidden:
        raise ValueError(
            f"generic counter neutralization is forbidden for lifecycle/system rows: {forbidden}"
        )
    selected = sorted((by_name[name] for name in names), key=lambda row: row.table_index)
    zero_counter = [row.name for row in selected if row.counter <= 0]
    if zero_counter:
        raise ValueError(f"selected rows already have Counter=0: {zero_counter}")
    if settings is None:
        raise ValueError(
            "runtime settings are required before a native counter can be neutralized"
        )
    try:
        duration_pair._validate_duration_owners(settings, set(names))
    except duration_pair.DurationPairError as error:
        raise ValueError(f"incomplete duration ownership: {error}") from error

    root = ET.Element("StatusEffectTable")
    ET.SubElement(root, "Version").text = "1"
    entries = ET.SubElement(root, "Entries")
    for row in selected:
        node = ET.SubElement(entries, "StatusEffect")
        ET.SubElement(node, "Id").text = str(row.table_index)
        ET.SubElement(node, "Counter").text = "0"
    ET.indent(root, space="  ")
    output.parent.mkdir(parents=True, exist_ok=True)
    ET.ElementTree(root).write(output, encoding="utf-8", xml_declaration=True)
    return selected


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def repo_output(path: Path) -> Path:
    result = (ROOT / path).resolve() if not path.is_absolute() else path.resolve()
    try:
        result.relative_to(ROOT)
    except ValueError as exc:
        raise ValueError(f"output must remain inside the repository: {path}") from exc
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--statuses", default="Immobilize,Disable")
    parser.add_argument("--settings", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        output = repo_output(args.output)
        settings_path = repo_output(args.settings)
        selected = build_patch(
            parse_names(args.statuses),
            output,
            load_settings(settings_path),
        )
    except (OSError, ValueError, ET.ParseError) as error:
        print(f"ERROR: {error}")
        return 1
    print("DCL status-counter patch build passed")
    for row in selected:
        print(f"  {row.name}: table_index={row.table_index} Counter={row.counter}->0")
    print(f"  output={output.relative_to(ROOT).as_posix()}")
    print(f"  sha256={sha256(output)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
