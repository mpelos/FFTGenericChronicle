#!/usr/bin/env python3
"""Copy the fresh canonical-admission live log into work/ and analyze it."""
from __future__ import annotations

import argparse
import shutil
import time
from pathlib import Path

from analyze_dcl_canonical_admission_template_live import ROOT, render_report


DEFAULT_GAME_LOG = Path(
    r"D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt"
)


def collect_and_analyze(
    log: Path,
    *,
    prefix: int,
    ability: int,
    target_count: int,
    strikes: int,
    require_damage: bool,
) -> tuple[Path, Path, bool]:
    if not log.is_file():
        raise FileNotFoundError(f"live log does not exist: {log}")
    copied = ROOT / "work" / f"{prefix}-raw-canonical-admission-live.log"
    report = ROOT / "work" / f"{prefix}-canonical-admission-template-live-analysis.md"
    copied.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(log, copied)
    rendered, ok = render_report(
        copied,
        ability=ability,
        target_count=target_count,
        strikes=strikes,
        require_damage=require_damage,
    )
    report.write_text(rendered, encoding="utf-8", newline="\n")
    return copied, report, ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", nargs="?", type=Path, default=DEFAULT_GAME_LOG)
    parser.add_argument("--prefix", type=int, default=None)
    parser.add_argument("--ability", type=int, default=16)
    parser.add_argument("--target-count", type=int, default=1)
    parser.add_argument("--strikes", type=int, default=1)
    parser.add_argument("--require-damage", action="store_true", default=False)
    parser.add_argument(
        "--allow-missing-damage",
        action="store_false",
        dest="require_damage",
        help="Do not require the admitted target to have a later positive [DAMAGE] line.",
    )
    args = parser.parse_args()

    prefix = int(time.time()) if args.prefix is None else args.prefix
    try:
        copied, report, ok = collect_and_analyze(
            args.log,
            prefix=prefix,
            ability=args.ability,
            target_count=args.target_count,
            strikes=args.strikes,
            require_damage=args.require_damage,
        )
    except OSError as error:
        print(f"ERROR: {error}")
        return 1
    print(f"copied {copied}")
    print(f"wrote {report}")
    print("canonical admission live collection PASS" if ok else "canonical admission live collection FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
