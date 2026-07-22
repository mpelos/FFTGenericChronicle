#!/usr/bin/env python3
"""Audit native status counters, DCL category decisions, and exclusive duration ownership.

This is a completion/frontier report, not a balance author.  It deliberately treats open category
and duration decisions as blockers and recognizes a counter transfer only through a validated
runtime/XML duration-pair manifest.
"""
from __future__ import annotations

import argparse
import csv
import io
import json
import time
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path

import analyze_dcl_status_counter_authority as counter_authority
import report_dcl_status_policy as status_policy
import validate_dcl_status_duration_pair as duration_pair


ROOT = Path(__file__).resolve().parents[1]
ADD_OPERATIONS = {"add-harmful", "add-buff-or-trait"}


@dataclass(frozen=True)
class FrontierRow:
    table_index: int
    native_status: str
    dcl_status: str
    native_counter: int
    add_producers: tuple[int, ...]
    operations: tuple[str, ...]
    categories: tuple[str, ...]
    category_state: str
    duration_design_state: str
    transfer_state: str
    duration_pair: str


FIELDS = [
    "table_index",
    "native_status",
    "dcl_status",
    "native_counter",
    "add_producer_count",
    "add_producers",
    "operations",
    "categories",
    "category_state",
    "duration_design_state",
    "transfer_state",
    "duration_pair",
]


DCL_CONCEPT_FRONTIER = (
    (
        "Fear",
        "Chicken bit (byte 2, mask 0x04)",
        "mental / high Brave",
        "one-turn technical carrier",
        "mechanism implemented; forced-flee/AoE authority remains live-gated",
    ),
    (
        "Taunt",
        "Berserk bit (byte 2, mask 0x08)",
        "mental inverted / low Brave",
        "one-turn fallback",
        "native-Berserk fallback implemented; directed ideal remains separate",
    ),
    (
        "Stun",
        "Disable",
        "physical / base HP",
        "one target turn",
        "exclusive duration profile implemented; live restriction/animation/expiry gate remains",
    ),
    (
        "Knockdown",
        "Immobilize",
        "physical / base HP",
        "one target turn",
        "exclusive duration profile implemented; live restriction/animation/expiry gate remains",
    ),
    (
        "Disease",
        "none assigned",
        "physical / base HP",
        "design open",
        "no native carrier, producer mapping, or ongoing-effect mechanism is assigned",
    ),
)


def _load_pairs(paths: list[Path]) -> tuple[dict[str, Path], list[str]]:
    covered: dict[str, Path] = {}
    errors: list[str] = []
    for raw_path in paths:
        path = raw_path.resolve()
        try:
            duration_pair.validate_pair(path)
            value = json.loads(path.read_text(encoding="utf-8-sig"))
            statuses = value["neutralized_statuses"]
        except (duration_pair.DurationPairError, OSError, json.JSONDecodeError, KeyError) as error:
            errors.append(f"invalid duration pair {path}: {error}")
            continue
        for status in statuses:
            if status in covered:
                errors.append(
                    f"native status {status} is owned by both {covered[status]} and {path}"
                )
            else:
                covered[status] = path
    return covered, errors


def _category_state(add_rows: list[dict[str, str]]) -> str:
    if not add_rows:
        return "not-applicable"
    categories = {row["resist_category"] for row in add_rows}
    if "unresolved-nature" in categories:
        return "design-open"
    if all(row["operation"] == "add-buff-or-trait" for row in add_rows):
        return "no-defensive-contest"
    if len(categories) > 1:
        return "source-specific-fixed"
    return "fixed"


def _duration_design_state(add_rows: list[dict[str, str]]) -> str:
    if not add_rows:
        return "not-applicable"
    policies = {row["duration_policy"] for row in add_rows}
    if "authoring-required" in policies:
        return "design-open"
    if "explicit-rule" in policies:
        return "special-open"
    return "fixed"


