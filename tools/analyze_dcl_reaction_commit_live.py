#!/usr/bin/env python3
"""Classify bounded LT23 reaction-commit live captures.

The original LT23 capture is a negative baseline: only generic pass-1 queue traffic is present.
The Counter and Auto-Potion scenarios prove that accepted native Reactions use pass 2. Counter
also correlates the commit with a surviving reactor plus the following native effect transactions.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "work" / "1784099229-lt23-dcl-reaction-commit-live.log"

HOOK_RE = re.compile(r"^\[DCL-REACTION-COMMIT-HOOK\] pass=(\d)", re.MULTILINE)
EVENT_RE = re.compile(
    r"^\[DCL-REACTION-COMMIT\] event=(?P<event>\d+) pass=(?P<pass>\d+) "
    r"actor=(?P<actor>0x[0-9A-F]+) reactorIdx=(?P<reactor>\d+) "
    r"sourceIdx=(?P<source>-?\d+) reactionId=(?P<id>\d+) "
    r"actor18C=(?P<id18c>\d+) actor142=(?P<id142>\d+) "
    r"idsAgree=(?P<agree>True|False) record=(?P<record>0x[0-9A-F]+) "
    r"targetCount=(?P<target_count>\d+) targets=\[(?P<targets>[^]]*)\]",
    re.MULTILINE,
)
NOISE_RE = re.compile(
    r"^\[DCL-REACTION-COMMIT-NOISE\] event=(?P<event>\d+) pass=(?P<pass>\d+) .*?"
    r"reactionId=(?P<id>\d+).*?noiseReason=(?P<reason>\S+)",
    re.MULTILINE,
)
DAMAGE_RE = re.compile(
    r"^\[DAMAGE ptr=(?P<ptr>0x[0-9A-F]+) id=(?P<char>0x[0-9A-F]+)\] "
    r"(?P<before>\d+) -> (?P<after>\d+) = (?P<amount>\d+)",
    re.MULTILINE,
)
QUEUE_RE = re.compile(r"^\[DCL-REACTION-COMMIT(?:-NOISE)?\]", re.MULTILINE)


def parse_events(text: str) -> list[dict[str, object]]:
    events: list[dict[str, object]] = []
    for match in EVENT_RE.finditer(text):
        groups = match.groupdict()
        targets = tuple(
            int(value.strip()) for value in groups["targets"].split(",") if value.strip()
        )
        events.append(
            {
                "event": int(groups["event"]),
                "pass": int(groups["pass"]),
                "actor": groups["actor"],
                "reactor": int(groups["reactor"]),
                "source": int(groups["source"]),
                "id": int(groups["id"]),
                "id18c": int(groups["id18c"]),
                "id142": int(groups["id142"]),
                "agree": groups["agree"] == "True",
                "record": groups["record"],
                "target_count": int(groups["target_count"]),
                "targets": targets,
                "start": match.start(),
            }
        )
    return events


def parse_damage(text: str) -> list[dict[str, object]]:
    result: list[dict[str, object]] = []
    for match in DAMAGE_RE.finditer(text):
        groups = match.groupdict()
        result.append(
            {
                "ptr": groups["ptr"],
                "char": groups["char"],
                "before": int(groups["before"]),
                "after": int(groups["after"]),
                "amount": int(groups["amount"]),
                "start": match.start(),
            }
        )
    return result


def baseline_checks(events: list[dict[str, object]]) -> tuple[list[tuple[str, bool]], list[str]]:
    checks = [
        ("capture contains exactly two raw queue events", len(events) == 2),
        (
            "both raw events came from pass 1",
            len(events) == 2 and all(event["pass"] == 1 for event in events),
        ),
        ("first event carried blank id 0", len(events) >= 1 and events[0]["id"] == 0),
        ("second event carried ordinary Claw id 280", len(events) >= 2 and events[1]["id"] == 280),
        (
            "no event carried a native Reaction id",
            all(not 422 <= int(event["id"]) <= 453 for event in events),
        ),
        ("no pass-2 Reaction commit was captured", all(event["pass"] != 2 for event in events)),
    ]
    interpretation = [
        "Pass 1 at `0x206743` is not Reaction-specific. It fired for blank action id `0` and",
        "ordinary ability id `280` (Claw), both without targets. The capture contains no accepted",
        "native Reaction id (`422..453`). This negative control refutes the universal three-pass",
        "accepted-Reaction hypothesis while leaving pass 2 as the mapped real-code Reaction path.",
    ]
    return checks, interpretation


def counter_checks(
    text: str,
    events: list[dict[str, object]],
    damage: list[dict[str, object]],
    reaction_id: int,
) -> tuple[list[tuple[str, bool]], list[str]]:
    native = [event for event in events if 422 <= int(event["id"]) <= 453]
    counter = [event for event in events if event["id"] == reaction_id]
    rion = [
        event
        for event in counter
        if event["reactor"] == 4
        and event["source"] == 0
        and event["target_count"] == 1
        and event["targets"] == (0,)
    ]
    primary = rion[0] if rion else None

    preceding: dict[str, object] | None = None
    following: list[dict[str, object]] = []
    if primary:
        earlier = [
            row
            for row in damage
            if int(row["start"]) < int(primary["start"]) and row["ptr"] == primary["record"]
        ]
        preceding = earlier[-1] if earlier else None
        next_queue = next(
            (
                match.start()
                for match in QUEUE_RE.finditer(text, int(primary["start"]) + 1)
                if match.start() > int(primary["start"])
            ),
            len(text),
        )
        following = [
            row
            for row in damage
            if int(primary["start"]) < int(row["start"]) < next_queue
        ]

    two_same_target = (
        len(following) >= 2
        and following[0]["ptr"] == following[1]["ptr"]
        and int(following[0]["after"]) == int(following[1]["before"])
    )
    survived = preceding is not None and int(preceding["after"]) > 0
    noise = [
        {"pass": int(match.group("pass")), "id": int(match.group("id"))}
        for match in NOISE_RE.finditer(text)
    ]

    checks = [
        (f"capture contains internal native Reaction record {reaction_id}", bool(counter)),
        ("every native Reaction commit came from pass 2", bool(native) and all(event["pass"] == 2 for event in native)),
        ("all captured Counter id copies agree", bool(counter) and all(event["agree"] for event in counter)),
        ("Rion 442 record owns reactor 4, source 0, early target [0]", bool(rion)),
        ("Rion survived the immediately preceding damage transaction", survived),
        ("442 record is followed by two temporally correlated same-target damage transactions", two_same_target),
        ("generic non-Reaction queue traffic remains isolated as pass-1 noise", any(row["pass"] == 1 and not 422 <= row["id"] <= 453 for row in noise)),
    ]

    if primary and preceding:
        damage_summary = (
            f"The primary event is event {primary['event']}: reactor {primary['reactor']} survived "
            f"`{preceding['before']} -> {preceding['after']}` HP, then pass {primary['pass']} committed "
            f"Reaction {primary['id']} for target `{primary['targets'][0]}`."
        )
    else:
        damage_summary = "The required Rion Counter event or its preceding damage transaction is missing."
    if two_same_target:
        effect_summary = (
            f"Before the next queue event, the same target received chained damage "
            f"`{following[0]['before']} -> {following[0]['after']}` and "
            f"`{following[1]['before']} -> {following[1]['after']}`. This temporal correlation does "
            "not prove visible Counter execution without materialized-order, apply, and presentation evidence."
        )
    else:
        effect_summary = "Two chained native effect transactions were not found after the commit."
    interpretation = [
        damage_summary,
        effect_summary,
        "Pass 2 at `0x206421` is therefore an accepted native Reaction-record boundary;",
        "pass-1 ordinary queue traffic remains outside Reaction ownership, while visible execution remains a separate proof obligation.",
    ]
    return checks, interpretation


def auto_potion_checks(
    text: str,
    events: list[dict[str, object]],
    reaction_id: int,
) -> tuple[list[tuple[str, bool]], list[str]]:
    native = [event for event in events if 422 <= int(event["id"]) <= 453]
    auto_potion = [event for event in events if event["id"] == reaction_id]
    josephine = [
        event
        for event in auto_potion
        if event["reactor"] == 2
        and event["source"] == 17
        and event["target_count"] == 0
        and event["targets"] == ()
    ]
    noise = [
        {"pass": int(match.group("pass")), "id": int(match.group("id"))}
        for match in NOISE_RE.finditer(text)
    ]

    checks = [
        (f"capture contains native Reaction {reaction_id}", bool(auto_potion)),
        (
            "every native Reaction commit came from pass 2",
            bool(native) and all(event["pass"] == 2 for event in native),
        ),
        (
            "all captured Auto-Potion id copies agree",
            bool(auto_potion) and all(event["agree"] for event in auto_potion),
        ),
        (
            "Josephine Auto-Potion commit owns reactor 2, source 17, and no explicit target",
            bool(josephine),
        ),
        (
            "generic non-Reaction queue traffic remains isolated as pass-1 noise",
            any(
                row["pass"] == 1 and not 422 <= row["id"] <= 453
                for row in noise
            ),
        ),
    ]
    interpretation = [
        "The accepted Auto-Potion event carries Reaction id `441` in both actor id fields and",
        "commits at pass 2 with Josephine as reactor and the attacking unit as source. Its empty",
        "target list is native behavior for this self-directed reaction, not a missing commit.",
        "Together with Counter, this establishes pass 2 at `0x206421` as the accepted native",
        "Reaction commit boundary across both an offensive response and an item-based response.",
    ]
    return checks, interpretation


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    parser.add_argument("-o", "--output", type=Path)
    parser.add_argument(
        "--scenario",
        choices=("baseline-no-reaction", "counter-pass2", "auto-potion-pass2"),
        default="baseline-no-reaction",
    )
    parser.add_argument("--reaction-id", type=int, default=442)
    args = parser.parse_args()

    text = args.log.read_text(encoding="utf-8", errors="replace")
    hooks = [int(value) for value in HOOK_RE.findall(text)]
    events = parse_events(text)
    damage = parse_damage(text)

    checks: list[tuple[str, bool]] = [
        ("all three guarded hooks installed", sorted(hooks) == [0, 1, 2]),
    ]
    if args.scenario == "baseline-no-reaction":
        scenario_checks, interpretation = baseline_checks(events)
    elif args.scenario == "counter-pass2":
        scenario_checks, interpretation = counter_checks(text, events, damage, args.reaction_id)
    else:
        scenario_checks, interpretation = auto_potion_checks(text, events, args.reaction_id)
    checks.extend(scenario_checks)
    ok = all(result for _, result in checks)

    lines = [
        "# LT23 reaction-commit live analysis",
        "",
        f"Source: `{args.log}`",
        f"Scenario: `{args.scenario}`",
        "",
        "## Checks",
        "",
        "| Check | Result |",
        "| --- | --- |",
    ]
    lines.extend(f"| {label} | {'PASS' if result else 'FAIL'} |" for label, result in checks)
    lines.extend(["", "## Native Reaction events", ""])
    native = [event for event in events if 422 <= int(event["id"]) <= 453]
    if native:
        lines.extend(
            [
                "| Event | Pass | Reaction | Reactor | Source | Targets | IDs agree |",
                "| ---: | ---: | ---: | ---: | ---: | --- | --- |",
            ]
        )
        for event in native:
            lines.append(
                f"| {event['event']} | {event['pass']} | {event['id']} | {event['reactor']} | "
                f"{event['source']} | `{event['targets']}` | {event['agree']} |"
            )
    else:
        lines.append("None.")
    lines.extend(["", "## Interpretation", "", *interpretation, "", f"Overall: **{'PASS' if ok else 'FAIL'}**"])

    output = "\n".join(lines) + "\n"
    if args.output:
        args.output.write_text(output, encoding="utf-8")
    else:
        print(output, end="")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
