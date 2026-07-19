#!/usr/bin/env python3
"""Validate a rejected-then-accepted native synthetic-Reaction delivery chain."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


HOOK_RE = re.compile(
    r"\[DCL-REACTION-DELIVERY-VALIDATION-HOOK\].*\bstage=(typed-family|typed-bonecrusher|final)\b"
)
EVENT_RE = re.compile(r"\[DCL-REACTION-DELIVERY-VALIDATION\]")
TYPED_FAMILY_IDS = {435, 436, 437, 442}


def field(line: str, name: str) -> int | None:
    match = re.search(rf"\b{re.escape(name)}=(-?\d+)\b", line)
    return int(match.group(1)) if match else None


def synthetic_transition(line: str) -> tuple[int, int] | None:
    match = re.search(r"\bsyntheticState=(-?\d+)->(-?\d+)\b", line)
    return (int(match.group(1)), int(match.group(2))) if match else None


def analyze_text(
    text: str,
    *,
    reaction_id: int,
    reactor: int,
    rejected_source: int,
    accepted_source: int,
) -> tuple[dict[str, int], list[str]]:
    lines = text.splitlines()
    typed_stage = "typed-bonecrusher" if reaction_id == 434 else "typed-family"
    hook_stages = {match.group(1) for line in lines if (match := HOOK_RE.search(line))}
    events = [(position, line) for position, line in enumerate(lines) if EVENT_RE.search(line)]
    relevant = [
        (position, line) for position, line in events
        if field(line, "reactionId") == reaction_id and field(line, "reactorIdx") == reactor
    ]
    rejected_typed = [
        (position, line) for position, line in relevant
        if f"stage={typed_stage}" in line
        and field(line, "sourceIdx") == rejected_source
        and field(line, "result") not in (None, 0)
        and "accepted=0" in line
        and "syntheticState=2->6" in line
    ]
    accepted_typed = [
        (position, line) for position, line in relevant
        if f"stage={typed_stage}" in line
        and field(line, "sourceIdx") == accepted_source
        and field(line, "result") == 0
        and "accepted=1" in line
        and "syntheticState=2->2" in line
    ]
    accepted_final = [
        (position, line) for position, line in relevant
        if "stage=final" in line
        and field(line, "sourceIdx") == accepted_source
        and field(line, "result") == 0
        and "accepted=1" in line
        and "syntheticState=2->2" in line
    ]
    canonical_positions = {
        position
        for rows in (rejected_typed, accepted_typed, accepted_final)
        for position, _ in rows
    }
    unexpected_staged = [
        line for position, line in relevant
        if synthetic_transition(line) is not None
        and synthetic_transition(line)[0] == 2
        and position not in canonical_positions
    ]
    ordered = (
        len(rejected_typed) == 1
        and len(accepted_typed) == 1
        and len(accepted_final) == 1
        and rejected_typed[0][0] < accepted_typed[0][0] < accepted_final[0][0]
    )
    failures = [
        line for line in lines
        if "[DCL-REACTION-DELIVERY-VALIDATION-" in line
        and ("-FAILED]" in line or "-SKIP]" in line or "-LOST" in line)
    ]
    counts = {
        "hooks": len(hook_stages),
        "events": len(events),
        "relevant": len(relevant),
        "rejected_typed": len(rejected_typed),
        "accepted_typed": len(accepted_typed),
        "accepted_final": len(accepted_final),
        "unexpected_staged": len(unexpected_staged),
        "failures": len(failures),
    }
    errors: list[str] = []
    expected_hooks = {"typed-family", "typed-bonecrusher", "final"}
    if hook_stages != expected_hooks:
        errors.append(f"expected all three validation hooks, observed {sorted(hook_stages)}")
    if reaction_id != 434 and reaction_id not in TYPED_FAMILY_IDS:
        errors.append(f"Reaction {reaction_id} has no typed-helper result stage supported by this analyzer")
    if not rejected_typed:
        errors.append(f"missing exact {typed_stage} rejection with staged->rejected mailbox ownership")
    elif len(rejected_typed) != 1:
        errors.append(f"expected exactly one {typed_stage} rejection, observed {len(rejected_typed)}")
    if not accepted_typed:
        errors.append(f"missing later exact {typed_stage} acceptance for the adjacent source")
    elif len(accepted_typed) != 1:
        errors.append(f"expected exactly one {typed_stage} acceptance, observed {len(accepted_typed)}")
    if not accepted_final:
        errors.append("missing later exact final-validator acceptance for the adjacent source")
    elif len(accepted_final) != 1:
        errors.append(f"expected exactly one final-validator acceptance, observed {len(accepted_final)}")
    if rejected_typed and accepted_typed and accepted_final and not ordered:
        errors.append(f"delivery events are not ordered rejected {typed_stage} -> accepted {typed_stage} -> accepted final")
    if unexpected_staged:
        errors.append(f"unexpected staged synthetic validation transition(s): {len(unexpected_staged)}")
    if failures:
        errors.append(f"validation hook failures/skips/losses observed: {len(failures)}")
    return counts, errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--reaction-id", type=int, required=True)
    parser.add_argument("--reactor", type=int, required=True)
    parser.add_argument("--rejected-source", type=int, required=True)
    parser.add_argument("--accepted-source", type=int, required=True)
    args = parser.parse_args()
    counts, errors = analyze_text(
        args.log.read_text(encoding="utf-8", errors="replace"),
        reaction_id=args.reaction_id,
        reactor=args.reactor,
        rejected_source=args.rejected_source,
        accepted_source=args.accepted_source,
    )
    print(" ".join(f"{key}={value}" for key, value in counts.items()))
    for error in errors:
        print(f"ERROR: {error}")
    print("reaction delivery validation live PASS" if not errors else "reaction delivery validation live FAIL")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
