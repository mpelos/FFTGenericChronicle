#!/usr/bin/env python3
"""Regression tests for synthetic-Reaction live evidence analysis."""
from __future__ import annotations

from analyze_dcl_synthetic_reaction_live import analyze_text


LOG_ONLY = """
[DCL-REACTION-PRESELECT-HOOK] synthetic=log-only:carrier=443
[DCL-SYNTHETIC-REACTION-GATE] carrier=443 defender=0x80 hitKnown=1 hit=1 chance=100 roll=0 accepted=1 replay=0 mailbox=armed reason=accepted
[DCL-REACTION-PRESELECT] event=1 producer=synthetic-would-stage:unit=-1:carrier=-1 syntheticStates=[3:4]:carrier=443 now=1
"""

LIVE = """
[DCL-REACTION-PRESELECT-HOOK] synthetic=live:carrier=443
[DCL-SYNTHETIC-REACTION-GATE] carrier=443 defender=0x80 hitKnown=1 hit=1 chance=100 roll=0 accepted=1 replay=0 mailbox=armed reason=accepted
[DCL-REACTION-PRESELECT] event=1 producer=synthetic-staged:unit=-1:carrier=-1 syntheticStates=[3:2]:carrier=443 now=1
[DCL-REACTION-MATERIALIZED] event=1 reactorIdx=3 sourceIdx=16 reactionId=443 actionType=1 actionId=0
[DCL-REACTION-COMMIT] event=1 pass=2 reactionId=443 idsAgree=True
[DCL-SYNTHETIC-REACTION-COMMIT] carrier=443 defender=0x80 sourceIdx=16 cadence=consumed delivery=accepted-order-owned
"""


def main() -> int:
    counts, errors = analyze_text(LOG_ONLY, 443, "log-only")
    assert not errors, errors
    assert counts["would_stage"] == 1 and counts["staged"] == 0

    counts, errors = analyze_text(LIVE, 443, "live")
    assert not errors, errors
    assert counts["staged"] == counts["materialized"] == counts["native_commits"] == counts["consumed"] == 1

    _, errors = analyze_text(LOG_ONLY.replace("synthetic-would-stage", "none"), 443, "log-only")
    assert errors
    _, errors = analyze_text(LIVE.replace("cadence=consumed", "cadence=duplicate"), 443, "live")
    assert errors
    _, errors = analyze_text(LOG_ONLY + "\n[DCL-REACTION-PRESELECT-FAILED] boom", 443, "log-only")
    assert errors

    print("synthetic-Reaction live analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
