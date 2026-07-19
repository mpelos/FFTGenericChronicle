#!/usr/bin/env python3
"""Offline regression tests for Reaction materialization live correlation."""
from __future__ import annotations

import analyze_dcl_reaction_materialization_live as analyzer


LOG = """\
[DCL-REACTION-MATERIALIZED-HOOK] rva=0x2831BD addr=0x1402831BD stage=special-pre-target-build
[DCL-REACTION-COMMIT-HOOK] pass=2 rva=0x206421
[DCL-REACTION-EFFECT-HOOK] rva=0x212C2E
[DCL-REACTION-MATERIALIZED] event=5 reactorIdx=4 sourceIdx=3 reactionId=442 unit=0x1418544E0 order=0x141854680 casterIdx=4 actionType=1 actionId=0 itemId=0 targetMode=5 targetIdx=3 target=(10,0,7) raw=0401000000000000000005030A00000007000000 now=100
[DCL-REACTION-COMMIT] event=8 pass=2 actor=0x140D31558 reactorIdx=4 sourceIdx=3 reactionId=442 actor18C=442 actor142=442 idsAgree=True record=0x1418544E0 targetCount=1 targets=[3] replacement=none retarget=none:-1 noiseReason=none now=110
[DCL-REACTION-EFFECT] event=11 state=0x2C actor=0x140D31558 actorIdx=4 sourceIdx=3 reactionId=442 actionId=0 targetCount=1 targets=[3] now=200
[DCL-REACTION-EFFECT] event=12 state=0x2C actor=0x140D31558 actorIdx=4 sourceIdx=3 reactionId=442 actionId=0 targetCount=1 targets=[3] now=210
"""


def main() -> int:
    checks, rows, effects = analyzer.analyze(LOG, 442, 4, 3, 1, 0, 3, 1, 2)
    assert rows and effects and all(passed for _, passed in checks), checks

    wrong_target = LOG.replace("targetIdx=3", "targetIdx=4")
    checks, _, _ = analyzer.analyze(wrong_target, 442, 4, 3, 1, 0, 3, 1, 2)
    assert not all(passed for _, passed in checks)

    wrong_action = LOG.replace("actionType=1 actionId=0", "actionType=0 actionId=442")
    checks, _, _ = analyzer.analyze(wrong_action, 442, 4, 3, 1, 0, 3, 1, 2)
    assert not all(passed for _, passed in checks)

    commit_line = next(line for line in LOG.splitlines() if line.startswith("[DCL-REACTION-COMMIT]"))
    duplicate = LOG + commit_line + "\n"
    checks, _, _ = analyzer.analyze(duplicate, 442, 4, 3, 1, 0, 3, 1, 2)
    assert not all(passed for _, passed in checks)

    split_index_log = (
        LOG.replace("reactorIdx=4 sourceIdx=3 reactionId=442 unit=", "reactorIdx=3 sourceIdx=3 reactionId=442 unit=", 1)
        .replace("casterIdx=4", "casterIdx=3", 1)
        .replace("reactorIdx=4 sourceIdx=3 reactionId=442 actor18C=", "reactorIdx=1 sourceIdx=3 reactionId=442 actor18C=")
        .replace("actorIdx=4 sourceIdx=3 reactionId=442", "actorIdx=1 sourceIdx=3 reactionId=442")
    )
    checks, rows, effects = analyzer.analyze(split_index_log, 442, 3, 3, 1, 0, 3, 1, 2, actor_reactor=1)
    assert rows and effects and all(passed for _, passed in checks), checks

    print("reaction materialization live analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
