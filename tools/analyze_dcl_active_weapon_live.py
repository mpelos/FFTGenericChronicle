#!/usr/bin/env python3
"""Validate live native right/left weapon routing on a Dual Wield calculation pair."""
from __future__ import annotations

import argparse
import hashlib
import re
import time
from dataclasses import dataclass, replace
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]

CALC_RE = re.compile(
    r"\[DCL-CALC-PROVENANCE\] n=(?P<n>\d+) origin=(?P<origin>\S+) .*?"
    r"battleState=0x(?P<state>[0-9A-Fa-f]+) turnOwner=(?P<turn_owner>-?\d+) .*?"
    r"casterIdx=(?P<caster>-?\d+) "
    r"type=0x(?P<type>[0-9A-Fa-f]+) abilityId=(?P<ability>-?\d+) "
    r"payload=(?P<payload>-?\d+) activeWeapon=(?P<active>-?\d+) "
    r"repeat=(?P<index>-?\d+)/(?P<count>-?\d+) "
    r"nativeWeapons=(?P<right>-?\d+)/(?P<left>-?\d+) targetIdx=(?P<target>-?\d+)"
)

PRECLAMP_RE = re.compile(
    r"\[DCL-PRECLAMP\] .*?targetIdx=(?P<target>-?\d+) debit=(?P<debit>-?\d+) .*?"
    r"latest=outer-sweep:state=0x(?P<state>[0-9A-Fa-f]+):caster=(?P<caster>-?\d+):"
    r"type=0x(?P<type>[0-9A-Fa-f]+):ability=(?P<ability>-?\d+):payload=(?P<payload>-?\d+)"
)


@dataclass(frozen=True)
class Calc:
    line: int
    n: int
    state: int
    turn_owner: int
    caster: int
    action_type: int
    ability: int
    payload: int
    active: int
    index: int
    count: int
    right: int
    left: int
    target: int
    native_debit: int | None = None

    @property
    def order_identity(self) -> tuple[int, int, int, int, int]:
        return self.caster, self.action_type, self.ability, self.payload, self.target

    @property
    def ordinary_owner(self) -> bool:
        return self.caster == self.turn_owner


def parse_calcs(text: str) -> list[Calc]:
    rows: list[Calc] = []
    pending_row: int | None = None
    for line_number, line in enumerate(text.splitlines(), 1):
        match = CALC_RE.search(line)
        if match and match["origin"] == "outer-sweep":
            rows.append(Calc(
                line=line_number,
                n=int(match["n"]),
                state=int(match["state"], 16),
                turn_owner=int(match["turn_owner"]),
                caster=int(match["caster"]),
                action_type=int(match["type"], 16),
                ability=int(match["ability"]),
                payload=int(match["payload"]),
                active=int(match["active"]),
                index=int(match["index"]),
                count=int(match["count"]),
                right=int(match["right"]),
                left=int(match["left"]),
                target=int(match["target"]),
            ))
            pending_row = len(rows) - 1
            continue

        preclamp = PRECLAMP_RE.search(line)
        if preclamp is None or pending_row is None:
            continue
        row = rows[pending_row]
        preclamp_identity = (
            int(preclamp["caster"]),
            int(preclamp["type"], 16),
            int(preclamp["ability"]),
            int(preclamp["payload"]),
            int(preclamp["target"]),
        )
        if int(preclamp["state"], 16) == row.state and preclamp_identity == row.order_identity:
            rows[pending_row] = replace(row, native_debit=int(preclamp["debit"]))
            pending_row = None
    return rows


