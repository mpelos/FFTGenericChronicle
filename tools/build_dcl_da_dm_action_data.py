#!/usr/bin/env python3
"""Build the isolated action-data half of DCL Don't Move/Don't Act duration ownership."""
from __future__ import annotations

import argparse
import shutil
import sqlite3
from contextlib import closing
from pathlib import Path

import build_dcl_integration_data_pair as integration
import build_neuter_data as neuter


ROOT = Path(__file__).resolve().parents[1]
STATUS_RIDER_IDS = (126, 131)


def repo_output(raw: Path, label: str) -> Path:
    path = (ROOT / raw).resolve() if not raw.is_absolute() else raw.resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as error:
        raise ValueError(f"{label} must stay inside the repository: {raw}") from error
    protected = {
        neuter.BASE_OVERRIDE_SQLITE.resolve(),
        neuter.NEUTER_OVERRIDE_SQLITE.resolve(),
    }
    if path in protected:
        raise ValueError(f"{label} cannot overwrite a shared source artifact: {path}")
    return path


def build_sqlite(output: Path) -> int:
    output.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(neuter.BASE_OVERRIDE_SQLITE, output)
    return neuter.apply_dcl_status_rider_neuter(output, STATUS_RIDER_IDS)


def changed_rows(path: Path) -> list[tuple[int, int]]:
    with closing(sqlite3.connect(path)) as con:
        return [
            (int(key), int(status))
            for key, status in con.execute(
                "SELECT Key, InflictStatus FROM OverrideAbilityActionData "
                "WHERE Key IN (?, ?) ORDER BY Key",
                STATUS_RIDER_IDS,
            )
        ]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sqlite-out", type=Path, required=True)
    parser.add_argument("--nxd-out", type=Path, required=True)
    parser.add_argument("--ff16tools", type=Path, default=neuter.DEFAULT_FF16TOOLS)
    args = parser.parse_args()
    try:
        sqlite_path = repo_output(args.sqlite_out, "sqlite output")
        nxd_path = repo_output(args.nxd_out, "NXD output")
        changed = build_sqlite(sqlite_path)
        if changed != len(STATUS_RIDER_IDS):
            raise RuntimeError(f"expected {len(STATUS_RIDER_IDS)} changed rows, got {changed}")
        rows = changed_rows(sqlite_path)
        if rows != [(126, 0), (131, 0)]:
            raise RuntimeError(f"unexpected neutralized rows: {rows}")
        integration.build_nxd(sqlite_path, nxd_path, args.ff16tools.resolve())
    except (OSError, ValueError, RuntimeError, sqlite3.Error) as error:
        print(f"ERROR: {error}")
        return 1

    print("DCL DA/DM action-data build passed")
    print(f"  neutralized_abilities={','.join(map(str, STATUS_RIDER_IDS))}")
    print(f"  sqlite={sqlite_path.relative_to(ROOT).as_posix()}")
    print(f"  sqlite_sha256={integration.sha256(sqlite_path)}")
    print(f"  nxd={nxd_path.relative_to(ROOT).as_posix()}")
    print(f"  nxd_sha256={integration.sha256(nxd_path)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
