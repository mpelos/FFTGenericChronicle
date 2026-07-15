#!/usr/bin/env python3
"""Offline regression tests for Reaction commit/effect live-log correlation."""

from __future__ import annotations

import analyze_dcl_reaction_effect_live as analyzer


HOOKS = """\
[DCL-REACTION-COMMIT-HOOK] pass=2 rva=0x206421
[DCL-REACTION-COMMIT-HOOK] pass=0 rva=0x2066AE
[DCL-REACTION-COMMIT-HOOK] pass=1 rva=0x206743
[DCL-REACTION-EFFECT-HOOK] rva=0x212C2E
"""
COMMIT = """\
[DCL-REACTION-COMMIT] event=3 pass=2 actor=0x140D30AC8 reactorIdx=4 sourceIdx=0 reactionId=442 actor18C=442 actor142=442 idsAgree=True record=0x141855CE0 targetCount=1 targets=[0] replacement=none retarget=none:-1 noiseReason=none now=100
"""
EFFECT = """\
[DCL-REACTION-EFFECT] event=7 state=0x2C actor=0x140D30AC8 actorIdx=4 sourceIdx=0 reactionId=442 actionId=0 targetCount=1 targets=[0] now=200
[DCL-REACTION-EFFECT] event=8 state=0x2C actor=0x140D30AC8 actorIdx=4 sourceIdx=0 reactionId=442 actionId=0 targetCount=1 targets=[0] now=210
"""


def main() -> int:
    checks, commits, effects = analyzer.analyze(HOOKS + COMMIT + EFFECT, 442, 4, 0, 0, 2, True)
    assert commits and effects and all(result for _, result in checks), checks

    changed_target = EFFECT.replace("targets=[0]", "targets=[1]")
    checks, _, _ = analyzer.analyze(HOOKS + COMMIT + changed_target, 442, 4, 0, 0, 2, True)
    assert not all(result for _, result in checks)

    duplicate = HOOKS + COMMIT + EFFECT + EFFECT.replace("event=7", "event=9").replace("event=8", "event=10")
    checks, _, _ = analyzer.analyze(duplicate, 442, 4, 0, 0, 2, True)
    assert not all(result for _, result in checks)

    wrong_state = EFFECT.replace("state=0x2C", "state=0x2D")
    checks, _, _ = analyzer.analyze(HOOKS + COMMIT + wrong_state, 442, 4, 0, 0, 2, True)
    assert not all(result for _, result in checks)

    wrong_action = EFFECT.replace("actionId=0", "actionId=442")
    checks, _, _ = analyzer.analyze(HOOKS + COMMIT + wrong_action, 442, 4, 0, 0, 2, True)
    assert not all(result for _, result in checks)

    print("reaction effect live analyzer tests PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