def _transfer_state(
    native_name: str,
    token: str | None,
    counter: int,
    category_state: str,
    covered: dict[str, Path],
) -> str:
    if native_name in covered:
        return "exclusive-runtime-owned"
    if counter <= 0:
        return "not-required-counter-zero"
    if native_name == "Doom":
        return "lifecycle-forbidden"
    if native_name == "Empty_32":
        return "system-forbidden"
    if token is None:
        return "native-engine-owned"
    if category_state == "design-open":
        return "blocked-category"
    return "blocked-duration-ownership"


def analyze(pair_paths: list[Path]) -> tuple[list[FrontierRow], dict[str, Path], list[str]]:
    policy_rows, policy_errors = status_policy.load_manifest(status_policy.DEFAULT_CATALOG)
    covered, pair_errors = _load_pairs(pair_paths)
    errors = list(policy_errors) + pair_errors
    native_rows = counter_authority.load_rows()
    if len(native_rows) != 40:
        errors.append(f"expected 40 native status rows, found {len(native_rows)}")

    by_token: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in policy_rows:
        by_token[row["status"]].append(row)
    token_by_native = {native: token for token, native in counter_authority.DCL_TO_NATIVE.items()}

    unknown_covered = sorted(set(covered) - {row.name for row in native_rows})
    if unknown_covered:
        errors.append(f"duration pairs contain unknown native statuses: {unknown_covered}")

    rows: list[FrontierRow] = []
    for native in native_rows:
        token = token_by_native.get(native.name)
        token_rows = by_token.get(token, []) if token else []
        add_rows = [row for row in token_rows if row["operation"] in ADD_OPERATIONS]
        category_state = _category_state(add_rows)
        duration_state = _duration_design_state(add_rows)
        pair = covered.get(native.name)
        rows.append(FrontierRow(
            table_index=native.table_index,
            native_status=native.name,
            dcl_status=token or "",
            native_counter=native.counter,
            add_producers=tuple(sorted({int(row["ability_id"]) for row in add_rows})),
            operations=tuple(sorted({row["operation"] for row in token_rows})),
            categories=tuple(sorted({row["resist_category"] for row in add_rows})),
            category_state=category_state,
            duration_design_state=duration_state,
            transfer_state=_transfer_state(
                native.name, token, native.counter, category_state, covered
            ),
            duration_pair=(pair.relative_to(ROOT).as_posix() if pair else ""),
        ))

    unsafe = [
        row.native_status
        for row in rows
        if row.native_counter > 0
        and row.transfer_state == "exclusive-runtime-owned"
        and row.native_status not in covered
    ]
    if unsafe:
        errors.append(f"statuses marked exclusive without a validated duration pair: {unsafe}")
    return rows, covered, errors


def csv_text(rows: list[FrontierRow]) -> str:
    buffer = io.StringIO(newline="")
    writer = csv.DictWriter(buffer, fieldnames=FIELDS, lineterminator="\n")
    writer.writeheader()
    for row in rows:
        writer.writerow({
            "table_index": row.table_index,
            "native_status": row.native_status,
            "dcl_status": row.dcl_status,
            "native_counter": row.native_counter,
            "add_producer_count": len(row.add_producers),
            "add_producers": "|".join(map(str, row.add_producers)),
            "operations": "|".join(row.operations),
            "categories": "|".join(row.categories),
            "category_state": row.category_state,
            "duration_design_state": row.duration_design_state,
            "transfer_state": row.transfer_state,
            "duration_pair": row.duration_pair,
        })
    return buffer.getvalue()


