#!/usr/bin/env python3
"""Smoke tests for canonical-admission live log collection."""
from __future__ import annotations

import tempfile
from pathlib import Path

from collect_dcl_canonical_admission_live_log import ROOT, collect_and_analyze


SYNTHETIC = ROOT / "work" / "1784673223-canonical-admission-template-live-synthetic.log"


def main() -> int:
    collector_source = (ROOT / "tools" / "collect_dcl_canonical_admission_live_log.py").read_text(encoding="utf-8")
    assert 'parser.add_argument("--require-damage", action="store_true", default=False)' in collector_source

    prefix = 1784673896
    copied = ROOT / "work" / f"{prefix}-raw-canonical-admission-live.log"
    report = ROOT / "work" / f"{prefix}-canonical-admission-template-live-analysis.md"
    copied.unlink(missing_ok=True)
    report.unlink(missing_ok=True)

    copied_path, report_path, ok = collect_and_analyze(
        SYNTHETIC,
        prefix=prefix,
        ability=16,
        target_count=1,
        strikes=1,
        require_damage=True,
    )
    assert ok
    assert copied_path == copied and copied_path.read_bytes() == SYNTHETIC.read_bytes()
    assert report_path == report and "Overall live-evidence gate: **PASS**" in report.read_text(encoding="utf-8")

    with tempfile.TemporaryDirectory() as tmp:
        missing = Path(tmp) / "missing.log"
        try:
            collect_and_analyze(
                missing,
                prefix=prefix + 1,
                ability=16,
                target_count=1,
                strikes=1,
                require_damage=True,
            )
        except FileNotFoundError as error:
            assert "live log does not exist" in str(error)
        else:
            raise AssertionError("collector accepted a missing live log")

    print("canonical admission live log collector tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
