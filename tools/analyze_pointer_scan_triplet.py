#!/usr/bin/env python3
"""Rank baseline/pending/post live unit-pointer scan differences.

This complements compare_live_unit_pointer_scans.py. Pairwise diffs are noisy in
FFT Ivalice Chronicles because turn-order arrays rotate constantly. The useful
question for pending-action RE is more specific:

- Which pointer groups appear only while the charged action is pending?
- Which address-stable groups merely changed from one actor to another and
  survived after resolution?
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Analyze baseline/pending/post live pointer scan JSONs.")
    p.add_argument("baseline", type=Path)
    p.add_argument("pending", type=Path)
    p.add_argument("post", type=Path)
    p.add_argument("-o", "--output", type=Path, default=ROOT / "work" / "live_unit_pointer_scan_triplet.md")
    p.add_argument("--focus", action="append", default=[], help="Focus unit name. Repeatable, e.g. Cloud Beowulf.")
    p.add_argument("--limit", type=int, default=80)
    return p.parse_args()


def main() -> int:
    args = parse_args()
    baseline = json.loads(args.baseline.read_text(encoding="utf-8"))
    pending = json.loads(args.pending.read_text(encoding="utf-8"))
    post = json.loads(args.post.read_text(encoding="utf-8"))
    focus = {item.lower() for item in args.focus}

    report = render_report(baseline, pending, post, args.baseline, args.pending, args.post, focus, args.limit)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    print(f"wrote {args.output}")
    return 0


def render_report(
    baseline: dict,
    pending: dict,
    post: dict,
    baseline_path: Path,
    pending_path: Path,
    post_path: Path,
    focus: set[str],
    limit: int,
) -> str:
    exact = {
        "baseline": {exact_group_key(group): group for group in baseline.get("groups", [])},
        "pending": {exact_group_key(group): group for group in pending.get("groups", [])},
        "post": {exact_group_key(group): group for group in post.get("groups", [])},
    }
    shape = {
        "baseline": {shape_group_key(group): group for group in baseline.get("groups", [])},
        "pending": {shape_group_key(group): group for group in pending.get("groups", [])},
        "post": {shape_group_key(group): group for group in post.get("groups", [])},
    }

    pending_only = [
        group
        for key, group in exact["pending"].items()
        if key not in exact["baseline"] and key not in exact["post"]
    ]
    pending_new_survived = [
        group
        for key, group in exact["pending"].items()
        if key not in exact["baseline"] and key in exact["post"]
    ]
    stable_changed = []
    for key, pending_group in shape["pending"].items():
        baseline_group = shape["baseline"].get(key)
        post_group = shape["post"].get(key)
        if not baseline_group:
            continue
        if hit_names(baseline_group) == hit_names(pending_group):
            continue
        stable_changed.append((baseline_group, pending_group, post_group))

    lines: list[str] = []
    lines.append("# Live Unit Pointer Scan Triplet")
    lines.append("")
    lines.append(f"- Baseline: `{baseline_path}` groups `{len(exact['baseline'])}`")
    lines.append(f"- Pending: `{pending_path}` groups `{len(exact['pending'])}`")
    lines.append(f"- Post: `{post_path}` groups `{len(exact['post'])}`")
    if focus:
        lines.append(f"- Focus: `{', '.join(sorted(focus))}`")
    lines.append("")

    lines.append("## Pending-Only Exact Groups")
    lines.append(
        "Groups present only in the pending scan are the best pointer-based candidates for charged-action context."
    )
    render_group_table(lines, sorted(pending_only, key=lambda group: score_group(group, focus), reverse=True)[:limit])
    lines.append("")

    lines.append("## New Pending Groups That Survived Post")
    lines.append(
        "These are usually turn-order or cache structures. They changed after the action was scheduled, but remained after resolution."
    )
    render_group_table(lines, sorted(pending_new_survived, key=lambda group: score_group(group, focus), reverse=True)[:limit])
    lines.append("")

    lines.append("## Address-Stable Name Changes")
    lines.append(
        "Same addresses, different unit names. If pending and post match each other, this is likely queue/current-turn state."
    )
    if not stable_changed:
        lines.append("No address-stable groups changed names.")
    else:
        lines.append("| Start | Span | Baseline | Pending | Post | Region |")
        lines.append("| --- | ---: | --- | --- | --- | --- |")
        rows = sorted(stable_changed, key=lambda item: score_group(item[1], focus), reverse=True)[:limit]
        for baseline_group, pending_group, post_group in rows:
            post_names = hit_names(post_group) if post_group else "<missing>"
            lines.append(
                f"| `0x{int(pending_group.get('start', 0)):X}` | `0x{int(pending_group.get('span', 0)):X}` | "
                f"`{hit_names(baseline_group)}` | `{hit_names(pending_group)}` | `{post_names}` | "
                f"`0x{int(pending_group.get('regionBase', 0)):X}` |"
            )
    lines.append("")
    return "\n".join(lines)


def exact_group_key(group: dict) -> tuple:
    return (
        int(group.get("start", 0)),
        int(group.get("end", 0)),
        tuple(group.get("names", [])),
        tuple((hit.get("name"), int(hit.get("address", 0))) for hit in group.get("hits", [])),
    )


def shape_group_key(group: dict) -> tuple:
    return (
        int(group.get("start", 0)),
        int(group.get("end", 0)),
        tuple(int(hit.get("address", 0)) for hit in group.get("hits", [])),
    )


def hit_names(group: dict | None) -> str:
    if not group:
        return "<missing>"
    return ", ".join(str(hit.get("name", "?")) for hit in group.get("hits", [])[:12])


def score_group(group: dict, focus: set[str]) -> float:
    names = {str(name).lower() for name in group.get("names", [])}
    span = int(group.get("span", 0))
    hits = group.get("hits", [])
    score = 0.0
    if focus and focus <= names:
        score += 1000.0
    score += 100.0 * len(names & focus)
    if len(names) <= 3:
        score += 40.0
    score -= span / 16.0
    score -= max(0, len(hits) - 4) * 8.0
    return score


def render_group_table(lines: list[str], groups: list[dict]) -> None:
    if not groups:
        lines.append("No groups.")
        return
    lines.append("| Start | Span | Names | Hits | Region |")
    lines.append("| --- | ---: | --- | --- | --- |")
    for group in groups:
        hits = ", ".join(f"{hit.get('name')}@0x{int(hit.get('address', 0)):X}" for hit in group.get("hits", [])[:12])
        lines.append(
            f"| `0x{int(group.get('start', 0)):X}` | `0x{int(group.get('span', 0)):X}` | "
            f"`{', '.join(group.get('names', []))}` | `{hits}` | `0x{int(group.get('regionBase', 0)):X}` |"
        )


if __name__ == "__main__":
    raise SystemExit(main())
