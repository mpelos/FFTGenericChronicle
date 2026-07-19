#!/usr/bin/env python3
"""Smoke tests for the turn-epoch hit-decision parity live analyzer."""
from __future__ import annotations

import tempfile
from pathlib import Path

from analyze_dcl_hit_parity_live import parse_rows, render


PREFIX = (
    "[DCL-HIT] caster=0x01 target=0x82 ability=0 type=0x01 payload=0 "
    "activeWeapon=37 repeat=0/1 "
)


def main() -> int:
    with tempfile.TemporaryDirectory() as raw:
        temp = Path(raw)
        good = temp / "good.log"
        good.write_text(
            PREFIX + "pct=5 roll=12 outcome=miss cached=0 turnEpoch=3 model=physical\n" +
            PREFIX + "pct=5 roll=12 outcome=miss cached=1 turnEpoch=3 model=physical\n",
            encoding="utf-8",
        )
        report, passed = render(good, parse_rows(good.read_text(encoding="utf-8")), 1)
        assert passed and "Exact reused pairs: `1`" in report

        changed = temp / "changed.log"
        changed.write_text(
            PREFIX + "pct=5 roll=12 outcome=miss cached=0 turnEpoch=3 model=physical\n" +
            PREFIX + "pct=5 roll=8 outcome=hit cached=1 turnEpoch=3 model=physical\n",
            encoding="utf-8",
        )
        report, passed = render(changed, parse_rows(changed.read_text(encoding="utf-8")), 1)
        assert not passed and "cached decision changed" in report

        new_turn = temp / "new-turn.log"
        new_turn.write_text(
            PREFIX + "pct=5 roll=12 outcome=miss cached=0 turnEpoch=3 model=physical\n" +
            PREFIX + "pct=5 roll=12 outcome=miss cached=1 turnEpoch=4 model=physical\n",
            encoding="utf-8",
        )
        report, passed = render(new_turn, parse_rows(new_turn.read_text(encoding="utf-8")), 1)
        assert not passed and "no same-turn producer" in report

    print("DCL hit parity live analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
