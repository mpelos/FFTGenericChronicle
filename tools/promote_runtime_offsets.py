#!/usr/bin/env python3
"""Promote stable runtime slot scans into exact Offset/Width settings.

Run after a live mapping pass with LogResolvedRuntimeContext enabled:

    python tools/promote_runtime_offsets.py --min-events 3 --also-policy

The tool reads [RUNTIME] lines, finds stable target/attacker slot offsets, and writes a copy of
the base runtime settings with scan probes replaced by exact Offset/Width probes.
"""
from __future__ import annotations

import argparse
import copy
import json
import re
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from analyze_battleprobe_log import DEFAULT_LOG, RUNTIME_RE, parse_runtime_contexts

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_BASE = ROOT / "work" / "battle-runtime-settings.v0.2.scan.live-noop.json"
DEFAULT_OUTPUT = ROOT / "work" / "battle-runtime-settings.v0.2.exact-from-log.json"
DEFAULT_POLICY_BASE = ROOT / "work" / "battle-runtime-settings.v0.2.scan.generated.json"
DEFAULT_POLICY_OUTPUT = ROOT / "work" / "battle-runtime-settings.v0.2.policy.exact-from-log.json"


@dataclass(frozen=True)
class SlotPromotion:
    scope: str
    slot: str
    offset: int
    width: str
    events: int
    item_ids: tuple[int, ...]

    @property
    def settings_key(self) -> str:
        return "EquipmentSlots" if self.scope == "target" else "AttackerEquipmentSlots"


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Promote stable [RUNTIME] scan slots to exact settings offsets.")
    p.add_argument("log", nargs="?", type=Path, default=DEFAULT_LOG)
    p.add_argument("--base-settings", type=Path, default=DEFAULT_BASE)
    p.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    p.add_argument("--also-policy", action="store_true", help="Also write an exact policy settings file.")
    p.add_argument("--policy-base-settings", type=Path, default=DEFAULT_POLICY_BASE)
    p.add_argument("--policy-output", type=Path, default=DEFAULT_POLICY_OUTPUT)
    p.add_argument("--min-events", type=int, default=2)
    p.add_argument("--allow-empty", action="store_true", help="Write output even if no slots can be promoted.")
    return p.parse_args()


def main() -> int:
    args = parse_args()
    if not args.log.exists():
        raise SystemExit(f"log not found: {args.log}")
    if not args.base_settings.exists():
        raise SystemExit(f"base settings not found: {args.base_settings}")
    if args.also_policy and not args.policy_base_settings.exists():
        raise SystemExit(f"policy base settings not found: {args.policy_base_settings}")

    runtime_contexts = read_runtime_contexts(args.log)
    runtime_details = parse_runtime_contexts(runtime_contexts)
    promotions, rejected = find_promotions(runtime_details, max(1, args.min_events))

    if not promotions and not args.allow_empty:
        print("no stable runtime slot offsets found; no settings written")
        for line in rejected:
            print(f"- {line}")
        return 2

    write_promoted_settings(args.base_settings, args.output, promotions)
    print(f"wrote {args.output}")
    if args.also_policy:
        write_promoted_settings(args.policy_base_settings, args.policy_output, promotions)
        print(f"wrote {args.policy_output}")
    if promotions:
        for promotion in promotions:
            items = ",".join(str(item_id) for item_id in promotion.item_ids[:8]) or "-"
            print(
                f"promoted {promotion.scope}.{promotion.slot}: "
                f"Offset={promotion.offset} Width={promotion.width} events={promotion.events} itemIds={items}"
            )
    else:
        print("no promotions applied")
    for line in rejected:
        print(f"not promoted: {line}")
    return 0


def write_promoted_settings(base_settings: Path, output: Path, promotions: list[SlotPromotion]) -> None:
    settings = json.loads(base_settings.read_text(encoding="utf-8"))
    promoted_settings = apply_promotions(settings, promotions)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(promoted_settings, indent=2) + "\n", encoding="utf-8")


