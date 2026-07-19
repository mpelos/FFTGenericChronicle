#!/usr/bin/env python3
"""Require a forecast decision to be reused unchanged by a later calc in the same caster turn."""
from __future__ import annotations

import argparse
import hashlib
import re
import time
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = Path(
    r"D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles"
    r"\battleprobe_log.txt"
)
HIT_RE = re.compile(
    r"^\[DCL-HIT\] caster=(?P<caster>0x[0-9A-F]+) target=(?P<target>0x[0-9A-F]+) "
    r"ability=(?P<ability>\d+) type=(?P<type>0x[0-9A-F]+) payload=(?P<payload>-?\d+) "
    r"activeWeapon=(?P<weapon>-?\d+) repeat=(?P<repeat>\d+/\d+) "
    r"pct=(?P<pct>\d+) roll=(?P<roll>\d+) outcome=(?P<outcome>hit|miss) "
    r"cached=(?P<cached>[01]) turnEpoch=(?P<epoch>-?\d+)"
)


@dataclass(frozen=True)
class HitRow:
    line_number: int
    caster: str
    target: str
    ability: int
    action_type: str
    payload: int
    weapon: int
    repeat: str
    pct: int
    roll: int
    outcome: str
    cached: bool
    epoch: int

    @property
    def key(self) -> tuple[object, ...]:
        return (
            self.caster,
            self.target,
            self.ability,
            self.action_type,
            self.payload,
            self.weapon,
            self.repeat,
            self.epoch,
        )

    @property
    def decision(self) -> tuple[int, int, str]:
        return self.pct, self.roll, self.outcome


def parse_rows(text: str) -> list[HitRow]:
    rows: list[HitRow] = []
    for line_number, line in enumerate(text.splitlines(), start=1):
        match = HIT_RE.match(line)
        if match is None:
            continue
        values = match.groupdict()
        rows.append(
            HitRow(
                line_number=line_number,
                caster=values["caster"],
                target=values["target"],
                ability=int(values["ability"]),
                action_type=values["type"],
                payload=int(values["payload"]),
                weapon=int(values["weapon"]),
                repeat=values["repeat"],
                pct=int(values["pct"]),
                roll=int(values["roll"]),
                outcome=values["outcome"],
                cached=values["cached"] == "1",
                epoch=int(values["epoch"]),
            )
        )
    return rows


def analyze(rows: list[HitRow]) -> tuple[list[tuple[HitRow, HitRow]], list[str]]:
    fresh_by_key: dict[tuple[object, ...], HitRow] = {}
    pairs: list[tuple[HitRow, HitRow]] = []
    errors: list[str] = []
    for row in rows:
        if not row.cached:
            fresh_by_key[row.key] = row
            continue
        fresh = fresh_by_key.get(row.key)
        if fresh is None:
            errors.append(f"line {row.line_number}: cached decision has no same-turn producer")
            continue
        if row.decision != fresh.decision:
            errors.append(
                f"lines {fresh.line_number}->{row.line_number}: cached decision changed "
                f"{fresh.decision}->{row.decision}"
            )
            continue
        pairs.append((fresh, row))
    return pairs, errors


def render(log: Path, rows: list[HitRow], minimum_pairs: int) -> tuple[str, bool]:
    pairs, errors = analyze(rows)
    if len(pairs) < minimum_pairs:
        errors.append(f"expected at least {minimum_pairs} reused pair(s), found {len(pairs)}")
    digest = hashlib.sha256(log.read_bytes()).hexdigest().upper()
    lines = [
        "# DCL hit forecast/execution parity live analysis",
        "",
        f"- Log: `{log}`",
        f"- SHA-256: `{digest}`",
        f"- Parsed turn-epoch hit rows: `{len(rows)}`",
        f"- Exact reused pairs: `{len(pairs)}`",
        "",
        "| Producer -> reuse | Action key | Decision |",
        "| --- | --- | --- |",
    ]
    for fresh, cached in pairs:
        key = (
            f"caster={fresh.caster} target={fresh.target} ability={fresh.ability} "
            f"type={fresh.action_type} payload={fresh.payload} weapon={fresh.weapon} "
            f"repeat={fresh.repeat} epoch={fresh.epoch}"
        )
        lines.append(
            f"| `{fresh.line_number}->{cached.line_number}` | `{key}` | "
            f"`pct={fresh.pct} roll={fresh.roll} outcome={fresh.outcome}` |"
        )
    lines.extend(["", "## Errors", ""])
    if errors:
        lines.extend(f"- {error}" for error in errors)
    else:
        lines.append("- None.")
    passed = not errors
    lines.extend(["", f"Overall live-evidence gate: **{'PASS' if passed else 'FAIL'}**.", ""])
    return "\n".join(lines), passed


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    parser.add_argument("--minimum-pairs", type=int, default=1)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if args.minimum_pairs < 1:
        parser.error("--minimum-pairs must be positive")
    log = args.log.resolve()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-hit-parity-live-analysis.md"
    try:
        text = log.read_text(encoding="utf-8-sig")
        report, passed = render(log, parse_rows(text), args.minimum_pairs)
        output.write_text(report, encoding="utf-8", newline="\n")
    except OSError as error:
        print(f"ERROR: {error}")
        return 1
    print(f"wrote {output}")
    print(f"hit forecast/execution parity {'PASS' if passed else 'FAIL'}")
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
