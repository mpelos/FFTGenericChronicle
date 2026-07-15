#!/usr/bin/env python3
"""Regression tests for the generic synthetic-Reaction transaction analyzer."""
from __future__ import annotations

import analyze_dcl_synthetic_reaction_transaction as analyzer


def main() -> int:
    paths = {
        "mod": analyzer.DEFAULT_MOD,
        "core": analyzer.DEFAULT_CORE,
        "cadence": analyzer.DEFAULT_CADENCE,
        "validator": analyzer.DEFAULT_VALIDATOR,
    }
    texts = {owner: path.read_text(encoding="utf-8") for owner, path in paths.items()}
    results = analyzer.analyze(texts)
    assert results and all(passed for _, passed in results), results

    for anchor in analyzer.ANCHORS:
        mutated = dict(texts)
        mutated[anchor.owner] = mutated[anchor.owner].replace(anchor.needle, "REMOVED")
        by_name = {candidate.name: passed for candidate, passed in analyzer.analyze(mutated)}
        assert not by_name[anchor.name], f"missing anchor did not fail closed: {anchor.name}"

    for owner, needle, _ in analyzer.FORBIDDEN:
        mutated = dict(texts)
        mutated[owner] += "\n" + needle
        assert not all(passed for _, passed in analyzer.analyze(mutated)), needle

    print("synthetic-Reaction transaction analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
