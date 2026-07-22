#!/usr/bin/env python3
"""Smoke tests for the DCL live proof sequence validator."""
from __future__ import annotations

import copy
import json
import tempfile
from pathlib import Path

from validate_dcl_live_proof_sequence import DEFAULT_SEQUENCE, ROOT, SequenceError, validate_sequence


def expect_error(sequence: dict, fragment: str, directory: Path, name: str) -> None:
    path = directory / name
    path.write_text(json.dumps(sequence), encoding="utf-8")
    try:
        validate_sequence(path)
    except SequenceError as error:
        assert fragment in str(error), (fragment, str(error))
    else:
        raise AssertionError(f"expected SequenceError containing {fragment!r}")


def main() -> int:
    source = json.loads(DEFAULT_SEQUENCE.read_text(encoding="utf-8"))
    details = validate_sequence(DEFAULT_SEQUENCE)
    assert len(details) == 2
    assert details[0].startswith("proof=canonical-admission-template-bridge")

    with tempfile.TemporaryDirectory(dir=ROOT) as raw:
        temp = Path(raw)

        wrong_first = copy.deepcopy(source)
        wrong_first["proofs"].reverse()
        expect_error(wrong_first, "first proof", temp, "wrong-first.json")

        later_dependency = copy.deepcopy(source)
        later_dependency["proofs"][0]["depends_on"] = ["integrated-clean-regression"]
        expect_error(later_dependency, "missing or later proofs", temp, "later-dependency.json")

        writes_save = copy.deepcopy(source)
        writes_save["proofs"][0]["writes_save"] = True
        expect_error(writes_save, "writes_save=false", temp, "writes-save.json")

        missing_collect = copy.deepcopy(source)
        missing_collect["proofs"][0]["collect_command"] = "python tools/manual.py"
        expect_error(missing_collect, "collect_dcl_canonical_admission_live_log.py", temp, "missing-collect.json")

        forbidden_scope = copy.deepcopy(source)
        forbidden_scope["proofs"][0]["purpose"] += " Fear compatibility"
        expect_error(forbidden_scope, "forbidden draft/retired scope", temp, "forbidden-scope.json")

        historical_pair = copy.deepcopy(source)
        historical_pair["proofs"][1]["preflight_commands"].append(
            "python tools/validate_dcl_live_install.py --pair work/1784470893-dcl-unified-clean-v1-runtime-data-pair.json"
        )
        expect_error(historical_pair, "historical inactive artifact", temp, "historical-pair.json")

    print("DCL live proof sequence tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