def markdown_text(
    rows: list[FrontierRow],
    covered: dict[str, Path],
    pair_paths: list[Path],
) -> str:
    transfer_counts = Counter(row.transfer_state for row in rows)
    category_counts = Counter(row.category_state for row in rows if row.add_producers)
    timed = [row for row in rows if row.native_counter > 0]
    blocked_timed = [
        row for row in timed
        if row.transfer_state in {"blocked-category", "blocked-duration-ownership"}
    ]
    unresolved = [row for row in rows if row.category_state == "design-open"]
    lines = [
        "# DCL status-duration completion frontier",
        "",
        "Generated by `tools/analyze_dcl_status_duration_frontier.py`.",
        "",
        "This report distinguishes producer authority from exclusive duration authority. A native",
        "counter is considered transferred only when a runtime/XML duration-pair manifest validates",
        "complete producer coverage. Open category or duration choices remain explicit design gates.",
        "",
        "## Summary",
        "",
        f"- Native status rows: `{len(rows)}`; rows with nonzero native counters: `{len(timed)}`.",
        f"- Validated duration-pair manifests: `{len(pair_paths)}`.",
        f"- Exclusive DCL-owned counters: `{len(covered)}` ({', '.join(sorted(covered)) or 'none'}).",
        f"- Timed rows still blocked by category/duration ownership: `{len(blocked_timed)}`.",
        f"- Status tokens with an unresolved harmful nature: `{len(unresolved)}` "
        f"({', '.join(row.dcl_status for row in unresolved) or 'none'}).",
        "",
        "### Transfer states",
        "",
        "| State | Rows |",
        "| --- | ---: |",
    ]
    lines.extend(f"| {state} | {count} |" for state, count in sorted(transfer_counts.items()))
    lines.extend([
        "",
        "### Category states for catalog add producers",
        "",
        "| State | Native rows |",
        "| --- | ---: |",
    ])
    lines.extend(f"| {state} | {count} |" for state, count in sorted(category_counts.items()))
    lines.extend([
        "",
        "## Native status frontier",
        "",
        "| Id | Native status | DCL token | Counter | Add producers | Categories | Category | Duration design | Transfer |",
        "| ---: | --- | --- | ---: | ---: | --- | --- | --- | --- |",
    ])
    for row in rows:
        lines.append(
            f"| {row.table_index} | {row.native_status} | {row.dcl_status or '-'} | "
            f"{row.native_counter} | {len(row.add_producers)} | {', '.join(row.categories) or '-'} | "
            f"{row.category_state} | {row.duration_design_state} | {row.transfer_state} |"
        )
    lines.extend([
        "",
        "## DCL concepts that are not one-to-one native catalog statuses",
        "",
        "| DCL concept | Carrier | Resistance | Duration state | Mechanism frontier |",
        "| --- | --- | --- | --- | --- |",
    ])
    for concept in DCL_CONCEPT_FRONTIER:
        lines.append("| " + " | ".join(concept) + " |")
    lines.extend([
        "",
        "## Consequences",
        "",
        "- The 150 status-bearing catalog actions have mechanism owners, but producer authority alone",
        "  never permits a native counter edit.",
        "- A native counter is not exclusive-runtime-owned unless a current duration-pair manifest",
        "  validates complete runtime ownership. Historical pairs with stale hashes or unresolved",
        "  category coverage remain evidence only, not active ownership.",
        "- Doom remains engine-owned because its counter terminates in KO, not an ordinary status clear.",
        "- Blindness/Darkness, Silence, Vampire/BloodSuck, Oil, and Undead still need explicit DCL",
        "  nature decisions before full-roster contest authoring.",
        "- Disease is named by the DCL physical category but has no assigned native carrier or effect",
        "  mechanism. That is a design/mechanism gap, not a job-authoring task.",
        "",
        "## Validated pair inputs",
        "",
    ])
    lines.extend(f"- `{path.relative_to(ROOT).as_posix()}`" for path in pair_paths)
    lines.append("")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--duration-pair",
        type=Path,
        action="append",
        default=None,
        help="validated runtime/XML duration-pair manifest (repeatable)",
    )
    parser.add_argument("--output", type=Path)
    parser.add_argument("--csv-output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    pair_paths = [path.resolve() for path in (args.duration_pair or [])]
    rows, covered, errors = analyze(pair_paths)
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        return 1
    if not args.check_only:
        stamp = int(time.time())
        output = (args.output or ROOT / "work" / f"{stamp}-dcl-status-duration-frontier.md").resolve()
        csv_output = (args.csv_output or output.with_suffix(".csv")).resolve()
        output.write_text(markdown_text(rows, covered, pair_paths), encoding="utf-8", newline="\n")
        csv_output.write_text(csv_text(rows), encoding="utf-8", newline="\n")
        print(f"wrote {output}")
        print(f"wrote {csv_output}")
    print("DCL status-duration frontier checks PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
