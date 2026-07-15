#!/usr/bin/env python3
"""Fail closed when managed Instant-KO runtime rules and action data diverge."""
from __future__ import annotations

import argparse
import hashlib
import json
import sqlite3
from contextlib import closing
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
TABLE = "OverrideAbilityActionData"
EXPECTED_KO_ROW = {"Formula": 8, "X": 1, "Y": 1, "InflictStatus": 0}


class PairValidationError(ValueError):
    pass


def _object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise PairValidationError(f"cannot read {label} {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise PairValidationError(f"{label} must be a JSON object: {path}")
    return value


def _repo_path(raw: object, label: str) -> Path:
    if not isinstance(raw, str) or not raw:
        raise PairValidationError(f"{label} must be a non-empty repository-relative path")
    path = (ROOT / raw).resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as exc:
        raise PairValidationError(f"{label} escapes the repository: {raw}") from exc
    return path


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    try:
        with path.open("rb") as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(block)
    except OSError as exc:
        raise PairValidationError(f"cannot hash {path}: {exc}") from exc
    return digest.hexdigest().upper()


def validate_pair(manifest_path: Path) -> list[str]:
    manifest = _object(manifest_path, "pair manifest")
    settings_path = _repo_path(manifest.get("settings"), "settings")
    sqlite_path = _repo_path(manifest.get("action_data_sqlite"), "action_data_sqlite")
    nxd_path = _repo_path(manifest.get("action_data_nxd"), "action_data_nxd")
    settings = _object(settings_path, "runtime settings")

    hashes = manifest.get("sha256")
    if not isinstance(hashes, dict):
        raise PairValidationError("sha256 must be an object")
    for key, path in (("action_data_sqlite", sqlite_path), ("action_data_nxd", nxd_path)):
        expected = hashes.get(key)
        if not isinstance(expected, str) or len(expected) != 64:
            raise PairValidationError(f"sha256.{key} must be a 64-digit digest")
        actual = _sha256(path)
        if actual != expected.upper():
            raise PairValidationError(f"{key} hash mismatch: expected {expected.upper()}, got {actual}")

    required_raw = manifest.get("required_instant_ko_abilities")
    if not isinstance(required_raw, list) or not required_raw or not all(isinstance(value, int) for value in required_raw):
        raise PairValidationError("required_instant_ko_abilities must be a non-empty integer list")
    required = set(required_raw)
    if len(required) != len(required_raw):
        raise PairValidationError("required_instant_ko_abilities contains duplicates")

    if settings.get("DclInstantKoControlEnabled") is not True:
        raise PairValidationError("paired settings do not enable DclInstantKoControlEnabled")
    if settings.get("DclComputePointNumericEnabled") is not True:
        raise PairValidationError("paired settings do not enable the AI-facing compute-point writer")
    for key in ("DclStatusForcedRoll", "DclHitForcedRoll"):
        if int(settings.get(key, -1)) != -1:
            raise PairValidationError(f"paired integration settings retain probe-only {key}")
    for key in ("CalcEntryProbeEnabled", "DclCalcProvenanceProbeEnabled", "StagedBundleProbeEnabled"):
        if settings.get(key, False) is True:
            raise PairValidationError(f"paired integration settings retain probe-only {key}")

    rules = settings.get("DclInstantKoRules")
    if not isinstance(rules, list) or not rules:
        raise PairValidationError("paired settings have no DclInstantKoRules")
    runtime_ids: set[int] = set()
    for index, rule in enumerate(rules, start=1):
        if not isinstance(rule, dict):
            raise PairValidationError(f"DclInstantKoRules[{index}] must be an object")
        ability_id = rule.get("AbilityId")
        if not isinstance(ability_id, int):
            raise PairValidationError(f"DclInstantKoRules[{index}].AbilityId must be an integer")
        if rule.get("NativeKoSuppressedByData") is not True:
            raise PairValidationError(f"ability {ability_id} does not acknowledge data-side KO suppression")
        runtime_ids.add(ability_id)
    if runtime_ids != required:
        raise PairValidationError(
            f"runtime/data ability set mismatch: runtime={sorted(runtime_ids)}, required={sorted(required)}"
        )

    columns = ", ".join(EXPECTED_KO_ROW)
    placeholders = ", ".join("?" for _ in required)
    try:
        with closing(sqlite3.connect(sqlite_path)) as con:
            rows = con.execute(
                f"SELECT Key, {columns} FROM {TABLE} WHERE Key IN ({placeholders}) ORDER BY Key",
                tuple(sorted(required)),
            ).fetchall()
    except sqlite3.Error as exc:
        raise PairValidationError(f"cannot inspect {TABLE} in {sqlite_path}: {exc}") from exc
    found = {int(row[0]): dict(zip(EXPECTED_KO_ROW, row[1:], strict=True)) for row in rows}
    missing = required - found.keys()
    if missing:
        raise PairValidationError(f"action data lacks required ability rows: {sorted(missing)}")
    for ability_id in sorted(required):
        if found[ability_id] != EXPECTED_KO_ROW:
            raise PairValidationError(
                f"ability {ability_id} is not safely neutralized: expected {EXPECTED_KO_ROW}, got {found[ability_id]}"
            )

    return [
        f"settings={settings_path.relative_to(ROOT).as_posix()}",
        f"instant_ko_abilities={','.join(map(str, sorted(required)))}",
        f"action_data_sqlite_sha256={hashes['action_data_sqlite'].upper()}",
        f"action_data_nxd_sha256={hashes['action_data_nxd'].upper()}",
    ]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    args = parser.parse_args()
    try:
        details = validate_pair(args.manifest.resolve())
    except PairValidationError as exc:
        print(f"ERROR: {exc}")
        return 1
    print("DCL runtime/data pair validation passed")
    for detail in details:
        print(f"  {detail}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
