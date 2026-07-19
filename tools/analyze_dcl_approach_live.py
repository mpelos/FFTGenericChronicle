#!/usr/bin/env python3
"""Validate a bounded live DCL Approach interruption transaction."""
from __future__ import annotations

import argparse
import re
from pathlib import Path


def field_int(line: str, name: str) -> int | None:
    match = re.search(rf"\b{re.escape(name)}=(0x[0-9A-Fa-f]+|-?\d+)\b", line)
    return int(match.group(1), 0) if match else None


def event_id(line: str) -> int | None:
    return field_int(line, "event")


def parse_tile(value: str) -> tuple[int, int, int]:
    match = re.fullmatch(r"(\d+),(\d+),(\d+)", value)
    if not match:
        raise ValueError(f"invalid tile tuple: {value}")
    return tuple(int(part) for part in match.groups())  # type: ignore[return-value]


def tile_field(line: str, name: str) -> tuple[int, int, int] | None:
    match = re.search(rf"\b{re.escape(name)}=(\d+,\d+,\d+)\b", line)
    return parse_tile(match.group(1)) if match else None


def parse_loan(line: str) -> list[tuple[int, int, int, int]]:
    match = re.search(r"\bunitLoan=([^ ]+)", line)
    if not match:
        return []
    result: list[tuple[int, int, int, int]] = []
    for token in match.group(1).split("->"):
        part = re.fullmatch(r"(\d+),(\d+),(\d+)/raw51=0x([0-9A-Fa-f]{2})", token)
        if not part:
            return []
        result.append(tuple(int(value, 16) if index == 3 else int(value)
                            for index, value in enumerate(part.groups())))
    return result


