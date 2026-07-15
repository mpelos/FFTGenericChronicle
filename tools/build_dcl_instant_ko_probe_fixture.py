#!/usr/bin/env python3
"""Build an audited, non-deployed Instant-KO NXD fixture for a live probe.

Unlike ``build_neuter_data.py``, this helper never writes the mod package or the shared neuter
SQLite. It stages every intermediate in a temporary directory, proves the selected-row delta and
the NXD round trip, then emits timestamp-prefixed evidence artifacts under ``work/``.
"""

from __future__ import annotations

import argparse
import hashlib
import shutil
import sqlite3
import subprocess
import tempfile
from pathlib import Path

import build_neuter_data as neuter


REPO = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = REPO / "work"
TABLE = "OverrideAbilityActionData"
OWNED_COLUMNS = ("Formula", "X", "Y", "InflictStatus")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build and round-trip-audit a staged DCL Instant-KO probe fixture."
    )
    parser.add_argument(
        "--prefix",
        required=True,
        help="Unix timestamp used as the mandatory leading filename prefix.",
    )
    parser.add_argument(
        "--ability-id",
        type=int,
        nargs="+",
        choices=neuter.DCL_INSTANT_KO_ABILITY_IDS,
        default=[30],
        help="Instant-KO ability ids to neutralize. Default: Death (30).",
    )
    parser.add_argument(
        "--source-sqlite",
        type=Path,
        default=neuter.NEUTER_OVERRIDE_SQLITE,
        help="Existing full neuter SQLite used as the immutable source.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help="Destination for timestamp-prefixed fixture artifacts. Default: work/.",
    )
    parser.add_argument(
        "--ff16tools",
        type=Path,
        default=neuter.DEFAULT_FF16TOOLS,
        help="Path to FF16Tools.CLI.exe.",
    )
    return parser.parse_args()


def table_columns(path: Path) -> list[str]:
    with sqlite3.connect(path) as con:
        return [str(row[1]) for row in con.execute(f"PRAGMA table_info({TABLE})")]


def table_rows(path: Path, columns: list[str]) -> dict[int, tuple[object, ...]]:
    key_index = columns.index("Key")
    with sqlite3.connect(path) as con:
        rows = con.execute(f"SELECT * FROM {TABLE} ORDER BY Key").fetchall()
    return {int(row[key_index]): row for row in rows}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def validate_selected_delta(
    source: Path,
    candidate: Path,
    selected_ids: tuple[int, ...],
) -> tuple[list[str], dict[int, dict[str, object]], dict[int, dict[str, object]]]:
    source_columns = table_columns(source)
    candidate_columns = table_columns(candidate)
    if source_columns != candidate_columns:
        raise AssertionError("candidate schema differs from its immutable source")

    before_rows = table_rows(source, source_columns)
    after_rows = table_rows(candidate, candidate_columns)
    if before_rows.keys() != after_rows.keys():
        raise AssertionError("candidate key set differs from its immutable source")

    selected = set(selected_ids)
    changed_ids = {ability_id for ability_id in before_rows if before_rows[ability_id] != after_rows[ability_id]}
    unexpected = changed_ids - selected
    if unexpected:
        raise AssertionError(f"unselected rows changed: {sorted(unexpected)}")

    expected_values = {"Formula": 8, "X": 1, "Y": 1, "InflictStatus": 0}
    before_selected: dict[int, dict[str, object]] = {}
    after_selected: dict[int, dict[str, object]] = {}
    for ability_id in selected_ids:
        if ability_id not in before_rows:
            raise AssertionError(f"selected ability {ability_id} is absent from the source")
        before = dict(zip(source_columns, before_rows[ability_id], strict=True))
        after = dict(zip(source_columns, after_rows[ability_id], strict=True))
        before_selected[ability_id] = before
        after_selected[ability_id] = after
        for column, expected in expected_values.items():
            if after[column] != expected:
                raise AssertionError(
                    f"ability {ability_id} expected {column}={expected}, got {after[column]}"
                )
        for column in source_columns:
            if column not in OWNED_COLUMNS and after[column] != before[column]:
                raise AssertionError(
                    f"ability {ability_id} changed non-owned column {column}: "
                    f"{before[column]} -> {after[column]}"
                )

    return source_columns, before_selected, after_selected


def validate_round_trip(candidate: Path, roundtrip: Path) -> None:
    candidate_columns = table_columns(candidate)
    roundtrip_columns = table_columns(roundtrip)
    if candidate_columns != roundtrip_columns:
        raise AssertionError("round-tripped NXD schema differs from staged SQLite")
    if table_rows(candidate, candidate_columns) != table_rows(roundtrip, roundtrip_columns):
        raise AssertionError("round-tripped NXD rows differ from staged SQLite")


def row_summary(row: dict[str, object]) -> str:
    return ", ".join(f"{column}={row[column]}" for column in OWNED_COLUMNS)


