#!/usr/bin/env python3
"""Regression tests for the isolated DCL DA/DM action-data builder."""
from __future__ import annotations

import sqlite3
import tempfile
from contextlib import closing
from pathlib import Path

import build_dcl_da_dm_action_data as build
import build_neuter_data as neuter


def rows(path: Path) -> dict[int, tuple[object, ...]]:
    with closing(sqlite3.connect(path)) as con:
        return {
            int(row[0]): tuple(row[1:])
            for row in con.execute("SELECT * FROM OverrideAbilityActionData ORDER BY Key")
        }


def main() -> int:
    before = rows(neuter.BASE_OVERRIDE_SQLITE)
    with tempfile.TemporaryDirectory(
        dir=build.ROOT / "tools",
        prefix=".tmp_dcl_da_dm_action_data_",
        ignore_cleanup_errors=True,
    ) as raw:
        output = Path(raw) / "pair.sqlite"
        assert build.build_sqlite(output) == 2
        after = rows(output)
        assert before.keys() == after.keys()
        with closing(sqlite3.connect(output)) as con:
            names = [column[1] for column in con.execute(
                "PRAGMA table_info(OverrideAbilityActionData)"
            )]
        status_index = names.index("InflictStatus") - 1
        for ability_id in before:
            if ability_id in build.STATUS_RIDER_IDS:
                assert after[ability_id][status_index] == 0
                for index, (old, new) in enumerate(zip(before[ability_id], after[ability_id], strict=True)):
                    if index != status_index:
                        assert old == new, (ability_id, names[index + 1], old, new)
            else:
                assert after[ability_id] == before[ability_id], ability_id
        assert build.changed_rows(output) == [(126, 0), (131, 0)]
    print("DCL DA/DM action-data tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
