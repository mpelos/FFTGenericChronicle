#!/usr/bin/env python3
"""Validate live state-0x2A -> state-0x2F native-repeat execution transactions."""
from __future__ import annotations

import argparse
import hashlib
import re
import time
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "work" / "1784201674-lt41h-dcl-dual-wield-safe-final-hook-live.log"

CALC_RE = re.compile(
    r"\[DCL-CALC-PROVENANCE\] n=(?P<n>\d+) origin=(?P<origin>\S+) .*?"
    r"battleState=0x(?P<state>[0-9A-Fa-f]+) .*?casterIdx=(?P<caster>-?\d+) "
    r"type=0x(?P<type>[0-9A-Fa-f]+) abilityId=(?P<ability>-?\d+) "
    r"payload=(?P<payload>-?\d+) targetIdx=(?P<target>-?\d+)"
)
PRECLAMP_RE = re.compile(r"\[DCL-PRECLAMP\].*? debit=(?P<debit>\d+) ")
DCL_RE = re.compile(r"\[DCL\].*? result=(?P<result>-?\d+) debit=(?P<debit>\d+) oldDebit=(?P<old>\d+)")
DAMAGE_RE = re.compile(r"\[DAMAGE .*?\] (?P<before>\d+) -> (?P<after>\d+) = (?P<delta>\d+)")


@dataclass
class Transaction:
    line: int
    n: int
    state: int
    caster: int
    action_type: int
    ability: int
    payload: int
    target: int
    preclamp_debit: int | None = None
    dcl_old_debit: int | None = None
    dcl_debit: int | None = None
    damage_delta: int | None = None

    @property
    def identity(self) -> tuple[int, int, int, int, int]:
        return self.caster, self.action_type, self.ability, self.payload, self.target


def parse_transactions(text: str) -> list[Transaction]:
    transactions: list[Transaction] = []
    current: Transaction | None = None
    for line_number, line in enumerate(text.splitlines(), 1):
        calc = CALC_RE.search(line)
        if calc:
            current = Transaction(
                line=line_number,
                n=int(calc["n"]),
                state=int(calc["state"], 16),
                caster=int(calc["caster"]),
                action_type=int(calc["type"], 16),
                ability=int(calc["ability"]),
                payload=int(calc["payload"]),
                target=int(calc["target"]),
            )
            if calc["origin"] == "outer-sweep" and current.state in (0x2A, 0x2F):
                transactions.append(current)
            else:
                current = None
            continue

        if current is None:
            continue
        preclamp = PRECLAMP_RE.search(line)
        if preclamp and current.preclamp_debit is None:
            current.preclamp_debit = int(preclamp["debit"])
        dcl = DCL_RE.search(line)
        if dcl and current.dcl_old_debit is None:
            current.dcl_old_debit = int(dcl["old"])
            current.dcl_debit = int(dcl["debit"])
        damage = DAMAGE_RE.search(line)
        if damage and current.damage_delta is None:
            current.damage_delta = int(damage["delta"])
    return transactions


