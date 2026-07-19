#!/usr/bin/env python3
"""Validate the canonical job-free live regression matrix against an exact DCL pair."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

from validate_dcl_runtime_data_pair import PairValidationError, ROOT, validate_pair


DEFAULT_MATRIX = ROOT / "work/1784470893-dcl-clean-v1-live-regression-matrix.json"
REQUIRED_TAGS = {
    "preflight",
    "player-forecast",
    "execution",
    "ai-scoring",
    "physical-single",
    "facing",
    "finite-guard",
    "damage-types-dr",
    "weapon-skill",
    "dual-wield",
    "managed-multistrike",
    "native-multistrike",
    "magic-single",
    "magic-aoe",
    "magic-evade",
    "healing",
    "undead-inversion",
    "mp-budget",
    "atomic-hp-mp",
    "status-ordinary",
    "status-retained",
    "status-postcalc",
    "status-grouped",
    "status-randomfire",
    "status-performance",
    "duration-physical",
    "duration-magical",
    "taunt",
    "interrupt",
    "instant-ko",
    "revive-corpse",
    "reaction-real",
    "reaction-vm",
    "reaction-synthetic",
    "final-tile",
    "weapon-los",
    "weight-move",
    "lifecycle-reset",
    "item-metadata",
    "native-special-actions",
    "presentation-native",
}
ALLOWED_STAGES = {"offline-preflight", "live-observe", "live-mutate", "live-regression"}


class MatrixError(ValueError):
    pass


def _object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as error:
        raise MatrixError(f"cannot read {label} {path}: {error}") from error
    if not isinstance(value, dict):
        raise MatrixError(f"{label} must be a JSON object")
    return value


def _repo_path(raw: object, label: str) -> Path:
    if not isinstance(raw, str) or not raw:
        raise MatrixError(f"{label} must be a non-empty repository-relative path")
    path = (ROOT / raw).resolve()
    try:
        path.relative_to(ROOT)
    except ValueError as error:
        raise MatrixError(f"{label} escapes the repository: {raw}") from error
    return path


def _hash(path: Path) -> str:
    try:
        return hashlib.sha256(path.read_bytes()).hexdigest().upper()
    except OSError as error:
        raise MatrixError(f"cannot hash {path}: {error}") from error


def validate_matrix(path: Path, *, validate_runtime_pair: bool = True) -> list[str]:
    matrix = _object(path, "regression matrix")
    note = str(matrix.get("note", "")).lower()
    for token in ("job-free", "clean", "no final balance"):
        if token not in note:
            raise MatrixError(f"matrix note must preserve the {token!r} boundary")

    pair_path = _repo_path(matrix.get("runtime_data_pair"), "runtime_data_pair")
    if validate_runtime_pair:
        try:
            validate_pair(pair_path)
        except PairValidationError as error:
            raise MatrixError(f"invalid runtime/data pair: {error}") from error
    pair = _object(pair_path, "runtime/data pair")
    expected_pair_hash = matrix.get("runtime_data_pair_sha256")
    if not isinstance(expected_pair_hash, str) or _hash(pair_path) != expected_pair_hash.upper():
        raise MatrixError("runtime_data_pair_sha256 does not bind the selected pair")
    pair_hashes = pair.get("sha256")
    if not isinstance(pair_hashes, dict) or matrix.get("settings_sha256") != pair_hashes.get("settings"):
        raise MatrixError("settings_sha256 does not match the paired runtime settings")
    settings_path = _repo_path(pair.get("settings"), "paired settings")
    settings = _object(settings_path, "paired settings")

    preflight = matrix.get("preflight_command")
    if not isinstance(preflight, str) or "validate_dcl_live_install.py" not in preflight:
        raise MatrixError("preflight_command must invoke the hash-bound live-install validator")
    if matrix.get("writes_save") is not False:
        raise MatrixError("canonical regression must declare writes_save=false")

    declared_tags = matrix.get("required_tags")
    if not isinstance(declared_tags, list) or not all(isinstance(tag, str) for tag in declared_tags):
        raise MatrixError("required_tags must be a string list")
    if set(declared_tags) != REQUIRED_TAGS or len(declared_tags) != len(REQUIRED_TAGS):
        raise MatrixError("required_tags must equal the canonical job-free mechanism tag set")

    cases = matrix.get("cases")
    if not isinstance(cases, list) or not cases:
        raise MatrixError("cases must be a non-empty list")
    seen_ids: set[str] = set()
    covered_tags: set[str] = set()
    completed_ids: set[str] = set()
    details: list[str] = []
    for index, case in enumerate(cases, start=1):
        if not isinstance(case, dict):
            raise MatrixError(f"case {index} must be an object")
        case_id = case.get("id")
        if not isinstance(case_id, str) or not case_id:
            raise MatrixError(f"case {index} requires a non-empty id")
        if case_id in seen_ids:
            raise MatrixError(f"duplicate case id: {case_id}")
        seen_ids.add(case_id)
        if case.get("job_free") is not True:
            raise MatrixError(f"case {case_id} must declare job_free=true")
        if case.get("writes_save") is not False:
            raise MatrixError(f"case {case_id} must declare writes_save=false")
        stage = case.get("stage")
        if stage not in ALLOWED_STAGES:
            raise MatrixError(f"case {case_id} has invalid stage {stage!r}")
        for key in ("purpose", "fixture"):
            if not isinstance(case.get(key), str) or not case[key].strip():
                raise MatrixError(f"case {case_id} requires non-empty {key}")
        for key in ("actions", "pass_evidence"):
            value = case.get(key)
            if not isinstance(value, list) or not value or not all(
                isinstance(item, str) and item.strip() for item in value
            ):
                raise MatrixError(f"case {case_id}.{key} must be a non-empty string list")
        tags = case.get("tags")
        if not isinstance(tags, list) or not tags or not all(isinstance(tag, str) for tag in tags):
            raise MatrixError(f"case {case_id}.tags must be a non-empty string list")
        unknown_tags = set(tags) - REQUIRED_TAGS
        if unknown_tags:
            raise MatrixError(f"case {case_id} has unknown tags: {sorted(unknown_tags)}")
        covered_tags.update(tags)
        dependencies = case.get("depends_on", [])
        if not isinstance(dependencies, list) or not all(isinstance(value, str) for value in dependencies):
            raise MatrixError(f"case {case_id}.depends_on must be a string list")
        missing_predecessors = set(dependencies) - completed_ids
        if missing_predecessors:
            raise MatrixError(
                f"case {case_id} depends on missing or later cases: {sorted(missing_predecessors)}"
            )
        ability_ids = case.get("ability_ids", [])
        if not isinstance(ability_ids, list) or not all(
            isinstance(value, int) and 0 <= value <= 511 for value in ability_ids
        ):
            raise MatrixError(f"case {case_id}.ability_ids must stay within 0..511")
        settings_requirements = case.get("settings_requirements", {})
        if not isinstance(settings_requirements, dict):
            raise MatrixError(f"case {case_id}.settings_requirements must be an object")
        mismatches = [
            f"{key}={settings.get(key)!r} (expected {expected!r})"
            for key, expected in settings_requirements.items()
            if settings.get(key) != expected
        ]
        if mismatches:
            raise MatrixError(
                f"case {case_id} does not match paired settings: " + "; ".join(mismatches)
            )
        completed_ids.add(case_id)
        details.append(f"case={case_id} stage={stage} tags={len(set(tags))}")

    if covered_tags != REQUIRED_TAGS:
        raise MatrixError(f"matrix leaves mechanism tags uncovered: {sorted(REQUIRED_TAGS - covered_tags)}")
    return details


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("matrix", type=Path, nargs="?", default=DEFAULT_MATRIX)
    args = parser.parse_args()
    try:
        details = validate_matrix(args.matrix.resolve())
    except MatrixError as error:
        print(f"ERROR: {error}")
        return 1
    print("DCL clean live regression matrix validation passed")
    print(f"  cases={len(details)}")
    print(f"  mechanism_tags={len(REQUIRED_TAGS)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
