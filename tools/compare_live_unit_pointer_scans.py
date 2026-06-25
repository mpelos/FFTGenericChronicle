#!/usr/bin/env python3
"""Compare two JSON outputs from scan_live_unit_pointers.py."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Compare baseline and pending live unit pointer scans.")
    p.add_argument("baseline", type=Path)
    p.add_argument("pending", type=Path)
    p.add_argument("-o", "--output", type=Path, default=ROOT / "work" / "live_unit_pointer_scan_diff.md")
    return p.parse_args()


def main() -> int:
    args = parse_args()
    baseline = json.loads(args.baseline.read_text(encoding="utf-8"))
    pending = json.loads(args.pending.read_text(encoding="utf-8"))
    report = render_diff(baseline, pending, args.baseline, args.pending)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(report, encoding="utf-8")
    print(f"wrote {args.output}")
    return 0


def render_diff(baseline: dict, pending: dict, baseline_path: Path, pending_path: Path) -> str:
    baseline_keys = {group_key(group): group for group in baseline.get("groups", [])}
    pending_keys = {group_key(group): group for group in pending.get("groups", [])}
    new_keys = [key for key in pending_keys if key not in baseline_keys]
    gone_keys = [key for key in baseline_keys if key not in pending_keys]

    lines: list[str] = []
    lines.append("# Live Unit Pointer Scan Diff")
    lines.append("")
    lines.append(f"- Baseline: `{baseline_path}`")
    lines.append(f"- Pending: `{pending_path}`")
    lines.append(f"- Baseline groups: `{len(baseline_keys)}`")
    lines.append(f"- Pending groups: `{len(pending_keys)}`")
    lines.append(f"- New pending groups: `{len(new_keys)}`")
    lines.append(f"- Gone groups: `{len(gone_keys)}`")
    lines.append("")
    lines.append("## New Pending Groups")
    if not new_keys:
        lines.append("No new nearby unit-pointer groups.")
    else:
        lines.append("| Start | Span | Names | Hits | Region |")
        lines.append("| --- | ---: | --- | --- | --- |")
        for key in sorted(new_keys, key=lambda item: (pending_keys[item].get("span", 0), pending_keys[item].get("start", 0)))[:120]:
            group = pending_keys[key]
            lines.append(render_group_row(group))
    lines.append("")
    lines.append("## Gone Groups")
    if not gone_keys:
        lines.append("No baseline groups disappeared.")
    else:
        lines.append("| Start | Span | Names | Hits | Region |")
        lines.append("| --- | ---: | --- | --- | --- |")
        for key in sorted(gone_keys, key=lambda item: (baseline_keys[item].get("span", 0), baseline_keys[item].get("start", 0)))[:80]:
            group = baseline_keys[key]
            lines.append(render_group_row(group))
    lines.append("")
    return "\n".join(lines)


def group_key(group: dict) -> tuple:
    # Exact addresses matter here: a pending action object should appear at a concrete new address,
    # while permanent duplicated unit-pointer arrays tend to remain stable across nearby snapshots.
    return (
        int(group.get("start", 0)),
        int(group.get("end", 0)),
        tuple(group.get("names", [])),
        tuple((hit.get("name"), int(hit.get("address", 0))) for hit in group.get("hits", [])),
    )


def render_group_row(group: dict) -> str:
    hits = ", ".join(f"{hit.get('name')}@0x{int(hit.get('address', 0)):X}" for hit in group.get("hits", [])[:12])
    names = ", ".join(group.get("names", []))
    return (
        f"| `0x{int(group.get('start', 0)):X}` | `0x{int(group.get('span', 0)):X}` | `{names}` | `{hits}` | "
        f"`base=0x{int(group.get('regionBase', 0)):X} size=0x{int(group.get('regionSize', 0)):X} "
        f"protect=0x{int(group.get('protect', 0)):X} type=0x{int(group.get('memType', 0)):X}` |"
    )


if __name__ == "__main__":
    raise SystemExit(main())
