#!/usr/bin/env python3
"""Smoke tests for the isolated unified-DCL action-data builder."""
from __future__ import annotations

import sqlite3
import tempfile
from pathlib import Path

import build_dcl_integration_data_pair as integration
import build_neuter_data as neuter


def row(path: Path, ability_id: int) -> tuple[int, int, int, int]:
    with sqlite3.connect(path) as con:
        value = con.execute(
            "SELECT Formula, X, Y, InflictStatus FROM OverrideAbilityActionData WHERE Key=?",
            (ability_id,),
        ).fetchone()
    assert value is not None, ability_id
    return tuple(int(part) for part in value)


def main() -> None:
    with tempfile.TemporaryDirectory(prefix="gc_dcl_integration_pair_", ignore_cleanup_errors=True) as tmp:
        output = Path(tmp) / "integration.sqlite"
        baseline = Path(tmp) / "baseline.sqlite"
        neuter.build_ability_neuter(output_path=baseline)
        counts = integration.build_sqlite(output)

        assert counts["instant_ko"] == 1
        assert counts["ordinary_status_rider"] == 2
        assert counts["support_status_rider"] == 1
        assert counts["conditional_status_rider"] == 1
        assert row(output, 30) == (8, 1, 1, 0)
        assert row(output, 53) == row(baseline, 53)

        for ability_id in (219, 252, 277, 357):
            before = row(baseline, ability_id)
            after = row(output, ability_id)
            assert after[:3] == before[:3], (ability_id, before, after)
            assert after[3] == 0, (ability_id, after)

        extended = Path(tmp) / "integration-extended.sqlite"
        extended_counts = integration.build_sqlite(
            extended, extra_status_rider_ids=(126, 131)
        )
        assert extended_counts["ordinary_status_rider"] == 4
        for ability_id in (126, 131, 219, 252, 277, 357):
            before = row(baseline, ability_id)
            after = row(extended, ability_id)
            assert after[:3] == before[:3], (ability_id, before, after)
            assert after[3] == 0, (ability_id, after)

        assert integration.parse_ids("131,126,131") == (126, 131)

    print("unified DCL sentinel action-data builder tests passed")


if __name__ == "__main__":
    main()
