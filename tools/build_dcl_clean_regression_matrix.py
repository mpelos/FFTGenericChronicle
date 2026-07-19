#!/usr/bin/env python3
"""Build the canonical live-regression matrix with retired movement/Fear controls absent."""
from __future__ import annotations

import argparse
import copy
import json
from pathlib import Path

from validate_dcl_live_regression_matrix import REQUIRED_TAGS, ROOT


SOURCE = ROOT / "work/1784401467-dcl-v4-live-regression-matrix.json"
OUTPUT = ROOT / "work/1784470893-dcl-clean-v1-live-regression-matrix.json"
PAIR = "work/1784470893-dcl-unified-clean-v1-runtime-data-pair.json"
PAIR_SHA256 = "7AADD61C00660A0113D3F4986E6ED83810BF93AA52351230FA6E6EE8A44C17B3"
SETTINGS_SHA256 = "F9C3A5BC2B70A07AF75AA25C52DA232FC320275A36362A270C70791BF6939830"
REMOVED_CASES = {"fear-reskin-route", "approach-and-reaction-arbitration"}
RETIRED_SETTING_PREFIXES = ("DclFear", "DclApproach")


def _text(value: str) -> str:
    return (
        value.replace("exact v4", "exact clean-v1")
        .replace("canonical v4", "canonical clean-v1")
        .replace("v4 pair", "clean-v1 pair")
    )


def _final_tile_case(preflight_id: str) -> dict[str, object]:
    return {
        "id": "final-tile-position-producer",
        "stage": "live-observe",
        "job_free": True,
        "writes_save": False,
        "purpose": "Validate exactly one post-movement event per completed route and no event for preview, cancellation, Wait, or current-tile selection.",
        "fixture": "One player-controlled mover plus at least one AI mover in an ordinary battle; no authored final-tile effect is enabled.",
        "actions": [
            "Let one AI unit complete a nonempty route.",
            "Confirm one nonempty player route and wait for its walking animation to finish.",
            "On a later player turn preview a different tile, cancel Move, and complete the turn with Wait.",
            "Exercise Move against the unit's current tile without displacement.",
        ],
        "pass_evidence": [
            "Each completed AI/player route publishes exactly one accepted completed-movement row after coordinate convergence.",
            "Preview/cancel, Wait without movement, and current-tile selection publish no player event.",
            "Movement remains smooth and indivisible, with no hook failure, per-tile event, pause, slowdown, or game-state write.",
        ],
        "tags": ["final-tile"],
        "depends_on": [preflight_id],
        "ability_ids": [],
        "settings_requirements": {"DclFinalTileEventProbeEnabled": True},
    }


def build() -> dict[str, object]:
    matrix = json.loads(SOURCE.read_text(encoding="utf-8-sig"))
    matrix["note"] = (
        "Canonical job-free exact clean-v1 technical regression matrix; it validates DCL combat "
        "mechanisms with retired Approach and Fear compatibility controls absent and preserves no "
        "final balance or job-design claims."
    )
    matrix["runtime_data_pair"] = PAIR
    matrix["runtime_data_pair_sha256"] = PAIR_SHA256
    matrix["settings_sha256"] = SETTINGS_SHA256
    matrix["preflight_command"] = f"python tools/validate_dcl_live_install.py --pair {PAIR}"
    matrix["required_tags"] = [
        tag for tag in matrix["required_tags"]
        if tag in REQUIRED_TAGS
    ]
    reaction_index = matrix["required_tags"].index("reaction-synthetic") + 1
    matrix["required_tags"].insert(reaction_index, "final-tile")

    preflight_id = "clean-v1-preflight"
    cases: list[dict[str, object]] = []
    for raw_case in matrix["cases"]:
        if raw_case["id"] in REMOVED_CASES:
            continue
        case = copy.deepcopy(raw_case)
        if case["id"] == "v4-preflight":
            case["id"] = preflight_id
        case["purpose"] = _text(case["purpose"])
        case["fixture"] = _text(case["fixture"])
        case["actions"] = [_text(value) for value in case["actions"]]
        case["pass_evidence"] = [_text(value) for value in case["pass_evidence"]]
        case["tags"] = [tag for tag in case["tags"] if tag in REQUIRED_TAGS]
        case["depends_on"] = [
            preflight_id if dependency == "v4-preflight" else dependency
            for dependency in case["depends_on"]
            if dependency not in REMOVED_CASES
        ]
        case["settings_requirements"] = {
            key: value
            for key, value in case["settings_requirements"].items()
            if not key.startswith(RETIRED_SETTING_PREFIXES)
            and key != "DclReactionReservationArbitrationEnabled"
        }

        if case["id"] == "fire-aoe-authority":
            case["purpose"] = (
                "Prove authoritative target expansion and one calculation per legal target for an "
                "ordinary area spell without importing any status or forced-control mechanic."
            )
            case["actions"][-1] = "Let the spell resolve through its ordinary per-target calculations."
            case["pass_evidence"][-1] = "No unrelated status or forced-control transaction is attributed to Fire."
        elif case["id"] == "battle-lifecycle-reset":
            case["fixture"] = (
                "A battle that has exercised status groups, hit decisions, Interrupt, synthetic "
                "Reaction, and final-tile publications before ending; then start a fresh battle without saving."
            )
            case["depends_on"] = [
                "taunt-and-interrupt",
                "reaction-families",
                "final-tile-position-producer",
            ]
            case["ability_ids"] = [0, 368, 442, 443]
            case["pass_evidence"][1] = (
                "The new battle contains no stale target, status plan, hit decision, pending "
                "cancellation, Reaction reservation, or final-tile publication state."
            )

        cases.append(case)
        if case["id"] == "reaction-families":
            cases.append(_final_tile_case(preflight_id))

    matrix["cases"] = cases
    return matrix


def render() -> str:
    return json.dumps(build(), indent=2, ensure_ascii=False) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    value = render()
    if args.check_only:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding="utf-8-sig") != value:
            print(f"ERROR: clean regression matrix is missing or stale: {OUTPUT}")
            return 1
        print(f"clean regression matrix is current: {OUTPUT}")
        return 0
    OUTPUT.write_text(value, encoding="utf-8", newline="\n")
    print(f"wrote {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
