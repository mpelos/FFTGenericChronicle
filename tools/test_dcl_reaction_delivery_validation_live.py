#!/usr/bin/env python3
"""Smoke tests for native synthetic-Reaction delivery validation evidence."""
from __future__ import annotations

from analyze_dcl_reaction_delivery_validation_live import analyze_text


VALID = """
[DCL-REACTION-DELIVERY-VALIDATION-HOOK] stage=typed-family rva=0x283019
[DCL-REACTION-DELIVERY-VALIDATION-HOOK] stage=typed-bonecrusher rva=0x283148
[DCL-REACTION-DELIVERY-VALIDATION-HOOK] stage=final rva=0x283157 behavior=ExecuteAfter hookLength=7
[DCL-REACTION-DELIVERY-VALIDATION] event=1 stage=typed-family reactorIdx=16 sourceIdx=6 reactionId=442 result=-1 accepted=0 unit=0x1 order=0x2 actionType=1 actionId=0 targetMode=5 targetIdx=6 syntheticState=2->6 now=1
[DCL-REACTION-DELIVERY-VALIDATION] event=2 stage=typed-family reactorIdx=16 sourceIdx=0 reactionId=442 result=0 accepted=1 unit=0x1 order=0x2 actionType=1 actionId=0 targetMode=5 targetIdx=0 syntheticState=2->2 now=2
[DCL-REACTION-DELIVERY-VALIDATION] event=3 stage=final reactorIdx=16 sourceIdx=0 reactionId=442 result=0 accepted=1 unit=0x1 order=0x2 actionType=1 actionId=0 targetMode=5 targetIdx=0 syntheticState=2->2 now=3
"""


def main() -> int:
    counts, errors = analyze_text(
        VALID,
        reaction_id=442,
        reactor=16,
        rejected_source=6,
        accepted_source=0,
    )
    assert not errors, errors
    assert counts["rejected_typed"] == 1
    assert counts["accepted_typed"] == 1
    assert counts["accepted_final"] == 1
    assert counts["unexpected_staged"] == 0

    _, missing_rejection = analyze_text(
        VALID.replace("syntheticState=2->6", "syntheticState=2->2"),
        reaction_id=442,
        reactor=16,
        rejected_source=6,
        accepted_source=0,
    )
    assert any("typed-family rejection" in error for error in missing_rejection)

    _, missing_final = analyze_text(
        VALID.replace("stage=final reactorIdx=16", "stage=final reactorIdx=15"),
        reaction_id=442,
        reactor=16,
        rejected_source=6,
        accepted_source=0,
    )
    assert any("final-validator acceptance" in error for error in missing_final)

    valid_lines = VALID.strip().splitlines()
    reversed_chain = "\n".join(valid_lines[:3] + [valid_lines[4], valid_lines[3], valid_lines[5]])
    _, reversed_errors = analyze_text(
        reversed_chain,
        reaction_id=442,
        reactor=16,
        rejected_source=6,
        accepted_source=0,
    )
    assert any("not ordered" in error for error in reversed_errors)

    extra_staged = VALID + (
        "[DCL-REACTION-DELIVERY-VALIDATION] event=4 stage=typed-family reactorIdx=16 sourceIdx=6 "
        "reactionId=442 result=0 accepted=1 unit=0x1 order=0x2 actionType=1 actionId=0 "
        "targetMode=5 targetIdx=6 syntheticState=2->2 now=4\n"
    )
    extra_counts, extra_errors = analyze_text(
        extra_staged,
        reaction_id=442,
        reactor=16,
        rejected_source=6,
        accepted_source=0,
    )
    assert extra_counts["unexpected_staged"] == 1
    assert any("unexpected staged" in error for error in extra_errors)

    bonecrusher = VALID.replace("reactionId=442", "reactionId=434").replace(
        "stage=typed-family reactorIdx=16", "stage=typed-bonecrusher reactorIdx=16"
    )
    _, bonecrusher_errors = analyze_text(
        bonecrusher,
        reaction_id=434,
        reactor=16,
        rejected_source=6,
        accepted_source=0,
    )
    assert not bonecrusher_errors, bonecrusher_errors
    print("reaction delivery validation live analyzer tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
