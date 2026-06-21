#!/usr/bin/env python3
"""Build disabled RuntimeSettings.MemoryTableProbes from scanner CSV candidates."""
from __future__ import annotations

import argparse
import csv
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_INPUT = ROOT / "work" / "memtable_rip_candidates.csv"
DEFAULT_OUTPUT = ROOT / "work" / "memtable-probe-candidates.disabled.json"


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Convert MEMTABLE scanner candidates into disabled runtime settings.")
    p.add_argument("input", nargs="?", type=Path, default=DEFAULT_INPUT)
    p.add_argument("-o", "--output", type=Path, default=DEFAULT_OUTPUT)
    p.add_argument("--limit", type=int, default=8)
    p.add_argument("--min-score", type=int, default=10)
    p.add_argument("--allow-nonunique", action="store_true", help="Include rows whose context_matches is not exactly 1.")
    p.add_argument("--short-pattern", action="store_true", help="Use the short instruction pattern instead of context_pattern.")
    p.add_argument("--target-section", action="append", help="Only include candidates pointing to this section name.")
    p.add_argument("--count", type=int, default=55)
    p.add_argument("--stride", type=int, default=600)
    return p.parse_args()


def main() -> int:
    args = parse_args()
    if not args.input.exists():
        raise SystemExit(f"candidate CSV not found: {args.input}")

    rows = load_candidate_rows(args.input)
    rows = filter_candidate_rows(rows, args.min_score, not args.allow_nonunique, args.target_section)
    if args.limit > 0:
        rows = rows[: args.limit]

    settings = build_settings(rows, args.short_pattern, args.count, args.stride, args.input)
    args.output.parent.mkdir(exist_ok=True)
    args.output.write_text(json.dumps(settings, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {args.output} ({len(settings['MemoryTableProbes'])} disabled probe candidate(s))")
    return 0


def load_candidate_rows(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))


def filter_candidate_rows(
    rows: list[dict[str, str]],
    min_score: int,
    require_unique_context: bool,
    target_sections: list[str] | None,
) -> list[dict[str, str]]:
    wanted_targets = {target.lower() for target in target_sections} if target_sections else None
    filtered: list[dict[str, str]] = []
    for row in rows:
        if parse_int(row.get("score", "0")) < min_score:
            continue
        if require_unique_context and parse_int(row.get("context_matches", "0")) != 1:
            continue
        if wanted_targets and row.get("target_section", "").lower() not in wanted_targets:
            continue
        filtered.append(row)
    return filtered


def build_settings(rows: list[dict[str, str]], use_short_pattern: bool, count: int, stride: int, source_path: Path) -> dict[str, object]:
    return {
        "_note": (
            "Generated disabled MEMTABLE probe candidates. Review each row and keep Enabled=false "
            "until the candidate is independently validated live."
        ),
        "_source": str(source_path),
        "RewriteObservedDamage": False,
        "RewriteObservedHealing": False,
        "LogResolvedRuntimeContext": True,
        "MemoryTableProbes": [build_probe(row, use_short_pattern, count, stride) for row in rows],
    }


def build_probe(row: dict[str, str], use_short_pattern: bool, count: int, stride: int) -> dict[str, object]:
    if use_short_pattern:
        pattern = row["pattern"]
        rip_relative_offset = parse_int(row["rip_relative_offset"])
        instruction_length = parse_int(row["instruction_length"])
    else:
        pattern = row["context_pattern"]
        rip_relative_offset = parse_int(row["context_rip_relative_offset"])
        instruction_length = parse_int(row["context_instruction_length"])

    return {
        "Name": probe_name(row),
        "Enabled": False,
        "Pattern": pattern,
        "RipRelativeOffset": rip_relative_offset,
        "InstructionLength": instruction_length,
        "TargetAddend": parse_int(row.get("target_addend", "0")),
        "DereferenceCount": 0,
        "Count": count,
        "Stride": stride,
        "LogRows": True,
        "LogEmptyRows": False,
        "MaxRowsToLog": 16,
        "MinPresenceScore": 1,
        "_candidate": {
            "score": parse_int(row.get("score", "0")),
            "contextMatches": parse_int(row.get("context_matches", "0")),
            "instrRva": row.get("instr_rva", ""),
            "targetRva": row.get("target_rva", ""),
            "sourceSection": row.get("source_section", ""),
            "targetSection": row.get("target_section", ""),
            "reasons": row.get("reasons", ""),
        },
        "Fields": default_fields(),
    }


def probe_name(row: dict[str, str]) -> str:
    instr = sanitize_name(row.get("instr_rva", "unknown"))
    target = sanitize_name(row.get("target_section", "section"))
    return f"Candidate_{instr}_{target}"


def sanitize_name(text: str) -> str:
    return re.sub(r"[^A-Za-z0-9_]+", "_", text).strip("_") or "unknown"


def default_fields() -> list[dict[str, object]]:
    return [
        {"Name": "UnitIndex", "Offset": 1, "Width": "Byte", "EmptyValue": 255},
        {"Name": "Job", "Offset": 2, "Width": "Byte", "EmptyValue": 255},
        {"Name": "Index2", "Offset": 44, "Width": "Byte", "EmptyValue": 255},
    ]


def parse_int(text: str | None) -> int:
    if not text:
        return 0
    try:
        if text.lower().startswith("0x"):
            return int(text, 16)
        return int(text)
    except ValueError:
        return 0


if __name__ == "__main__":
    raise SystemExit(main())