def build_manifest(
    prefix: str,
    source: Path,
    staged_sqlite: Path,
    staged_nxd: Path,
    selected_ids: tuple[int, ...],
    before: dict[int, dict[str, object]],
    after: dict[int, dict[str, object]],
) -> str:
    lines = [
        "# DCL Instant-KO staged data fixture",
        "",
        "## Scope",
        "",
        "This fixture is generated outside the deployed mod package. It neutralizes only the selected",
        "native Instant-KO rows on top of the existing full neuter SQLite and is safe to deploy only",
        "together with matching `DclInstantKoRules`.",
        "",
        "## Artifacts",
        "",
        f"- Prefix: `{prefix}`",
        f"- Immutable source: `{source.resolve()}`",
        f"- Source SHA-256: `{sha256(source)}`",
        f"- Staged SQLite: `{staged_sqlite.name}`",
        f"- Staged SQLite SHA-256: `{sha256(staged_sqlite)}`",
        f"- Staged NXD: `{staged_nxd.name}`",
        f"- Staged NXD SHA-256: `{sha256(staged_nxd)}`",
        f"- Selected ability ids: `{', '.join(str(value) for value in selected_ids)}`",
        "",
        "## Exact selected-row delta",
        "",
    ]
    for ability_id in selected_ids:
        lines.extend(
            [
                f"- Ability `{ability_id}` before: `{row_summary(before[ability_id])}`",
                f"- Ability `{ability_id}` after: `{row_summary(after[ability_id])}`",
            ]
        )
    lines.extend(
        [
            "",
            "Every unselected row and every non-owned column in a selected row is byte-for-byte equal",
            "at the SQLite value level. FF16Tools NXD-to-SQLite round-trip reproduces every staged row",
            "and column exactly. The deployed mod package is not modified by this build.",
            "",
            "## Deployment gate",
            "",
            "Record the installed NXD backup hash before replacement, deploy this NXD only while the game",
            "is stopped, validate the matching runtime settings, and restore the installed backup after the",
            "probe. A profile with `DclInstantKoControlEnabled=true` must never be paired with native Death.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    args = parse_args()
    prefix = str(args.prefix).strip()
    if not prefix.isdigit() or len(prefix) < 10:
        raise SystemExit("--prefix must be a Unix timestamp with at least ten digits")

    selected_ids = tuple(dict.fromkeys(int(value) for value in args.ability_id))
    source = args.source_sqlite.resolve()
    output_dir = args.output_dir.resolve()
    ff16tools = args.ff16tools.resolve()
    if not source.is_file():
        raise SystemExit(f"source SQLite not found: {source}")
    if not ff16tools.is_file():
        raise SystemExit(f"FF16Tools CLI not found: {ff16tools}")
    output_dir.mkdir(parents=True, exist_ok=True)

    label = "death" if selected_ids == (30,) else "instant-ko-" + "-".join(map(str, selected_ids))
    final_sqlite = output_dir / f"{prefix}-lt37-{label}-neutralized.sqlite"
    final_nxd = output_dir / f"{prefix}-lt37-{label}-neutralized.nxd"
    final_manifest = output_dir / f"{prefix}-lt37-{label}-neutralized-manifest.md"
    destinations = (final_sqlite, final_nxd, final_manifest)
    existing = [path for path in destinations if path.exists()]
    if existing:
        raise SystemExit("refusing to overwrite existing fixture artifacts: " + ", ".join(map(str, existing)))

    with tempfile.TemporaryDirectory(prefix="gc_dcl_ko_fixture_", ignore_cleanup_errors=True) as tmp:
        temp = Path(tmp)
        staged_sqlite = temp / "fixture.sqlite"
        shutil.copyfile(source, staged_sqlite)
        changed = neuter.apply_dcl_instant_ko_neuter(staged_sqlite, list(selected_ids))
        if changed != len(selected_ids):
            raise AssertionError(f"expected {len(selected_ids)} selected rows, builder reported {changed}")
        _, before, after = validate_selected_delta(source, staged_sqlite, selected_ids)

        nxd_dir = temp / "nxd"
        nxd_dir.mkdir()
        subprocess.run(
            [
                str(ff16tools),
                "sqlite-to-nxd",
                "-i",
                str(staged_sqlite),
                "-o",
                str(nxd_dir),
                "-g",
                "fft",
                "-t",
                TABLE,
            ],
            check=True,
        )
        generated_nxd = nxd_dir / "overrideabilityactiondata.nxd"
        if not generated_nxd.is_file() or generated_nxd.stat().st_size == 0:
            raise AssertionError(f"FF16Tools did not produce the expected NXD: {generated_nxd}")

        roundtrip_sqlite = temp / "roundtrip.sqlite"
        subprocess.run(
            [
                str(ff16tools),
                "nxd-to-sqlite",
                "-i",
                str(nxd_dir),
                "-o",
                str(roundtrip_sqlite),
                "-g",
                "fft",
            ],
            check=True,
        )
        validate_round_trip(staged_sqlite, roundtrip_sqlite)

        shutil.copyfile(staged_sqlite, final_sqlite)
        shutil.copyfile(generated_nxd, final_nxd)
        manifest = build_manifest(
            prefix,
            source,
            final_sqlite,
            final_nxd,
            selected_ids,
            before,
            after,
        )
        final_manifest.write_text(manifest, encoding="utf-8")

    print(final_sqlite)
    print(final_nxd)
    print(final_manifest)
    print("staged Instant-KO fixture validation passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
