#!/usr/bin/env python3
"""Validate the LT32 ordinary damage-plus-status live transaction."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "work" / "1784097430-lt32-dcl-native-status-packet-live.log"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    parser.add_argument("--output", type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    text = args.log.read_text(encoding="utf-8-sig", errors="replace")

    status_lines = [line for line in text.splitlines() if line.startswith("[DCL-STATUS]")]
    attack_lines = [
        line
        for line in text.splitlines()
        if line.startswith("[DCL]")
        and "caster=0x81 target=0x80 abilityId=0" in line
        and "actionType=0x01" in line
    ]
    auto_potion_lines = [
        line
        for line in text.splitlines()
        if line.startswith("[DCL]")
        and "caster=0x80 target=0x80 abilityId=441" in line
        and "actionType=0x06" in line
    ]

    status_exact = re.compile(
        r"^\[DCL-STATUS\] rule=LT32 Attack stages Blind in native packet "
        r"target=0x80 ability=0 byte=1 mask=0x20 resistance=10 roll=18 "
        r"outcome=packet-add-staged packetAdd=0x20 packetRemove=0x00 flags=0x88$",
        re.MULTILINE,
    )
    attack_exact = re.compile(
        r"^\[DCL\] caster=0x81 target=0x80 abilityId=0 .* actionType=0x01 "
        r"result=14 debit=14 oldDebit=14 credit=0 oldCredit=0 "
        r"mpDebit=0 oldMpDebit=0 mpCredit=0 oldMpCredit=0 flags=0x80->0x88 computePoint=0$",
        re.MULTILINE,
    )
    auto_potion_exact = re.compile(
        r"^\[DCL\] caster=0x80 target=0x80 abilityId=441 ability=Auto-Potion "
        r"actionType=0x06 result=0 debit=0 oldDebit=0 credit=30 oldCredit=30 "
        r"mpDebit=0 oldMpDebit=0 mpCredit=0 oldMpCredit=0 flags=0x40->0x40 computePoint=0$",
        re.MULTILINE,
    )

    checks = [
        (
            "isolated LT32 profile loaded",
            "DclStatusControlEnabled=True, DclStatusRules=1" in text
            and "DclDamageFormula=off" in text
            and "EnabledMods: fftivc.utility.modloader, fftivc.generic.chronicle.codemod" in text,
        ),
        (
            "ordinary managed pre-clamp carrier installed",
            "[PRECLAMP-REWRITE-HOOK]" in text
            and "managedCallback=1" in text
            and "dclPipeline=1" in text,
        ),
        ("exactly one authored status transaction", len(status_lines) == 1),
        ("Blind add bit staged in the native packet", status_exact.search(text) is not None),
        ("exactly one connected Attack delivery", len(attack_lines) == 1),
        (
            "Attack preserves its natural 14 HP debit and adds result bit 0x08",
            attack_exact.search(text) is not None,
        ),
        ("Auto-Potion reaction remains an ordinary native transaction", len(auto_potion_lines) == 1 and auto_potion_exact.search(text) is not None),
        (
            "no managed failure or rollback",
            not re.search(r"\[(?:DCL|PRECLAMP)[^\]]*\].*(?:ERROR|FAIL|ROLLBACK|exception)", text, re.IGNORECASE),
        ),
    ]

    passed = sum(ok for _, ok in checks)
    lines = [
        "# LT32 ordinary native status-packet live analysis",
        "",
        f"Source log: `{args.log.as_posix()}`.",
        "",
        "## Checks",
        "",
    ]
    lines.extend(f"- {'PASS' if ok else 'FAIL'} — {label}" for label, ok in checks)
    lines.extend(
        [
            "",
            f"Result: **{passed}/{len(checks)} PASS**.",
            "",
            "A full pass proves the ordinary DCL pre-clamp carrier can add an authored status bit",
            "to the paired native packet without replacing or duplicating the action's HP result.",
            "The native status committer's durable/effective write remains a separate visual/runtime",
            "observation recorded in the LT32 checkpoint.",
            "",
        ]
    )
    report = "\n".join(lines)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {args.output}")
    else:
        print(report)
    return 0 if passed == len(checks) else 1


if __name__ == "__main__":
    raise SystemExit(main())
