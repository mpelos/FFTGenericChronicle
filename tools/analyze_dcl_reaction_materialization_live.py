#!/usr/bin/env python3
"""Correlate accepted Reaction orders with pass-2 commits and delivered effects."""
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


HOOK_RE = re.compile(r"\[DCL-REACTION-MATERIALIZED-HOOK\].*rva=0x2063BD\b")
COMMIT_HOOK_RE = re.compile(r"\[DCL-REACTION-COMMIT-HOOK\]\s+pass=2\b.*rva=0x206421\b")
EFFECT_HOOK_RE = re.compile(r"\[DCL-REACTION-EFFECT-HOOK\].*rva=0x212C2E\b")
ROW_RE = re.compile(
    r"\[DCL-REACTION-MATERIALIZED\]\s+event=(?P<event>\d+)\s+"
    r"reactorIdx=(?P<reactor>-?\d+)\s+sourceIdx=(?P<source>-?\d+)\s+"
    r"reactionId=(?P<reaction>\d+)\s+unit=0x(?P<unit>[0-9A-Fa-f]+)\s+"
    r"order=0x(?P<order>[0-9A-Fa-f]+)\s+casterIdx=(?P<caster>\d+)\s+"
    r"actionType=(?P<action_type>\d+)\s+actionId=(?P<action_id>\d+)\s+"
    r"itemId=(?P<item_id>\d+)\s+targetMode=(?P<target_mode>\d+)\s+"
    r"targetIdx=(?P<target>\d+)\s+target=\((?P<x>\d+),(?P<layer>\d+),(?P<y>\d+)\)\s+"
    r"raw=(?P<raw>[0-9A-Fa-f]{40})\b"
)
COMMIT_RE = re.compile(
    r"\[DCL-REACTION-COMMIT\].*?pass=2\b.*?reactorIdx=(?P<reactor>-?\d+)\s+"
    r"sourceIdx=(?P<source>-?\d+)\s+reactionId=(?P<reaction>\d+)\b"
)
EFFECT_RE = re.compile(
    r"\[DCL-REACTION-EFFECT\].*?state=0x2C\b.*?actorIdx=(?P<reactor>-?\d+)\s+"
    r"sourceIdx=(?P<source>-?\d+)\s+reactionId=(?P<reaction>\d+)\s+"
    r"actionId=(?P<action_id>\d+)\s+targetCount=(?P<count>\d+)\s+targets=\[(?P<targets>[^]]*)\]"
)


@dataclass(frozen=True)
class Materialized:
    event: int
    reactor: int
    source: int
    reaction: int
    caster: int
    action_type: int
    action_id: int
    item_id: int
    target_mode: int
    target: int
    raw: str


@dataclass(frozen=True)
class Effect:
    reactor: int
    source: int
    reaction: int
    action_id: int
    targets: tuple[int, ...]


def parse(text: str) -> tuple[list[Materialized], list[tuple[int, int, int]], list[Effect]]:
    materialized: list[Materialized] = []
    commits: list[tuple[int, int, int]] = []
    effects: list[Effect] = []
    for line in text.splitlines():
        if match := ROW_RE.search(line):
            materialized.append(Materialized(
                event=int(match["event"]), reactor=int(match["reactor"]), source=int(match["source"]),
                reaction=int(match["reaction"]), caster=int(match["caster"]),
                action_type=int(match["action_type"]), action_id=int(match["action_id"]),
                item_id=int(match["item_id"]), target_mode=int(match["target_mode"]),
                target=int(match["target"]), raw=match["raw"].upper(),
            ))
        elif match := COMMIT_RE.search(line):
            commits.append((int(match["reactor"]), int(match["source"]), int(match["reaction"])))
        elif match := EFFECT_RE.search(line):
            target_text = match["targets"].strip()
            targets = tuple(int(value) for value in target_text.split(",") if value.strip())
            effects.append(Effect(
                reactor=int(match["reactor"]), source=int(match["source"]),
                reaction=int(match["reaction"]), action_id=int(match["action_id"]), targets=targets,
            ))
    return materialized, commits, effects


def analyze(
    text: str,
    reaction_id: int,
    reactor: int,
    source: int,
    expected_action_type: int,
    expected_action_id: int,
    expected_target: int,
    expected_materialized_count: int,
    expected_effect_count: int,
    actor_reactor: int | None = None,
) -> tuple[list[tuple[str, bool]], list[Materialized], list[Effect]]:
    materialized, commits, effects = parse(text)
    expected_actor_reactor = reactor if actor_reactor is None else actor_reactor
    rows = [row for row in materialized if row.reaction == reaction_id and row.reactor == reactor and row.source == source]
    matching_commits = [row for row in commits if row == (expected_actor_reactor, source, reaction_id)]
    matching_effects = [
        row for row in effects
        if row.reaction == reaction_id and row.reactor == expected_actor_reactor and row.source == source
    ]
    checks = [
        ("materialization hook installed at 0x2063BD", bool(HOOK_RE.search(text))),
        ("pass-2 commit hook installed at 0x206421", bool(COMMIT_HOOK_RE.search(text))),
        ("effect hook installed at 0x212C2E", bool(EFFECT_HOOK_RE.search(text))),
        (f"exactly {expected_materialized_count} matching materialized row(s)", len(rows) == expected_materialized_count),
        (f"one matching accepted pass-2 commit for actor reactor {expected_actor_reactor}", len(matching_commits) == 1),
        (f"exactly {expected_effect_count} matching effect row(s)", len(matching_effects) == expected_effect_count),
        ("materialized caster equals selected unit index", bool(rows) and all(row.caster == reactor for row in rows)),
        (f"materialized action type is {expected_action_type}", bool(rows) and all(row.action_type == expected_action_type for row in rows)),
        (f"materialized action id is {expected_action_id}", bool(rows) and all(row.action_id == expected_action_id for row in rows)),
        ("materialized target mode is 5", bool(rows) and all(row.target_mode == 5 for row in rows)),
        (f"materialized target index is {expected_target}", bool(rows) and all(row.target == expected_target for row in rows)),
        (f"delivered action id is {expected_action_id}", bool(matching_effects) and all(row.action_id == expected_action_id for row in matching_effects)),
        (f"delivered target list is [{expected_target}]", bool(matching_effects) and all(row.targets == (expected_target,) for row in matching_effects)),
    ]
    return checks, rows, matching_effects


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--reaction-id", type=int, required=True)
    parser.add_argument("--reactor", type=int, required=True)
    parser.add_argument(
        "--actor-reactor",
        type=int,
        help="Actor/commit reactor index when it differs from the selected unit-table index (defaults to --reactor).",
    )
    parser.add_argument("--source", type=int, required=True)
    parser.add_argument("--expected-action-type", type=int, required=True)
    parser.add_argument("--expected-action-id", type=int, required=True)
    parser.add_argument("--expected-target", type=int, required=True)
    parser.add_argument("--expected-materialized-count", type=int, default=1)
    parser.add_argument("--expected-effect-count", type=int, default=1)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    text = args.log.read_text(encoding="utf-8", errors="replace")
    checks, rows, effects = analyze(
        text, args.reaction_id, args.reactor, args.source, args.expected_action_type,
        args.expected_action_id, args.expected_target, args.expected_materialized_count,
        args.expected_effect_count, args.actor_reactor,
    )
    print(f"materialized_rows={len(rows)} effect_rows={len(effects)}")
    for label, passed in checks:
        print(f"{'PASS' if passed else 'FAIL'}: {label}")
    return 0 if all(passed for _, passed in checks) else 1


if __name__ == "__main__":
    sys.exit(main())
