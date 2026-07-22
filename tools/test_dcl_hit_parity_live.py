#!/usr/bin/env python3
"""Smoke tests for the evaluation/execution hit parity live analyzer."""
from __future__ import annotations

import tempfile
from pathlib import Path

from analyze_dcl_hit_parity_live import parse_rows, render


PREFIX = (
    "[DCL-HIT] caster=0x01 target=0x82 ability=0 type=0x01 payload=0 "
    "activeWeapon=37 repeat=0/1 "
)


def row(*, roll: int, outcome: str, cached: int, epoch: int, phase: str, state: str) -> str:
    origin = "outer-sweep" if phase == "execution" else "forecast-trace"
    return (
        PREFIX
        + f"pct=47 roll={roll} outcome={outcome} cached={cached} turnEpoch={epoch} "
        + f"phase={phase} origin={origin} battleState={state} model=physical\n"
    )


def main() -> int:
    with tempfile.TemporaryDirectory() as raw:
        temp = Path(raw)
        good = temp / "good.log"
        good.write_text(
            row(roll=-1, outcome="hit", cached=0, epoch=3, phase="evaluation", state="0x19")
            + row(roll=12, outcome="miss", cached=0, epoch=3, phase="execution", state="0x2A")
            + row(roll=12, outcome="miss", cached=1, epoch=3, phase="execution", state="0x2A"),
            encoding="utf-8",
        )
        report, passed = render(good, parse_rows(good.read_text(encoding="utf-8")), 1, 1)
        assert passed and "RNG-free evaluation rows: `1`" in report
        assert "Exact execution reuses: `1`" in report

        evaluation_rng = temp / "evaluation-rng.log"
        evaluation_rng.write_text(
            row(roll=8, outcome="hit", cached=0, epoch=3, phase="evaluation", state="0x19")
            + row(roll=12, outcome="miss", cached=0, epoch=3, phase="execution", state="0x2A"),
            encoding="utf-8",
        )
        report, passed = render(
            evaluation_rng,
            parse_rows(evaluation_rng.read_text(encoding="utf-8")),
            1,
        )
        assert not passed and "evaluation consumed RNG" in report

        changed = temp / "changed.log"
        changed.write_text(
            row(roll=12, outcome="miss", cached=0, epoch=3, phase="execution", state="0x2A")
            + row(roll=8, outcome="hit", cached=1, epoch=3, phase="execution", state="0x2A"),
            encoding="utf-8",
        )
        report, passed = render(changed, parse_rows(changed.read_text(encoding="utf-8")), 1, 1)
        assert not passed and "cached execution changed" in report

        new_turn = temp / "new-turn.log"
        new_turn.write_text(
            row(roll=12, outcome="miss", cached=0, epoch=3, phase="execution", state="0x2A")
            + row(roll=12, outcome="miss", cached=1, epoch=4, phase="execution", state="0x2A"),
            encoding="utf-8",
        )
        report, passed = render(new_turn, parse_rows(new_turn.read_text(encoding="utf-8")), 1, 1)
        assert not passed and "no same-epoch producer" in report

    print("DCL hit parity live analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
