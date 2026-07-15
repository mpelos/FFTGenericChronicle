#!/usr/bin/env python3
"""Verify the LT38 forced-KO then fixed-credit Raise transaction."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    text = args.log.read_text(encoding="utf-8-sig", errors="replace")

    raise_execution = re.search(
        r"\[DCL-COMPUTE-POINT\].*battleState=0x2A caster=0x80 target=0x81 ability=5 .*"
        r"hp=\d+/\d+->0/111 .*cached=1", text)
    native_heal = re.search(r"\[HEALING [^\]]*id=0x81\] 0 -> 91 = 91", text)
    dead_clear = re.search(r"\[DIFF [^\]]*id=0x81\].*\+0x61:20->00", text)

    checks = {
        "compute-point writer installed": bool(re.search(
            r"\[POST-CALC-HOOK\].*numericWriter=1", text)),
        "forced Death executed once": bool(re.search(
            r"\[DCL-KO-COMPUTE\] phase=execution .*target=0x81 ability=30 .*roll=18 outcome=engine-owned-ko", text)),
        "Death reached native zero HP": bool(re.search(
            r"\[DAMAGE [^\]]*id=0x81\] \d+ -> 0 = \d+", text)),
        "Raise execution stages authored 111 credit": bool(raise_execution),
        "native max-HP clamp applies 91 of staged 111": bool(native_heal) and bool(re.search(
            r"\[UNIT [^\]]*id=0x81[^\]]*\] Lv39 HP91 ", text)),
        "native lifecycle clears effective Dead after HP apply": bool(
            raise_execution and native_heal and dead_clear
            and raise_execution.start() < native_heal.start() < dead_clear.start()),
        "legacy instant-KO fallback unused": "[DCL-KO]" not in text,
        "no managed failure": not any(token in text for token in (
            "[DCL-COMPUTE-POINT-ERR]", "[DCL-COMPUTE-POINT-ROLLBACK]", "[DCL-KO-ERR]", "[DCL-ERR]")),
    }

    lines = ["# DCL Raise/revive live analysis", "", "## Checks", ""]
    for name, passed in checks.items():
        lines.append(f"- {'PASS' if passed else 'FAIL'} — {name}")
    lines.extend([
        "",
        "## Interpretation",
        "",
        "A passing transaction proves that the unified compute-point writer replaces Raise's native",
        "46-credit packet with the authored 111 credit while preserving the native KO-target packet and",
        "lifecycle tail. The native apply clamps that credit to the target's 91 maximum HP, restores HP,",
        "and only then clears the effective Dead mirror; no direct status write is required.",
        "",
    ])
    report = "\n".join(lines)
    if args.output:
        args.output.write_text(report, encoding="utf-8")
        print(args.output)
    print(report)
    return 0 if all(checks.values()) else 1


if __name__ == "__main__":
    raise SystemExit(main())