def analyze_text(
    text: str,
    minimum_pairs: int = 2,
    *,
    expect_fixed: bool = False,
) -> tuple[list[tuple[Transaction, Transaction]], list[str]]:
    transactions = parse_transactions(text)
    pairs: list[tuple[Transaction, Transaction]] = []
    errors: list[str] = []
    for first, second in zip(transactions, transactions[1:]):
        if first.state == 0x2A and second.state == 0x2F and first.identity == second.identity:
            pairs.append((first, second))

    if len(pairs) < minimum_pairs:
        errors.append(f"expected at least {minimum_pairs} matching 0x2A->0x2F pairs, found {len(pairs)}")

    for first, second in pairs:
        label = f"n={first.n}->{second.n} identity={first.identity}"
        if first.preclamp_debit is None or second.preclamp_debit is None:
            errors.append(f"{label}: missing pre-clamp debit")
            continue
        if first.dcl_old_debit != first.preclamp_debit or first.dcl_debit != 1:
            errors.append(f"{label}: state-0x2A transaction was not rewritten to one")
        if first.damage_delta != 1:
            errors.append(f"{label}: state-0x2A applied delta is {first.damage_delta}, expected 1")
        if expect_fixed:
            if second.dcl_old_debit != second.preclamp_debit or second.dcl_debit != 1:
                errors.append(f"{label}: fixed state-0x2F transaction was not rewritten to one")
            if second.damage_delta != 1:
                errors.append(
                    f"{label}: fixed state-0x2F applied delta is {second.damage_delta}, expected 1"
                )
        else:
            if second.dcl_old_debit is not None:
                errors.append(f"{label}: historical state-0x2F transaction unexpectedly has a DCL rewrite")
            if second.damage_delta != second.preclamp_debit:
                errors.append(
                    f"{label}: historical state-0x2F delta {second.damage_delta} does not equal native debit {second.preclamp_debit}"
                )
    return pairs, errors


def render_report(log: Path, minimum_pairs: int, *, expect_fixed: bool = False) -> tuple[str, bool]:
    raw = log.read_bytes()
    text = raw.decode("utf-8", errors="replace")
    pairs, errors = analyze_text(text, minimum_pairs, expect_fixed=expect_fixed)
    rows = []
    for first, second in pairs:
        identity = (
            f"caster={first.caster}, type=0x{first.action_type:02X}, ability={first.ability}, "
            f"payload={first.payload}, target={first.target}"
        )
        rows.append(
            f"| `{first.n}` / `{second.n}` | `{first.line}` / `{second.line}` | {identity} | "
            f"`{first.preclamp_debit}` / `{second.preclamp_debit}` | "
            f"`{first.damage_delta}` / `{second.damage_delta}` |"
        )
    ok = not errors
    lines = [
        "# DCL native-repeat provenance live analysis",
        "",
        "Generated by `tools/analyze_dcl_native_repeat_provenance_live.py`.",
        "",
        f"- Log: `{log}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Expected behavior: `{'fixed' if expect_fixed else 'historical escape'}`",
        f"- Matching `0x2A -> 0x2F` transactions: `{len(pairs)}` (minimum `{minimum_pairs}`)",
        "",
        "| Calc n | Log lines | Exact action identity | Pre-clamp debit 0x2A / 0x2F | Applied delta 0x2A / 0x2F |",
        "| --- | --- | --- | --- | --- |",
        *rows,
        "",
        "## Conclusion",
        "",
        "State `0x2F` is a second outer-sweep execution transaction with the same exact caster, action type,",
        "ability, order payload, and target as the preceding state-`0x2A` transaction.",
        "The two transactions are validated against their own native pre-clamp debits; equal native amounts",
        "are not required because the engine can calculate a different debit for each repeated result.",
        (
            "In this fixed capture, both transactions independently enter the DCL, rewrite the native debit "
            "to one, and apply a one-point HP delta."
            if expect_fixed
            else "In this historical capture, the DCL rewrites the `0x2A` debit to one while the `0x2F` debit "
            "escapes and applies unchanged."
        ),
        "",
    ]
    if errors:
        lines.extend(["## Errors", "", *[f"- {error}" for error in errors], ""])
    lines.append(f"Overall live-evidence gate: **{'PASS' if ok else 'FAIL'}**.")
    lines.append("")
    return "\n".join(lines), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    parser.add_argument("--minimum-pairs", type=int, default=2)
    parser.add_argument(
        "--expect-fixed",
        action="store_true",
        help="Require state 0x2F to enter the DCL and apply the rewritten one-point debit.",
    )
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-native-repeat-provenance-live-analysis.md"
    report, ok = render_report(args.log, args.minimum_pairs, expect_fixed=args.expect_fixed)
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("native repeat provenance live evidence PASS" if ok else "native repeat provenance live evidence FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
