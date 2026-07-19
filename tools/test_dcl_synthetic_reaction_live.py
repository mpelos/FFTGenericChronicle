#!/usr/bin/env python3
"""Regression tests for synthetic-Reaction live evidence analysis."""
from __future__ import annotations

from analyze_dcl_synthetic_reaction_live import analyze_text


LOG_ONLY = """
[DCL-REACTION-PRESELECT-HOOK] synthetic=log-only:carrier=443:delivery=442
[DCL-SYNTHETIC-REACTION-GATE] carrier=443 delivery=442 defender=0x80 hitKnown=1 hit=1 chance=100 roll=0 accepted=1 replay=0 mailbox=armed reason=accepted
[DCL-REACTION-PRESELECT] event=1 producer=synthetic-would-stage:unit=-1:carrier=-1 syntheticStates=[3:4]:carrier=443:delivery=442 now=1
"""

LIVE = """
[DCL-REACTION-PRESELECT-HOOK] synthetic=live:carrier=443:delivery=442
[DCL-REACTION-MATERIALIZED-HOOK] rva=0x2831BD stage=special-pre-target-build rewrite=off synthetic=live:carrier=443:delivery=442
[DCL-SYNTHETIC-REACTION-GATE] carrier=443 delivery=442 defender=0x80 hitKnown=1 hit=1 chance=100 roll=0 accepted=1 replay=0 mailbox=armed reason=accepted
[DCL-REACTION-PRESELECT] event=1 producer=synthetic-staged:unit=-1:carrier=-1 syntheticStates=[3:2]:carrier=443:delivery=442 now=1
[DCL-REACTION-MATERIALIZED] event=1 reactorIdx=3 sourceIdx=16 reactionId=442 actionType=1 actionId=0 targetMode=5 targetIdx=16 rewrite=none originalActionType=1 originalActionId=0 syntheticDelivery=owned
[DCL-REACTION-COMMIT] event=1 pass=2 reactionId=442 idsAgree=True
[DCL-SYNTHETIC-REACTION-COMMIT] carrier=443 delivery=442 defender=0x80 sourceIdx=16 cadence=consumed ownership=materialized-delivery-owned
[DCL-REACTION-EFFECT] event=1 state=0x2C sourceIdx=16 reactionId=442 actionId=0 targetCount=1 targets=[16]
"""


def startup_dump(reaction_set: bytes = bytes.fromhex("00000400")) -> str:
    payload = bytearray(0x200)
    payload[0x14:0x16] = (443).to_bytes(2, "little")
    payload[0x94:0x98] = reaction_set
    payload[0x1CE:0x1D0] = (0).to_bytes(2, "little", signed=True)
    rendered = " | ".join(
        " ".join(f"{byte:02X}" for byte in payload[offset : offset + 16])
        for offset in range(0, len(payload), 16)
    )
    return f"[DUMP ptr=0x141855CE0 id=0x80] {rendered}"


def main() -> int:
    counts, errors = analyze_text(LOG_ONLY, 443, "log-only", delivery_id=442)
    assert not errors, errors
    assert counts["would_stage"] == 1 and counts["staged"] == 0

    counts, errors = analyze_text(
        LIVE,
        443,
        "live",
        delivery_id=442,
        require_source_retarget=True,
        expected_action_type=1,
        expected_action_id=0,
        expected_original_action_type=1,
        expected_original_action_id=0,
        require_effect=True,
    )
    assert not errors, errors
    assert counts["staged"] == counts["materialized"] == counts["native_commits"] == counts["consumed"] == 1
    assert counts["owned_materialized"] == counts["source_retargeted"] == 1
    assert counts["rewritten_materialized"] == 0
    assert counts["materialization_hooks"] == 1

    _, errors = analyze_text(
        LIVE.replace("rva=0x2831BD stage=special-pre-target-build", "rva=0x2063BD stage=post-selector"),
        443,
        "live",
        delivery_id=442,
        require_source_retarget=True,
        expected_action_type=1,
        expected_action_id=0,
        expected_original_action_type=1,
        expected_original_action_id=0,
        require_effect=True,
    )
    assert any("special-delivery" in error for error in errors), errors
    assert counts["expected_actions"] == counts["expected_original_actions"] == counts["delivery_effects"] == 1

    _, errors = analyze_text(LOG_ONLY.replace("synthetic-would-stage", "none"), 443, "log-only", delivery_id=442)
    assert errors
    _, errors = analyze_text(LIVE.replace("cadence=consumed", "cadence=duplicate"), 443, "live", delivery_id=442)
    assert errors
    strict_live = dict(
        delivery_id=442,
        require_source_retarget=True,
        expected_action_type=1,
        expected_action_id=0,
        expected_original_action_type=1,
        expected_original_action_id=0,
        require_effect=True,
    )
    _, errors = analyze_text(LIVE.replace("syntheticDelivery=owned", "syntheticDelivery=none"), 443, "live", **strict_live)
    assert errors and any("materialized delivery handshake" in error for error in errors)
    _, errors = analyze_text(LIVE.replace("targetIdx=16", "targetIdx=15"), 443, "live", **strict_live)
    assert errors and any("exact source index" in error for error in errors)
    _, errors = analyze_text(LIVE.replace("actionType=1", "actionType=20"), 443, "live", **strict_live)
    assert errors and any("expected materialized action" in error for error in errors)
    _, errors = analyze_text(LIVE.replace("originalActionType=1", "originalActionType=0"), 443, "live", **strict_live)
    assert errors and any("original materialized action" in error for error in errors)
    _, errors = analyze_text(LIVE.replace("targets=[16]", "targets=[15]"), 443, "live", **strict_live)
    assert errors and any("delivery-carrier effect" in error for error in errors)
    _, errors = analyze_text(LIVE.replace("pass=2", "pass=1"), 443, "live", **strict_live)
    assert errors and any("pass-2 native commit" in error for error in errors)
    replayed = LIVE.replace("replay=0", "replay=1")
    _, errors = analyze_text(replayed, 443, "live", **strict_live)
    assert errors and any("replays observed" in error for error in errors)
    _, errors = analyze_text(LOG_ONLY + "\n[DCL-REACTION-PRESELECT-FAILED] boom", 443, "log-only", delivery_id=442)
    assert errors

    strict_log = startup_dump() + "\n" + LOG_ONLY
    counts, errors = analyze_text(
        strict_log,
        443,
        "log-only",
        delivery_id=442,
        require_startup_owner=True,
        expected_reaction_set=bytes.fromhex("00000400"),
    )
    assert not errors, errors
    assert counts["startup_owner_dumps"] == counts["startup_owner_valid"] == 1

    wrong_bitfield = startup_dump(bytes.fromhex("00000800")) + "\n" + LOG_ONLY
    _, errors = analyze_text(
        wrong_bitfield,
        443,
        "log-only",
        delivery_id=442,
        require_startup_owner=True,
        expected_reaction_set=bytes.fromhex("00000400"),
    )
    assert errors and any("invalid startup owner" in error for error in errors)

    print("synthetic-Reaction live analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
