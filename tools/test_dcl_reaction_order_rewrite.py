#!/usr/bin/env python3
"""Regression tests for the accepted Reaction order rewrite source gate."""
from __future__ import annotations

import analyze_dcl_reaction_order_rewrite as analyzer


def main() -> int:
    mod_text = analyzer.DEFAULT_MOD.read_text(encoding="utf-8")
    validator_text = analyzer.DEFAULT_VALIDATOR.read_text(encoding="utf-8")
    results = analyzer.analyze(mod_text, validator_text)
    assert results and all(passed for _, passed in results), results

    for anchor in analyzer.ANCHORS:
        owner_text = mod_text if anchor.owner == "mod" else validator_text
        mutated = owner_text.replace(anchor.needle, "REMOVED")
        if anchor.owner == "mod":
            changed = analyzer.analyze(mutated, validator_text)
        else:
            changed = analyzer.analyze(mod_text, mutated)
        by_name = {candidate.name: passed for candidate, passed in changed}
        assert not by_name[anchor.name], f"missing anchor did not fail closed: {anchor.name}"

    print("reaction accepted-order rewrite analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
