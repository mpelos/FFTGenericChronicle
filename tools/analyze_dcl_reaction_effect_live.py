#!/usr/bin/env python3
"""Correlate accepted native Reaction commits with the state-0x2C effect boundary."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


COMMIT_HOOK_RE = re.compile(r"^\[DCL-REACTION-COMMIT-HOOK\] pass=(\d)", re.MULTILINE)
EFFECT_HOOK_RE = re.compile(r"^\[DCL-REACTION-EFFECT-HOOK\]", re.MULTILINE)
COMMIT_RE = re.compile(
    r"^\[DCL-REACTION-COMMIT\] event=(?P<event>\d+) pass=(?P<pass>\d+) "
    r"actor=(?P<actor>0x[0-9A-F]+) reactorIdx=(?P<reactor>\d+) "
    r"sourceIdx=(?P<source>-?\d+) reactionId=(?P<reaction>\d+) "
    r"actor18C=(?P<id18c>\d+) actor142=(?P<id142>\d+) "
    r"idsAgree=(?P<agree>True|False).*?targetCount=(?P<count>\d+) "
    r"targets=\[(?P<targets>[^]]*)\].*?now=(?P<now>\d+)",
    re.MULTILINE,
)
EFFECT_RE = re.compile(
    r"^\[DCL-REACTION-EFFECT\] event=(?P<event>\d+) state=0x(?P<state>[0-9A-F]+) "
    r"actor=(?P<actor>0x[0-9A-F]+) actorIdx=(?P<reactor>-?\d+) "
    r"sourceIdx=(?P<source>-?\d+) reactionId=(?P<reaction>-?\d+) "
    r"actionId=(?P<action>-?\d+) targetCount=(?P<count>\d+) "
    r"targets=\[(?P<targets>[^]]*)\] now=(?P<now>\d+)",
    re.MULTILINE,
)


def targets(value: str) -> tuple[int, ...]:
    return tuple(int(part.strip()) for part in value.split(",") if part.strip())


def parse_commits(text: str) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for match in COMMIT_RE.finditer(text):
        row = match.groupdict()
        rows.append(
            {
                "event": int(row["event"]),
                "pass": int(row["pass"]),
                "actor": row["actor"],
                "reactor": int(row["reactor"]),
                "source": int(row["source"]),
                "reaction": int(row["reaction"]),
                "id18c": int(row["id18c"]),
                "id142": int(row["id142"]),
                "agree": row["agree"] == "True",
                "count": int(row["count"]),
                "targets": targets(row["targets"]),
                "now": int(row["now"]),
                "start": match.start(),
            }
        )
    return rows


def parse_effects(text: str) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for match in EFFECT_RE.finditer(text):
        row = match.groupdict()
        rows.append(
            {
                "event": int(row["event"]),
                "state": int(row["state"], 16),
                "actor": row["actor"],
                "reactor": int(row["reactor"]),
                "source": int(row["source"]),
                "reaction": int(row["reaction"]),
                "action": int(row["action"]),
                "count": int(row["count"]),
                "targets": targets(row["targets"]),
                "now": int(row["now"]),
                "start": match.start(),
            }
        )
    return rows


def analyze(
    text: str,
    reaction_id: int,
    reactor: int | None = None,
    source: int | None = None,
    expected_action_id: int | None = None,
    expected_effect_count: int | None = None,
    expect_target_source: bool = False,
) -> tuple[list[tuple[str, bool]], list[dict[str, object]], list[dict[str, object]]]:
    commits = [row for row in parse_commits(text) if row["reaction"] == reaction_id]
    effects = [row for row in parse_effects(text) if row["reaction"] == reaction_id]
    if reactor is not None:
        commits = [row for row in commits if row["reactor"] == reactor]
        effects = [row for row in effects if row["reactor"] == reactor]
    if source is not None:
        commits = [row for row in commits if row["source"] == source]
        effects = [row for row in effects if row["source"] == source]

    if len(commits) == 1:
        commit = commits[0]
        effects = [
            row
            for row in effects
            if int(row["start"]) > int(commit["start"])
            and row["state"] == 0x2C
            and row["actor"] == commit["actor"]
        ]
    target_source_ok = (
        not expect_target_source
        or (
            source is not None
            and bool(effects)
            and all(row["count"] == 1 and row["targets"] == (source,) for row in effects)
        )
    )
    action_ok = expected_action_id is None or (
        bool(effects) and all(row["action"] == expected_action_id for row in effects)
    )
    count_ok = expected_effect_count is None or len(effects) == expected_effect_count
    checks = [
        ("all three guarded commit hooks installed", sorted(map(int, COMMIT_HOOK_RE.findall(text))) == [0, 1, 2]),
        ("state-0x2C effect hook installed", bool(EFFECT_HOOK_RE.search(text))),
        (f"capture contains exactly one selected Reaction {reaction_id} commit", len(commits) == 1),
        ("selected commit is pass 2 with agreeing ids", len(commits) == 1 and commits[0]["pass"] == 2 and commits[0]["agree"]),
        ("later effect rows preserve actor, reactor, source, and Reaction presentation id", bool(effects)),
        ("effect-row count matches the expected native transaction count", count_ok),
        ("effect executable action id matches the expected native payload", action_ok),
        ("effect target is the incoming source", target_source_ok),
    ]
    return checks, commits, effects


def render(
    source_path: Path,
    reaction_id: int,
    checks: list[tuple[str, bool]],
    commits: list[dict[str, object]],
    effects: list[dict[str, object]],
) -> str:
    ok = all(result for _, result in checks)
    lines = [
        "# DCL Reaction commit/effect live correlation",
        "",
        f"Source: `{source_path}`",
        f"Reaction: `{reaction_id}`",
        "",
        "## Checks",
        "",
        "| Check | Result |",
        "| --- | --- |",
        *(f"| {label} | {'PASS' if result else 'FAIL'} |" for label, result in checks),
        "",
        "## Selected rows",
        "",
        "| Kind | Event | Actor | Reactor | Source | Reaction/action | Targets |",
        "| --- | ---: | --- | ---: | ---: | --- | --- |",
    ]
    lines.extend(
        f"| commit | {row['event']} | `{row['actor']}` | {row['reactor']} | {row['source']} | "
        f"`{row['reaction']}/{row['id142']}` | `{row['targets']}` |"
        for row in commits
    )
    lines.extend(
        f"| effect | {row['event']} | `{row['actor']}` | {row['reactor']} | {row['source']} | "
        f"`{row['reaction']}/{row['action']}` | `{row['targets']}` |"
        for row in effects
    )
    lines.extend(
        [
            "",
            "## Interpretation",
            "",
            "A passing capture proves how one accepted pass-2 Reaction expands into native execution",
            "transactions at state `0x2C`. Presentation Reaction id, actor, reactor, and source remain",
            "available there; executable action id, final target, and row multiplicity are properties",
            "of the delivered native action and need not equal the earlier commit snapshot.",
            "",
            f"Overall: **{'PASS' if ok else 'FAIL'}**",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log", type=Path)
    parser.add_argument("--reaction-id", type=int, required=True)
    parser.add_argument("--reactor", type=int)
    parser.add_argument("--source", type=int)
    parser.add_argument("--expected-action-id", type=int)
    parser.add_argument("--expected-effect-count", type=int)
    parser.add_argument("--expect-target-source", action="store_true")
    parser.add_argument("-o", "--output", type=Path)
    args = parser.parse_args()

    text = args.log.read_text(encoding="utf-8", errors="replace")
    checks, commits, effects = analyze(
        text,
        args.reaction_id,
        args.reactor,
        args.source,
        args.expected_action_id,
        args.expected_effect_count,
        args.expect_target_source,
    )
    output = render(args.log, args.reaction_id, checks, commits, effects)
    if args.output:
        args.output.write_text(output, encoding="utf-8", newline="\n")
    else:
        print(output, end="")
    return 0 if all(result for _, result in checks) else 1


if __name__ == "__main__":
    sys.exit(main())