def analyze_text(
    text: str,
    *,
    owner: int = 443,
    delivery: int = 442,
    event: int | None = None,
    require_terminal: bool = True,
    minimum_effects: int = 1,
) -> tuple[dict[str, int], list[str]]:
    all_lines = text.splitlines()
    lines = all_lines
    scope_errors: list[str] = []
    if event is not None:
        starts = [
            index for index, line in enumerate(all_lines)
            if "[DCL-APPROACH-DECISION]" in line
            and event_id(line) == event
            and field_int(line, "delivery") == delivery
        ]
        if len(starts) != 1:
            scope_errors.append(
                f"expected one eligible decision for requested event {event}, found {len(starts)}"
            )
            lines = []
        else:
            start = starts[0]
            ends = [
                index for index in range(start, len(all_lines))
                if "[DCL-APPROACH-RESUME-RELEASE]" in all_lines[index]
                and event_id(all_lines[index]) == event
            ]
            if not ends:
                scope_errors.append(f"requested event {event} has no resume-release boundary")
                lines = all_lines[start:]
            else:
                lines = all_lines[start : ends[0] + 1]
    hooks = [
        line for line in all_lines
        if "[DCL-APPROACH-HOOK]" in line
        and field_int(line, "owner") == owner
        and field_int(line, "delivery") == delivery
    ]
    decisions = [
        line for line in lines
        if "[DCL-APPROACH-DECISION]" in line
        and field_int(line, "delivery") == delivery
        and field_int(line, "candidates") == 1
    ]
    accepted_queues = [
        line for line in lines
        if "[DCL-APPROACH-QUEUE]" in line and field_int(line, "accepted") == 1
    ]
    commits = [
        line for line in lines
        if "[DCL-REACTION-COMMIT]" in line
        and field_int(line, "pass") == 2
        and field_int(line, "reactionId") == delivery
        and "idsAgree=True" in line
    ]
    validations = [
        line for line in lines
        if "[DCL-REACTION-DELIVERY-VALIDATION]" in line
        and field_int(line, "reactionId") == delivery
    ]
    typed = [line for line in validations if "stage=typed-family" in line]
    final = [line for line in validations if "stage=final" in line]
    accepted_validations = [
        line for line in validations
        if field_int(line, "result") == 0 and field_int(line, "accepted") == 1
    ]
    materialized = [
        line for line in lines
        if "[DCL-REACTION-MATERIALIZED]" in line
        and field_int(line, "reactionId") == delivery
    ]
    targeted_materialized = [
        line for line in materialized
        if field_int(line, "targetMode") == 5
        and field_int(line, "targetIdx") == field_int(line, "sourceIdx")
        and field_int(line, "actionType") == 1
        and field_int(line, "actionId") == 0
    ]
    effects = [
        line for line in lines
        if "[DCL-REACTION-EFFECT]" in line
        and field_int(line, "reactionId") == delivery
        and "state=0x2C" in line
    ]
    failures = [
        line for line in lines
        if "[DCL-APPROACH-" in line
        and any(token in line for token in ("-FAILED]", "audit=fail", "outcome=-1"))
    ]

    counts = {
        "hooks": len(hooks),
        "decisions": len(decisions),
        "accepted_queues": len(accepted_queues),
        "commits": len(commits),
        "typed_validations": len(typed),
        "final_validations": len(final),
        "accepted_validations": len(accepted_validations),
        "materialized": len(materialized),
        "targeted_materialized": len(targeted_materialized),
        "effects": len(effects),
        "failures": len(failures),
        "later_events": 0,
    }
    errors: list[str] = list(scope_errors)
    if not hooks:
        errors.append(f"missing Approach hook for owner {owner} and delivery {delivery}")
    if len(decisions) != 1:
        errors.append(f"expected exactly one eligible delivery decision, found {len(decisions)}")
    if len(accepted_queues) != 1:
        errors.append(f"expected exactly one accepted Approach queue, found {len(accepted_queues)}")
    if failures:
        errors.append(f"Approach failures or failed audits observed: {len(failures)}")
    if len(commits) != 1:
        errors.append(f"expected exactly one pass-2 delivery commit, found {len(commits)}")
    if not typed or not final or len(accepted_validations) < 2:
        errors.append("delivery did not pass both typed-family and final validators")
    if len(targeted_materialized) != 1:
        errors.append("missing unique native action 1/0 materialized against the exact source index")
    if len(effects) < minimum_effects:
        errors.append(f"expected at least {minimum_effects} state-0x2C delivery effects, found {len(effects)}")

    if len(decisions) == 1 and len(accepted_queues) == 1:
        decision = decisions[0]
        queue = accepted_queues[0]
        event = event_id(decision)
        if event is None or event_id(queue) != event:
            errors.append("decision and accepted queue do not share one event id")
        entered = tile_field(decision, "entered")
        cursor_match = re.search(r"\bcursor=(\d+)/(\d+)\b", decision)
        if require_terminal and (not cursor_match or cursor_match.group(1) != cursor_match.group(2)):
            errors.append("eligible decision is not at the terminal route cursor")
        candidate_mask = field_int(decision, "mask")
        commit_mask = field_int(queue, "commitMask")
        if candidate_mask is None or candidate_mask == 0 or candidate_mask.bit_count() != 1:
            errors.append("eligible decision does not carry one exact candidate bit")
        if commit_mask != candidate_mask:
            errors.append("queue commit mask does not equal the exact candidate mask")
        if field_int(queue, "bridgeStage") != 9:
            errors.append("coordinate/mark bridge did not complete diagnostic stage 9")

        mark = re.search(r"\btargetMark=0x([0-9A-Fa-f]+)->0x([0-9A-Fa-f]+)->0x([0-9A-Fa-f]+)", queue)
        if not mark:
            errors.append("missing target-mark before/forced/restored trace")
        else:
            before, forced, restored = (int(value, 16) for value in mark.groups())
            if restored != before or forced != (before | 0x40):
                errors.append("target mark was not borrowed and restored byte-exactly")

        loan = parse_loan(queue)
        unit_tile = tile_field(queue, "unitTile")
        if len(loan) != 3:
            errors.append("missing unit coordinate before/forced/restored trace")
        else:
            before, forced, restored = loan
            if restored != before:
                errors.append("battle-unit coordinate tuple was not restored byte-exactly")
            if unit_tile != before[:3]:
                errors.append("unitTile diagnostic does not equal the borrowed tuple origin")
            if entered != forced[:3]:
                errors.append("forced battle-unit coordinates do not equal the entered actor tile")
            if (forced[3] & 0x7F) != (before[3] & 0x7F):
                errors.append("coordinate loan changed low unit+0x51 facing bits")

        resumes = [
            line for line in lines
            if "[DCL-APPROACH-RESUME]" in line and event_id(line) == event
        ]
        releases = [
            line for line in lines
            if "[DCL-APPROACH-RESUME-RELEASE]" in line and event_id(line) == event
        ]
        if len(resumes) != 1 or not all(token in resumes[0] for token in (
            "native=0x28", "replacement=0x11", "audit=pass", "writes=1"
        )):
            errors.append("missing unique guarded 0x28 -> 0x11 resume substitution")
        if len(releases) != 1 or not all(token in releases[0] for token in ("audit=pass", "writes=1")):
            errors.append("missing unique audited resumed-route release")
        if event is not None:
            later = [
                line for line in all_lines
                if "[DCL-APPROACH-" in line
                and event_id(line) is not None
                and event_id(line) > event
            ]
            counts["later_events"] = len(later)
            if not later:
                errors.append("no later movement event proves battle control continued after resume")

    return counts, errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--owner", type=int, default=443)
    parser.add_argument("--delivery", type=int, default=442)
    parser.add_argument("--event", type=int, help="Isolate one exact Approach event transaction.")
    parser.add_argument("--allow-nonterminal", action="store_true")
    parser.add_argument("--minimum-effects", type=int, default=1)
    args = parser.parse_args()
    counts, errors = analyze_text(
        args.log.read_text(encoding="utf-8", errors="replace"),
        owner=args.owner,
        delivery=args.delivery,
        event=args.event,
        require_terminal=not args.allow_nonterminal,
        minimum_effects=args.minimum_effects,
    )
    print(" ".join(f"{key}={value}" for key, value in counts.items()))
    for error in errors:
        print(f"ERROR: {error}")
    print("Approach live evidence PASS" if not errors else "Approach live evidence FAIL")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