def read_runtime_contexts(log: Path) -> list[tuple[str, str]]:
    contexts: list[tuple[str, str]] = []
    for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
        if m := RUNTIME_RE.search(line):
            contexts.append((m.group(1), m.group("body")))
    return contexts


def find_promotions(runtime_details: list[dict[str, object]], min_events: int) -> tuple[list[SlotPromotion], list[str]]:
    grouped: dict[tuple[str, str], list[dict[str, object]]] = defaultdict(list)
    for ctx in runtime_details:
        add_slots(grouped, "target", ctx.get("target_slots", []))
        add_slots(grouped, "attacker", ctx.get("attacker_slots", []))

    promotions: list[SlotPromotion] = []
    rejected: list[str] = []
    for (scope, slot), rows in sorted(grouped.items()):
        label = f"{scope}.{slot}"
        if len(rows) < min_events:
            rejected.append(f"{label}: only {len(rows)} event(s), needs {min_events}")
            continue
        if any(row.get("state") != "present" for row in rows):
            states = ",".join(sorted({str(row.get("state")) for row in rows}))
            rejected.append(f"{label}: non-present state(s): {states}")
            continue
        if any(int(row.get("matches") or 0) != 1 for row in rows):
            matches = ",".join(sorted({str(row.get("matches")) for row in rows}))
            rejected.append(f"{label}: scan matches not uniquely 1: {matches}")
            continue

        offsets = {row.get("offset_int") for row in rows}
        widths = {str(row.get("width")) for row in rows}
        if len(offsets) != 1 or None in offsets or len(widths) != 1 or "?" in widths:
            rendered_offsets = ",".join(str(value) for value in sorted(offsets, key=lambda v: -1 if v is None else int(v)))
            rejected.append(f"{label}: unstable offset/width offsets={rendered_offsets} widths={','.join(sorted(widths))}")
            continue

        offset = next(iter(offsets))
        width = next(iter(widths))
        item_ids = tuple(sorted({int(row.get("item_id") or 0) for row in rows}))
        promotions.append(SlotPromotion(scope, slot, int(offset), width, len(rows), item_ids))

    return promotions, rejected


def add_slots(grouped: dict[tuple[str, str], list[dict[str, object]]], scope: str, slots: object) -> None:
    if not isinstance(slots, list):
        return
    for slot in slots:
        if not isinstance(slot, dict):
            continue
        grouped[(scope, str(slot.get("slot", "")))].append(slot)


def apply_promotions(settings: dict[str, Any], promotions: list[SlotPromotion]) -> dict[str, Any]:
    result = copy.deepcopy(settings)
    promotion_map = {(promotion.settings_key, normalize_name(promotion.slot)): promotion for promotion in promotions}

    for settings_key in ("EquipmentSlots", "AttackerEquipmentSlots"):
        slots = result.get(settings_key)
        if not isinstance(slots, list):
            continue
        result[settings_key] = [
            promote_slot(slot, promotion_map.get((settings_key, normalize_name(str(slot.get("Name", ""))))))
            if isinstance(slot, dict)
            else slot
            for slot in slots
        ]

    note = str(result.get("_note", "")).strip()
    suffix = "Exact offsets promoted from live [RUNTIME] observations."
    result["_note"] = f"{note} {suffix}".strip()
    result["_promotedOffsets"] = [
        {
            "Scope": promotion.scope,
            "Slot": promotion.slot,
            "Offset": promotion.offset,
            "Width": promotion.width,
            "Events": promotion.events,
            "ItemIds": list(promotion.item_ids),
        }
        for promotion in promotions
    ]
    return result


def promote_slot(slot: dict[str, Any], promotion: SlotPromotion | None) -> dict[str, Any]:
    if promotion is None:
        return slot
    return {
        "Name": slot.get("Name") or promotion.slot,
        "Offset": promotion.offset,
        "Width": promotion.width,
    }


def normalize_name(value: str) -> str:
    normalized = re.sub(r"[^a-zA-Z0-9]+", "_", value.strip().lower()).strip("_")
    return normalized or "unnamed"


if __name__ == "__main__":
    raise SystemExit(main())