def analyze_text(
    text: str,
    *,
    payload: int,
    expected_right: int,
    expected_left: int,
) -> tuple[list[tuple[Calc, Calc]], list[str]]:
    rows = parse_calcs(text)
    pairs = [
        (first, second)
        for first, second in zip(rows, rows[1:])
        if first.state == 0x2A
        and second.state == 0x2F
        and first.order_identity == second.order_identity
        and first.action_type == 1
        and first.payload == payload
    ]
    errors: list[str] = []
    if not pairs:
        errors.append(f"no matching type-1 payload-{payload} state-0x2A -> state-0x2F pair")
        return pairs, errors

    completed_ordinary_pairs = 0
    for first, second in pairs:
        label = f"n={first.n}->{second.n}"
        expected_common = (2, expected_right, expected_left)
        if (first.count, first.right, first.left) != expected_common:
            errors.append(
                f"{label}: first row carrier {(first.count, first.right, first.left)} != {expected_common}"
            )
        if (second.count, second.right, second.left) != expected_common:
            errors.append(
                f"{label}: second row carrier {(second.count, second.right, second.left)} != {expected_common}"
            )
        expected_first_active = expected_right if first.count > 2 or first.index == 0 else expected_left
        expected_second_active = expected_right if second.count > 2 or second.index == 0 else expected_left
        if first.active != expected_first_active:
            errors.append(
                f"{label}: state-0x2A selected {(first.index, first.active)}, expected active {expected_first_active}"
            )
        if second.active != expected_second_active:
            errors.append(
                f"{label}: state-0x2F selected {(second.index, second.active)}, expected active {expected_second_active}"
            )
        completed = (
            first.native_debit is not None and first.native_debit > 0
            and second.native_debit is not None and second.native_debit > 0
        )
        if completed and first.ordinary_owner and second.ordinary_owner:
            completed_ordinary_pairs += 1
            if (first.index, first.active) != (0, expected_right):
                errors.append(
                    f"{label}: completed ordinary first strike selected {(first.index, first.active)}, "
                    f"expected {(0, expected_right)}"
                )
            if (second.index, second.active) != (1, expected_left):
                errors.append(
                    f"{label}: completed ordinary second strike selected {(second.index, second.active)}, "
                    f"expected {(1, expected_left)}"
                )
    if completed_ordinary_pairs == 0:
        errors.append("no completed ordinary-owner pair with positive native debits on both strikes")
    return pairs, errors


def render_report(
    log: Path,
    *,
    payload: int,
    expected_right: int,
    expected_left: int,
) -> tuple[str, bool]:
    raw = log.read_bytes()
    pairs, errors = analyze_text(
        raw.decode("utf-8", errors="replace"),
        payload=payload,
        expected_right=expected_right,
        expected_left=expected_left,
    )
    rows = [
        f"| `{first.n}` / `{second.n}` | `{first.line}` / `{second.line}` | `{first.payload}` / `{second.payload}` | "
        f"`{first.index}/{first.count}` / `{second.index}/{second.count}` | "
        f"`{first.right}/{first.left}` / `{second.right}/{second.left}` | "
        f"`{first.active}` / `{second.active}` | "
        f"`{first.native_debit if first.native_debit is not None else '-'} / "
        f"{second.native_debit if second.native_debit is not None else '-'}` | "
        f"`{'ordinary' if first.ordinary_owner and second.ordinary_owner else 'reaction/other'}` |"
        for first, second in pairs
    ]
    lines = [
        "# DCL active-weapon live analysis",
        "",
        "Generated by `tools/analyze_dcl_active_weapon_live.py`.",
        "",
        f"- Log: `{log}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Required order payload: `{payload}`",
        f"- Expected normalized weapons: right `{expected_right}`, left `{expected_left}`",
        "",
        "| Calc n | Log lines | Payload 0x2A / 0x2F | Repeat index/count | Native right/left | Active weapon | Native debit | Owner |",
        "| --- | --- | --- | --- | --- | --- | --- | --- |",
        *rows,
        "",
    ]
    if errors:
        lines.extend(["## Errors", "", *[f"- {error}" for error in errors], ""])
    else:
        lines.extend([
            "## Conclusion",
            "",
            "At least one completed ordinary Dual Wield pair carries positive native debits on both",
            "transactions and selects right/index `0` followed by left/index `1`. The native repeat",
            "carrier, not the unchanged order payload or battle-state label alone, owns hand identity.",
            "Rows whose follow-up native debit is zero remain diagnostic cancellation/reinitialization",
            "attempts; their active item must still agree with their captured native index, but they do",
            "not prove or falsify a completed off-hand strike.",
            "",
        ])
    lines.append(f"Overall live-evidence gate: **{'PASS' if not errors else 'FAIL'}**.")
    lines.append("")
    return "\n".join(lines), not errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--payload", type=int, required=True)
    parser.add_argument("--expected-right", type=int, required=True)
    parser.add_argument("--expected-left", type=int, required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-active-weapon-live-analysis.md"
    report, ok = render_report(
        args.log,
        payload=args.payload,
        expected_right=args.expected_right,
        expected_left=args.expected_left,
    )
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("active-weapon live evidence PASS" if ok else "active-weapon live evidence FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
