#!/usr/bin/env python3
"""Build an isolated action-data artifact for the unified DCL sentinel profile."""
from __future__ import annotations

import argparse
import hashlib
import shutil
import sqlite3
import subprocess
import tempfile
from pathlib import Path

import build_neuter_data as neuter


ROOT = Path(__file__).resolve().parents[1]
TABLE = "OverrideAbilityActionData"
INSTANT_KO_IDS = (30,)
STATUS_RIDER_IDS = (219, 357)
SUPPORT_STATUS_RIDER_IDS = (252,)
CONDITIONAL_STATUS_RIDER_IDS = (277,)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def all_rows(path: Path) -> list[tuple[object, ...]]:
    with sqlite3.connect(path) as con:
        return con.execute(f"SELECT * FROM {TABLE} ORDER BY Key").fetchall()


def parse_ids(raw: str) -> tuple[int, ...]:
    if not raw.strip():
        return ()
    try:
        values = tuple(sorted({int(token.strip()) for token in raw.split(",") if token.strip()}))
    except ValueError as error:
        raise ValueError("extra status-rider ids must be comma-separated integers") from error
    if any(value < 0 or value > 511 for value in values):
        raise ValueError("extra status-rider ids must remain in the ability range 0..511")
    return values


def build_sqlite(
    output: Path,
    placeholder_mode: str = "uniform",
    extra_status_rider_ids: tuple[int, ...] = (),
) -> dict[str, int]:
    status_rider_ids = tuple(sorted(set(STATUS_RIDER_IDS) | set(extra_status_rider_ids)))
    neutered, skipped, _ = neuter.build_ability_neuter(placeholder_mode, output)
    counts = {
        "damaging_placeholders": neutered,
        "skipped_out_of_range": skipped,
        "instant_ko": neuter.apply_dcl_instant_ko_neuter(output, INSTANT_KO_IDS),
        "ordinary_status_rider": neuter.apply_dcl_status_rider_neuter(output, status_rider_ids),
        "support_status_rider": neuter.apply_dcl_support_status_rider_neuter(
            output, SUPPORT_STATUS_RIDER_IDS
        ),
        "conditional_status_rider": neuter.apply_dcl_conditional_status_rider_neuter(
            output, CONDITIONAL_STATUS_RIDER_IDS
        ),
    }
    return counts


def build_nxd(sqlite_path: Path, nxd_path: Path, ff16tools: Path) -> None:
    if not ff16tools.exists():
        raise FileNotFoundError(f"FF16Tools CLI not found: {ff16tools}")
    nxd_path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="gc_dcl_integration_nxd_", ignore_cleanup_errors=True) as tmp:
        temp = Path(tmp)
        subprocess.run(
            [
                str(ff16tools),
                "sqlite-to-nxd",
                "-i",
                str(sqlite_path),
                "-o",
                str(temp),
                "-g",
                "fft",
                "-t",
                TABLE,
            ],
            check=True,
        )
        generated = temp / "overrideabilityactiondata.nxd"
        if not generated.exists() or generated.stat().st_size == 0:
            raise RuntimeError(f"FF16Tools did not produce {generated}")
        shutil.copyfile(generated, nxd_path)

        roundtrip = temp / "roundtrip.sqlite"
        subprocess.run(
            [
                str(ff16tools),
                "nxd-to-sqlite",
                "-i",
                str(temp),
                "-o",
                str(roundtrip),
                "-g",
                "fft",
            ],
            check=True,
        )
        if all_rows(roundtrip) != all_rows(sqlite_path):
            raise RuntimeError("generated NXD does not round-trip to the source integration SQLite")


def repo_output(raw: Path, label: str) -> Path:
    path = (ROOT / raw).resolve() if not raw.is_absolute() else raw.resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as exc:
        raise ValueError(f"{label} must stay inside the repository: {raw}") from exc
    protected = {neuter.BASE_OVERRIDE_SQLITE.resolve(), neuter.NEUTER_OVERRIDE_SQLITE.resolve()}
    if path in protected:
        raise ValueError(f"{label} cannot overwrite a shared source artifact: {path}")
    return path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sqlite-out", required=True, type=Path)
    parser.add_argument("--nxd-out", required=True, type=Path)
    parser.add_argument("--ff16tools", type=Path, default=neuter.DEFAULT_FF16TOOLS)
    parser.add_argument("--placeholder-mode", choices=neuter.PLACEHOLDER_MODES, default="uniform")
    parser.add_argument(
        "--extra-status-rider-ids",
        default="",
        help="additional ordinary status riders to neutralize in this isolated artifact",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        sqlite_path = repo_output(args.sqlite_out, "sqlite output")
        nxd_path = repo_output(args.nxd_out, "NXD output")
        extra_status_rider_ids = parse_ids(args.extra_status_rider_ids)
        counts = build_sqlite(sqlite_path, args.placeholder_mode, extra_status_rider_ids)
        build_nxd(sqlite_path, nxd_path, args.ff16tools.resolve())
    except (OSError, ValueError, RuntimeError, sqlite3.Error, subprocess.CalledProcessError) as exc:
        print(f"ERROR: {exc}")
        return 1

    print("unified DCL sentinel action-data build passed")
    for name, count in counts.items():
        print(f"  {name}={count}")
    if extra_status_rider_ids:
        print(f"  extra_status_rider_ids={','.join(map(str, extra_status_rider_ids))}")
    print(f"  sqlite={sqlite_path.relative_to(ROOT).as_posix()}")
    print(f"  sqlite_sha256={sha256(sqlite_path)}")
    print(f"  nxd={nxd_path.relative_to(ROOT).as_posix()}")
    print(f"  nxd_sha256={sha256(nxd_path)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
