#!/usr/bin/env python3
"""Smoke tests for the exact, job-free clean DCL live regression matrix gate."""
from __future__ import annotations

import copy
import json
import tempfile
from pathlib import Path

from validate_dcl_live_regression_matrix import (
    DEFAULT_MATRIX,
    MatrixError,
    REQUIRED_TAGS,
    ROOT,
    validate_matrix,
)


def expect_error(matrix: dict, fragment: str, directory: Path, name: str) -> None:
    path = directory / name
    path.write_text(json.dumps(matrix), encoding="utf-8")
    try:
        validate_matrix(path, validate_runtime_pair=False)
    except MatrixError as error:
        assert fragment in str(error), (fragment, str(error))
    else:
        raise AssertionError(f"expected MatrixError containing {fragment!r}")


def main() -> int:
    source = json.loads(DEFAULT_MATRIX.read_text(encoding="utf-8"))
    details = validate_matrix(DEFAULT_MATRIX, validate_runtime_pair=False)
    assert len(details) == 17
    assert len(REQUIRED_TAGS) == 41
    assert any("case=final-tile-position-producer" in detail for detail in details)
    assert "fear" not in REQUIRED_TAGS and "approach" not in REQUIRED_TAGS
    assert "v4" not in DEFAULT_MATRIX.read_text(encoding="utf-8").lower()
    assert DEFAULT_MATRIX.name == "1784683300-dcl-active-integrated-live-regression-matrix.json"
    assert "active integrated" in DEFAULT_MATRIX.read_text(encoding="utf-8").lower()

    with tempfile.TemporaryDirectory(dir=ROOT) as raw:
        temp = Path(raw)

        missing_tag = copy.deepcopy(source)
        missing_tag["required_tags"].remove("final-tile")
        expect_error(missing_tag, "canonical job-free mechanism tag set", temp, "missing-tag.json")

        stale_pair_hash = copy.deepcopy(source)
        stale_pair_hash["runtime_data_pair_sha256"] = "00" * 32
        expect_error(stale_pair_hash, "does not bind the selected pair", temp, "stale-pair.json")

        settings_mismatch = copy.deepcopy(source)
        settings_mismatch["cases"][1]["settings_requirements"]["DclPipelineEnabled"] = False
        expect_error(settings_mismatch, "does not match paired settings", temp, "settings-mismatch.json")

        later_dependency = copy.deepcopy(source)
        later_dependency["cases"][0]["depends_on"] = ["battle-lifecycle-reset"]
        expect_error(later_dependency, "missing or later cases", temp, "later-dependency.json")

        invalid_ability = copy.deepcopy(source)
        invalid_ability["cases"][1]["ability_ids"] = [512]
        expect_error(invalid_ability, "within 0..511", temp, "invalid-ability.json")

    print("DCL clean live regression matrix tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
